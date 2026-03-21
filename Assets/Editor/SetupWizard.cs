using UnityEngine;
using UnityEditor;
using KitchenEmpire;

/// <summary>
/// Editor wizard to auto-generate all ScriptableObject assets for machines,
/// recipes, upgrades, and game config. Run via menu: KitchenEmpire > Setup All Data.
/// </summary>
public class SetupWizard : EditorWindow
{
    [MenuItem("KitchenEmpire/Setup All Data")]
    public static void SetupAll()
    {
        CreateGameConfig();
        CreateMachineDefinitions();
        CreateRecipes();
        CreateUpgrades();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Kitchen Empire: All ScriptableObject data created!");
    }

    static T CreateAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    static void CreateGameConfig()
    {
        var config = CreateAsset<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
        EditorUtility.SetDirty(config);
    }

    static void CreateMachineDefinitions()
    {
        string folder = "Assets/ScriptableObjects/Machines";
        EnsureFolder(folder);

        // Counter
        var counter = CreateAsset<MachineDefinition>($"{folder}/Counter.asset");
        counter.machineType = MachineType.Counter;
        counter.displayName = "Counter";
        counter.description = "Basic surface for placing food.";
        counter.cost = 15;
        counter.powerUsage = 0;
        counter.category = MachineCategory.Basic;
        counter.unlockDay = 1;
        counter.topColor = HexColor("#f5e6d0");
        counter.frontColor = HexColor("#d4c4a8");
        counter.sideColor = HexColor("#c9b898");
        counter.accentColor = HexColor("#fef3c7");
        EditorUtility.SetDirty(counter);

        // Stove
        var stove = CreateAsset<MachineDefinition>($"{folder}/Stove.asset");
        stove.machineType = MachineType.Stove;
        stove.displayName = "Stove";
        stove.description = "Cooks raw ingredients into meals.";
        stove.cost = 50;
        stove.powerUsage = 2;
        stove.processTime = 5f;
        stove.category = MachineCategory.Cooking;
        stove.unlockDay = 1;
        stove.topColor = HexColor("#ef4444");
        stove.frontColor = HexColor("#dc2626");
        stove.sideColor = HexColor("#b91c1c");
        stove.accentColor = HexColor("#fbbf24");
        stove.processingRecipes = new ProcessingRecipe[]
        {
            new() { input = IngredientType.RawMeat, output = IngredientType.CookedMeat },
            new() { input = IngredientType.RawFish, output = IngredientType.CookedFish },
            new() { input = IngredientType.RawVeggie, output = IngredientType.CookedVeggie },
        };
        EditorUtility.SetDirty(stove);

        // Prep Table
        var prep = CreateAsset<MachineDefinition>($"{folder}/PrepTable.asset");
        prep.machineType = MachineType.PrepTable;
        prep.displayName = "Prep Table";
        prep.description = "Chops and prepares ingredients.";
        prep.cost = 35;
        prep.powerUsage = 1;
        prep.processTime = 3f;
        prep.category = MachineCategory.Cooking;
        prep.unlockDay = 1;
        prep.topColor = HexColor("#a3e635");
        prep.frontColor = HexColor("#84cc16");
        prep.sideColor = HexColor("#65a30d");
        prep.accentColor = HexColor("#ecfccb");
        prep.processingRecipes = new ProcessingRecipe[]
        {
            new() { input = IngredientType.RawVeggie, output = IngredientType.ChoppedVeggie },
            new() { input = IngredientType.RawMeat, output = IngredientType.ChoppedMeat },
        };
        EditorUtility.SetDirty(prep);

        // Serving Counter
        var serving = CreateAsset<MachineDefinition>($"{folder}/ServingCounter.asset");
        serving.machineType = MachineType.ServingCounter;
        serving.displayName = "Serving Hatch";
        serving.description = "Where customers pick up orders.";
        serving.cost = 40;
        serving.powerUsage = 0;
        serving.category = MachineCategory.Service;
        serving.unlockDay = 1;
        serving.topColor = HexColor("#2dd4bf");
        serving.frontColor = HexColor("#14b8a6");
        serving.sideColor = HexColor("#0d9488");
        serving.accentColor = HexColor("#f0fdfa");
        EditorUtility.SetDirty(serving);

        // Table
        var table = CreateAsset<MachineDefinition>($"{folder}/Table.asset");
        table.machineType = MachineType.Table;
        table.displayName = "Table";
        table.description = "Customers sit and eat here.";
        table.cost = 25;
        table.powerUsage = 0;
        table.seatCount = 2;
        table.category = MachineCategory.Service;
        table.unlockDay = 1;
        table.topColor = HexColor("#d4a574");
        table.frontColor = HexColor("#b8956a");
        table.sideColor = HexColor("#a07850");
        table.accentColor = HexColor("#f5e6d0");
        EditorUtility.SetDirty(table);

        // Fridge
        var fridge = CreateAsset<MachineDefinition>($"{folder}/Fridge.asset");
        fridge.machineType = MachineType.Fridge;
        fridge.displayName = "Fridge";
        fridge.description = "Stores ingredients. Auto-restocks between days.";
        fridge.cost = 60;
        fridge.powerUsage = 1;
        fridge.storageCapacity = 6;
        fridge.category = MachineCategory.Storage;
        fridge.unlockDay = 2;
        fridge.topColor = HexColor("#93c5fd");
        fridge.frontColor = HexColor("#60a5fa");
        fridge.sideColor = HexColor("#3b82f6");
        fridge.accentColor = HexColor("#dbeafe");
        EditorUtility.SetDirty(fridge);

        // Oven
        var oven = CreateAsset<MachineDefinition>($"{folder}/Oven.asset");
        oven.machineType = MachineType.Oven;
        oven.displayName = "Oven";
        oven.description = "Bakes dishes - slower but handles complex recipes.";
        oven.cost = 80;
        oven.powerUsage = 3;
        oven.processTime = 8f;
        oven.category = MachineCategory.Cooking;
        oven.unlockDay = 3;
        oven.topColor = HexColor("#78716c");
        oven.frontColor = HexColor("#57534e");
        oven.sideColor = HexColor("#44403c");
        oven.accentColor = HexColor("#f97316");
        oven.processingRecipes = new ProcessingRecipe[]
        {
            new() { input = IngredientType.Dough, output = IngredientType.Bread },
            new() { input = IngredientType.ChoppedVeggie, output = IngredientType.RoastedVeggie },
        };
        EditorUtility.SetDirty(oven);

        // Fryer
        var fryer = CreateAsset<MachineDefinition>($"{folder}/Fryer.asset");
        fryer.machineType = MachineType.Fryer;
        fryer.displayName = "Fryer";
        fryer.description = "Deep fries food quickly.";
        fryer.cost = 70;
        fryer.powerUsage = 2;
        fryer.processTime = 3.5f;
        fryer.category = MachineCategory.Cooking;
        fryer.unlockDay = 4;
        fryer.topColor = HexColor("#eab308");
        fryer.frontColor = HexColor("#ca8a04");
        fryer.sideColor = HexColor("#a16207");
        fryer.accentColor = HexColor("#fef3c7");
        fryer.processingRecipes = new ProcessingRecipe[]
        {
            new() { input = IngredientType.ChoppedMeat, output = IngredientType.FriedMeat },
            new() { input = IngredientType.RawVeggie, output = IngredientType.Fries },
        };
        EditorUtility.SetDirty(fryer);

        // Dishwasher
        var dishwasher = CreateAsset<MachineDefinition>($"{folder}/Dishwasher.asset");
        dishwasher.machineType = MachineType.Dishwasher;
        dishwasher.displayName = "Dishwasher";
        dishwasher.description = "Auto-cleans dirty plates.";
        dishwasher.cost = 90;
        dishwasher.powerUsage = 2;
        dishwasher.processTime = 4f;
        dishwasher.category = MachineCategory.Utility;
        dishwasher.unlockDay = 3;
        dishwasher.topColor = HexColor("#c084fc");
        dishwasher.frontColor = HexColor("#a855f7");
        dishwasher.sideColor = HexColor("#9333ea");
        dishwasher.accentColor = HexColor("#f3e8ff");
        dishwasher.processingRecipes = new ProcessingRecipe[]
        {
            new() { input = IngredientType.DirtyPlate, output = IngredientType.CleanPlate },
        };
        EditorUtility.SetDirty(dishwasher);

        // Mixer
        var mixer = CreateAsset<MachineDefinition>($"{folder}/Mixer.asset");
        mixer.machineType = MachineType.Mixer;
        mixer.displayName = "Mixer";
        mixer.description = "Mixes ingredients together.";
        mixer.cost = 75;
        mixer.powerUsage = 2;
        mixer.processTime = 4f;
        mixer.category = MachineCategory.Cooking;
        mixer.unlockDay = 5;
        mixer.topColor = HexColor("#fb923c");
        mixer.frontColor = HexColor("#f97316");
        mixer.sideColor = HexColor("#ea580c");
        mixer.accentColor = HexColor("#fed7aa");
        mixer.processingRecipes = new ProcessingRecipe[]
        {
            new() { input = IngredientType.Flour, output = IngredientType.Dough },
            new() { input = IngredientType.ChoppedVeggie, output = IngredientType.Salad },
        };
        EditorUtility.SetDirty(mixer);

        // Sink
        var sink = CreateAsset<MachineDefinition>($"{folder}/Sink.asset");
        sink.machineType = MachineType.Sink;
        sink.displayName = "Sink";
        sink.description = "Manual dishwashing - free but slow.";
        sink.cost = 30;
        sink.powerUsage = 0;
        sink.processTime = 6f;
        sink.category = MachineCategory.Utility;
        sink.unlockDay = 1;
        sink.topColor = HexColor("#67e8f9");
        sink.frontColor = HexColor("#22d3ee");
        sink.sideColor = HexColor("#06b6d4");
        sink.accentColor = HexColor("#cffafe");
        sink.processingRecipes = new ProcessingRecipe[]
        {
            new() { input = IngredientType.DirtyPlate, output = IngredientType.CleanPlate },
        };
        EditorUtility.SetDirty(sink);

        // Conveyor
        var conveyor = CreateAsset<MachineDefinition>($"{folder}/Conveyor.asset");
        conveyor.machineType = MachineType.Conveyor;
        conveyor.displayName = "Conveyor";
        conveyor.description = "Moves items between machines automatically.";
        conveyor.cost = 45;
        conveyor.powerUsage = 1;
        conveyor.canRotate = true;
        conveyor.category = MachineCategory.Automation;
        conveyor.unlockDay = 4;
        conveyor.topColor = HexColor("#9ca3af");
        conveyor.frontColor = HexColor("#6b7280");
        conveyor.sideColor = HexColor("#4b5563");
        conveyor.accentColor = HexColor("#d1d5db");
        EditorUtility.SetDirty(conveyor);

        // Generator
        var generator = CreateAsset<MachineDefinition>($"{folder}/Generator.asset");
        generator.machineType = MachineType.Generator;
        generator.displayName = "Generator";
        generator.description = "Provides 5W of power to your kitchen.";
        generator.cost = 120;
        generator.powerUsage = -5; // Negative = generates
        generator.category = MachineCategory.Power;
        generator.unlockDay = 2;
        generator.topColor = HexColor("#fbbf24");
        generator.frontColor = HexColor("#f59e0b");
        generator.sideColor = HexColor("#d97706");
        generator.accentColor = HexColor("#dc2626");
        EditorUtility.SetDirty(generator);

        // Door (Entrance)
        var door = CreateAsset<MachineDefinition>($"{folder}/Door.asset");
        door.machineType = MachineType.Door;
        door.displayName = "Entrance";
        door.description = "Customer entrance - cannot be removed.";
        door.cost = 0;
        door.powerUsage = 0;
        door.isEntrance = true;
        door.category = MachineCategory.Special;
        door.unlockDay = 1;
        door.topColor = HexColor("#a78bfa");
        door.frontColor = HexColor("#8b5cf6");
        door.sideColor = HexColor("#7c3aed");
        door.accentColor = HexColor("#c4b5fd");
        EditorUtility.SetDirty(door);
    }

