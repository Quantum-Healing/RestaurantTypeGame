using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace KitchenEmpire
{
    /// <summary>
    /// UI element for a single item in the shop panel.
    /// </summary>
    public class ShopItemUI : MonoBehaviour
    {
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI descText;
        public TextMeshProUGUI powerText;
        public Button button;
        public Image background;
        public GameObject lockedOverlay;
        public GameObject selectedIndicator;

        public event Action OnClicked;

        private MachineDefinition _definition;

        public void Setup(MachineDefinition def, bool locked, bool canAfford)
        {
            _definition = def;

            if (nameText != null) nameText.text = def.displayName;
            if (costText != null) costText.text = $"${def.cost}";
            if (descText != null) descText.text = locked ? $"Unlocks Day {def.unlockDay}" : def.description;
            if (icon != null && def.icon != null) icon.sprite = def.icon;

            // Power info
            if (powerText != null)
            {
                if (def.powerUsage > 0) powerText.text = $"{def.powerUsage}W";
                else if (def.powerUsage < 0) powerText.text = $"+{-def.powerUsage}W";
                else powerText.gameObject.SetActive(false);
            }

            // State
            if (lockedOverlay != null) lockedOverlay.SetActive(locked);
            if (button != null) button.interactable = !locked && canAfford;

            // Cost color
            if (costText != null)
            {
                costText.color = canAfford ? new Color(0.29f, 0.85f, 0.5f) : new Color(0.97f, 0.44f, 0.44f);
            }

            // Selected state
            bool isSelected = GameManager.Instance?.SelectedShopItem == def;
            if (selectedIndicator != null) selectedIndicator.SetActive(isSelected);
            if (background != null)
            {
                background.color = isSelected
                    ? new Color(0.29f, 0.85f, 0.5f, 0.15f)
                    : new Color(1f, 1f, 1f, 0.05f);
            }

            if (button != null) button.onClick.AddListener(() => OnClicked?.Invoke());
        }
    }
}
