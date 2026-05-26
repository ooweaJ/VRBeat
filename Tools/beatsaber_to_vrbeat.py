#!/usr/bin/env python3
"""
Beat Saber 맵 → VRBeat 채보 변환기

BeatSage (beatsage.com) 또는 ChroMapper 등에서 내보낸
Beat Saber 포맷을 VRBeat JSON으로 변환한다.

Beat Saber 맵 구조 (입력):
  ExpertPlusStandard.dat  또는  EasyStandard.dat  (노트 데이터)
  Info.dat                                         (BPM, songOffset 등)

VRBeat 출력:
  <out_dir>/<song_id>/
    ├─ info.json
    ├─ chart_easy.json    (Easy/Normal/Hard → easy)
    └─ chart_normal.json  (Expert/ExpertPlus → normal)

사용 예:
  # BeatSage 다운로드 후 압축 해제한 폴더 전체 지정
  python beatsaber_to_vrbeat.py ./MyBeatSageMap --song-id my_song --audio song.mp3

  # 특정 .dat 파일만 easy/normal 지정
  python beatsaber_to_vrbeat.py ./map --easy EasyStandard.dat --normal ExpertStandard.dat
"""

import argparse
import json
import os
import shutil
import sys

import numpy as np

try:
    import librosa
except ImportError:
    librosa = None


# Beat Saber cutDirection → VRBeat direction
CUT_DIR_MAP = {
    0: "up",
    1: "down",
    2: "left",
    3: "right",
    4: "upLeft",
    5: "upRight",
    6: "downLeft",
    7: "downRight",
    8: "any",   # dot note
}

# Beat Saber _type → VRBeat color
TYPE_COLOR = {0: "red", 1: "blue"}


def load_json(path):
    with open(path, encoding="utf-8-sig") as f:
        return json.load(f)


def quantize_beat(beat, grid=0.25):
    """박자를 grid 단위로 스냅 (기본: 1/4박자)"""
    return round(round(beat / grid) * grid, 4)


def convert_notes(bs_notes, note_speed, beat_grid=0.25):
    """Beat Saber _notes 배열 → VRBeat notes 배열"""
    vrbeat_notes = []
    for n in bs_notes:
        note_type = n.get("_type", n.get("b", -1))  # v2 vs v3
        if note_type not in (0, 1):
            continue  # 폭탄(type=3) 제외

        # v2 필드명 우선, 없으면 v3 필드명
        beat      = float(n.get("_time",          n.get("b", 0)))
        lane      = int(  n.get("_lineIndex",      n.get("x", 0)))
        row       = int(  n.get("_lineLayer",      n.get("y", 0)))
        cut_dir   = int(  n.get("_cutDirection",   n.get("d", 8)))

        if beat_grid > 0:
            beat = quantize_beat(beat, beat_grid)

        color     = TYPE_COLOR.get(note_type, "red")
        direction = CUT_DIR_MAP.get(cut_dir, "any")

        vrbeat_notes.append({
            "type":      "normal",
            "beat":      beat,
            "duration":  0,
            "lane":      max(0, min(3, lane)),
            "row":       max(0, min(2, row)),
            "direction": direction,
            "color":     color,
        })

    vrbeat_notes.sort(key=lambda n: n["beat"])
    return vrbeat_notes


def find_dat_files(folder):
    """폴더에서 Beat Saber .dat 파일 목록 반환"""
    dats = {}
    for f in os.listdir(folder):
        fl = f.lower()
        if not fl.endswith(".dat"):
            continue
        if "info" in fl:
            dats["info"] = os.path.join(folder, f)
        elif "expertplus" in fl or "expertPlus" in f:
            dats["expert_plus"] = os.path.join(folder, f)
        elif "expert" in fl:
            dats["expert"] = os.path.join(folder, f)
        elif "hard" in fl:
            dats["hard"] = os.path.join(folder, f)
        elif "normal" in fl:
            dats["normal"] = os.path.join(folder, f)
        elif "easy" in fl:
            dats["easy"] = os.path.join(folder, f)
    return dats


