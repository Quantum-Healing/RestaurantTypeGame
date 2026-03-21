using UnityEngine;
using System.Collections.Generic;

namespace KitchenEmpire
{
    /// <summary>
    /// Handles conveyor belt chain logic. Conveyors move items in their
    /// facing direction to the next machine. This enables players to
    /// build automated production lines:
    ///
    /// Fridge → Conveyor → Prep Table → Conveyor → Stove → Conveyor → Serving Counter
    ///
    /// The optimization challenge: build faster, more efficient chains
    /// using less power. This is the "mini Factorio" element.
    /// </summary>
    public class ConveyorSystem : MonoBehaviour
    {
        [Header("References")]
        public MachineManager machineManager;

        [Header("Settings")]
        public float moveInterval = 1f; // Seconds between conveyor moves

        private float _moveTimer;

        void Update()
        {
            if (GameManager.Instance?.Phase != GamePhase.DayActive) return;

            float dt = Time.deltaTime * (GameManager.Instance?.GameSpeed ?? 1);
            _moveTimer += dt;

            if (_moveTimer >= moveInterval)
            {
                _moveTimer = 0f;
                TickConveyors();
            }
        }

        /// <summary>
        /// Process all conveyors in a single tick.
        /// Must process in correct order to avoid items jumping multiple conveyors.
        /// </summary>
        private void TickConveyors()
        {
            // Collect all conveyors
            List<(Vector2Int pos, MachineInstance machine)> conveyors = new();

            foreach (var kvp in machineManager.AllMachines)
            {
                if (kvp.Value.definition.machineType == MachineType.Conveyor &&
                    kvp.Value.isPowered &&
                    kvp.Value.heldItem != IngredientType.None)
                {
                    conveyors.Add((kvp.Key, kvp.Value));
                }
            }

            // Sort by direction to process downstream first (prevents double-moves)
            // Process in reverse direction order
            conveyors.Sort((a, b) =>
            {
                var dirA = GetDirectionOffset(a.machine.facingDirection);
                var dirB = GetDirectionOffset(b.machine.facingDirection);
                // Sort by how "far along" in their direction they are
                float scoreA = a.pos.x * dirA.x + a.pos.y * dirA.y;
                float scoreB = b.pos.x * dirB.x + b.pos.y * dirB.y;
                return scoreB.CompareTo(scoreA); // Process furthest downstream first
            });

            // Move items
            HashSet<Vector2Int> movedThisTick = new();

            foreach (var (pos, conveyor) in conveyors)
            {
                if (movedThisTick.Contains(pos)) continue;

                Vector2Int dir = GetDirectionOffset(conveyor.facingDirection);
                Vector2Int targetPos = pos + dir;

                var target = machineManager.GetMachine(targetPos);
                if (target == null) continue;
                if (target.heldItem != IngredientType.None) continue;
                if (target.definition.isEntrance) continue;

                IngredientType item = conveyor.PickUpItem();
                if (item != IngredientType.None)
                {
                    target.TryPlaceItem(item);
                    movedThisTick.Add(targetPos);
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
    }
}
