using UnityEngine;
using System.IO;

namespace KitchenEmpire
{
    /// <summary>
    /// Handles saving and loading game state to persistent storage.
    /// Uses JSON serialization to a file in Application.persistentDataPath.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private const string SAVE_FILE = "kitchen_empire_save.json";

        private string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE);

        public bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public void SaveGame()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            var data = gm.GetSaveData();
            string json = JsonUtility.ToJson(data, true);

            try
            {
                File.WriteAllText(SavePath, json);
                Debug.Log($"Game saved to {SavePath}");
                GameEvents.FireToast("Game Saved!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save: {e.Message}");
            }
        }

        public bool LoadGame()
        {
            if (!HasSave()) return false;

            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<GameSaveData>(json);

                var gm = GameManager.Instance;
                if (gm != null)
                {
                    gm.LoadSaveData(data);
                    Debug.Log("Game loaded successfully");
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load: {e.Message}");
            }

            return false;
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
        }
    }

    [System.Serializable]
    public class GameSaveData
    {
        public int money;
        public int day;
        public int reputation;
        public int gridWidth;
        public int gridHeight;
        public MachineSaveData[] machines;
        public WireSaveData[] wires;
        public string[] upgrades;
    }
}
