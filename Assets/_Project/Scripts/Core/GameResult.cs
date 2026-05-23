/// <summary>
/// 한 곡 플레이 결과 스냅샷. ScoreManager는 씬 단위 싱글톤이라 Gameplay→Result
/// 전환 시 파괴되므로, 전환 직전에 이 스냅샷으로 값을 떠서 DontDestroyOnLoad인
/// GameManager가 들고 Result 씬으로 넘긴다.
/// </summary>
public class GameResult
{
    public int   score;
    public int   maxCombo;
    public int   totalHits;
    public int   perfectHits;
    public int   missedHits;
    public float accuracy;
    public bool  fullCombo;
    public bool  isNewRecord;
}
