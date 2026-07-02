namespace WindowsTetris.Game;

public class LockResult
{
    public int LinesCleared { get; init; }
    public bool IsGameOver { get; init; }
    public bool IsGameClear { get; init; }
    public int ScoreGained { get; init; }
}
