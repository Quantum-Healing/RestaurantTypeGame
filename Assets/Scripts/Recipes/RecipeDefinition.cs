using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// ScriptableObject defining a recipe (customer order).
    /// Each recipe has required ingredients, a price, and unlock day.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "KitchenEmpire/Recipe Definition")]
    public class RecipeDefinition : ScriptableObject
    {
        public string recipeName;
        public Sprite icon;
        [TextArea] public string description;

        [Header("Requirements")]
        public IngredientType[] requiredIngredients;
        public IngredientType outputItem; // The final combined item type

        [Header("Economy")]
        public int price;
        public int unlockDay = 1;
        public float prepTime = 3f; // Seconds to assemble at serving counter
    }
}