def fill_long_notes(notes, bpm, audio_path, min_gap_beats=2.5, harm_threshold=0.3):
    """갭이 크고 하모닉 성분이 있는 구간을 롱노트로 채운다."""
    if not notes or librosa is None or not audio_path or not os.path.isfile(audio_path):
        return notes

    y, sr = librosa.load(audio_path, mono=True)
    y_harm, _ = librosa.effects.hpss(y)
    hop = 512
    rms_harm  = librosa.feature.rms(y=y_harm, hop_length=hop)[0]
    times_all = librosa.frames_to_time(np.arange(len(rms_harm)), sr=sr, hop_length=hop)
    harm_max  = float(rms_harm.max()) + 1e-9
    spb       = 60.0 / bpm

    result = list(notes)
    for i in range(len(result) - 1):
        n      = result[i]
        next_n = result[i + 1]
        gap    = next_n["beat"] - n["beat"]
        if gap < min_gap_beats:
            continue

        t_start = n["beat"] * spb
        t_end   = next_n["beat"] * spb
        mask    = (times_all >= t_start) & (times_all <= t_end)
        if mask.sum() == 0:
            continue
        harm_mean = float(rms_harm[mask].mean()) / harm_max
        if harm_mean < harm_threshold:
            continue

        duration = round(max(gap * 0.8, 1.5), 2)
        result[i] = {**n, "type": "long", "duration": duration, "direction": "any"}

    return result


def write_json(path, obj):
    with open(path, "w", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)


