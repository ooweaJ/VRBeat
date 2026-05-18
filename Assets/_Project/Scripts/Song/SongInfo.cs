using System;

[Serializable]
public class DifficultyInfo
{
    public string name;
    public string chartFile;
    public int level;
}

[Serializable]
public class SongInfo
{
    public string songId;
    public string title;
    public string artist;
    public string mapper;
    public float bpm;
    public string audioFile;
    public string coverFile;
    public float previewStart;
    public float previewDuration;
    public float songOffset;
    public DifficultyInfo[] difficulties;
}
