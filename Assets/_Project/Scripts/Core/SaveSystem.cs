using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    [Serializable]
    public class SongRecord
    {
        public string songId;
        public string difficulty;
        public int highScore;
        public float accuracy;
        public bool fullCombo;
    }

    [Serializable]
    class SaveData
    {
        public List<SongRecord> records = new List<SongRecord>();
    }

    static string SavePath => Path.Combine(Application.persistentDataPath, "saves.json");

    public static void SaveRecord(SongRecord incoming)
    {
        var data = LoadAll();
        var existing = data.records.Find(r => r.songId == incoming.songId && r.difficulty == incoming.difficulty);
        if (existing == null)
        {
            data.records.Add(incoming);
        }
        else
        {
            if (incoming.highScore > existing.highScore) existing.highScore = incoming.highScore;
            if (incoming.accuracy > existing.accuracy) existing.accuracy = incoming.accuracy;
            if (incoming.fullCombo) existing.fullCombo = true;
        }
        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }

    public static SongRecord LoadRecord(string songId, string difficulty)
    {
        var data = LoadAll();
        return data.records.Find(r => r.songId == songId && r.difficulty == difficulty);
    }

    static SaveData LoadAll()
    {
        try
        {
            if (File.Exists(SavePath))
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        }
        catch { }
        return new SaveData();
    }
}
