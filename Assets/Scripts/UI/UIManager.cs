using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace KitchenEmpire
{
    /// <summary>
    /// Manages all UI panels: HUD, shop, upgrades, day summary, tooltips, toasts.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        public TextMeshProUGUI moneyText;
        public TextMeshProUGUI dayText;
        public TextMeshProUGUI reputationText;
        public TextMeshProUGUI powerText;
        public TextMeshProUGUI dayTimerText;

        [Header("Panels")]
        public GameObject shopPanel;
        public GameObject upgradePanel;
        public GameObject daySummaryPanel;
        public GameObject startScreen;
        public GameObject tooltipPanel;
        public TextMeshProUGUI tooltipText;

        [Header("Shop")]
        public Transform shopItemContainer;
        public GameObject shopItemPrefab;

        [Header("Upgrades")]
        public Transform upgradeItemContainer;
        public GameObject upgradeItemPrefab;

        [Header("Day Summary")]
        public TextMeshProUGUI summaryServedText;
        public TextMeshProUGUI summaryFailedText;
        public TextMeshProUGUI summaryRevenueText;
        public TextMeshProUGUI summaryTipsText;
        public TextMeshProUGUI summaryTotalText;
        public TextMeshProUGUI summaryReputationText;
        public Button nextDayButton;

        [Header("Action Buttons")]
        public Button shopButton;
        public Button upgradeButton;
        public Button startDayButton;
        public TextMeshProUGUI startDayButtonText;

        [Header("Tool Buttons")]
        public Button[] toolButtons; // Select, Move, Wire, Demolish
        public Button[] speedButtons; // Pause, Normal, Fast

        [Header("Toast")]
        public GameObject toastPrefab;
        public Transform toastContainer;

        [Header("Held Item Display")]
        public GameObject heldItemDisplay;
        public Image heldItemIcon;
        public TextMeshProUGUI heldItemName;

        private bool _shopOpen;
        private bool _upgradeOpen;

        void OnEnable()
        {
            GameEvents.OnMoneyChanged += UpdateMoney;
            GameEvents.OnReputationChanged += UpdateReputation;
            GameEvents.OnPowerChanged += UpdatePower;
            GameEvents.OnDayTimerUpdated += UpdateDayTimer;
            GameEvents.OnDayStarted += OnDayStarted;
            GameEvents.OnDayEnded += ShowDaySummary;
            GameEvents.OnToastMessage += ShowToast;
            GameEvents.OnToolChanged += UpdateToolButtons;
        }

        void OnDisable()
        {
            GameEvents.OnMoneyChanged -= UpdateMoney;
            GameEvents.OnReputationChanged -= UpdateReputation;
            GameEvents.OnPowerChanged -= UpdatePower;
            GameEvents.OnDayTimerUpdated -= UpdateDayTimer;
            GameEvents.OnDayStarted -= OnDayStarted;
            GameEvents.OnDayEnded -= ShowDaySummary;
            GameEvents.OnToastMessage -= ShowToast;
            GameEvents.OnToolChanged -= UpdateToolButtons;
        }

        void Start()
        {
            // Wire up buttons
            if (shopButton != null) shopButton.onClick.AddListener(ToggleShop);
            if (upgradeButton != null) upgradeButton.onClick.AddListener(ToggleUpgrades);
            if (startDayButton != null) startDayButton.onClick.AddListener(OnStartDayClicked);
            if (nextDayButton != null) nextDayButton.onClick.AddListener(OnNextDayClicked);

            // Tool buttons
            for (int i = 0; i < toolButtons.Length; i++)
            {
                int toolIndex = i;
                if (toolButtons[i] != null)
                {
                    toolButtons[i].onClick.AddListener(() => OnToolClicked(toolIndex));
                }
            }

            // Speed buttons
            for (int i = 0; i < speedButtons.Length; i++)
            {
                int speed = i;
                if (speedButtons[i] != null)
                {
                    speedButtons[i].onClick.AddListener(() => OnSpeedClicked(speed));
                }
            }

            CloseAllPanels();
            UpdateAll();
        }

        // ===== HUD UPDATES =====

        private void UpdateMoney(int amount)
        {
            if (moneyText != null) moneyText.text = $"${amount}";
        }

        private void UpdateReputation(int rep)
        {
            if (reputationText != null) reputationText.text = $"★ {rep}";
        }

        private void UpdatePower(int used, int max)
        {
            if (powerText != null) powerText.text = $"⚡ {used}/{max}W";
        }

        private void UpdateDayTimer(int secondsLeft)
        {
            if (dayTimerText != null)
            {
                int min = secondsLeft / 60;
                int sec = secondsLeft % 60;
                dayTimerText.text = $"{min}:{sec:D2}";
            }
        }

        private void UpdateAll()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            UpdateMoney(gm.Money);
            if (dayText != null) dayText.text = $"Day {gm.Day}";
            UpdateReputation(gm.Reputation);
        }

        // ===== DAY LIFECYCLE =====

        private void OnDayStarted()
        {
            CloseAllPanels();
            if (startDayButtonText != null) startDayButtonText.text = "Day Active";
            if (startDayButton != null) startDayButton.interactable = false;
        }

        private void OnStartDayClicked()
        {
            GameManager.Instance?.StartDay();
        }

        private void OnNextDayClicked()
        {
            if (daySummaryPanel != null) daySummaryPanel.SetActive(false);
            GameManager.Instance?.AdvanceToNextDay();
            UpdateAll();
            if (startDayButtonText != null) startDayButtonText.text = "☀ Start Day";
            if (startDayButton != null) startDayButton.interactable = true;
        }

        private void ShowDaySummary(DaySummaryData data)
        {
            if (daySummaryPanel == null) return;
            daySummaryPanel.SetActive(true);

            if (summaryServedText != null) summaryServedText.text = data.customersServed.ToString();
            if (summaryFailedText != null) summaryFailedText.text = data.customersFailed.ToString();
            if (summaryRevenueText != null) summaryRevenueText.text = $"${data.revenue}";
            if (summaryTipsText != null) summaryTipsText.text = $"${data.tips}";
            if (summaryTotalText != null) summaryTotalText.text = $"${data.totalEarned}";
            if (summaryReputationText != null) summaryReputationText.text = $"★ {data.reputation}";
        }

        // ===== SHOP =====

        public void ToggleShop()
        {
            _shopOpen = !_shopOpen;
            if (shopPanel != null) shopPanel.SetActive(_shopOpen);
            if (_shopOpen)
            {
                _upgradeOpen = false;
                if (upgradePanel != null) upgradePanel.SetActive(false);
                RefreshShop();
            }
        }

        private void RefreshShop()
        {
            if (shopItemContainer == null || shopItemPrefab == null) return;

            // Clear existing
            foreach (Transform child in shopItemContainer)
            {
                Destroy(child.gameObject);
            }

            var gm = GameManager.Instance;
            if (gm == null) return;

            var machineMgr = gm.machineManager;
            if (machineMgr == null) return;

            foreach (var def in machineMgr.allMachineDefinitions)
            {
                if (def.cost <= 0) continue; // Skip free/special machines

                var item = Instantiate(shopItemPrefab, shopItemContainer);
                var shopItem = item.GetComponent<ShopItemUI>();
                if (shopItem != null)
                {
                    bool locked = def.unlockDay > gm.Day;
                    bool canAfford = gm.Money >= def.cost;
                    shopItem.Setup(def, locked, canAfford);
                    shopItem.OnClicked += () => OnShopItemClicked(def);
                }
            }
        }

        private void OnShopItemClicked(MachineDefinition def)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.SelectedShopItem = (gm.SelectedShopItem == def) ? null : def;
            if (gm.SelectedShopItem != null) gm.SetTool(ToolMode.Place);

            RefreshShop();
        }

        // ===== UPGRADES =====

        public void ToggleUpgrades()
        {
            _upgradeOpen = !_upgradeOpen;
            if (upgradePanel != null) upgradePanel.SetActive(_upgradeOpen);
            if (_upgradeOpen)
            {
                _shopOpen = false;
                if (shopPanel != null) shopPanel.SetActive(false);
                RefreshUpgrades();
            }
        }

        private void RefreshUpgrades()
        {
            if (upgradeItemContainer == null || upgradeItemPrefab == null) return;

            foreach (Transform child in upgradeItemContainer)
            {
                Destroy(child.gameObject);
            }

            var gm = GameManager.Instance;
            if (gm?.progressionManager == null) return;

            foreach (var def in gm.progressionManager.GetAllUpgrades())
            {
                var item = Instantiate(upgradeItemPrefab, upgradeItemContainer);
                var upgradeItem = item.GetComponent<UpgradeItemUI>();
                if (upgradeItem != null)
                {
                    bool purchased = gm.progressionManager.HasUpgrade(def.upgradeId);
                    bool canBuy = gm.progressionManager.CanPurchaseUpgrade(def.upgradeId);
                    upgradeItem.Setup(def, purchased, canBuy);
                    upgradeItem.OnClicked += () => OnUpgradeClicked(def);
                }
            }
        }

        private void OnUpgradeClicked(UpgradeDefinition def)
        {
            var gm = GameManager.Instance;
            if (gm?.progressionManager == null) return;

            if (gm.progressionManager.PurchaseUpgrade(def.upgradeId))
            {
                RefreshUpgrades();
                UpdateAll();
            }
        }

        // ===== TOOLS =====

        private void OnToolClicked(int index)
        {
            ToolMode[] tools = { ToolMode.Select, ToolMode.Move, ToolMode.Wire, ToolMode.Demolish };
            if (index < tools.Length)
            {
                GameManager.Instance?.SetTool(tools[index]);
            }
        }

        private void OnSpeedClicked(int speed)
        {
            GameManager.Instance?.SetGameSpeed(speed);
            // Update button visuals
            for (int i = 0; i < speedButtons.Length; i++)
            {
                if (speedButtons[i] != null)
                {
                    var colors = speedButtons[i].colors;
                    colors.normalColor = (i == speed) ? new Color(0.376f, 0.647f, 0.980f, 0.4f) : Color.white;
                    speedButtons[i].colors = colors;
                }
            }
        }

        private void UpdateToolButtons(ToolMode mode)
        {
            ToolMode[] tools = { ToolMode.Select, ToolMode.Move, ToolMode.Wire, ToolMode.Demolish };
            for (int i = 0; i < toolButtons.Length && i < tools.Length; i++)
            {
                if (toolButtons[i] != null)
                {
                    var colors = toolButtons[i].colors;
                    colors.normalColor = (tools[i] == mode) ? new Color(0.376f, 0.647f, 0.980f, 0.4f) : Color.white;
                    toolButtons[i].colors = colors;
                }
            }
        }

        // ===== HELD ITEM =====

        public void UpdateHeldItem(IngredientType item)
        {
            if (heldItemDisplay == null) return;
            heldItemDisplay.SetActive(item != IngredientType.None);
            if (heldItemName != null) heldItemName.text = item.ToString();
        }

        // ===== TOOLTIP =====

        public void ShowTooltip(Vector3 screenPos, string text)
        {
            if (tooltipPanel == null) return;
            tooltipPanel.SetActive(true);
            tooltipPanel.transform.position = screenPos + new Vector3(15, -10, 0);
            if (tooltipText != null) tooltipText.text = text;
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
        }

        // ===== TOAST =====

        private void ShowToast(string message)
        {
            if (toastPrefab == null || toastContainer == null) return;

            var toast = Instantiate(toastPrefab, toastContainer);
            var text = toast.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = message;

            Destroy(toast, 2.5f);
        }

        // ===== UTILITY =====

        public void CloseAllPanels()
        {
            _shopOpen = false;
            _upgradeOpen = false;
            if (shopPanel != null) shopPanel.SetActive(false);
            if (upgradePanel != null) upgradePanel.SetActive(false);
            if (daySummaryPanel != null) daySummaryPanel.SetActive(false);
        }

        public void HideStartScreen()
        {
            if (startScreen != null) startScreen.SetActive(false);
        }

        public void ShowStartScreen()
        {
            if (startScreen != null) startScreen.SetActive(true);
        }
    }
}
