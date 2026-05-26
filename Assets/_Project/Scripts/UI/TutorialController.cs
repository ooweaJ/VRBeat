using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI stepText;

    SongData tutorialSong;

    void Start()
    {
        var songs = SongLibrary.Instance?.LoadAllSongs() ?? new List<SongData>();
        tutorialSong = songs.Find(s => s.info.songId == "chanran_bit");

        if (tutorialSong == null)
            Debug.LogWarning("[Tutorial] chanran_bit song not found in StreamingAssets/Songs/");

        if (stepText != null)
            stepText.text = "찬란한 빛 (30초)";
    }

    public void StartTutorial()
    {
        if (tutorialSong == null)
        {
            Debug.LogWarning("[Tutorial] chanran_bit not loaded. Cannot start.");
            return;
        }
        var difficulties = tutorialSong.info.difficulties;
        if (difficulties == null || difficulties.Length == 0) return;
        GameManager.Instance?.StartGame(tutorialSong, difficulties[0]);
    }

    public void Back()
    {
        GameManager.Instance?.GoToSongSelect();
    }
}