    static void CreateRecipes()
    {
        string folder = "Assets/ScriptableObjects/Recipes";
        EnsureFolder(folder);

        CreateRecipe(folder, "Soup", IngredientType.CookedVeggie, new[] { IngredientType.CookedVeggie }, 12, 1);
        CreateRecipe(folder, "Salad", IngredientType.Salad, new[] { IngredientType.Salad }, 10, 1);
        CreateRecipe(folder, "Steak", IngredientType.CookedMeat, new[] { IngredientType.CookedMeat }, 20, 2);
        CreateRecipe(folder, "FriedChicken", IngredientType.FriedMeat, new[] { IngredientType.FriedMeat }, 16, 4);
        CreateRecipe(folder, "FishAndChips", IngredientType.CookedFish, new[] { IngredientType.CookedFish, IngredientType.Fries }, 18, 4);

        // Create recipe database
        var db = CreateAsset<RecipeDatabase>($"{folder}/RecipeDatabase.asset");
        db.allRecipes = new RecipeDefinition[]
        {
            AssetDatabase.LoadAssetAtPath<RecipeDefinition>($"{folder}/Soup.asset"),
            AssetDatabase.LoadAssetAtPath<RecipeDefinition>($"{folder}/Salad.asset"),
            AssetDatabase.LoadAssetAtPath<RecipeDefinition>($"{folder}/Steak.asset"),
            AssetDatabase.LoadAssetAtPath<RecipeDefinition>($"{folder}/FriedChicken.asset"),
            AssetDatabase.LoadAssetAtPath<RecipeDefinition>($"{folder}/FishAndChips.asset"),
        };
        EditorUtility.SetDirty(db);
    }

