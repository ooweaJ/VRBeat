using System;
using System.IO;
using UnityEngine;

[Serializable]
public class GameSettings
{
    public float noteSpeed = 10f;
    public bool leftHandedMode = false;
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public float userOffset = 0f;
    public bool reducedMotion = false;

    static string FilePath => Path.Combine(Application.persistentDataPath, "settings.json");

    public static GameSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonUtility.FromJson<GameSettings>(File.ReadAllText(FilePath));
        }
        catch { }
        return new GameSettings();
    }

    public void Save()
    {
        File.WriteAllText(FilePath, JsonUtility.ToJson(this, true));
    }
}
