using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace KitchenEmpire
{
    /// <summary>
    /// Manages permanent progression: purchased upgrades and their
    /// cumulative effects. Nothing resets between days.
    ///
    /// This is what replaces roguelike replayability:
    /// - Kitchen grows over time
    /// - Cooking gets faster
    /// - Customers are more patient
    /// - Power capacity increases
    /// - Automation improves
    /// </summary>
    public class ProgressionManager : MonoBehaviour
    {
        [Header("Upgrade Database")]
        public UpgradeDefinition[] allUpgrades;

        private HashSet<string> _purchasedUpgrades = new();

        // Cached effective values
        public float CookSpeedMultiplier { get; private set; } = 1f;
        public float PatienceMultiplier { get; private set; } = 1f;
        public float TipMultiplier { get; private set; } = 1f;
        public int BonusPower { get; private set; }
        public bool HasAutoRestock { get; private set; }

        public bool HasUpgrade(string id) => _purchasedUpgrades.Contains(id);

        public bool CanPurchaseUpgrade(string id)
        {
            if (_purchasedUpgrades.Contains(id)) return false;

            var def = GetUpgradeById(id);
            if (def == null) return false;

            // Check prerequisite
            if (!string.IsNullOrEmpty(def.requiresUpgradeId) &&
                !_purchasedUpgrades.Contains(def.requiresUpgradeId))
                return false;

            // Check money
            var gm = GameManager.Instance;
            if (gm != null && gm.Money < def.cost) return false;

            return true;
        }

        public bool PurchaseUpgrade(string id)
        {
            if (!CanPurchaseUpgrade(id)) return false;

            var def = GetUpgradeById(id);
            var gm = GameManager.Instance;

            if (gm != null && !gm.SpendMoney(def.cost)) return false;

            _purchasedUpgrades.Add(id);
            ApplyUpgradeEffects(def);

            GameEvents.FireUpgradePurchased(id);
            GameEvents.FireToast($"Upgraded: {def.displayName}!");

            return true;
        }

        private void ApplyUpgradeEffects(UpgradeDefinition def)
        {
            foreach (var effect in def.effects)
            {
                switch (effect.type)
                {
                    case UpgradeType.GridExpansionWidth:
                    case UpgradeType.GridExpansionHeight:
                        ApplyGridExpansion(def);
                        break;
                    case UpgradeType.CookSpeedMultiplier:
                        CookSpeedMultiplier = effect.value;
                        break;
                    case UpgradeType.PatienceMultiplier:
                        PatienceMultiplier = effect.value;
                        break;
                    case UpgradeType.TipMultiplier:
                        TipMultiplier = effect.value;
                        break;
                    case UpgradeType.BonusPower:
                        BonusPower = Mathf.RoundToInt(effect.value);
                        break;
                    case UpgradeType.AutoRestock:
                        HasAutoRestock = true;
                        break;
                }
            }

            // Recalculate power after upgrade
            GameManager.Instance?.powerManager?.RecalculatePower();
        }

        private void ApplyGridExpansion(UpgradeDefinition def)
        {
            int newW = 0, newH = 0;
            foreach (var effect in def.effects)
            {
                if (effect.type == UpgradeType.GridExpansionWidth) newW = Mathf.RoundToInt(effect.value);
                if (effect.type == UpgradeType.GridExpansionHeight) newH = Mathf.RoundToInt(effect.value);
            }
            if (newW > 0 || newH > 0)
            {
                GameManager.Instance?.gridManager?.ExpandGrid(newW, newH);
            }
        }

        public UpgradeDefinition GetUpgradeById(string id)
        {
            foreach (var def in allUpgrades)
            {
                if (def.upgradeId == id) return def;
            }
            return null;
        }

        public List<UpgradeDefinition> GetAllUpgrades()
        {
            return allUpgrades?.ToList() ?? new List<UpgradeDefinition>();
        }

        public void Reset()
        {
            _purchasedUpgrades.Clear();
            CookSpeedMultiplier = 1f;
            PatienceMultiplier = 1f;
            TipMultiplier = 1f;
            BonusPower = 0;
            HasAutoRestock = false;
        }

        // ===== SAVE/LOAD =====

        public string[] GetSaveData()
        {
            return _purchasedUpgrades.ToArray();
        }

        public void LoadSaveData(string[] data)
        {
            Reset();
            if (data == null) return;

            foreach (var id in data)
            {
                _purchasedUpgrades.Add(id);
                var def = GetUpgradeById(id);
                if (def != null) ApplyUpgradeEffects(def);
            }
        }
    }
}
