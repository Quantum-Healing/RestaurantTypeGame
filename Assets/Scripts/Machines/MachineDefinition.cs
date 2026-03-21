using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// ScriptableObject defining a machine type.
    /// Each machine (stove, fridge, conveyor, etc.) gets one of these.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMachine", menuName = "KitchenEmpire/Machine Definition")]
    public class MachineDefinition : ScriptableObject
    {
        [Header("Identity")]
        public MachineType machineType;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public MachineCategory category;

        [Header("Cost & Unlock")]
        public int cost;
        public int unlockDay = 1;

        [Header("Power")]
        [Tooltip("Positive = consumes power. Negative = generates power.")]
        public int powerUsage;

        [Header("Processing")]
        [Tooltip("Time in seconds to process an item. 0 = no processing.")]
        public float processTime;
        public ProcessingRecipe[] processingRecipes;

        [Header("Special Properties")]
        public int storageCapacity; // For fridges
        public int seatCount;       // For tables
        public bool isEntrance;     // For door
        public bool canRotate;      // For conveyors

        [Header("Visuals")]
        public GameObject prefab;
        public Color topColor = Color.white;
        public Color frontColor = Color.gray;
        public Color sideColor = Color.gray;
        public Color accentColor = Color.yellow;

        public bool RequiresPower => powerUsage > 0;
        public bool GeneratesPower => powerUsage < 0;
        public int PowerGenerated => Mathf.Abs(Mathf.Min(0, powerUsage));
        public bool CanProcess => processTime > 0 && processingRecipes != null && processingRecipes.Length > 0;
    }

    [System.Serializable]
    public struct ProcessingRecipe
    {
        public IngredientType input;
        public IngredientType output;
    }
}
