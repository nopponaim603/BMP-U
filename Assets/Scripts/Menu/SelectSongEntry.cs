﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

using BMSKeyLayout = BMS.BMSKeyLayout;

[RequireComponent(typeof(RectTransform))]
public class SelectSongEntry: MonoBehaviour {
    [SerializeField]
    Text directory;
    [SerializeField]
    Text songName;
    [SerializeField]
    Text artist;
    [SerializeField]
    Text otherInfo;
    [SerializeField]
    Button selectButton;
    [SerializeField]
    RawImageFitter banner;
    [SerializeField]
    RankControl rankControl;
    [SerializeField]
    Sprite dirBackground, songInfoBackground;
    [SerializeField]
    Image backgroundHolder;
    [SerializeField]
    RectTransform[] displayAnchors;
    [SerializeField]
    Marquee[] displayMarquees;

    SelectSongScrollView parent;

    bool isDirectory, isParentDirectory;
    SongInfo songInfo;
    DirectoryInfo dirInfo;

    [HideInInspector]
    public new RectTransform transform;

    void Awake() {
        transform = GetComponent<RectTransform>();
        selectButton.onClick.AddListener(OnSelect);
    }

    public void Load(SongInfo songInfo, SelectSongScrollView parent) {
        if(!isDirectory && songInfo.Equals(this.songInfo)) return;
        this.parent = parent;
        this.songInfo = songInfo;
        isDirectory = false;
        UpdateDisplay();
    }
    
    public void Load(DirectoryInfo dirInfo, bool isParent, SelectSongScrollView parent) {
        if(isDirectory && dirInfo == this.dirInfo) return;
        this.parent = parent;
        this.dirInfo = dirInfo;
        isParentDirectory = isParent;
        isDirectory = true;
        UpdateDisplay();
    }

    public void UpdateDisplay() {
        directory.enabled = isDirectory;
        songName.enabled = !isDirectory;
        artist.enabled = !isDirectory;
        otherInfo.enabled = !isDirectory;
        banner.gameObject.SetActive(!isDirectory);
        if(isDirectory) {
            directory.text = isParentDirectory ? string.Concat(dirInfo.Parent.Name, " << ", dirInfo.Name) : dirInfo.Name;
            backgroundHolder.sprite = dirBackground;
        } else {
            songName.text = songInfo.name;
            artist.text = string.IsNullOrEmpty(songInfo.subArtist) ?
                songInfo.artist :
                string.Concat(songInfo.artist, " / ", songInfo.subArtist.Replace("\n", " / "));
            otherInfo.text = string.Format("{2} Lv{0} {1}BPM", songInfo.level, songInfo.bpm, GetLayoutName(songInfo.layout));
            banner.SetTexture(songInfo.banner);
            banner.gameObject.SetActive(songInfo.banner);
            
            var record = SongInfoDetails.GetCurrentrecord(songInfo.bmsHash);
            if(record.HasValue)
                otherInfo.text += string.Format(" <size=28>{0}</size>",
                    SongInfoDetails.GetFormattedRankString(rankControl, record.Value.score));
            backgroundHolder.sprite = songInfoBackground;
        }
    }

    public void UpdateChildTransform(float lerpLeft) {
        foreach(RectTransform anchorTransform in displayAnchors) {
            Vector2 offsetMin = anchorTransform.offsetMin;
            offsetMin.x = lerpLeft;
            anchorTransform.offsetMin = offsetMin;
        }
        foreach(Marquee marquee in displayMarquees)
            marquee.CheckSize();
    }

    void OnSelect() {
        if(isDirectory)
            SongInfoLoader.CurrentDirectory = isParentDirectory ? dirInfo.Parent : dirInfo;
        else
            SongInfoLoader.SelectedSong = songInfo;
    }

    string GetLayoutName(BMSKeyLayout layout) {
        switch(layout) {
            case BMSKeyLayout.Single5Key: return "5-SP";
            case BMSKeyLayout.Single7Key: return "7-SP";
            case BMSKeyLayout.Single9Key:
            case BMSKeyLayout.Single9KeyAlt: return "9-SP";
            case BMSKeyLayout.Duel10Key: return "10-DP";
            case BMSKeyLayout.Duel14Key: return "14-DP";
            default: return "Custom";
        }
    }
}
