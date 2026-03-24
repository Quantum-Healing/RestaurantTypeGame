using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// Top-level game orchestrator. Manages game phase, day cycle, and
    /// coordinates all subsystems.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Config")]
        public GameConfig config;

        [Header("References")]
        public GridManager gridManager;
        public MachineManager machineManager;
        public CustomerManager customerManager;
        public PowerManager powerManager;
        public ProgressionManager progressionManager;
        public SaveManager saveManager;
        public UIManager uiManager;
        public InputHandler inputHandler;
        public IsometricCamera isoCamera;

        // State
        public GamePhase Phase { get; private set; } = GamePhase.Planning;
        public int Money { get; private set; }
        public int Day { get; private set; } = 1;
        public int Reputation { get; private set; }
        public ToolMode CurrentTool { get; private set; } = ToolMode.Select;
        public MachineDefinition SelectedShopItem { get; set; }
        public IngredientType HeldItem { get; set; } = IngredientType.None;

        // Day timer
        public float DayTimer { get; private set; }
        public float DayProgress => Mathf.Clamp01(DayTimer / config.dayLengthSeconds);
        public int GameSpeed { get; private set; } = 1;

        // Day stats
        private DaySummaryData _dayStats;

        private float _customerSpawnTimer;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (config == null)
            {
                Debug.LogError("GameManager: No GameConfig assigned!");
                return;
            }

            Money = config.startingMoney;

            if (gridManager != null)
                gridManager.InitializeGrid(config.initialGridWidth, config.initialGridHeight);

            if (machineManager != null && machineManager.allMachineDefinitions != null
                && machineManager.allMachineDefinitions.Length > 0)
                machineManager.PlaceStartingLayout();

            if (powerManager != null)
                powerManager.RecalculatePower();

            if (isoCamera != null && gridManager != null)
                isoCamera.CenterOnGrid(gridManager);

            GameEvents.FireMoneyChanged(Money);
        }

        void Update()
        {
            if (Phase == GamePhase.DayActive)
            {
                float dt = Time.deltaTime * GameSpeed;
                DayTimer += dt;

                // Spawn customers
                _customerSpawnTimer += dt;
                float spawnRate = GetCustomerSpawnInterval();
                if (_customerSpawnTimer >= spawnRate)
                {
                    _customerSpawnTimer = 0f;
                    if (customerManager.ActiveCustomerCount < GetMaxCustomers())
                    {
                        customerManager.SpawnCustomer();
                    }
                }

                // Update customers
                customerManager.UpdateCustomers(dt);

                // Update machines (processing, conveyors)
                machineManager.UpdateMachines(dt);

                // Auto-serve check
                machineManager.TryAutoServe(customerManager);

                // Update day timer UI
                int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(config.dayLengthSeconds - DayTimer));
                GameEvents.FireDayTimerUpdated(secondsLeft);

                // Check day end
                if (DayTimer >= config.dayLengthSeconds)
                {
                    EndDay();
                }
            }
        }

        // ===== DAY LIFECYCLE =====

        public void StartDay()
        {
            if (Phase != GamePhase.Planning) return;

            Phase = GamePhase.DayActive;
            DayTimer = 0f;
            _customerSpawnTimer = 0f;
            _dayStats = new DaySummaryData { day = Day };

            // Restock fridges if unlocked
            if (progressionManager.HasUpgrade("auto_restock"))
            {
                machineManager.RestockAllFridges();
            }

            CurrentTool = ToolMode.Interact;
            GameEvents.FireToolChanged(CurrentTool);
            GameEvents.FireDayStarted();
        }

        private void EndDay()
        {
            Phase = GamePhase.DaySummary;

            // Dismiss remaining customers
            customerManager.DismissAll();

            _dayStats.totalEarned = _dayStats.revenue + _dayStats.tips;
            _dayStats.reputation = Reputation;

            GameEvents.FireDayEnded(_dayStats);
        }

        public void AdvanceToNextDay()
        {
            Day++;
            Phase = GamePhase.Planning;
            CurrentTool = ToolMode.Select;
            GameEvents.FireToolChanged(CurrentTool);

            // Auto-save
            saveManager.SaveGame();
        }

        // ===== ECONOMY =====

        public bool SpendMoney(int amount)
        {
            if (Money < amount) return false;
            Money -= amount;
            GameEvents.FireMoneyChanged(Money);
            return true;
        }

        public void AddMoney(int amount)
        {
            Money += amount;
            GameEvents.FireMoneyChanged(Money);
        }

        public void AddRevenue(int price, float patiencePercent)
        {
            int tip = 0;
            if (patiencePercent > config.tipBonusThreshold)
            {
                tip = Mathf.FloorToInt(price * config.baseTipPercent * progressionManager.TipMultiplier);
            }

            _dayStats.revenue += price;
            _dayStats.tips += tip;
            _dayStats.customersServed++;

            AddMoney(price + tip);
            GameEvents.FireRevenueEarned(price);
            if (tip > 0) GameEvents.FireTipEarned(tip);

            // Reputation gain
            int repGain = patiencePercent > 0.75f ? config.reputationGainGood :
                          patiencePercent > 0.5f ? config.reputationGainOkay :
                          config.reputationGainBad;
            AddReputation(repGain);
        }

        public void OnCustomerTimeout()
        {
            _dayStats.customersFailed++;
            AddReputation(-config.reputationLossTimeout);
        }

        public void AddReputation(int amount)
        {
            Reputation = Mathf.Max(0, Reputation + amount);
            GameEvents.FireReputationChanged(Reputation);
        }

        // ===== TOOLS =====

        public void SetTool(ToolMode tool)
        {
            CurrentTool = tool;
            SelectedShopItem = null;
            GameEvents.FireToolChanged(tool);
        }

        public void SetGameSpeed(int speed)
        {
            GameSpeed = Mathf.Clamp(speed, 0, 3);
        }

        // ===== DIFFICULTY SCALING =====

        public float GetCustomerSpawnInterval()
        {
            float interval = config.baseCustomerSpawnInterval - (Day - 1) * config.spawnIntervalReductionPerDay;
            return Mathf.Max(config.minCustomerSpawnInterval, interval);
        }

        public int GetMaxCustomers()
        {
            int max = config.baseMaxCustomers + Mathf.FloorToInt((Day - 1) * config.customersPerDayIncrease);
            return Mathf.Min(config.maxCustomersCap, max);
        }

        public float GetCustomerPatience()
        {
            float patience = config.basePatienceSeconds - (Day - 1) * config.patienceReductionPerDay;
            patience *= progressionManager.PatienceMultiplier;
            return Mathf.Max(config.minPatienceSeconds, patience);
        }

        // ===== SAVE/LOAD =====

        public GameSaveData GetSaveData()
        {
            return new GameSaveData
            {
                money = Money,
                day = Day,
                reputation = Reputation,
                gridWidth = gridManager.GridWidth,
                gridHeight = gridManager.GridHeight,
                machines = machineManager.GetSaveData(),
                wires = powerManager.GetSaveData(),
                upgrades = progressionManager.GetSaveData()
            };
        }

        public void LoadSaveData(GameSaveData data)
        {
            Money = data.money;
            Day = data.day;
            Reputation = data.reputation;
            gridManager.InitializeGrid(data.gridWidth, data.gridHeight);
            machineManager.LoadSaveData(data.machines);
            powerManager.LoadSaveData(data.wires);
            progressionManager.LoadSaveData(data.upgrades);
            powerManager.RecalculatePower();
            GameEvents.FireMoneyChanged(Money);
            GameEvents.FireReputationChanged(Reputation);
        }

        public void NewGame()
        {
            Money = config.startingMoney;
            Day = 1;
            Reputation = 0;
            Phase = GamePhase.Planning;
            gridManager.InitializeGrid(config.initialGridWidth, config.initialGridHeight);
            machineManager.ClearAll();
            machineManager.PlaceStartingLayout();
            powerManager.ClearAllWires();
            powerManager.RecalculatePower();
            progressionManager.Reset();
            GameEvents.FireMoneyChanged(Money);
            GameEvents.FireReputationChanged(Reputation);
        }
    }
}