def main():
    ap = argparse.ArgumentParser(description="Beat Saber 맵 → VRBeat 채보 변환기")
    ap.add_argument("map_folder",  help="Beat Saber 맵 폴더 (압축 해제된 폴더)")
    ap.add_argument("--song-id",   help="곡 폴더 이름 (기본: 폴더명)")
    ap.add_argument("--title",     default=None)
    ap.add_argument("--artist",    default="Unknown")
    ap.add_argument("--audio",     default=None, help="오디오 파일 경로 (지정 시 복사)")
    ap.add_argument("--easy",      default=None, help="Easy 채보로 쓸 .dat 파일명 (기본: Normal.dat)")
    ap.add_argument("--normal",    default=None, help="Normal 채보로 쓸 .dat 파일명 (기본: Hard.dat)")
    ap.add_argument("--hard",      default=None, help="Hard 채보로 쓸 .dat 파일명 (기본: Expert.dat)")
    ap.add_argument("--note-speed",type=float,   default=8.0)
    ap.add_argument("--no-long",   action="store_true", help="롱노트 자동 삽입 비활성화")
    ap.add_argument("--long-gap",  type=float,   default=2.5, help="롱노트 최소 갭(박자, 기본 2.5)")
    ap.add_argument("--beat-grid", type=float,   default=0.25, help="박자 스냅 단위(기본 0.25=1/4박). 0=비활성화")
    ap.add_argument("--out",       default=None,
                    help="출력 루트 (기본: <repo>/Assets/StreamingAssets/Songs)")
    args = ap.parse_args()

    folder = args.map_folder
    if not os.path.isdir(folder):
        sys.exit(f"폴더가 없습니다: {folder}")

    dats = find_dat_files(folder)
    print(f"발견된 .dat 파일: {list(dats.keys())}")

    # Info.dat 에서 BPM/offset 읽기
    bpm       = 120.0
    offset    = 0.0
    title     = args.title
    artist    = args.artist
    audio_src = args.audio

    if "info" in dats:
        info_data = load_json(dats["info"])
        bpm    = float(info_data.get("_beatsPerMinute", info_data.get("bpm", 120.0)))
        offset = float(info_data.get("_songTimeOffset",  0.0))
        if title  is None: title  = info_data.get("_songName",   info_data.get("song", {}).get("title", "Unknown"))
        if artist is None: artist = info_data.get("_songAuthorName", "Unknown")
        if audio_src is None:
            audio_fn = info_data.get("_songFilename", info_data.get("audio", {}).get("filename"))
            if audio_fn:
                candidate = os.path.join(folder, audio_fn)
                if os.path.isfile(candidate):
                    audio_src = candidate
    title  = title  or os.path.basename(folder)
    artist = artist or "Unknown"

    # 채보 파일 결정: Normal→Easy, Hard→Normal, Expert→Hard
    def resolve(arg, fallbacks):
        if arg: return os.path.join(folder, arg)
        for k in fallbacks:
            if k in dats: return dats[k]
        return None

    easy_path   = resolve(args.easy,   ["normal"])
    normal_path = resolve(args.normal, ["hard"])
    hard_path   = resolve(args.hard,   ["expert_plus", "expert"])

    if normal_path == easy_path:   normal_path = None
    if hard_path   in (easy_path, normal_path): hard_path = None

    if easy_path is None:
        sys.exit("Easy 채보로 쓸 .dat 파일을 찾을 수 없습니다.")

    print(f"Easy   ← {os.path.basename(easy_path)}")
    print(f"Normal ← {os.path.basename(normal_path) if normal_path else '(없음)'}")
    print(f"Hard   ← {os.path.basename(hard_path)   if hard_path   else '(없음)'}")

    # 출력 경로
    song_id = args.song_id or os.path.basename(os.path.abspath(folder))
    if args.out:
        out_root = args.out
    else:
        repo     = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        out_root = os.path.join(repo, "Assets", "StreamingAssets", "Songs")
    out_dir = os.path.join(out_root, song_id)
    os.makedirs(out_dir, exist_ok=True)

    # 노트 변환
    grid = args.beat_grid
    easy_data   = load_json(easy_path)
    easy_bs     = easy_data.get("_notes", easy_data.get("colorNotes", []))
    easy_notes  = convert_notes(easy_bs, args.note_speed, beat_grid=grid)

    normal_notes = []
    if normal_path:
        normal_data  = load_json(normal_path)
        normal_notes = convert_notes(normal_data.get("_notes", normal_data.get("colorNotes", [])), args.note_speed, beat_grid=grid)

    hard_notes = []
    if hard_path:
        hard_data  = load_json(hard_path)
        hard_notes = convert_notes(hard_data.get("_notes", hard_data.get("colorNotes", [])), args.note_speed, beat_grid=grid)

    print(f"변환 완료: Easy={len(easy_notes)}개  Normal={len(normal_notes)}개  Hard={len(hard_notes)}개")

    # 롱노트 삽입
    if not args.no_long and audio_src and os.path.isfile(audio_src):
        print("롱노트 분석 중 (HPSS)...")
        easy_notes   = fill_long_notes(easy_notes,   bpm, audio_src, min_gap_beats=args.long_gap)
        normal_notes = fill_long_notes(normal_notes, bpm, audio_src, min_gap_beats=args.long_gap)
        hard_notes   = fill_long_notes(hard_notes,   bpm, audio_src, min_gap_beats=args.long_gap)
        print(f"롱노트: Easy {sum(1 for n in easy_notes if n['type']=='long')}개 / "
              f"Normal {sum(1 for n in normal_notes if n['type']=='long')}개 / "
              f"Hard {sum(1 for n in hard_notes if n['type']=='long')}개")

    # 오디오 복사
    audio_name = "audio.mp3"
    if audio_src and os.path.isfile(audio_src):
        ext = os.path.splitext(audio_src)[1].lower()
        audio_name = "audio" + ext
        shutil.copy(audio_src, os.path.join(out_dir, audio_name))
        print(f"오디오 복사: {audio_name}")
    else:
        print("오디오 파일 없음 — 나중에 수동으로 복사하세요.")

    # 커버 이미지 복사
    for ext in (".jpg", ".jpeg", ".png"):
        cover_src = os.path.join(folder, "cover" + ext)
        if os.path.isfile(cover_src):
            shutil.copy(cover_src, os.path.join(out_dir, "cover" + ext))
            break

    # JSON 쓰기
    difficulties = [{"name": "Easy",   "chartFile": "chart_easy.json",   "level": 3}]
    if normal_notes:
        difficulties.append({"name": "Normal", "chartFile": "chart_normal.json", "level": 5})
    if hard_notes:
        difficulties.append({"name": "Hard",   "chartFile": "chart_hard.json",   "level": 7})

    write_json(os.path.join(out_dir, "info.json"), {
        "songId":          song_id,
        "title":           title,
        "artist":          artist,
        "mapper":          "beatsage",
        "bpm":             round(bpm, 2),
        "audioFile":       audio_name,
        "coverFile":       "cover.jpg",
        "previewStart":    0.0,
        "previewDuration": 10.0,
        "songOffset":      round(offset, 3),
        "difficulties":    difficulties,
    })
    write_json(os.path.join(out_dir, "chart_easy.json"),
               {"version": "1.0", "noteSpeed": args.note_speed, "notes": easy_notes})
    if normal_notes:
        write_json(os.path.join(out_dir, "chart_normal.json"),
                   {"version": "1.0", "noteSpeed": args.note_speed, "notes": normal_notes})
    if hard_notes:
        write_json(os.path.join(out_dir, "chart_hard.json"),
                   {"version": "1.0", "noteSpeed": args.note_speed, "notes": hard_notes})

    print(f"\n완료 → {out_dir}")
    print("Unity 에서 Ctrl+R 후 SongSelect 에 곡이 나타납니다.")


if __name__ == "__main__":
    main()
