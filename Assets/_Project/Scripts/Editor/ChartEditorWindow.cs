// This file is Editor-only. It must reside in an "Editor" folder.
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ChartEditorWindow : EditorWindow
{
    [MenuItem("VRBeat/Chart Editor")]
    static void Open() => GetWindow<ChartEditorWindow>("Chart Editor");

    // ── State ────────────────────────────────────────────────────────────────
    string   chartPath  = "";
    ChartData chart;
    Vector2  scroll;

    // Note placement
    float  beatInput   = 1f;
    int    laneInput   = 0;
    int    rowInput    = 0;
    string dirInput    = "down";
    string colorInput  = "red";
    string typeInput   = "normal";
    float  durationInput = 1f;

    static readonly string[] Directions =
        { "up","down","left","right","upLeft","upRight","downLeft","downRight","any" };

    // ── GUI ──────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        EditorGUILayout.LabelField("VRBeat Chart Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        DrawFileBar();
        EditorGUILayout.Space(4);

        if (chart == null) { EditorGUILayout.HelpBox("Open or create a chart to start.", MessageType.Info); return; }

        DrawAddNote();
        EditorGUILayout.Space(4);
        DrawNoteList();
    }

    void DrawFileBar()
    {
        EditorGUILayout.BeginHorizontal();
        chartPath = EditorGUILayout.TextField("Chart JSON", chartPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
            chartPath = EditorUtility.OpenFilePanel("Open chart JSON", Application.streamingAssetsPath, "json");
        if (GUILayout.Button("Load",   GUILayout.Width(50))) LoadChart();
        if (GUILayout.Button("New",    GUILayout.Width(50))) NewChart();
        if (GUILayout.Button("Save",   GUILayout.Width(50))) SaveChart();
        EditorGUILayout.EndHorizontal();
    }

    void DrawAddNote()
    {
        EditorGUILayout.LabelField("Add Note", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        beatInput     = EditorGUILayout.FloatField("Beat",     beatInput,    GUILayout.Width(150));
        laneInput     = EditorGUILayout.IntField("Lane(0-3)", laneInput,     GUILayout.Width(120));
        rowInput      = EditorGUILayout.IntField("Row(0-2)",  rowInput,      GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        int dirIdx    = System.Array.IndexOf(Directions, dirInput);
        dirIdx        = EditorGUILayout.Popup("Direction", dirIdx, Directions, GUILayout.Width(200));
        dirInput      = Directions[Mathf.Clamp(dirIdx, 0, Directions.Length - 1)];
        colorInput    = EditorGUILayout.TextField("Color", colorInput,  GUILayout.Width(120));
        typeInput     = EditorGUILayout.TextField("Type",  typeInput,   GUILayout.Width(120));
        if (typeInput == "long") durationInput = EditorGUILayout.FloatField("Duration", durationInput, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Add Note"))
        {
            var list = new List<NoteData>(chart.notes ?? new NoteData[0]);
            list.Add(new NoteData
            {
                beat     = beatInput,
                lane     = laneInput,
                row      = rowInput,
                direction= dirInput,
                color    = colorInput,
                type     = typeInput,
                duration = durationInput,
            });
            list.Sort((a, b) => a.beat.CompareTo(b.beat));
            chart.notes = list.ToArray();
        }
    }

    void DrawNoteList()
    {
        EditorGUILayout.LabelField($"Notes ({chart.notes?.Length ?? 0})", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(300));

        int removeIdx = -1;
        for (int i = 0; i < (chart.notes?.Length ?? 0); i++)
        {
            var n = chart.notes[i];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"[{i:D3}] beat:{n.beat:F2}  lane:{n.lane}  row:{n.row}  {n.direction}  {n.color}  {n.type}" +
                                       (n.type == "long" ? $"  dur:{n.duration}" : ""));
            if (GUILayout.Button("X", GUILayout.Width(22))) removeIdx = i;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (removeIdx >= 0)
        {
            var list = new List<NoteData>(chart.notes);
            list.RemoveAt(removeIdx);
            chart.notes = list.ToArray();
        }
    }

    // ── File operations ──────────────────────────────────────────────────────
    void NewChart()
    {
        chart = new ChartData { version = "1.0", noteSpeed = 10f, notes = new NoteData[0] };
        chartPath = "";
    }

    void LoadChart()
    {
        if (!File.Exists(chartPath)) { Debug.LogError($"File not found: {chartPath}"); return; }
        chart = ChartParser.Parse(File.ReadAllText(chartPath));
    }

    void SaveChart()
    {
        if (chart == null) return;
        if (string.IsNullOrEmpty(chartPath))
            chartPath = EditorUtility.SaveFilePanel("Save Chart", Application.streamingAssetsPath, "chart_normal", "json");
        if (string.IsNullOrEmpty(chartPath)) return;

        File.WriteAllText(chartPath, JsonUtility.ToJson(chart, true));
        AssetDatabase.Refresh();
        Debug.Log($"[ChartEditor] Saved to {chartPath}");
    }
}
#endif
