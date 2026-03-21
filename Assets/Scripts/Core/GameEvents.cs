using System;

namespace KitchenEmpire
{
    /// <summary>
    /// Central event bus for decoupled communication between systems.
    /// </summary>
    public static class GameEvents
    {
        // Day lifecycle
        public static event Action OnDayStarted;
        public static event Action<DaySummaryData> OnDayEnded;
        public static event Action<int> OnDayTimerUpdated; // seconds remaining

        // Money
        public static event Action<int> OnMoneyChanged;
        public static event Action<int> OnRevenueEarned; // amount earned from serving
        public static event Action<int> OnTipEarned;

        // Machines
        public static event Action<Vector2Int, MachineType> OnMachinePlaced;
        public static event Action<Vector2Int> OnMachineRemoved;
        public static event Action<Vector2Int, IngredientType> OnMachineProcessingComplete;
        public static event Action<Vector2Int, IngredientType> OnItemPlacedOnMachine;
        public static event Action<Vector2Int, IngredientType> OnItemPickedUp;

        // Customers
        public static event Action<int> OnCustomerSpawned; // customer id
        public static event Action<int, bool> OnCustomerLeft; // id, satisfied
        public static event Action<int> OnCustomerServed;

        // Power
        public static event Action<int, int> OnPowerChanged; // used, max

        // Progression
        public static event Action<string> OnUpgradePurchased;
        public static event Action<int> OnReputationChanged;
        public static event Action<int, int> OnKitchenExpanded; // newW, newH

        // UI
        public static event Action<string> OnToastMessage;
        public static event Action<ToolMode> OnToolChanged;

        // Fire methods
        public static void FireDayStarted() => OnDayStarted?.Invoke();
        public static void FireDayEnded(DaySummaryData data) => OnDayEnded?.Invoke(data);
        public static void FireDayTimerUpdated(int seconds) => OnDayTimerUpdated?.Invoke(seconds);
        public static void FireMoneyChanged(int amount) => OnMoneyChanged?.Invoke(amount);
        public static void FireRevenueEarned(int amount) => OnRevenueEarned?.Invoke(amount);
        public static void FireTipEarned(int amount) => OnTipEarned?.Invoke(amount);
        public static void FireMachinePlaced(Vector2Int pos, MachineType type) => OnMachinePlaced?.Invoke(pos, type);
        public static void FireMachineRemoved(Vector2Int pos) => OnMachineRemoved?.Invoke(pos);
        public static void FireMachineProcessingComplete(Vector2Int pos, IngredientType item) => OnMachineProcessingComplete?.Invoke(pos, item);
        public static void FireItemPlacedOnMachine(Vector2Int pos, IngredientType item) => OnItemPlacedOnMachine?.Invoke(pos, item);
        public static void FireItemPickedUp(Vector2Int pos, IngredientType item) => OnItemPickedUp?.Invoke(pos, item);
        public static void FireCustomerSpawned(int id) => OnCustomerSpawned?.Invoke(id);
        public static void FireCustomerLeft(int id, bool satisfied) => OnCustomerLeft?.Invoke(id, satisfied);
        public static void FireCustomerServed(int id) => OnCustomerServed?.Invoke(id);
        public static void FirePowerChanged(int used, int max) => OnPowerChanged?.Invoke(used, max);
        public static void FireUpgradePurchased(string id) => OnUpgradePurchased?.Invoke(id);
        public static void FireReputationChanged(int rep) => OnReputationChanged?.Invoke(rep);
        public static void FireKitchenExpanded(int w, int h) => OnKitchenExpanded?.Invoke(w, h);
        public static void FireToast(string msg) => OnToastMessage?.Invoke(msg);
        public static void FireToolChanged(ToolMode mode) => OnToolChanged?.Invoke(mode);
    }

    [System.Serializable]
    public struct DaySummaryData
    {
        public int day;
        public int customersServed;
        public int customersFailed;
        public int revenue;
        public int tips;
        public int totalEarned;
        public int reputation;
    }

    // Using UnityEngine.Vector2Int but defining a local one for non-Unity compilation reference
    // In actual Unity, use UnityEngine.Vector2Int
}
