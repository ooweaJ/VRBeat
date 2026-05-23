# 자동 채보 생성기 (mp3 → VRBeat 차트)

mp3/wav/ogg 한 곡을 분석해 게임이 그대로 읽는 차트(`info.json` + `chart_*.json`)를 만든다.
**게임 코드는 바꾸지 않는다.** 출력은 `Assets/StreamingAssets/Songs/<song_id>/` 에 생성된다.

## 1. 설치 (최초 1회)

Python 3.9+ 필요. mp3 디코딩에는 ffmpeg 가 있으면 가장 안정적이다.

```bash
cd Tools
python -m venv .venv
# Windows PowerShell
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

> mp3 로딩 오류가 나면 ffmpeg 설치: `winget install Gyan.FFmpeg` (Windows).

## 2. 실행

```bash
python generate_beatmap.py "C:\path\to\song.mp3" --title "My Song" --artist "Someone"
```

자주 쓰는 옵션:

| 옵션 | 설명 |
|---|---|
| `--song-id song_002` | 곡 폴더 이름(기본: 파일명) |
| `--bpm 128` | BPM 강제 지정 (검출이 절반/2배로 틀릴 때) |
| `--note-speed 10` | 노트 속도 |
| `--seed 42` | 패턴 재현용 시드 |
| `--out <경로>` | 출력 루트 변경 |

실행하면 `Songs/<song_id>/` 에 `audio.*`, `info.json`, `chart_easy.json`, `chart_normal.json` 가 생성된다.

## 3. 게임에 반영

Unity 로 돌아와 에셋 새로고침(자동 또는 Ctrl+R) → SongSelect 곡 목록에 새 곡이 나타난다.

## 동작 원리 (요약)

- `librosa.beat.beat_track` 으로 BPM + 비트 시각, `librosa.onset.onset_detect` 로 타격 지점(온셋)을 검출.
- `songOffset = 0`, 각 노트 `beat = time(초) * bpm / 60`. → Conductor 가 beat 를 시간으로 환산하면 그 오디오 지점에 노트가 도달.
- Easy = 비트마다(듬성), Normal = 온셋마다(촘촘, 1/2박 양자화). 색은 좌우 교대, lane/방향은 시드 기반 패턴.

## 한계 / 튜닝

- **고정 BPM 가정**: 현재 게임은 단일 BPM 격자라 변박 곡은 뒤로 갈수록 어긋날 수 있다.
- 박자가 전체적으로 밀리면 `info.json` 의 `songOffset` 을 ±0.02 단위로 조정.
- BPM 이 절반/두 배로 잡히면 `--bpm` 으로 강제.
- lane/방향/색은 음악적 의미가 아니라 패턴 생성이다(플레이감 위주). 더 정교한 배치는 madmom/규칙 기반으로 확장 가능.
