using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// Runtime instance of a placed machine on the grid.
    /// Holds state like held items, processing progress, power status.
    /// </summary>
    public class MachineInstance : MonoBehaviour
    {
        [Header("Definition")]
        public MachineDefinition definition;

        [Header("Runtime State")]
        public Vector2Int gridPosition;
        public bool isPowered;
        public IngredientType heldItem = IngredientType.None;
        public bool isProcessing;
        public float processTimer;
        public float processProgress;
        public Direction facingDirection = Direction.Right;
        public bool isSelected;

        // Table-specific
        public int currentOccupants;

        // Fridge-specific
        public IngredientType[] storedItems;

        // Visual references
        private GameObject _itemVisual;
        private GameObject _progressBar;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _propBlock;

        void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            _renderers = GetComponentsInChildren<Renderer>();
        }

        public void Initialize(MachineDefinition def, Vector2Int pos)
        {
            definition = def;
            gridPosition = pos;
            isPowered = !def.RequiresPower;

            if (def.storageCapacity > 0)
            {
                storedItems = new IngredientType[def.storageCapacity];
            }

            ApplyColors();
        }

        /// <summary>
        /// Update processing logic each frame during active day.
        /// </summary>
        public void UpdateProcessing(float dt, float cookSpeedMult)
        {
            if (!isPowered && definition.RequiresPower) return;
            if (!isProcessing || heldItem == IngredientType.None) return;
            if (!definition.CanProcess) return;

            float effectiveTime = definition.processTime * cookSpeedMult;
            processTimer += dt;
            processProgress = Mathf.Clamp01(processTimer / effectiveTime);

            if (processTimer >= effectiveTime)
            {
                CompleteProcessing();
            }
        }

        private void CompleteProcessing()
        {
            // Find the output for our current input
            foreach (var recipe in definition.processingRecipes)
            {
                if (recipe.input == heldItem)
                {
                    IngredientType output = recipe.output;
                    heldItem = output;
                    isProcessing = false;
                    processTimer = 0f;
                    processProgress = 0f;

                    GameEvents.FireMachineProcessingComplete(gridPosition, output);
                    UpdateItemVisual();
                    return;
                }
            }

            // No matching recipe - just stop processing
            isProcessing = false;
            processTimer = 0f;
            processProgress = 0f;
        }

        /// <summary>
        /// Try to place an item on this machine.
        /// Returns true if accepted.
        /// </summary>
        public bool TryPlaceItem(IngredientType item)
        {
            if (heldItem != IngredientType.None) return false;
            if (definition.isEntrance) return false;

            heldItem = item;
            GameEvents.FireItemPlacedOnMachine(gridPosition, item);

            // Check if this machine can process the item
            if (definition.CanProcess)
            {
                foreach (var recipe in definition.processingRecipes)
                {
                    if (recipe.input == item)
                    {
                        isProcessing = true;
                        processTimer = 0f;
                        processProgress = 0f;
                        break;
                    }
                }
            }

            UpdateItemVisual();
            return true;
        }

        /// <summary>
        /// Pick up the held item. Returns the item type, or None.
        /// </summary>
        public IngredientType PickUpItem()
        {
            if (heldItem == IngredientType.None) return IngredientType.None;
            if (isProcessing) return IngredientType.None; // Can't pick up while processing

            IngredientType item = heldItem;
            heldItem = IngredientType.None;
            isProcessing = false;
            processTimer = 0f;
            processProgress = 0f;

            GameEvents.FireItemPickedUp(gridPosition, item);
            UpdateItemVisual();
            return item;
        }

        /// <summary>
        /// For fridges: grab one stored ingredient.
        /// </summary>
        public IngredientType GrabFromStorage()
        {
            if (storedItems == null) return IngredientType.None;

            for (int i = storedItems.Length - 1; i >= 0; i--)
            {
                if (storedItems[i] != IngredientType.None)
                {
                    IngredientType item = storedItems[i];
                    storedItems[i] = IngredientType.None;
                    return item;
                }
            }
            return IngredientType.None;
        }

        /// <summary>
        /// For fridges: restock with random raw ingredients.
        /// </summary>
        public void RestockStorage()
        {
            if (storedItems == null) return;
            IngredientType[] rawTypes = {
                IngredientType.RawMeat, IngredientType.RawFish,
                IngredientType.RawVeggie, IngredientType.Flour
            };

            for (int i = 0; i < storedItems.Length; i++)
            {
                if (storedItems[i] == IngredientType.None)
                {
                    storedItems[i] = rawTypes[Random.Range(0, rawTypes.Length)];
                }
            }
        }

        public int GetStoredItemCount()
        {
            if (storedItems == null) return 0;
            int count = 0;
            foreach (var item in storedItems)
            {
                if (item != IngredientType.None) count++;
            }
            return count;
        }

        /// <summary>
        /// Rotate conveyor direction.
        /// </summary>
        public void Rotate()
        {
            if (!definition.canRotate) return;
            facingDirection = (Direction)(((int)facingDirection + 1) % 4);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            // Visual feedback
            if (_renderers != null)
            {
                foreach (var r in _renderers)
                {
                    r.GetPropertyBlock(_propBlock);
                    _propBlock.SetFloat("_Selected", selected ? 1f : 0f);
                    r.SetPropertyBlock(_propBlock);
                }
            }
        }

        private void ApplyColors()
        {
            // Apply the cartoony colors from definition to child renderers
            if (_renderers == null || definition == null) return;

            foreach (var r in _renderers)
            {
                r.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_TopColor", definition.topColor);
                _propBlock.SetColor("_FrontColor", definition.frontColor);
                _propBlock.SetColor("_SideColor", definition.sideColor);
                _propBlock.SetColor("_AccentColor", definition.accentColor);
                r.SetPropertyBlock(_propBlock);
            }
        }

        private void UpdateItemVisual()
        {
            // This would update a floating item sprite above the machine
            // Implementation depends on how items are visualized (sprite, 3D model, etc.)
        }

        // ===== SAVE DATA =====

        public MachineSaveData ToSaveData()
        {
            return new MachineSaveData
            {
                gridX = gridPosition.x,
                gridY = gridPosition.y,
                machineType = definition.machineType,
                heldItem = heldItem,
                direction = facingDirection,
                storedItems = storedItems != null ? (IngredientType[])storedItems.Clone() : null
            };
        }
    }

    [System.Serializable]
    public struct MachineSaveData
    {
        public int gridX;
        public int gridY;
        public MachineType machineType;
        public IngredientType heldItem;
        public Direction direction;
        public IngredientType[] storedItems;
    }
}
