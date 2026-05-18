using System;

[Serializable]
public class NoteData
{
    public string type;       // "normal" | "long"
    public float beat;
    public float duration;    // long note only (beats)
    public int lane;          // 0-3 (left→right)
    public int row;           // 0-2 (bottom→top)
    public string direction;  // up/down/left/right/upLeft/upRight/downLeft/downRight/any
    public string color;      // "red" | "blue"

    public NoteType NoteType  => type == "long" ? NoteType.Long : NoteType.Normal;
    public SaberColor SaberColor => color == "blue" ? SaberColor.Blue : SaberColor.Red;
}
