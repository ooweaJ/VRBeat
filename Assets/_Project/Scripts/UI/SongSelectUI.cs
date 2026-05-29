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

    static readonly Color SongColorSelected = new Color(0.95f, 0.68f, 0.16f, 0.96f);
    static readonly Color SongColorNormal   = new Color(0.07f, 0.12f, 0.20f, 0.92f);
    static readonly Color TextColorSelected = new Color(0.05f, 0.04f, 0.02f, 1f);
    static readonly Color TextColorNormal   = MenuPalette.TextBright;
    static readonly Color DiffColorSelected = new Color(0.95f, 0.68f, 0.16f, 0.96f);
    static readonly Color DiffColorNormal   = new Color(0.08f, 0.22f, 0.42f, 0.92f);

    List<SongData> songs;
    SongData selectedSong;
    DifficultyInfo selectedDiff;
    readonly List<(DifficultyInfo diff, Image img)>                           diffButtons  = new();
    readonly List<(SongData song, Image img, TextMeshProUGUI txt)>            songButtons  = new();

    void Start()
    {
        var all = SongLibrary.Instance?.LoadAllSongs() ?? new List<SongData>();
        songs = all.FindAll(s => s.info.mapper != "tutorial");
        PopulateSongList();
        if (songs.Count > 0) SelectSong(songs[0]);
    }

    void PopulateSongList()
    {
        foreach (Transform t in songListParent) Destroy(t.gameObject);
        songButtons.Clear();
        foreach (var song in songs)
        {
            var item = Instantiate(songItemPrefab, songListParent);
            var txt  = item.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = song.info.title;
            var img  = item.GetComponent<Image>();
            var btn  = item.GetComponent<Button>();
            var s    = song;
            if (img != null) { img.color = SongColorNormal; songButtons.Add((s, img, txt)); }
            btn?.onClick.AddListener(() => SelectSong(s));
        }
        RestyleDynamicButtons();
    }

    void SelectSong(SongData song)
    {
        selectedSong = song;
        if (titleText   != null) titleText.text  = song.info.title;
        if (artistText  != null) artistText.text = song.info.artist;

        PopulateDifficulties(song);
        RefreshSongButtonSelection(song);
        if (song.coverSprite != null && coverImage != null)
            coverImage.sprite = song.coverSprite;
    }

    void PopulateDifficulties(SongData song)
    {
        foreach (Transform t in difficultyParent) Destroy(t.gameObject);
        diffButtons.Clear();
        if (song.info.difficulties == null) return;

        foreach (var diff in song.info.difficulties)
        {
            var btn  = Instantiate(difficultyButtonPrefab, difficultyParent);
            var txt  = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = $"{diff.name} Lv.{diff.level}";
            var img  = btn.GetComponent<Image>();
            var b    = btn.GetComponent<Button>();
            var d    = diff;
            if (img != null) diffButtons.Add((d, img));
            b?.onClick.AddListener(() => SelectDifficulty(d));
        }

        RestyleDynamicButtons();
        if (song.info.difficulties.Length > 0) SelectDifficulty(song.info.difficulties[0]);
    }

    void SelectDifficulty(DifficultyInfo diff)
    {
        selectedDiff = diff;

        foreach (var (d, img) in diffButtons)
        {
            bool sel = d == diff;
            if (img != null)
            {
                img.color = sel ? DiffColorSelected : DiffColorNormal;
                MenuStyleApplier.StyleSelectableGraphic(img.gameObject, sel);
            }
        }

        var record = SaveSystem.LoadRecord(selectedSong?.info.songId, diff.name);
        if (highScoreText != null)
            highScoreText.text = record != null ? $"Best: {record.highScore:N0}" : "No record";
    }

    public void Play()
    {
        if (selectedSong == null || selectedDiff == null) return;
        GameManager.Instance?.StartGame(selectedSong, selectedDiff);
    }

    public void GoToSettings()    => SceneLoader.Load("Settings");
    public void GoToCalibration() => SceneLoader.Load("Calibration");
    public void GoToTutorial()    => SceneLoader.Load("Tutorial");

    void RefreshSongButtonSelection(SongData song)
    {
        foreach (var (s, img, txt2) in songButtons)
        {
            bool sel = s == song;
            img.color = sel ? SongColorSelected : SongColorNormal;
            MenuStyleApplier.StyleSelectableGraphic(img.gameObject, sel);
            if (txt2 != null) txt2.color = sel ? TextColorSelected : TextColorNormal;
        }
    }

    static void RestyleDynamicButtons()
    {
        var applier = FindFirstObjectByType<MenuStyleApplier>();
        if (applier != null) applier.Apply();
    }
}
