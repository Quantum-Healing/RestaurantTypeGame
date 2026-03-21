using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace KitchenEmpire
{
    /// <summary>
    /// UI element for a single upgrade in the upgrade panel.
    /// </summary>
    public class UpgradeItemUI : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descText;
        public TextMeshProUGUI costText;
        public Button button;
        public Image background;
        public GameObject purchasedCheckmark;

        public event Action OnClicked;

        public void Setup(UpgradeDefinition def, bool purchased, bool canBuy)
        {
            if (nameText != null)
            {
                nameText.text = (purchased ? "✅ " : "") + def.displayName;
            }

            if (descText != null) descText.text = def.description;

            if (costText != null)
            {
                if (purchased)
                {
                    costText.text = "Purchased";
                    costText.color = new Color(0.29f, 0.85f, 0.5f);
                }
                else if (!string.IsNullOrEmpty(def.requiresUpgradeId) &&
                         !GameManager.Instance.progressionManager.HasUpgrade(def.requiresUpgradeId))
                {
                    var reqDef = GameManager.Instance.progressionManager.GetUpgradeById(def.requiresUpgradeId);
                    costText.text = $"🔒 Requires: {reqDef?.displayName ?? def.requiresUpgradeId}";
                    costText.color = new Color(1f, 1f, 1f, 0.5f);
                }
                else
                {
                    costText.text = $"${def.cost}";
                    costText.color = canBuy ? new Color(0.29f, 0.85f, 0.5f) : new Color(0.97f, 0.44f, 0.44f);
                }
            }

            if (button != null)
            {
                button.interactable = canBuy && !purchased;
                button.onClick.AddListener(() => OnClicked?.Invoke());
            }

            if (purchasedCheckmark != null) purchasedCheckmark.SetActive(purchased);

            if (background != null)
            {
                background.color = purchased
                    ? new Color(0.29f, 0.85f, 0.5f, 0.08f)
                    : new Color(1f, 1f, 1f, 0.05f);
            }
        }
    }
}
