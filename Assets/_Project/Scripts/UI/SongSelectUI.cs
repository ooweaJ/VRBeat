using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongSelectUI : MonoBehaviour
{
    [SerializeField] Transform    songListParent;
    [SerializeField] GameObject   songItemPrefab;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI artistText;
    [SerializeField] TextMeshProUGUI highScoreText;
    [SerializeField] Image        coverImage;
    [SerializeField] Transform    difficultyParent;
    [SerializeField] GameObject   difficultyButtonPrefab;

    List<SongData> songs;
    SongData selectedSong;
    DifficultyInfo selectedDiff;

    void Start()
    {
        songs = SongLibrary.Instance?.LoadAllSongs() ?? new List<SongData>();
        PopulateSongList();
        if (songs.Count > 0) SelectSong(songs[0]);
    }

    void PopulateSongList()
    {
        foreach (Transform t in songListParent) Destroy(t.gameObject);
        foreach (var song in songs)
        {
            var item = Instantiate(songItemPrefab, songListParent);
            var txt  = item.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = song.info.title;
            var btn  = item.GetComponent<Button>();
            var s    = song;
            btn?.onClick.AddListener(() => SelectSong(s));
        }
    }

    void SelectSong(SongData song)
    {
        selectedSong = song;
        if (titleText   != null) titleText.text  = song.info.title;
        if (artistText  != null) artistText.text = song.info.artist;

        PopulateDifficulties(song);
        if (song.coverSprite != null && coverImage != null)
            coverImage.sprite = song.coverSprite;
    }

    void PopulateDifficulties(SongData song)
    {
        foreach (Transform t in difficultyParent) Destroy(t.gameObject);
        if (song.info.difficulties == null) return;

        foreach (var diff in song.info.difficulties)
        {
            var btn  = Instantiate(difficultyButtonPrefab, difficultyParent);
            var txt  = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = $"{diff.name} Lv.{diff.level}";
            var b    = btn.GetComponent<Button>();
            var d    = diff;
            b?.onClick.AddListener(() => SelectDifficulty(d));
        }

        if (song.info.difficulties.Length > 0) SelectDifficulty(song.info.difficulties[0]);
    }

    void SelectDifficulty(DifficultyInfo diff)
    {
        selectedDiff = diff;
        var record = SaveSystem.LoadRecord(selectedSong?.info.songId, diff.name);
        if (highScoreText != null)
            highScoreText.text = record != null ? $"Best: {record.highScore:N0}" : "No record";
    }

    public void Play()
    {
        if (selectedSong == null || selectedDiff == null) return;
        GameManager.Instance?.StartGame(selectedSong, selectedDiff);
    }
}
