using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace KitchenEmpire
{
    /// <summary>
    /// Manages the power grid: wires between machines, power generation,
    /// and distribution. Players must wire machines to generators or rely
    /// on base mains power.
    ///
    /// During chaos moments, players can toggle machines on/off and
    /// reroute power to manage overloads.
    /// </summary>
    public class PowerManager : MonoBehaviour
    {
        [Header("References")]
        public MachineManager machineManager;

        public int PowerUsed { get; private set; }
        public int PowerMax { get; private set; }

        private List<Wire> _wires = new();

        // Machines player has manually toggled off
        private HashSet<Vector2Int> _manuallyDisabled = new();

        public IReadOnlyList<Wire> AllWires => _wires;

        /// <summary>
        /// Add a wire between two machine positions.
        /// Wires are cosmetic/visual but also part of the power network.
        /// Max distance of 3 tiles.
        /// </summary>
        public bool AddWire(Vector2Int from, Vector2Int to)
        {
            if (from == to) return false;
            if (!machineManager.HasMachine(from) || !machineManager.HasMachine(to)) return false;

            // Check distance
            int dist = Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
            if (dist > 3) return false;

            // Check duplicate
            foreach (var w in _wires)
            {
                if ((w.from == from && w.to == to) || (w.from == to && w.to == from))
                    return false;
            }

            _wires.Add(new Wire { from = from, to = to });
            RecalculatePower();
            return true;
        }

        public bool RemoveWire(Vector2Int from, Vector2Int to)
        {
            int idx = _wires.FindIndex(w =>
                (w.from == from && w.to == to) || (w.from == to && w.to == from));
            if (idx < 0) return false;

            _wires.RemoveAt(idx);
            RecalculatePower();
            return true;
        }

        public void RemoveWiresAt(Vector2Int pos)
        {
            _wires.RemoveAll(w => w.from == pos || w.to == pos);
            RecalculatePower();
        }

        /// <summary>
        /// Toggle a machine on/off manually (player power management during day).
        /// </summary>
        public void ToggleMachine(Vector2Int pos)
        {
            if (_manuallyDisabled.Contains(pos))
                _manuallyDisabled.Remove(pos);
            else
                _manuallyDisabled.Add(pos);

            RecalculatePower();
        }

        /// <summary>
        /// Recalculate power distribution across all machines.
        ///
        /// Power budget = base mains power + all generator outputs.
        /// Machines are powered in order of lowest power cost first.
        /// Manually disabled machines don't consume power.
        /// </summary>
        public void RecalculatePower()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // Calculate total available power
            int basePower = gm.config.basePowerCapacity;
            int bonusPower = gm.progressionManager?.BonusPower ?? 0;
            int generatedPower = 0;

            foreach (var kvp in machineManager.AllMachines)
            {
                if (kvp.Value.definition.GeneratesPower)
                {
                    generatedPower += kvp.Value.definition.PowerGenerated;
                }
            }

            PowerMax = basePower + bonusPower + generatedPower;

            // Reset all machines to unpowered
            foreach (var kvp in machineManager.AllMachines)
            {
                kvp.Value.isPowered = !kvp.Value.definition.RequiresPower;
            }

            // Distribute power to machines, prioritizing lowest cost
            var needsPower = machineManager.AllMachines
                .Where(kvp => kvp.Value.definition.RequiresPower && !_manuallyDisabled.Contains(kvp.Key))
                .OrderBy(kvp => kvp.Value.definition.powerUsage)
                .ToList();

            int used = 0;
            HashSet<Vector2Int> poweredPositions = new();

            foreach (var kvp in needsPower)
            {
                int cost = kvp.Value.definition.powerUsage;
                if (used + cost <= PowerMax)
                {
                    kvp.Value.isPowered = true;
                    used += cost;
                    poweredPositions.Add(kvp.Key);
                }
            }

            PowerUsed = used;

            // Update wire visual state
            foreach (var wire in _wires)
            {
                var fromMachine = machineManager.GetMachine(wire.from);
                var toMachine = machineManager.GetMachine(wire.to);
                wire.isPowered = fromMachine != null && toMachine != null &&
                    (fromMachine.isPowered || !fromMachine.definition.RequiresPower) &&
                    (toMachine.isPowered || !toMachine.definition.RequiresPower);
            }

            GameEvents.FirePowerChanged(PowerUsed, PowerMax);
        }

        public void ClearAllWires()
        {
            _wires.Clear();
            _manuallyDisabled.Clear();
        }

        // ===== SAVE/LOAD =====

        public WireSaveData[] GetSaveData()
        {
            return _wires.Select(w => new WireSaveData
            {
                fromX = w.from.x, fromY = w.from.y,
                toX = w.to.x, toY = w.to.y
            }).ToArray();
        }

        public void LoadSaveData(WireSaveData[] data)
        {
            ClearAllWires();
            if (data == null) return;

            foreach (var wsd in data)
            {
                _wires.Add(new Wire
                {
                    from = new Vector2Int(wsd.fromX, wsd.fromY),
                    to = new Vector2Int(wsd.toX, wsd.toY)
                });
            }
            RecalculatePower();
        }
    }

    [System.Serializable]
    public class Wire
    {
        public Vector2Int from;
        public Vector2Int to;
        public bool isPowered;
    }

    [System.Serializable]
    public struct WireSaveData
    {
        public int fromX, fromY, toX, toY;
    }
}
