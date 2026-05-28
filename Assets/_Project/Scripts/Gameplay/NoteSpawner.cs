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
        SongData targetSong = null;
        DifficultyInfo targetDifficulty = null;

        // Fallback for direct testing in Gameplay scene
        if (gm == null || gm.SelectedSong == null)
        {
            Debug.Log("[NoteSpawner] No song selected. Attempting to load first song from library as fallback.");
            var library = SongLibrary.Instance;
            if (library == null)
            {
                Debug.Log("[NoteSpawner] SongLibrary instance missing, creating one.");
                library = new GameObject("SongLibrary").AddComponent<SongLibrary>();
            }
            
            var songs = library.LoadAllSongs();
            Debug.Log($"[NoteSpawner] Library scan complete. Found {songs?.Count ?? 0} songs.");
            
            if (songs != null && songs.Count > 0)
            {
                targetSong = songs[0];
                targetDifficulty = targetSong.info.difficulties[0];
                Debug.Log($"[NoteSpawner] Fallback selected: {targetSong.info.title} ({targetDifficulty.name})");
            }
            else
            {
                Debug.LogError("[NoteSpawner] Library is empty! Check StreamingAssets/Songs/ folder structure and info.json.");
            }
        }
        else
        {
            targetSong = gm.SelectedSong;
            targetDifficulty = gm.SelectedDifficulty;
        }

        if (targetSong == null || targetDifficulty == null)
        {
            Debug.LogError("[NoteSpawner] Failed to find a song to load.");
            yield break;
        }

        // Load the actual audio clip (using SongLoader)
        var loader = gameObject.AddComponent<SongLoader>();
        string audioPath = Path.Combine(targetSong.folderPath, targetSong.info.audioFile);
        yield return loader.LoadAudio(audioPath, clip => targetSong.audioClip = clip);

        if (targetSong.audioClip == null)
        {
            Debug.LogError($"[NoteSpawner] Failed to load audio at {audioPath}");
            yield break;
        }

        string chartPath = Path.Combine(targetSong.folderPath, targetDifficulty.chartFile);
        ChartData loaded = null;
        yield return ChartParser.LoadFromPath(chartPath, c => loaded = c);

        if (loaded == null) yield break;
        chart = loaded;
        if (chart.noteSpeed > 0) noteSpeed = chart.noteSpeed;

        float bpm    = targetSong.info.bpm;
        float offset = targetSong.info.songOffset;

        Conductor.Instance.StartSong(targetSong.audioClip, bpm, offset);
        isReady = true;
    }

    void Update()
    {
        if (!isReady || chart == null) return;

        float currentBeat = Conductor.Instance.SongBeat;
        float despawnZ    = config != null ? config.despawnDistance : -2f;
        float syncDist    = config != null ? config.noteSyncStartDistance : 20f;
        float preSyncTime = config != null ? config.notePreSyncDuration : 0.35f;

        float spawnLeadSeconds = (syncDist / noteSpeed) + preSyncTime;
        float spawnLeadBeats = spawnLeadSeconds / Conductor.Instance.SecondsPerBeat;

        // Spawn notes at z=40, then let the pre-sync approach bring them to z=20.
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
        if (Conductor.Instance.IsSongFinished && nextNoteIndex >= chart.notes.Length)
        {
            // 남은 노트(롱노트 포함) 강제 정리
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                if (!activeNotes[i].WasHit)
                    ScoreManager.Instance?.RegisterMiss(activeNotes[i]);
                pool.Return(activeNotes[i]);
                activeNotes.RemoveAt(i);
            }
            GameManager.Instance?.GoToResult();
        }
    }

    void SpawnNote(NoteData data)
    {
        float hitZ = config != null ? config.hitDistance : 0f;
        NoteBase note = pool.Get(data.type);
        note.Initialize(data, noteSpeed, hitZ, config);
        activeNotes.Add(note);
        BackgroundSphere.NoteSpawn(data.color);
    }

    public void RemoveActiveNote(NoteBase note)
    {
        activeNotes.Remove(note);
        pool.Return(note);
    }
}
