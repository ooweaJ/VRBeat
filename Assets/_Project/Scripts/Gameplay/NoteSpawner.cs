using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] NotePool pool;
    [SerializeField] GameConfig config;

    public float noteSpeed = 10f;

    ChartData chart;
    int nextNoteIndex;
    readonly List<NoteBase> activeNotes = new List<NoteBase>();

    bool isReady;

    void Start()
    {
        if (config == null) config = Resources.Load<GameConfig>("GameConfig");
        pool.Warmup(config != null ? config.poolWarmupCount : 64);
        StartCoroutine(LoadAndStart());
    }

    IEnumerator LoadAndStart()
    {
        var gm = GameManager.Instance;
        if (gm?.SelectedSong == null || gm.SelectedDifficulty == null)
        {
            Debug.LogWarning("[NoteSpawner] No song/difficulty selected.");
            yield break;
        }

        string chartPath = Path.Combine(gm.SelectedSong.folderPath, gm.SelectedDifficulty.chartFile);
        ChartData loaded = null;
        yield return ChartParser.LoadFromPath(chartPath, c => loaded = c);

        if (loaded == null) yield break;
        chart = loaded;
        if (chart.noteSpeed > 0) noteSpeed = chart.noteSpeed;

        float hitZ   = config != null ? config.hitDistance   : 0f;
        float bpm    = gm.SelectedSong.info.bpm;
        float offset = gm.SelectedSong.info.songOffset;

        Conductor.Instance.StartSong(gm.SelectedSong.audioClip, bpm, offset);
        isReady = true;
    }

    void Update()
    {
        if (!isReady || chart == null) return;

        float currentBeat = Conductor.Instance.SongBeat;
        float hitZ        = config != null ? config.hitDistance   : 0f;
        float despawnZ    = config != null ? config.despawnDistance : -2f;
        float spawnDist   = config != null ? config.spawnDistance  : 30f;

        float spawnLeadBeats = (spawnDist / noteSpeed) / Conductor.Instance.SecondsPerBeat;

        // Spawn notes that are within spawnDistance
        while (nextNoteIndex < chart.notes.Length &&
               chart.notes[nextNoteIndex].beat <= currentBeat + spawnLeadBeats)
        {
            SpawnNote(chart.notes[nextNoteIndex]);
            nextNoteIndex++;
        }

        // Miss detection and pool return
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var note = activeNotes[i];
            if (note.ShouldDespawn(despawnZ))
            {
                if (!note.WasHit)
                    ScoreManager.Instance?.RegisterMiss(note);
                pool.Return(note);
                activeNotes.RemoveAt(i);
            }
        }

        // Song end → Result
        if (Conductor.Instance.IsSongFinished
            && activeNotes.Count == 0
            && nextNoteIndex >= chart.notes.Length)
        {
            GameManager.Instance?.GoToResult();
        }
    }

    void SpawnNote(NoteData data)
    {
        float hitZ = config != null ? config.hitDistance : 0f;
        NoteBase note = pool.Get(data.type);
        note.Initialize(data, noteSpeed, hitZ);
        activeNotes.Add(note);
    }

    public void RemoveActiveNote(NoteBase note)
    {
        activeNotes.Remove(note);
        pool.Return(note);
    }
}
