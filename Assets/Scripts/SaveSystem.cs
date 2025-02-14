using UnityEngine;
using System.IO;

public static class SaveSystem
{
    private static string fileName = "gameData.json";

    public static void SaveGame(GameData data)
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(path, json);
            Debug.Log($"[SaveSystem] Data saved to {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveSystem] Failed to save data: " + e.Message);
        }
    }

    public static GameData LoadGame()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                GameData data = JsonUtility.FromJson<GameData>(json);
                Debug.Log($"[SaveSystem] Data loaded from {path}");
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[SaveSystem] Failed to load data: " + e.Message);
                // Return a new GameData if something goes wrong
                return new GameData();
            }
        }
        else
        {
            Debug.Log("[SaveSystem] No save file found, returning new GameData");
            return new GameData();
        }
    }
}
