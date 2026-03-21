using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// Handles all player input: clicking on tiles, keyboard shortcuts,
    /// tool interactions. Translates screen clicks to grid actions.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("References")]
        public IsometricCamera isoCamera;
        public GridManager gridManager;
        public MachineManager machineManager;
        public PowerManager powerManager;
        public UIManager uiManager;

        private Vector2Int? _hoveredTile;
        private Vector2Int? _wireStartPos;
        private MachineInstance _selectedMachine;

        void Update()
        {
            UpdateHoveredTile();
            HandleKeyboardShortcuts();
            HandleMouseClicks();
        }

        private void UpdateHoveredTile()
        {
            if (isoCamera.ScreenToGroundPoint(Input.mousePosition, out Vector3 worldPos))
            {
                Vector2Int gridPos = gridManager.WorldToGrid(worldPos);
                if (gridManager.IsValidGridPos(gridPos))
                {
                    _hoveredTile = gridPos;

                    // Show tooltip for machines
                    var machine = machineManager.GetMachine(gridPos);
                    if (machine != null)
                    {
                        string tip = $"{machine.definition.displayName}";
                        if (machine.heldItem != IngredientType.None)
                            tip += $"\nHolding: {machine.heldItem}";
                        if (machine.isProcessing)
                            tip += $"\nProcessing: {Mathf.RoundToInt(machine.processProgress * 100)}%";
                        uiManager.ShowTooltip(Input.mousePosition, tip);
                    }
                    else
                    {
                        uiManager.HideTooltip();
                    }
                }
                else
                {
                    _hoveredTile = null;
                    uiManager.HideTooltip();
                }
            }
        }

        private void HandleKeyboardShortcuts()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // Tool selection (only in planning phase)
            if (gm.Phase == GamePhase.Planning)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) gm.SetTool(ToolMode.Select);
                if (Input.GetKeyDown(KeyCode.Alpha2)) gm.SetTool(ToolMode.Move);
                if (Input.GetKeyDown(KeyCode.Alpha3)) gm.SetTool(ToolMode.Wire);
                if (Input.GetKeyDown(KeyCode.Alpha4)) gm.SetTool(ToolMode.Demolish);
            }

            // Shop / Upgrades
            if (Input.GetKeyDown(KeyCode.B)) uiManager.ToggleShop();
            if (Input.GetKeyDown(KeyCode.U)) uiManager.ToggleUpgrades();

            // Start day
            if (Input.GetKeyDown(KeyCode.Space) && gm.Phase == GamePhase.Planning)
            {
                gm.StartDay();
            }

            // Escape - cancel current action
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                gm.SelectedShopItem = null;
                gm.HeldItem = IngredientType.None;
                _wireStartPos = null;
                uiManager.CloseAllPanels();
                uiManager.UpdateHeldItem(IngredientType.None);
            }

            // Rotate conveyor
            if (Input.GetKeyDown(KeyCode.R) && _hoveredTile.HasValue)
            {
                var machine = machineManager.GetMachine(_hoveredTile.Value);
                if (machine != null && machine.definition.canRotate)
                {
                    machine.Rotate();
                }
            }

            // Game speed
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                gm.SetGameSpeed(Mathf.Max(0, gm.GameSpeed - 1));
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
                gm.SetGameSpeed(Mathf.Min(3, gm.GameSpeed + 1));
        }

        private void HandleMouseClicks()
        {
            if (!_hoveredTile.HasValue) return;
            if (UnityEngine.EventSystems.EventSystem.current?.IsPointerOverGameObject() == true) return;

            var gm = GameManager.Instance;
            if (gm == null) return;
            var pos = _hoveredTile.Value;

            // Left click
            if (Input.GetMouseButtonDown(0))
            {
                switch (gm.CurrentTool)
                {
                    case ToolMode.Select:
                        HandleSelect(pos);
                        break;
                    case ToolMode.Place:
                        HandlePlace(pos);
                        break;
                    case ToolMode.Demolish:
                        HandleDemolish(pos);
                        break;
                    case ToolMode.Wire:
                        HandleWire(pos);
                        break;
                    case ToolMode.Interact:
                        HandleInteract(pos);
                        break;
                }
            }

            // Right click - cancel / deselect
            if (Input.GetMouseButtonDown(1))
            {
                gm.SelectedShopItem = null;
                gm.HeldItem = IngredientType.None;
                _wireStartPos = null;
                ClearSelection();
                uiManager.UpdateHeldItem(IngredientType.None);
            }
        }

        private void HandleSelect(Vector2Int pos)
        {
            ClearSelection();

            var machine = machineManager.GetMachine(pos);
            if (machine != null)
            {
                _selectedMachine = machine;
                machine.SetSelected(true);
            }
        }

        private void HandlePlace(Vector2Int pos)
        {
            var gm = GameManager.Instance;
            if (gm.SelectedShopItem == null) return;

            var instance = machineManager.PlaceMachine(pos, gm.SelectedShopItem);
            if (instance != null)
            {
                GameEvents.FireToast($"Placed {gm.SelectedShopItem.displayName}");
                powerManager.RecalculatePower();

                // Keep placing if holding shift
                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                {
                    gm.SelectedShopItem = null;
                    gm.SetTool(ToolMode.Select);
                }
            }
        }

        private void HandleDemolish(Vector2Int pos)
        {
            if (machineManager.RemoveMachine(pos))
            {
                powerManager.RemoveWiresAt(pos);
                powerManager.RecalculatePower();
                GameEvents.FireToast("Demolished");
            }
        }

        private void HandleWire(Vector2Int pos)
        {
            if (!machineManager.HasMachine(pos)) return;

            if (_wireStartPos == null)
            {
                _wireStartPos = pos;
                GameEvents.FireToast("Click another machine to connect wire");
            }
            else
            {
                if (powerManager.AddWire(_wireStartPos.Value, pos))
                {
                    GameEvents.FireToast("Wire connected!");
                }
                else
                {
                    GameEvents.FireToast("Can't connect wire there");
                }
                _wireStartPos = null;
            }
        }

        private void HandleInteract(Vector2Int pos)
        {
            var gm = GameManager.Instance;
            var machine = machineManager.GetMachine(pos);
            if (machine == null) return;

            // If player is holding an item, try to place it
            if (gm.HeldItem != IngredientType.None)
            {
                if (machine.TryPlaceItem(gm.HeldItem))
                {
                    gm.HeldItem = IngredientType.None;
                    uiManager.UpdateHeldItem(IngredientType.None);
                }
                return;
            }

            // If machine has a finished item, pick it up
            if (machine.heldItem != IngredientType.None && !machine.isProcessing)
            {
                IngredientType item = machine.PickUpItem();
                if (item != IngredientType.None)
                {
                    gm.HeldItem = item;
                    uiManager.UpdateHeldItem(item);
                }
                return;
            }

            // If it's a fridge, grab from storage
            if (machine.definition.machineType == MachineType.Fridge)
            {
                IngredientType item = machine.GrabFromStorage();
                if (item != IngredientType.None)
                {
                    gm.HeldItem = item;
                    uiManager.UpdateHeldItem(item);
                }
                return;
            }

            // If it's a generator or machine with power, toggle it
            if (machine.definition.RequiresPower)
            {
                powerManager.ToggleMachine(pos);
                GameEvents.FireToast(machine.isPowered ? "Machine ON" : "Machine OFF");
            }
        }

        private void ClearSelection()
        {
            if (_selectedMachine != null)
            {
                _selectedMachine.SetSelected(false);
                _selectedMachine = null;
            }
        }
    }
}
