using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KitchenEmpire
{
    /// <summary>
    /// Drop this on an empty GameObject in your scene.
    /// It auto-creates all managers, camera, UI, and wires everything up.
    /// Run KitchenEmpire > Setup All Data first to generate ScriptableObjects.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Optional - will auto-find if left null")]
        public GameConfig gameConfig;
        public MachineDefinition[] machineDefinitions;
        public RecipeDatabase recipeDatabase;
        public UpgradeDefinition[] upgradeDefinitions;

        void Awake()
        {
            // Try to load config from Resources if not assigned
            if (gameConfig == null)
                gameConfig = Resources.Load<GameConfig>("GameConfig");
            if (gameConfig == null)
                gameConfig = ScriptableObject.CreateInstance<GameConfig>();

            if (recipeDatabase == null)
                recipeDatabase = Resources.Load<RecipeDatabase>("RecipeDatabase");

            BuildGame();
        }

        private void BuildGame()
        {
            // ===== CAMERA =====
            var camObj = new GameObject("IsometricCamera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.20f, 0.25f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            camObj.tag = "MainCamera";
            var isoCamera = camObj.AddComponent<IsometricCamera>();
            camObj.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
            camObj.transform.position = new Vector3(0, 10, -10);
            camObj.AddComponent<AudioListener>();

            // Destroy default camera if exists
            var defaultCam = Camera.main;
            if (defaultCam != null && defaultCam.gameObject != camObj)
                Destroy(defaultCam.gameObject);

            // ===== LIGHTING =====
            var lightObj = new GameObject("DirectionalLight");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.2f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // ===== GRID =====
            var gridObj = new GameObject("GridManager");
            var gridManager = gridObj.AddComponent<GridManager>();

            // ===== MACHINES =====
            var machineObj = new GameObject("MachineManager");
            var machineManager = machineObj.AddComponent<MachineManager>();
            machineManager.allMachineDefinitions = machineDefinitions ?? new MachineDefinition[0];

            // ===== POWER =====
            var powerObj = new GameObject("PowerManager");
            var powerManager = powerObj.AddComponent<PowerManager>();
            powerManager.machineManager = machineManager;

            // ===== CUSTOMERS =====
            var customerObj = new GameObject("CustomerManager");
            var customerManager = customerObj.AddComponent<CustomerManager>();
            customerManager.machineManager = machineManager;
            customerManager.gridManager = gridManager;
            customerManager.recipeDatabase = recipeDatabase;

            // ===== PROGRESSION =====
            var progressionObj = new GameObject("ProgressionManager");
            var progressionManager = progressionObj.AddComponent<ProgressionManager>();
            progressionManager.allUpgrades = upgradeDefinitions ?? new UpgradeDefinition[0];

            // ===== SAVE =====
            var saveObj = new GameObject("SaveManager");
            var saveManager = saveObj.AddComponent<SaveManager>();

            // ===== CONVEYOR =====
            var conveyorObj = new GameObject("ConveyorSystem");
            var conveyorSystem = conveyorObj.AddComponent<ConveyorSystem>();
            conveyorSystem.machineManager = machineManager;

            // ===== VFX =====
            var vfxObj = new GameObject("VFXManager");
            vfxObj.AddComponent<VFXManager>();

            // ===== UI =====
            var uiManager = BuildUI();

            // ===== INPUT =====
            var inputObj = new GameObject("InputHandler");
            var inputHandler = inputObj.AddComponent<InputHandler>();
            inputHandler.isoCamera = isoCamera;
            inputHandler.gridManager = gridManager;
            inputHandler.machineManager = machineManager;
            inputHandler.powerManager = powerManager;
            inputHandler.uiManager = uiManager;

            // ===== GAME MANAGER =====
            var gmObj = new GameObject("GameManager");
            var gm = gmObj.AddComponent<GameManager>();
            gm.config = gameConfig;
            gm.gridManager = gridManager;
            gm.machineManager = machineManager;
            gm.customerManager = customerManager;
            gm.powerManager = powerManager;
            gm.progressionManager = progressionManager;
            gm.saveManager = saveManager;
            gm.uiManager = uiManager;
            gm.inputHandler = inputHandler;
            gm.isoCamera = isoCamera;

            // ===== EVENT SYSTEM (for UI clicks) =====
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Debug.Log("Kitchen Empire: Bootstrap complete! All systems ready.");
        }

        private UIManager BuildUI()
        {
            // Canvas
            var canvasObj = new GameObject("UI_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            var uiMgr = canvasObj.AddComponent<UIManager>();

            // ===== TOP BAR (HUD) =====
            var topBar = CreatePanel(canvasObj.transform, "TopBar",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -10), new Vector2(0, -50),
                new Color(0.12f, 0.14f, 0.18f, 0.9f));

            uiMgr.dayText = CreateText(topBar.transform, "DayText", "Day 1",
                new Vector2(20, -5), new Vector2(120, 35), 20, TextAlignmentOptions.Left);

            uiMgr.moneyText = CreateText(topBar.transform, "MoneyText", "$150",
                new Vector2(140, -5), new Vector2(260, 35), 22, TextAlignmentOptions.Left,
                new Color(0.29f, 0.85f, 0.5f));

            uiMgr.reputationText = CreateText(topBar.transform, "RepText", "★ 0",
                new Vector2(280, -5), new Vector2(380, 35), 18, TextAlignmentOptions.Left,
                new Color(0.984f, 0.749f, 0.149f));

            uiMgr.powerText = CreateText(topBar.transform, "PowerText", "⚡ 0/10W",
                new Vector2(400, -5), new Vector2(530, 35), 18, TextAlignmentOptions.Left,
                new Color(0.376f, 0.647f, 0.980f));

            uiMgr.dayTimerText = CreateText(topBar.transform, "TimerText", "2:00",
                new Vector2(-140, -5), new Vector2(-20, 35), 24, TextAlignmentOptions.Right,
                Color.white);
            var timerRT = uiMgr.dayTimerText.GetComponent<RectTransform>();
            timerRT.anchorMin = new Vector2(1, 1);
            timerRT.anchorMax = new Vector2(1, 1);

            // ===== BOTTOM BAR (Buttons) =====
            var bottomBar = CreatePanel(canvasObj.transform, "BottomBar",
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 60), new Vector2(0, 10),
                new Color(0.12f, 0.14f, 0.18f, 0.9f));

            // Start Day Button
            uiMgr.startDayButton = CreateButton(bottomBar.transform, "StartDayBtn",
                new Vector2(20, 8), new Vector2(160, 45),
                "Start Day", new Color(0.29f, 0.85f, 0.5f), Color.white);
            uiMgr.startDayButtonText = uiMgr.startDayButton.GetComponentInChildren<TextMeshProUGUI>();

            // Shop Button
            uiMgr.shopButton = CreateButton(bottomBar.transform, "ShopBtn",
                new Vector2(175, 8), new Vector2(280, 45),
                "Shop [B]", new Color(0.376f, 0.647f, 0.980f), Color.white);

            // Upgrade Button
            uiMgr.upgradeButton = CreateButton(bottomBar.transform, "UpgradeBtn",
                new Vector2(295, 8), new Vector2(420, 45),
                "Upgrades [U]", new Color(0.655f, 0.545f, 0.980f), Color.white);

            // Tool buttons
            string[] toolNames = { "Select [1]", "Move [2]", "Wire [3]", "Demolish [4]" };
            uiMgr.toolButtons = new Button[toolNames.Length];
            for (int i = 0; i < toolNames.Length; i++)
            {
                float x = 450 + i * 110;
                uiMgr.toolButtons[i] = CreateButton(bottomBar.transform, $"Tool{i}Btn",
                    new Vector2(x, 8), new Vector2(x + 100, 45),
                    toolNames[i], new Color(0.25f, 0.27f, 0.32f), Color.white);
            }

            // Speed buttons
            string[] speedNames = { "||", "1x", "2x" };
            uiMgr.speedButtons = new Button[speedNames.Length];
            for (int i = 0; i < speedNames.Length; i++)
            {
                uiMgr.speedButtons[i] = CreateButton(bottomBar.transform, $"Speed{i}Btn",
                    new Vector2(-30 - (speedNames.Length - i) * 55, 8),
                    new Vector2(-30 - (speedNames.Length - i - 1) * 55, 45),
                    speedNames[i], new Color(0.25f, 0.27f, 0.32f), Color.white);
                var rt = uiMgr.speedButtons[i].GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
            }

            // ===== SHOP PANEL =====
            uiMgr.shopPanel = CreatePanel(canvasObj.transform, "ShopPanel",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(10, 70), new Vector2(350, 500),
                new Color(0.15f, 0.17f, 0.22f, 0.95f));

            var shopTitle = CreateText(uiMgr.shopPanel.transform, "ShopTitle", "SHOP",
                new Vector2(10, -10), new Vector2(330, 40), 22, TextAlignmentOptions.Center,
                new Color(0.376f, 0.647f, 0.980f));

            var shopScroll = CreateScrollArea(uiMgr.shopPanel.transform, "ShopScroll",
                new Vector2(5, 45), new Vector2(335, 425));
            uiMgr.shopItemContainer = shopScroll.transform;

            // Shop item prefab
            uiMgr.shopItemPrefab = CreateShopItemPrefab();

            uiMgr.shopPanel.SetActive(false);

            // ===== UPGRADE PANEL =====
            uiMgr.upgradePanel = CreatePanel(canvasObj.transform, "UpgradePanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-200, -250), new Vector2(200, 250),
                new Color(0.15f, 0.17f, 0.22f, 0.95f));

            CreateText(uiMgr.upgradePanel.transform, "UpgradeTitle", "UPGRADES",
                new Vector2(10, -10), new Vector2(390, 40), 22, TextAlignmentOptions.Center,
                new Color(0.655f, 0.545f, 0.980f));

            var upgradeScroll = CreateScrollArea(uiMgr.upgradePanel.transform, "UpgradeScroll",
                new Vector2(5, 45), new Vector2(395, 490));
            uiMgr.upgradeItemContainer = upgradeScroll.transform;

            uiMgr.upgradeItemPrefab = CreateUpgradeItemPrefab();

            uiMgr.upgradePanel.SetActive(false);

            // ===== DAY SUMMARY =====
            uiMgr.daySummaryPanel = CreatePanel(canvasObj.transform, "DaySummaryPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-180, -160), new Vector2(180, 160),
                new Color(0.12f, 0.14f, 0.18f, 0.97f));

            CreateText(uiMgr.daySummaryPanel.transform, "SummaryTitle", "DAY COMPLETE",
                new Vector2(10, -10), new Vector2(350, 40), 24, TextAlignmentOptions.Center,
                new Color(0.984f, 0.749f, 0.149f));

            float sy = 55;
            CreateText(uiMgr.daySummaryPanel.transform, "LblServed", "Customers Served:",
                new Vector2(20, sy), new Vector2(200, sy + 25), 16, TextAlignmentOptions.Left);
            uiMgr.summaryServedText = CreateText(uiMgr.daySummaryPanel.transform, "ValServed", "0",
                new Vector2(210, sy), new Vector2(340, sy + 25), 16, TextAlignmentOptions.Right,
                new Color(0.29f, 0.85f, 0.5f));

            sy += 30;
            CreateText(uiMgr.daySummaryPanel.transform, "LblFailed", "Customers Lost:",
                new Vector2(20, sy), new Vector2(200, sy + 25), 16, TextAlignmentOptions.Left);
            uiMgr.summaryFailedText = CreateText(uiMgr.daySummaryPanel.transform, "ValFailed", "0",
                new Vector2(210, sy), new Vector2(340, sy + 25), 16, TextAlignmentOptions.Right,
                new Color(0.97f, 0.44f, 0.44f));

            sy += 30;
            CreateText(uiMgr.daySummaryPanel.transform, "LblRevenue", "Revenue:",
                new Vector2(20, sy), new Vector2(200, sy + 25), 16, TextAlignmentOptions.Left);
            uiMgr.summaryRevenueText = CreateText(uiMgr.daySummaryPanel.transform, "ValRevenue", "$0",
                new Vector2(210, sy), new Vector2(340, sy + 25), 16, TextAlignmentOptions.Right,
                new Color(0.29f, 0.85f, 0.5f));

            sy += 30;
            CreateText(uiMgr.daySummaryPanel.transform, "LblTips", "Tips:",
                new Vector2(20, sy), new Vector2(200, sy + 25), 16, TextAlignmentOptions.Left);
            uiMgr.summaryTipsText = CreateText(uiMgr.daySummaryPanel.transform, "ValTips", "$0",
                new Vector2(210, sy), new Vector2(340, sy + 25), 16, TextAlignmentOptions.Right,
                new Color(0.984f, 0.749f, 0.149f));

            sy += 35;
            CreateText(uiMgr.daySummaryPanel.transform, "LblTotal", "TOTAL:",
                new Vector2(20, sy), new Vector2(200, sy + 30), 20, TextAlignmentOptions.Left, Color.white);
            uiMgr.summaryTotalText = CreateText(uiMgr.daySummaryPanel.transform, "ValTotal", "$0",
                new Vector2(210, sy), new Vector2(340, sy + 30), 20, TextAlignmentOptions.Right,
                new Color(0.29f, 0.85f, 0.5f));

            sy += 35;
            uiMgr.summaryReputationText = CreateText(uiMgr.daySummaryPanel.transform, "ValRep", "★ 0",
                new Vector2(20, sy), new Vector2(340, sy + 25), 18, TextAlignmentOptions.Center,
                new Color(0.984f, 0.749f, 0.149f));

            uiMgr.nextDayButton = CreateButton(uiMgr.daySummaryPanel.transform, "NextDayBtn",
                new Vector2(80, 270), new Vector2(280, 310),
                "Next Day", new Color(0.29f, 0.85f, 0.5f), Color.white);

            uiMgr.daySummaryPanel.SetActive(false);

            // ===== HELD ITEM DISPLAY =====
            uiMgr.heldItemDisplay = CreatePanel(canvasObj.transform, "HeldItem",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-75, 70), new Vector2(75, 110),
                new Color(0.18f, 0.20f, 0.25f, 0.9f));

            uiMgr.heldItemName = CreateText(uiMgr.heldItemDisplay.transform, "HeldName", "",
                new Vector2(5, 5), new Vector2(145, 35), 14, TextAlignmentOptions.Center,
                new Color(0.984f, 0.749f, 0.149f));

            uiMgr.heldItemDisplay.SetActive(false);

            // ===== TOOLTIP =====
            uiMgr.tooltipPanel = CreatePanel(canvasObj.transform, "Tooltip",
                new Vector2(0, 0), new Vector2(0, 0),
                Vector2.zero, new Vector2(200, 60),
                new Color(0.08f, 0.09f, 0.12f, 0.95f));

            uiMgr.tooltipText = CreateText(uiMgr.tooltipPanel.transform, "TooltipText", "",
                new Vector2(8, 5), new Vector2(192, 55), 12, TextAlignmentOptions.TopLeft);

            uiMgr.tooltipPanel.SetActive(false);

            // ===== TOAST CONTAINER =====
            var toastContainer = new GameObject("ToastContainer");
            toastContainer.transform.SetParent(canvasObj.transform, false);
            var toastRT = toastContainer.AddComponent<RectTransform>();
            toastRT.anchorMin = new Vector2(0.5f, 0.8f);
            toastRT.anchorMax = new Vector2(0.5f, 0.8f);
            toastRT.sizeDelta = new Vector2(400, 200);
            var vlg = toastContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            uiMgr.toastContainer = toastContainer.transform;
            uiMgr.toastPrefab = CreateToastPrefab();

            return uiMgr;
        }

        // ===== UI HELPERS =====

        private GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            Color bgColor)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var img = obj.AddComponent<Image>();
            img.color = bgColor;

            return obj;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text,
            Vector2 posMin, Vector2 posMax, float fontSize,
            TextAlignmentOptions alignment, Color? color = null)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.offsetMin = posMin;
            rt.offsetMax = posMax;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color ?? Color.white;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }

        private Button CreateButton(Transform parent, string name,
            Vector2 posMin, Vector2 posMax,
            string label, Color bgColor, Color textColor)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.offsetMin = posMin;
            rt.offsetMax = posMax;

            var img = obj.AddComponent<Image>();
            img.color = bgColor;

            var btn = obj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.2f;
            colors.pressedColor = bgColor * 0.8f;
            btn.colors = colors;

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            var textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;

            return btn;
        }

        private GameObject CreateScrollArea(Transform parent, string name,
            Vector2 posMin, Vector2 posMax)
        {
            var scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);
            var scrollRT = scrollObj.AddComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.zero;
            scrollRT.offsetMin = posMin;
            scrollRT.offsetMax = posMax;

            var content = new GameObject("Content");
            content.transform.SetParent(scrollObj.transform, false);
            var contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 0);

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.padding = new RectOffset(5, 5, 5, 5);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollObj.AddComponent<Image>().color = Color.clear;
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.content = contentRT;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            scrollObj.AddComponent<Mask>().showMaskGraphic = false;

            return content.gameObject;
        }

        private GameObject CreateShopItemPrefab()
        {
            var obj = new GameObject("ShopItemPrefab");
            obj.SetActive(false);

            var rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320, 70);

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.05f);

            var le = obj.AddComponent<LayoutElement>();
            le.minHeight = 70;
            le.preferredHeight = 70;

            var shopItem = obj.AddComponent<ShopItemUI>();

            // Name
            shopItem.nameText = CreateText(obj.transform, "Name", "Machine",
                new Vector2(10, 35), new Vector2(250, 60), 16, TextAlignmentOptions.Left);

            // Cost
            shopItem.costText = CreateText(obj.transform, "Cost", "$50",
                new Vector2(250, 35), new Vector2(315, 60), 16, TextAlignmentOptions.Right,
                new Color(0.29f, 0.85f, 0.5f));

            // Description
            shopItem.descText = CreateText(obj.transform, "Desc", "Description here",
                new Vector2(10, 5), new Vector2(315, 30), 12, TextAlignmentOptions.Left,
                new Color(1f, 1f, 1f, 0.6f));

            shopItem.button = obj.AddComponent<Button>();
            shopItem.background = bg;

            // Don't destroy - it's a template
            DontDestroyOnLoad(obj);
            return obj;
        }

        private GameObject CreateUpgradeItemPrefab()
        {
            var obj = new GameObject("UpgradeItemPrefab");
            obj.SetActive(false);

            var rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380, 65);

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.05f);

            var le = obj.AddComponent<LayoutElement>();
            le.minHeight = 65;
            le.preferredHeight = 65;

            var upgradeItem = obj.AddComponent<UpgradeItemUI>();

            upgradeItem.nameText = CreateText(obj.transform, "Name", "Upgrade",
                new Vector2(10, 32), new Vector2(280, 55), 15, TextAlignmentOptions.Left);

            upgradeItem.costText = CreateText(obj.transform, "Cost", "$100",
                new Vector2(280, 32), new Vector2(370, 55), 15, TextAlignmentOptions.Right,
                new Color(0.29f, 0.85f, 0.5f));

            upgradeItem.descText = CreateText(obj.transform, "Desc", "Description",
                new Vector2(10, 5), new Vector2(370, 28), 12, TextAlignmentOptions.Left,
                new Color(1f, 1f, 1f, 0.6f));

            upgradeItem.button = obj.AddComponent<Button>();
            upgradeItem.background = bg;

            DontDestroyOnLoad(obj);
            return obj;
        }

        private GameObject CreateToastPrefab()
        {
            var obj = new GameObject("ToastPrefab");
            obj.SetActive(false);

            var rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 35);

            var img = obj.AddComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.18f, 0.9f);

            var le = obj.AddComponent<LayoutElement>();
            le.preferredWidth = 300;
            le.preferredHeight = 35;

            CreateText(obj.transform, "Text", "Toast message",
                new Vector2(10, 2), new Vector2(290, 32), 14, TextAlignmentOptions.Center);

            DontDestroyOnLoad(obj);
            return obj;
        }
    }
}