    static void CreateRecipe(string folder, string name, IngredientType output, IngredientType[] ingredients, int price, int unlockDay)
    {
        var recipe = CreateAsset<RecipeDefinition>($"{folder}/{name}.asset");
        recipe.recipeName = name;
        recipe.outputItem = output;
        recipe.requiredIngredients = ingredients;
        recipe.price = price;
        recipe.unlockDay = unlockDay;
        EditorUtility.SetDirty(recipe);
    }

    static void CreateUpgrades()
    {
        string folder = "Assets/ScriptableObjects/Upgrades";
        EnsureFolder(folder);

        CreateUpgrade(folder, "bigger_kitchen_1", "Kitchen Extension I", "Expand kitchen to 8x8", 200, "",
            new UpgradeEffect[] {
                new() { type = UpgradeType.GridExpansionWidth, value = 8 },
                new() { type = UpgradeType.GridExpansionHeight, value = 8 }
            });

        CreateUpgrade(folder, "bigger_kitchen_2", "Kitchen Extension II", "Expand kitchen to 10x10", 500, "bigger_kitchen_1",
            new UpgradeEffect[] {
                new() { type = UpgradeType.GridExpansionWidth, value = 10 },
                new() { type = UpgradeType.GridExpansionHeight, value = 10 }
            });

        CreateUpgrade(folder, "bigger_kitchen_3", "Kitchen Extension III", "Expand kitchen to 12x12", 1200, "bigger_kitchen_2",
            new UpgradeEffect[] {
                new() { type = UpgradeType.GridExpansionWidth, value = 12 },
                new() { type = UpgradeType.GridExpansionHeight, value = 12 }
            });

        CreateUpgrade(folder, "faster_cooking", "Better Burners", "All cooking 20% faster", 150, "",
            new UpgradeEffect[] { new() { type = UpgradeType.CookSpeedMultiplier, value = 0.8f } });

        CreateUpgrade(folder, "faster_cooking_2", "Pro Burners", "All cooking 40% faster", 400, "faster_cooking",
            new UpgradeEffect[] { new() { type = UpgradeType.CookSpeedMultiplier, value = 0.6f } });

        CreateUpgrade(folder, "more_patience", "Comfy Seats", "Customers wait 25% longer", 100, "",
            new UpgradeEffect[] { new() { type = UpgradeType.PatienceMultiplier, value = 1.25f } });

        CreateUpgrade(folder, "more_patience_2", "Premium Seats", "Customers wait 50% longer", 300, "more_patience",
            new UpgradeEffect[] { new() { type = UpgradeType.PatienceMultiplier, value = 1.5f } });

        CreateUpgrade(folder, "power_upgrade", "Mains Power I", "+10W base power capacity", 200, "",
            new UpgradeEffect[] { new() { type = UpgradeType.BonusPower, value = 10 } });

        CreateUpgrade(folder, "power_upgrade_2", "Mains Power II", "+20W base power capacity", 500, "power_upgrade",
            new UpgradeEffect[] { new() { type = UpgradeType.BonusPower, value = 20 } });

        CreateUpgrade(folder, "auto_restock", "Auto Restock", "Fridges auto-fill between days", 250, "",
            new UpgradeEffect[] { new() { type = UpgradeType.AutoRestock, value = 1 } });

        CreateUpgrade(folder, "tip_boost", "Tip Jar", "+15% tips from happy customers", 100, "",
            new UpgradeEffect[] { new() { type = UpgradeType.TipMultiplier, value = 1.15f } });

        CreateUpgrade(folder, "tip_boost_2", "Premium Service", "+30% tips from happy customers", 350, "tip_boost",
            new UpgradeEffect[] { new() { type = UpgradeType.TipMultiplier, value = 1.30f } });
    }

    static void CreateUpgrade(string folder, string id, string name, string desc, int cost, string requires, UpgradeEffect[] effects)
    {
        var upgrade = CreateAsset<UpgradeDefinition>($"{folder}/{id}.asset");
        upgrade.upgradeId = id;
        upgrade.displayName = name;
        upgrade.description = desc;
        upgrade.cost = cost;
        upgrade.requiresUpgradeId = requires;
        upgrade.effects = effects;
        EditorUtility.SetDirty(upgrade);
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
