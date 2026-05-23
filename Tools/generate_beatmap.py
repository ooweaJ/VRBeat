#!/usr/bin/env python3
"""
mp3(또는 wav/ogg) 한 곡을 분석해 VRBeat 차트(info.json + chart_*.json)를 생성한다.

게임 코드는 전혀 바꾸지 않는다 — 이 스크립트는 게임이 이미 읽는 포맷 그대로 출력한다:
  StreamingAssets/Songs/<song_id>/
    ├─ audio.mp3            (입력 오디오 복사본)
    ├─ info.json            (bpm, songOffset, difficulties ...)
    ├─ chart_easy.json      (비트 기반, 듬성)
    └─ chart_normal.json    (온셋 기반, 촘촘)

핵심 변환:
  songOffset = 0 으로 두고, 각 노트의 beat = onsetTime(초) * bpm / 60.
  → Conductor 가 beat 를 다시 시간으로 환산하면 정확히 그 오디오 지점에 노트가 도달한다.

필요 패키지:  pip install librosa soundfile numpy   (mp3 디코딩에 ffmpeg 권장)
사용 예:
  python generate_beatmap.py "song.mp3" --title "My Song" --artist "Someone"
  python generate_beatmap.py "song.mp3" --song-id song_002 --bpm 128   # BPM 강제
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
    sys.exit("librosa 가 필요합니다.  pip install librosa soundfile numpy")


LANES = 4   # 0..3 (좌→우)
ROWS = 3    # 0..2 (아래→위)
DIRECTIONS = ["down", "up", "left", "right", "downLeft", "downRight", "upLeft", "upRight"]


def analyze(path, forced_bpm=None):
    """오디오에서 BPM, 비트 시각, 온셋 시각을 추출."""
    y, sr = librosa.load(path, mono=True)
    duration = float(librosa.get_duration(y=y, sr=sr))

    tempo, beat_frames = librosa.beat.beat_track(y=y, sr=sr)
    detected_bpm = float(np.atleast_1d(tempo)[0])
    bpm = float(forced_bpm) if forced_bpm else detected_bpm

    beat_times = librosa.frames_to_time(beat_frames, sr=sr)

    onset_frames = librosa.onset.onset_detect(y=y, sr=sr, backtrack=True)
    onset_times = librosa.frames_to_time(onset_frames, sr=sr)

    return {
        "bpm": bpm,
        "detected_bpm": detected_bpm,
        "beat_times": np.asarray(beat_times, dtype=float),
        "onset_times": np.asarray(onset_times, dtype=float),
        "duration": duration,
    }


def make_notes(times, bpm, rng, min_gap_beats, quantize):
    """시각(초) 목록을 노트 목록으로 변환. 색은 좌우 교대, lane/방향은 패턴 생성."""
    notes = []
    last_beat = -1e9
    color_idx = 0

    for t in sorted(times):
        beat = t * bpm / 60.0
        if quantize > 0:
            beat = round(beat / quantize) * quantize
        if beat - last_beat < min_gap_beats:
            continue
        last_beat = beat

        color = "red" if color_idx % 2 == 0 else "blue"
        color_idx += 1

        lane = rng.randint(0, LANES - 1)
        row = 1 if rng.random() < 0.7 else rng.randint(0, ROWS - 1)
        direction = rng.choice(DIRECTIONS)

        notes.append({
            "type": "normal",
            "beat": round(beat, 3),
            "duration": 0,
            "lane": lane,
            "row": row,
            "direction": direction,
            "color": color,
        })

    return notes


def write_json(path, obj):
    with open(path, "w", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)


def main():
    ap = argparse.ArgumentParser(description="mp3 → VRBeat 차트 생성기")
    ap.add_argument("audio", help="입력 오디오 파일 (mp3/wav/ogg)")
    ap.add_argument("--song-id", help="곡 폴더 이름 (기본: 파일명)")
    ap.add_argument("--title", default=None)
    ap.add_argument("--artist", default="Unknown")
    ap.add_argument("--mapper", default="auto")
    ap.add_argument("--bpm", type=float, default=None, help="BPM 강제 지정(검출이 절반/2배로 틀릴 때)")
    ap.add_argument("--note-speed", type=float, default=8.0)
    ap.add_argument("--seed", type=int, default=1234, help="패턴 재현용 시드")
    ap.add_argument("--out", default=None,
                    help="출력 루트 (기본: <repo>/Assets/StreamingAssets/Songs)")
    args = ap.parse_args()

    import random
    rng = random.Random(args.seed)

    if not os.path.isfile(args.audio):
        sys.exit(f"파일 없음: {args.audio}")

    base = os.path.splitext(os.path.basename(args.audio))[0]
    song_id = args.song_id or base
    title = args.title or base

    # 출력 루트: 인자 없으면 이 스크립트 기준 ../Assets/StreamingAssets/Songs
    if args.out:
        out_root = args.out
    else:
        repo = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        out_root = os.path.join(repo, "Assets", "StreamingAssets", "Songs")
    out_dir = os.path.join(out_root, song_id)
    os.makedirs(out_dir, exist_ok=True)

    print(f"[1/4] 분석 중: {args.audio}")
    a = analyze(args.audio, forced_bpm=args.bpm)
    bpm = a["bpm"]
    print(f"      BPM(검출)={a['detected_bpm']:.2f}  사용={bpm:.2f}  "
          f"길이={a['duration']:.1f}s  비트={len(a['beat_times'])}  온셋={len(a['onset_times'])}")

    print("[2/4] 차트 생성 중")
    easy_notes = make_notes(a["beat_times"], bpm, rng, min_gap_beats=1.0, quantize=1.0)
    normal_notes = make_notes(a["onset_times"], bpm, rng, min_gap_beats=0.5, quantize=0.5)
    print(f"      Easy={len(easy_notes)}개  Normal={len(normal_notes)}개")

    # 오디오 복사 (확장자 유지)
    ext = os.path.splitext(args.audio)[1].lower()
    audio_name = "audio" + ext
    shutil.copy(args.audio, os.path.join(out_dir, audio_name))

    print("[3/4] info.json / chart_*.json 쓰기")
    info = {
        "songId": song_id,
        "title": title,
        "artist": args.artist,
        "mapper": args.mapper,
        "bpm": round(bpm, 2),
        "audioFile": audio_name,
        "coverFile": "cover.png",
        "previewStart": 0.0,
        "previewDuration": 10.0,
        "songOffset": 0.0,
        "difficulties": [
            {"name": "Easy", "chartFile": "chart_easy.json", "level": 3},
            {"name": "Normal", "chartFile": "chart_normal.json", "level": 5},
        ],
    }
    write_json(os.path.join(out_dir, "info.json"), info)
    write_json(os.path.join(out_dir, "chart_easy.json"),
               {"version": "1.0", "noteSpeed": args.note_speed, "notes": easy_notes})
    write_json(os.path.join(out_dir, "chart_normal.json"),
               {"version": "1.0", "noteSpeed": args.note_speed, "notes": normal_notes})

    print(f"[4/4] 완료 → {out_dir}")
    print("      Unity 에서 Refresh 후 SongSelect 에 곡이 나타납니다.")
    print("      박자가 어긋나면: --bpm 으로 BPM 강제, 또는 info.json 의 songOffset 미세조정.")


if __name__ == "__main__":
    main()
