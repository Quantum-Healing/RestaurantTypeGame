using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// Global game configuration constants.
    /// Tweak these to balance the game.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "KitchenEmpire/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid")]
        public int initialGridWidth = 6;
        public int initialGridHeight = 6;
        public int maxGridWidth = 16;
        public int maxGridHeight = 16;

        [Header("Isometric Rendering")]
        public float tileWidth = 1f;
        public float tileHeight = 0.5f;
        public float tileDepth = 0.3f;

        [Header("Starting Resources")]
        public int startingMoney = 150;
        public int basePowerCapacity = 10;

        [Header("Day Timing")]
        public float dayLengthSeconds = 120f;
        public float baseCustomerSpawnInterval = 8f;
        public float minCustomerSpawnInterval = 2f;
        public float spawnIntervalReductionPerDay = 0.3f;

        [Header("Customer Patience")]
        public float basePatienceSeconds = 30f;
        public float patienceReductionPerDay = 0.5f;
        public float minPatienceSeconds = 10f;
        public float eatingDurationSeconds = 5f;

        [Header("Difficulty Scaling")]
        public int baseMaxCustomers = 3;
        public int maxCustomersCap = 12;
        public float customersPerDayIncrease = 0.5f;

        [Header("Economy")]
        public float demolishRefundPercent = 0.5f;
        public float tipBonusThreshold = 0.5f; // patience % above which tips are given
        public float baseTipPercent = 0.2f;
        public int reputationGainGood = 3;   // patience > 75%
        public int reputationGainOkay = 2;   // patience > 50%
        public int reputationGainBad = 1;    // patience > 0
        public int reputationLossTimeout = 2;

        [Header("Visual")]
        public float customerBobSpeed = 2f;
        public float customerBobAmount = 0.05f;
        public float itemBobSpeed = 3f;
        public float itemBobAmount = 0.03f;
        public float floatingTextSpeed = 1f;
        public float floatingTextDuration = 1.5f;
    }
}
