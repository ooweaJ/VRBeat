#!/usr/bin/env python3
"""
mp3(또는 wav/ogg) 한 곡을 분석해 VRBeat 차트(info.json + chart_*.json)를 생성한다.

개선 사항:
  - Easy/Normal 모두 onset 기반으로 통일 (beat_times의 갭 문제 해결)
  - onset strength 기반 강한 온셋만 선택
  - 스펙트럼 무게중심으로 row 높이 결정 (고음=위, 저음=아래)
  - 좌우 손 교대 + lane 공간 배치 (red=왼쪽 2칸, blue=오른쪽 2칸)
  - 이전 방향 기반 자연스러운 direction 전환
  - 갭 구간 하모닉 감지 → 롱노트 자동 삽입

필요 패키지:  pip install librosa soundfile numpy
사용 예:
  python generate_beatmap.py "song.mp3" --title "My Song" --artist "Someone"
  python generate_beatmap.py "song.mp3" --bpm 128
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


LANES = 4
ROWS  = 3

RED_LANES  = [0, 1]
BLUE_LANES = [2, 3]

DIRECTION_FLOW = {
    "down":      ["down", "downLeft", "downRight", "left", "right"],
    "up":        ["up",   "upLeft",   "upRight",   "left", "right"],
    "left":      ["left", "downLeft", "upLeft",    "down", "up"],
    "right":     ["right","downRight","upRight",   "down", "up"],
    "downLeft":  ["downLeft",  "left", "down"],
    "downRight": ["downRight", "right","down"],
    "upLeft":    ["upLeft",    "left", "up"],
    "upRight":   ["upRight",   "right","up"],
}
ALL_DIRECTIONS = list(DIRECTION_FLOW.keys())


def analyze(path, forced_bpm=None):
    y, sr = librosa.load(path, mono=True)
    duration = float(librosa.get_duration(y=y, sr=sr))

    tempo, beat_frames = librosa.beat.beat_track(y=y, sr=sr)
    detected_bpm = float(np.atleast_1d(tempo)[0])
    bpm = float(forced_bpm) if forced_bpm else detected_bpm

    hop = 512
    onset_env    = librosa.onset.onset_strength(y=y, sr=sr, hop_length=hop)
    onset_frames = librosa.onset.onset_detect(y=y, sr=sr, hop_length=hop, backtrack=True)
    onset_times  = librosa.frames_to_time(onset_frames, sr=sr, hop_length=hop)
    onset_str    = onset_env[onset_frames.clip(0, len(onset_env) - 1)]

    centroid     = librosa.feature.spectral_centroid(y=y, sr=sr, hop_length=hop)[0]
    cent_onset   = centroid[onset_frames.clip(0, len(centroid) - 1)]

    # 하모닉 성분 RMS (롱노트 구간 감지용)
    y_harm, _ = librosa.effects.hpss(y)
    rms_harm  = librosa.feature.rms(y=y_harm, hop_length=hop)[0]
    times_all = librosa.frames_to_time(np.arange(len(rms_harm)), sr=sr, hop_length=hop)

    return {
        "bpm":          bpm,
        "detected_bpm": detected_bpm,
        "onset_times":  np.asarray(onset_times, dtype=float),
        "onset_str":    np.asarray(onset_str,   dtype=float),
        "cent_onset":   np.asarray(cent_onset,  dtype=float),
        "rms_harm":     rms_harm,
        "times_all":    times_all,
        "duration":     duration,
        "sr":           sr,
        "hop":          hop,
    }


def row_from_centroid(c, c_min, c_max):
    if c_max <= c_min:
        return 1
    ratio = (c - c_min) / (c_max - c_min)
    return int(np.clip(round(ratio * 2), 0, 2))


def pick_direction(prev_dir, rng):
    candidates = DIRECTION_FLOW.get(prev_dir, ALL_DIRECTIONS)
    weights = [3 if d == prev_dir else 1 for d in candidates]
    total = sum(weights)
    r = rng.random() * total
    cumul = 0
    for d, w in zip(candidates, weights):
        cumul += w
        if r < cumul:
            return d
    return candidates[-1]


def make_notes(times, bpm, rng, min_gap_beats, quantize,
               strengths=None, strength_threshold=0.0,
               centroids=None, c_min=0.0, c_max=1.0):
    notes = []
    last_beat  = -1e9
    color_turn = 0
    prev_dir   = {"red": "down", "blue": "down"}

    order = np.argsort(times)
    for i in order:
        t        = times[i]
        strength = float(strengths[i]) if strengths is not None else 1.0
        if strength < strength_threshold:
            continue

        beat = t * bpm / 60.0
        if quantize > 0:
            beat = round(beat / quantize) * quantize
        if beat - last_beat < min_gap_beats:
            continue
        last_beat = beat

        color     = "red" if color_turn % 2 == 0 else "blue"
        color_turn += 1
        lane_pool = RED_LANES if color == "red" else BLUE_LANES
        lane      = rng.choice(lane_pool)
        row       = row_from_centroid(centroids[i], c_min, c_max) if centroids is not None else 1
        direction = pick_direction(prev_dir[color], rng)
        prev_dir[color] = direction

        notes.append({
            "type":      "normal",
            "beat":      round(beat, 3),
            "duration":  0,
            "lane":      int(lane),
            "row":       int(row),
            "direction": direction,
            "color":     color,
        })

    return notes


def fill_long_notes(notes, bpm, rms_harm, times_all,
                    min_gap_beats=2.5, min_long_beats=1.5,
                    harm_threshold_ratio=0.3):
    """
    노트 사이 갭이 크고 하모닉 성분이 지속되는 구간에 롱노트를 삽입한다.
    기존 normal 노트를 long으로 교체하는 방식 (추가 노트 없음).
    """
    if not notes:
        return notes

    spb = 60.0 / bpm
    harm_max = float(rms_harm.max()) + 1e-9

    result = list(notes)  # 복사
    for i in range(len(result) - 1):
        n    = result[i]
        next_n = result[i + 1]
        gap  = next_n["beat"] - n["beat"]
        if gap < min_gap_beats:
            continue

        # 이 갭 구간 내 하모닉 평균 에너지 계산
        t_start = n["beat"] * spb
        t_end   = next_n["beat"] * spb
        mask    = (times_all >= t_start) & (times_all <= t_end)
        if mask.sum() == 0:
            continue
        harm_mean = float(rms_harm[mask].mean()) / harm_max

        if harm_mean < harm_threshold_ratio:
            continue  # 실제 무음 구간은 롱노트 삽입 안 함

        # 롱노트 duration: 갭의 80%, 최소 min_long_beats
        duration = round(max(gap * 0.8, min_long_beats), 2)
        if duration < min_long_beats:
            continue

        result[i] = {
            **n,
            "type":      "long",
            "duration":  duration,
            "direction": "any",
        }

    return result


def write_json(path, obj):
    with open(path, "w", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)


def main():
    ap = argparse.ArgumentParser(description="mp3 → VRBeat 차트 생성기")
    ap.add_argument("audio",              help="입력 오디오 파일 (mp3/wav/ogg)")
    ap.add_argument("--song-id",          help="곡 폴더 이름 (기본: 파일명)")
    ap.add_argument("--title",            default=None)
    ap.add_argument("--artist",           default="Unknown")
    ap.add_argument("--mapper",           default="auto")
    ap.add_argument("--bpm",              type=float, default=None)
    ap.add_argument("--note-speed",       type=float, default=8.0)
    ap.add_argument("--seed",             type=int,   default=1234)
    ap.add_argument("--easy-threshold",   type=float, default=0.1,
                    help="Easy onset 강도 임계값 0~1 (기본 0.1)")
    ap.add_argument("--normal-threshold", type=float, default=0.1,
                    help="Normal onset 강도 임계값 0~1 (기본 0.1)")
    ap.add_argument("--long-gap",         type=float, default=2.5,
                    help="롱노트 삽입 최소 갭(박자, 기본 2.5)")
    ap.add_argument("--no-long",          action="store_true",
                    help="롱노트 자동 삽입 비활성화")
    ap.add_argument("--out",              default=None)
    args = ap.parse_args()

    import random
    rng  = random.Random(args.seed)
    rng2 = random.Random(args.seed + 1)

    if not os.path.isfile(args.audio):
        sys.exit(f"파일 없음: {args.audio}")

    base    = os.path.splitext(os.path.basename(args.audio))[0]
    song_id = args.song_id or base
    title   = args.title   or base

    if args.out:
        out_root = args.out
    else:
        repo     = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        out_root = os.path.join(repo, "Assets", "StreamingAssets", "Songs")
    out_dir = os.path.join(out_root, song_id)
    os.makedirs(out_dir, exist_ok=True)

    print(f"[1/4] 분석 중: {args.audio}")
    a = analyze(args.audio, forced_bpm=args.bpm)
    bpm = a["bpm"]
    print(f"      BPM={bpm:.2f}  길이={a['duration']:.1f}s  onset={len(a['onset_times'])}개")

    s      = a["onset_str"]
    s_norm = (s - s.min()) / (s.max() - s.min() + 1e-9)
    c      = a["cent_onset"]
    c_min  = float(c.min()) if len(c) > 0 else 0.0
    c_max  = float(c.max()) if len(c) > 0 else 1.0

    print("[2/4] 차트 생성 중")

    easy_raw = make_notes(
        a["onset_times"], bpm, rng,
        min_gap_beats=2.0, quantize=1.0,
        strengths=s_norm, strength_threshold=args.easy_threshold,
        centroids=c, c_min=c_min, c_max=c_max,
    )
    normal_raw = make_notes(
        a["onset_times"], bpm, rng2,
        min_gap_beats=0.25, quantize=0.5,
        strengths=s_norm, strength_threshold=args.normal_threshold,
        centroids=c, c_min=c_min, c_max=c_max,
    )

    if not args.no_long:
        easy_notes   = fill_long_notes(easy_raw,   bpm, a["rms_harm"], a["times_all"],
                                       min_gap_beats=args.long_gap)
        normal_notes = fill_long_notes(normal_raw, bpm, a["rms_harm"], a["times_all"],
                                       min_gap_beats=args.long_gap)
        easy_long   = sum(1 for n in easy_notes   if n["type"] == "long")
        normal_long = sum(1 for n in normal_notes if n["type"] == "long")
        print(f"      Easy={len(easy_notes)}개 (롱노트 {easy_long}개)  "
              f"Normal={len(normal_notes)}개 (롱노트 {normal_long}개)")
    else:
        easy_notes, normal_notes = easy_raw, normal_raw
        print(f"      Easy={len(easy_notes)}개  Normal={len(normal_notes)}개")

    ext       = os.path.splitext(args.audio)[1].lower()
    audio_name = "audio" + ext
    audio_dst  = os.path.join(out_dir, audio_name)
    if os.path.abspath(args.audio) != os.path.abspath(audio_dst):
        shutil.copy(args.audio, audio_dst)
    else:
        print("      오디오가 이미 출력 폴더에 있음. 복사 생략.")

    print("[3/4] info.json / chart_*.json 쓰기")
    write_json(os.path.join(out_dir, "info.json"), {
        "songId":          song_id,
        "title":           title,
        "artist":          args.artist,
        "mapper":          args.mapper,
        "bpm":             round(bpm, 2),
        "audioFile":       audio_name,
        "coverFile":       "cover.png",
        "previewStart":    0.0,
        "previewDuration": 10.0,
        "songOffset":      0.0,
        "difficulties": [
            {"name": "Easy",   "chartFile": "chart_easy.json",   "level": 3},
            {"name": "Normal", "chartFile": "chart_normal.json", "level": 5},
        ],
    })
    write_json(os.path.join(out_dir, "chart_easy.json"),
               {"version": "1.0", "noteSpeed": args.note_speed, "notes": easy_notes})
    write_json(os.path.join(out_dir, "chart_normal.json"),
               {"version": "1.0", "noteSpeed": args.note_speed, "notes": normal_notes})

    print(f"[4/4] 완료 → {out_dir}")
    print("      노트 수 조절: --easy-threshold / --normal-threshold (0~1, 높을수록 적게)")
    print("      롱노트 감도: --long-gap (박자, 낮을수록 더 많은 구간에 삽입)")


if __name__ == "__main__":
    main()
