using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// ScriptableObject for a permanent upgrade the player can purchase.
    /// These persist across days and never reset.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "KitchenEmpire/Upgrade Definition")]
    public class UpgradeDefinition : ScriptableObject
    {
        public string upgradeId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public int cost;

        [Header("Prerequisites")]
        [Tooltip("Upgrade ID that must be purchased first. Leave empty for no prerequisite.")]
        public string requiresUpgradeId;

        [Header("Effects")]
        public UpgradeEffect[] effects;
    }

    [System.Serializable]
    public struct UpgradeEffect
    {
        public UpgradeType type;
        public float value;
    }

    public enum UpgradeType
    {
        GridExpansionWidth,   // value = new grid width
        GridExpansionHeight,  // value = new grid height
        CookSpeedMultiplier,  // value = multiplier (0.8 = 20% faster)
        PatienceMultiplier,   // value = multiplier (1.25 = 25% more patience)
        TipMultiplier,        // value = multiplier
        BonusPower,           // value = extra watts
        AutoRestock,          // value ignored, enables fridge auto-restock
    }
}
