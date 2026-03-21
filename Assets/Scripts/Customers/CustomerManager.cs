using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace KitchenEmpire
{
    /// <summary>
    /// Manages customer spawning, AI, and serving logic.
    /// Customers spawn at the door, walk to tables, order food,
    /// wait with patience, eat, and leave.
    /// </summary>
    public class CustomerManager : MonoBehaviour
    {
        [Header("References")]
        public MachineManager machineManager;
        public GridManager gridManager;
        public RecipeDatabase recipeDatabase;

        [Header("Prefab")]
        public GameObject customerPrefab;

        private List<CustomerInstance> _customers = new();
        private int _nextCustomerId;

        public int ActiveCustomerCount => _customers.Count(c =>
            c.State != CustomerState.Leaving && c.State != CustomerState.LeavingAngry);

        public IReadOnlyList<CustomerInstance> AllCustomers => _customers;

        public void SpawnCustomer()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // Get available recipes for current day
            var recipes = recipeDatabase.GetAvailableRecipes(gm.Day);
            if (recipes.Count == 0) return;

            // Pick random recipe
            var recipe = recipes[Random.Range(0, recipes.Count)];

            // Find door position
            Vector2Int? doorPos = machineManager.GetDoorPosition();
            if (!doorPos.HasValue) return;

            Vector3 spawnWorld = gridManager.GridToWorld(doorPos.Value);

            // Create customer
            GameObject go;
            if (customerPrefab != null)
            {
                go = Instantiate(customerPrefab, spawnWorld, Quaternion.identity, transform);
            }
            else
            {
                go = CreateDefaultCustomerVisual(spawnWorld);
            }

            var customer = go.GetComponent<CustomerInstance>();
            if (customer == null) customer = go.AddComponent<CustomerInstance>();

            float patience = gm.GetCustomerPatience();
            customer.Initialize(_nextCustomerId++, recipe, patience, spawnWorld);

            // Find available table
            Vector2Int? tablePos = FindAvailableTable();
            if (tablePos.HasValue)
            {
                Vector3 tableWorld = gridManager.GridToWorld(tablePos.Value);
                // Offset slightly so customers don't overlap
                var tableMachine = machineManager.GetMachine(tablePos.Value);
                int occupantIndex = tableMachine?.currentOccupants ?? 0;
                tableWorld += new Vector3(occupantIndex * 0.15f - 0.075f, 0, -0.15f);

                customer.AssignTable(tablePos.Value, tableWorld);

                if (tableMachine != null) tableMachine.currentOccupants++;
            }

            _customers.Add(customer);
            GameEvents.FireCustomerSpawned(customer.CustomerId);
        }

        public void UpdateCustomers(float dt)
        {
            for (int i = _customers.Count - 1; i >= 0; i--)
            {
                var customer = _customers[i];
                customer.UpdateCustomer(dt);

                // Handle timeout
                if (customer.State == CustomerState.LeavingAngry && !customer.IsSatisfied)
                {
                    GameManager.Instance?.OnCustomerTimeout();
                    // Mark as handled so we don't double-count
                    // (State change already happened in CustomerInstance)
                }

                // Handle leaving after eating - generate dirty plate
                if (customer.State == CustomerState.Leaving && customer.IsSatisfied)
                {
                    GenerateDirtyPlate(customer);
                    FreeTable(customer);
                }

                // Remove when done
                if (customer.IsReadyToRemove())
                {
                    if (!customer.IsSatisfied)
                    {
                        FreeTable(customer);
                    }
                    _customers.RemoveAt(i);
                    Destroy(customer.gameObject);
                }
            }
        }

        /// <summary>
        /// Try to serve a specific ingredient to any waiting customer.
        /// Returns the served customer, or null.
        /// </summary>
        public CustomerInstance TryServeItem(IngredientType item)
        {
            foreach (var customer in _customers)
            {
                if (customer.State != CustomerState.WaitingForFood) continue;

                if (DoesItemSatisfyOrder(item, customer.Order))
                {
                    float patience = customer.PatiencePercent;
                    customer.Serve();
                    GameManager.Instance?.AddRevenue(customer.Order.price, patience);
                    GameEvents.FireCustomerLeft(customer.CustomerId, true);
                    return customer;
                }
            }
            return null;
        }

        private bool DoesItemSatisfyOrder(IngredientType item, RecipeDefinition recipe)
        {
            // Single-ingredient recipes: direct match
            if (recipe.requiredIngredients.Length == 1 && recipe.requiredIngredients[0] == item)
                return true;

            // Multi-ingredient: match by the recipe's output item type
            if (recipe.outputItem == item)
                return true;

            return false;
        }

        private Vector2Int? FindAvailableTable()
        {
            foreach (var kvp in machineManager.AllMachines)
            {
                if (kvp.Value.definition.machineType == MachineType.Table)
                {
                    int seats = kvp.Value.definition.seatCount;
                    if (seats <= 0) seats = 2;
                    if (kvp.Value.currentOccupants < seats)
                    {
                        return kvp.Key;
                    }
                }
            }
            return null;
        }

        private void FreeTable(CustomerInstance customer)
        {
            if (customer.AssignedTable.HasValue)
            {
                var table = machineManager.GetMachine(customer.AssignedTable.Value);
                if (table != null)
                {
                    table.currentOccupants = Mathf.Max(0, table.currentOccupants - 1);
                }
            }
        }

        private void GenerateDirtyPlate(CustomerInstance customer)
        {
            // Find nearest empty counter or serving counter and place a dirty plate
            Vector2Int searchFrom = customer.AssignedTable ?? Vector2Int.zero;
            var counter = machineManager.FindNearestEmpty(searchFrom, MachineType.Counter)
                       ?? machineManager.FindNearestEmpty(searchFrom, MachineType.ServingCounter);

            if (counter != null)
            {
                counter.TryPlaceItem(IngredientType.DirtyPlate);
            }
        }

        public void DismissAll()
        {
            foreach (var customer in _customers)
            {
                FreeTable(customer);
                if (customer != null) Destroy(customer.gameObject);
            }
            _customers.Clear();
        }

        private GameObject CreateDefaultCustomerVisual(Vector3 position)
        {
            GameObject root = new GameObject("Customer");
            root.transform.position = position;

            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.parent = root.transform;
            body.transform.localPosition = new Vector3(0, 0.25f, 0);
            body.transform.localScale = new Vector3(0.15f, 0.2f, 0.15f);
            body.name = "Body";
            Destroy(body.GetComponent<Collider>());

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.parent = root.transform;
            head.transform.localPosition = new Vector3(0, 0.5f, 0);
            head.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            head.name = "Head";
            head.GetComponent<Renderer>().material.color = new Color(0.988f, 0.835f, 0.706f);
            Destroy(head.GetComponent<Collider>());

            return root;
        }
    }
}
