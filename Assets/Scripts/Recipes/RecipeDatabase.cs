using UnityEngine;
using System.Collections.Generic;

namespace KitchenEmpire
{
    /// <summary>
    /// Holds all recipe definitions and provides lookup/filtering.
    /// </summary>
    [CreateAssetMenu(fileName = "RecipeDatabase", menuName = "KitchenEmpire/Recipe Database")]
    public class RecipeDatabase : ScriptableObject
    {
        public RecipeDefinition[] allRecipes;

        public List<RecipeDefinition> GetAvailableRecipes(int currentDay)
        {
            var result = new List<RecipeDefinition>();
            foreach (var recipe in allRecipes)
            {
                if (recipe.unlockDay <= currentDay)
                {
                    result.Add(recipe);
                }
            }
            return result;
        }

        public RecipeDefinition GetRecipeByOutput(IngredientType output)
        {
            foreach (var recipe in allRecipes)
            {
                if (recipe.outputItem == output) return recipe;
            }
            return null;
        }
    }
}
