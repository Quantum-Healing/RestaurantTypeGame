using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using KitchenEmpire;

/// <summary>
/// Creates a ready-to-play scene with the Bootstrap object.
/// Run via menu: KitchenEmpire > Create Game Scene
/// </summary>
public class SceneSetup
{
    [MenuItem("KitchenEmpire/Create Game Scene")]
    public static void CreateGameScene()
    {
        // Create a new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create Bootstrap GameObject
        var bootstrapObj = new GameObject("Bootstrap");
        var bootstrap = bootstrapObj.AddComponent<GameBootstrap>();

        // Try to load ScriptableObjects if they exist
        bootstrap.gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");

        // Load machine definitions
        var machineGuids = AssetDatabase.FindAssets("t:MachineDefinition", new[] { "Assets/ScriptableObjects/Machines" });
        var machines = new MachineDefinition[machineGuids.Length];
        for (int i = 0; i < machineGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(machineGuids[i]);
            machines[i] = AssetDatabase.LoadAssetAtPath<MachineDefinition>(path);
        }
        bootstrap.machineDefinitions = machines;

        // Load recipe database
        bootstrap.recipeDatabase = AssetDatabase.LoadAssetAtPath<RecipeDatabase>("Assets/ScriptableObjects/Recipes/RecipeDatabase.asset");

        // Load upgrade definitions
        var upgradeGuids = AssetDatabase.FindAssets("t:UpgradeDefinition", new[] { "Assets/ScriptableObjects/Upgrades" });
        var upgrades = new UpgradeDefinition[upgradeGuids.Length];
        for (int i = 0; i < upgradeGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(upgradeGuids[i]);
            upgrades[i] = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path);
        }
        bootstrap.upgradeDefinitions = upgrades;

        // Save scene
        string scenePath = "Assets/Scenes/MainScene.unity";
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
        EditorSceneManager.SaveScene(scene, scenePath);

        // Add to build settings
        var buildScenes = EditorBuildSettings.scenes;
        bool alreadyInBuild = false;
        foreach (var s in buildScenes)
        {
            if (s.path == scenePath) { alreadyInBuild = true; break; }
        }
        if (!alreadyInBuild)
        {
            var newScenes = new EditorBuildSettingsScene[buildScenes.Length + 1];
            buildScenes.CopyTo(newScenes, 0);
            newScenes[buildScenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }

        Debug.Log("Kitchen Empire: Game scene created and saved! Press Play to start.");
    }

    [MenuItem("KitchenEmpire/Full Setup (Data + Scene)")]
    public static void FullSetup()
    {
        // First create all data
        SetupWizard.SetupAll();

        // Then create the scene
        CreateGameScene();

        Debug.Log("Kitchen Empire: Full setup complete! Press Play.");
    }
}
