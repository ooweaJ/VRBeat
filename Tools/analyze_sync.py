#!/usr/bin/env python3
"""
채보 싱크 품질 분석기
- 각 노트의 beat 시간 vs 실제 오디오 onset 시간 오차 측정
- 띄엄띄엄 구간 (긴 갭) 감지
- 사용: python analyze_sync.py <song_folder>
"""

import json, os, sys
import numpy as np

try:
    import librosa
except ImportError:
    sys.exit("pip install librosa")

def analyze_song(song_folder):
    # info.json
    info_path = os.path.join(song_folder, "info.json")
    if not os.path.exists(info_path):
        print(f"info.json 없음: {song_folder}")
        return
    with open(info_path) as f:
        info = json.load(f)

    bpm    = info["bpm"]
    spb    = 60.0 / bpm   # seconds per beat

    audio_path = os.path.join(song_folder, info["audioFile"])
    print(f"\n{'='*60}")
    print(f"곡: {info['title']}  BPM={bpm}")
    print(f"오디오: {audio_path}")

    y, sr = librosa.load(audio_path, mono=True)
    duration = librosa.get_duration(y=y, sr=sr)

    # onset 감지
    onset_env    = librosa.onset.onset_strength(y=y, sr=sr)
    onset_frames = librosa.onset.onset_detect(y=y, sr=sr, backtrack=True)
    onset_times  = librosa.frames_to_time(onset_frames, sr=sr)
    onset_str    = onset_env[onset_frames]
    onset_str_n  = (onset_str - onset_str.min()) / (onset_str.max() - onset_str.min() + 1e-9)

    print(f"오디오 길이: {duration:.1f}s  |  검출 onset: {len(onset_times)}개")

    # 각 난이도 분석
    for diff in info.get("difficulties", []):
        chart_path = os.path.join(song_folder, diff["chartFile"])
        if not os.path.exists(chart_path):
            continue
        with open(chart_path) as f:
            chart = json.load(f)

        notes = chart.get("notes", [])
        if not notes:
            continue

        print(f"\n  [{diff['name']}]  노트 {len(notes)}개")

        # 각 노트의 실제 시간 계산
        note_times = np.array([n["beat"] * spb for n in notes])

        # 각 노트와 가장 가까운 onset 오차
        errors_ms = []
        for nt in note_times:
            diffs_arr = np.abs(onset_times - nt)
            nearest_err = diffs_arr.min() * 1000  # ms
            errors_ms.append(nearest_err)
        errors_ms = np.array(errors_ms)

        print(f"  싱크 오차(ms): 평균={errors_ms.mean():.1f}  중앙값={np.median(errors_ms):.1f}  최대={errors_ms.max():.1f}")
        print(f"  오차 50ms 이내: {(errors_ms < 50).sum()}/{len(errors_ms)} ({(errors_ms < 50).mean()*100:.0f}%)")
        print(f"  오차 100ms 이내: {(errors_ms < 100).sum()}/{len(errors_ms)} ({(errors_ms < 100).mean()*100:.0f}%)")

        # 갭 분석 (같은 색 노트 사이)
        beats = sorted([n["beat"] for n in notes])
        gaps  = np.diff(beats)
        big_gaps = gaps[gaps > 4.0]   # 4박 이상 공백
        if len(big_gaps) > 0:
            print(f"  4박 이상 공백: {len(big_gaps)}개  (최대 {big_gaps.max():.1f}박 = {big_gaps.max()*spb:.1f}s)")
            # 어디서 공백인지
            gap_positions = [(beats[i], beats[i+1], gaps[i]) for i in range(len(gaps)) if gaps[i] > 4.0]
            for start, end, g in gap_positions[:5]:
                t_start = start * spb
                t_end   = end   * spb
                print(f"    beat {start:.1f}~{end:.1f}  ({t_start:.1f}s~{t_end:.1f}s, {g:.1f}박 공백)")
        else:
            print(f"  4박 이상 공백: 없음")

        # 첫 노트 / 마지막 노트 vs 오디오 길이
        first_t = note_times[0]
        last_t  = note_times[-1]
        print(f"  첫 노트: {first_t:.1f}s  마지막 노트: {last_t:.1f}s  (오디오 {duration:.1f}s)")
        if duration - last_t > 5.0:
            print(f"  ⚠ 마지막 노트 이후 {duration - last_t:.1f}s 공백 (노래 끝까지 노트 없음)")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        # 인자 없으면 전체 Songs 폴더 스캔
        repo     = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        songs_dir = os.path.join(repo, "Assets", "StreamingAssets", "Songs")
        folders  = [os.path.join(songs_dir, d) for d in os.listdir(songs_dir)
                    if os.path.isdir(os.path.join(songs_dir, d))]
    else:
        folders = [sys.argv[1]]

    for folder in sorted(folders):
        analyze_song(folder)
