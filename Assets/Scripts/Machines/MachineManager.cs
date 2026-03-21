using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace KitchenEmpire
{
    /// <summary>
    /// Manages all placed machines: placement, removal, lookup, and per-frame updates.
    /// </summary>
    public class MachineManager : MonoBehaviour
    {
        [Header("Machine Database")]
        public MachineDefinition[] allMachineDefinitions;

        [Header("Fallback Prefab")]
        public GameObject defaultMachinePrefab;

        private Dictionary<Vector2Int, MachineInstance> _machines = new();
        private GridManager _grid;
        private GameManager _gm;

        void Awake()
        {
            _grid = GetComponentInParent<GridManager>() ?? FindFirstObjectByType<GridManager>();
            _gm = GameManager.Instance;
        }

        void Start()
        {
            if (_gm == null) _gm = GameManager.Instance;
        }

        public MachineDefinition GetDefinition(MachineType type)
        {
            foreach (var def in allMachineDefinitions)
            {
                if (def.machineType == type) return def;
            }
            return null;
        }

        public MachineInstance GetMachine(Vector2Int pos)
        {
            _machines.TryGetValue(pos, out var machine);
            return machine;
        }

        public bool HasMachine(Vector2Int pos)
        {
            return _machines.ContainsKey(pos);
        }

        public IReadOnlyDictionary<Vector2Int, MachineInstance> AllMachines => _machines;

        // ===== PLACEMENT =====

        public bool CanPlace(Vector2Int pos, MachineDefinition def)
        {
            if (def == null) return false;
            if (!_grid.IsValidGridPos(pos)) return false;
            if (_machines.ContainsKey(pos)) return false;
            if (_gm != null && _gm.Money < def.cost) return false;
            if (def.unlockDay > (_gm?.Day ?? 1)) return false;
            return true;
        }

        public MachineInstance PlaceMachine(Vector2Int pos, MachineDefinition def, bool free = false)
        {
            if (!free && !CanPlace(pos, def)) return null;
            if (!free && _gm != null && !_gm.SpendMoney(def.cost)) return null;

            // Create game object
            Vector3 worldPos = _grid.GridToWorld(pos);
            GameObject prefab = def.prefab != null ? def.prefab : defaultMachinePrefab;
            GameObject go;

            if (prefab != null)
            {
                go = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                go = CreateDefaultMachineVisual(worldPos, def);
            }

            go.name = $"Machine_{def.machineType}_{pos.x}_{pos.y}";

            MachineInstance instance = go.GetComponent<MachineInstance>();
            if (instance == null) instance = go.AddComponent<MachineInstance>();
            instance.Initialize(def, pos);

            _machines[pos] = instance;

            GameEvents.FireMachinePlaced(pos, def.machineType);
            return instance;
        }

        public bool RemoveMachine(Vector2Int pos)
        {
            if (!_machines.TryGetValue(pos, out var machine)) return false;
            if (machine.definition.isEntrance) return false; // Can't remove door

            // Refund
            if (_gm != null)
            {
                int refund = Mathf.FloorToInt(machine.definition.cost * _gm.config.demolishRefundPercent);
                _gm.AddMoney(refund);
            }

            _machines.Remove(pos);
            Destroy(machine.gameObject);

            GameEvents.FireMachineRemoved(pos);
            return true;
        }

        // ===== STARTING LAYOUT =====

        public void PlaceStartingLayout()
        {
            if (_grid == null) _grid = FindFirstObjectByType<GridManager>();
            int w = _grid.GridWidth;
            int h = _grid.GridHeight;

            // Door at front-right
            PlaceMachine(new Vector2Int(w - 1, h - 1), GetDefinition(MachineType.Door), true);

            // Serving counter near door
            PlaceMachine(new Vector2Int(w - 2, h - 1), GetDefinition(MachineType.ServingCounter), true);

            // Table for customers
            PlaceMachine(new Vector2Int(w - 1, h - 2), GetDefinition(MachineType.Table), true);

            // Stove
            PlaceMachine(new Vector2Int(1, 1), GetDefinition(MachineType.Stove), true);

            // Prep table
            PlaceMachine(new Vector2Int(2, 1), GetDefinition(MachineType.PrepTable), true);

            // Sink
            PlaceMachine(new Vector2Int(1, 2), GetDefinition(MachineType.Sink), true);

            // Counter for staging
            PlaceMachine(new Vector2Int(3, 1), GetDefinition(MachineType.Counter), true);
        }

        // ===== MACHINE UPDATES (during day) =====

        public void UpdateMachines(float dt)
        {
            float cookMult = _gm?.progressionManager?.CookSpeedMultiplier ?? 1f;

            foreach (var kvp in _machines)
            {
                var machine = kvp.Value;

                // Update processing
                machine.UpdateProcessing(dt, cookMult);

                // Conveyor logic
                if (machine.definition.machineType == MachineType.Conveyor &&
                    machine.heldItem != IngredientType.None && !machine.isProcessing)
                {
                    TryConveyorMove(machine);
                }
            }
        }

        private void TryConveyorMove(MachineInstance conveyor)
        {
            Vector2Int dir = GetDirectionOffset(conveyor.facingDirection);
            Vector2Int targetPos = conveyor.gridPosition + dir;

            if (_machines.TryGetValue(targetPos, out var target))
            {
                if (target.heldItem == IngredientType.None && !target.definition.isEntrance)
                {
                    IngredientType item = conveyor.PickUpItem();
                    if (item != IngredientType.None)
                    {
                        target.TryPlaceItem(item);
                    }
                }
            }
        }

        private Vector2Int GetDirectionOffset(Direction dir)
        {
            return dir switch
            {
                Direction.Right => new Vector2Int(1, 0),
                Direction.Down => new Vector2Int(0, 1),
                Direction.Left => new Vector2Int(-1, 0),
                Direction.Up => new Vector2Int(0, -1),
                _ => Vector2Int.zero
            };
        }

        // ===== AUTO-SERVE =====

        /// <summary>
        /// Check all serving counters - if they hold an item that matches
        /// a waiting customer's order, serve automatically.
        /// </summary>
        public void TryAutoServe(CustomerManager customerMgr)
        {
            foreach (var kvp in _machines)
            {
                var machine = kvp.Value;
                if (machine.definition.machineType != MachineType.ServingCounter) continue;
                if (machine.heldItem == IngredientType.None) continue;

                var served = customerMgr.TryServeItem(machine.heldItem);
                if (served != null)
                {
                    machine.heldItem = IngredientType.None;
                    // Visual feedback
                    GameEvents.FireCustomerServed(served.CustomerId);
                }
            }
        }

        // ===== FRIDGE RESTOCKING =====

        public void RestockAllFridges()
        {
            foreach (var machine in _machines.Values)
            {
                if (machine.definition.machineType == MachineType.Fridge)
                {
                    machine.RestockStorage();
                }
            }
        }

        // ===== CLEAR =====

        public void ClearAll()
        {
            foreach (var machine in _machines.Values)
            {
                if (machine != null) Destroy(machine.gameObject);
            }
            _machines.Clear();
        }

        // ===== SAVE/LOAD =====

        public MachineSaveData[] GetSaveData()
        {
            return _machines.Values.Select(m => m.ToSaveData()).ToArray();
        }

        public void LoadSaveData(MachineSaveData[] data)
        {
            ClearAll();
            if (data == null) return;

            foreach (var msd in data)
            {
                var def = GetDefinition(msd.machineType);
                if (def == null) continue;

                var instance = PlaceMachine(new Vector2Int(msd.gridX, msd.gridY), def, true);
                if (instance != null)
                {
                    instance.heldItem = msd.heldItem;
                    instance.facingDirection = msd.direction;
                    if (msd.storedItems != null) instance.storedItems = msd.storedItems;
                }
            }
        }

        // ===== UTILITY =====

        /// <summary>
        /// Find the nearest machine of a given type that has no held item.
        /// </summary>
        public MachineInstance FindNearestEmpty(Vector2Int from, MachineType type)
        {
            MachineInstance nearest = null;
            float bestDist = float.MaxValue;

            foreach (var kvp in _machines)
            {
                if (kvp.Value.definition.machineType != type) continue;
                if (kvp.Value.heldItem != IngredientType.None) continue;

                float dist = Vector2Int.Distance(from, kvp.Key);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = kvp.Value;
                }
            }
            return nearest;
        }

        /// <summary>
        /// Find the door (entrance) position.
        /// </summary>
        public Vector2Int? GetDoorPosition()
        {
            foreach (var kvp in _machines)
            {
                if (kvp.Value.definition.isEntrance) return kvp.Key;
            }
            return null;
        }

        // ===== DEFAULT VISUAL =====

        private GameObject CreateDefaultMachineVisual(Vector3 position, MachineDefinition def)
        {
            // Create a simple isometric box for machines without custom prefabs
            GameObject root = new GameObject();
            root.transform.position = position;

            // Main body - a cube scaled to look isometric
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.parent = root.transform;
            body.transform.localPosition = new Vector3(0, 0.15f, 0);
            body.transform.localScale = new Vector3(0.4f, 0.3f, 0.4f);
            body.name = "Body";

            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = def.topColor;
            }

            // Accent on top
            GameObject accent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            accent.transform.parent = root.transform;
            accent.transform.localPosition = new Vector3(0, 0.31f, 0);
            accent.transform.localScale = new Vector3(0.2f, 0.02f, 0.2f);
            accent.name = "Accent";

            var accentRenderer = accent.GetComponent<Renderer>();
            if (accentRenderer != null)
            {
                accentRenderer.material.color = def.accentColor;
            }

            // Remove colliders from children, add one to root
            Destroy(body.GetComponent<Collider>());
            Destroy(accent.GetComponent<Collider>());

            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.15f, 0);
            col.size = new Vector3(0.45f, 0.35f, 0.45f);

            return root;
        }
    }
}
