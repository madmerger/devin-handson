namespace WindowsTetris.Game;

public class GameEngine
{
    public const int Rows = 20;
    public const int Cols = 10;

    private static readonly int[] ScoreTable = { 0, 40, 100, 300, 1200 };

    private readonly CellType[,] _grid = new CellType[Rows, Cols];

    private int _currentPiece;
    private int _currentRotation;
    private int _currentRow;
    private int _currentCol;

    private int _nextPiece;
    private readonly List<int> _bag = new();
    private readonly Random _rng = new();

    public GameMode Mode { get; set; } = GameMode.Infinite;
    public GameStatus Status { get; private set; } = GameStatus.Playing;
    public int Score { get; private set; }
    public int Level { get; private set; } = 1;
    public int LinesCleared { get; private set; }
    public int CurrentStage { get; private set; }
    public int GemCount { get; private set; }
    public double PlayTimeSeconds { get; set; }

    public int NextPiece => _nextPiece;
    public int CurrentPiece => _currentPiece;
    public int CurrentRotation => _currentRotation;
    public int CurrentRow => _currentRow;
    public int CurrentCol => _currentCol;

    public double FallInterval { get; private set; } = 0.8;

    public event Action? BoardChanged;
    public event Action<LockResult>? PieceLocked;
    public event Action? GameOverEvent;
    public event Action? GameClearEvent;

    public void Init()
    {
        Array.Clear(_grid, 0, _grid.Length);
        Score = 0;
        Level = 1;
        LinesCleared = 0;
        GemCount = 0;
        PlayTimeSeconds = 0;
        FallInterval = 0.8;
        Status = GameStatus.Playing;

        if (Mode == GameMode.Stage)
        {
            LoadStage(CurrentStage);
        }

        _bag.Clear();
        _nextPiece = DrawFromBag();
        SpawnNext();
    }

    public void StartNewGame(GameMode mode, int stage = 0)
    {
        Mode = mode;
        CurrentStage = stage;
        Init();
    }

    private int DrawFromBag()
    {
        if (_bag.Count == 0)
        {
            for (int i = 0; i < Tetromino.Count; i++)
                _bag.Add(i);
            for (int i = _bag.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
            }
        }
        int piece = _bag[0];
        _bag.RemoveAt(0);
        return piece;
    }

    private void SpawnNext()
    {
        _currentPiece = _nextPiece;
        _nextPiece = DrawFromBag();
        _currentRotation = 0;
        _currentRow = 0;
        _currentCol = 3;

        if (!IsValidPosition(_currentPiece, _currentRotation, _currentRow, _currentCol))
        {
            Status = GameStatus.GameOver;
            GameOverEvent?.Invoke();
        }

        BoardChanged?.Invoke();
    }

    public CellType GetCell(int row, int col)
    {
        return _grid[row, col];
    }

    public int[][] GetCurrentPieceCells()
    {
        return Tetromino.Shapes[_currentPiece][_currentRotation];
    }

    public (int row, int col)[] GetGhostPositions()
    {
        int ghostRow = _currentRow;
        while (IsValidPosition(_currentPiece, _currentRotation, ghostRow + 1, _currentCol))
            ghostRow++;

        var cells = Tetromino.Shapes[_currentPiece][_currentRotation];
        var positions = new (int, int)[cells.Length];
        for (int i = 0; i < cells.Length; i++)
            positions[i] = (ghostRow + cells[i][0], _currentCol + cells[i][1]);
        return positions;
    }

    public bool MoveLeft()
    {
        if (Status != GameStatus.Playing) return false;
        if (IsValidPosition(_currentPiece, _currentRotation, _currentRow, _currentCol - 1))
        {
            _currentCol--;
            BoardChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool MoveRight()
    {
        if (Status != GameStatus.Playing) return false;
        if (IsValidPosition(_currentPiece, _currentRotation, _currentRow, _currentCol + 1))
        {
            _currentCol++;
            BoardChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool MoveDown()
    {
        if (Status != GameStatus.Playing) return false;
        if (IsValidPosition(_currentPiece, _currentRotation, _currentRow + 1, _currentCol))
        {
            _currentRow++;
            BoardChanged?.Invoke();
            return true;
        }
        else
        {
            LockPiece();
            return false;
        }
    }

    public bool Rotate()
    {
        if (Status != GameStatus.Playing) return false;
        int newRotation = (_currentRotation + 1) % 4;

        // Try normal rotation
        if (IsValidPosition(_currentPiece, newRotation, _currentRow, _currentCol))
        {
            _currentRotation = newRotation;
            BoardChanged?.Invoke();
            return true;
        }

        // Wall kick: try shifting left/right by 1
        if (IsValidPosition(_currentPiece, newRotation, _currentRow, _currentCol - 1))
        {
            _currentRotation = newRotation;
            _currentCol--;
            BoardChanged?.Invoke();
            return true;
        }
        if (IsValidPosition(_currentPiece, newRotation, _currentRow, _currentCol + 1))
        {
            _currentRotation = newRotation;
            _currentCol++;
            BoardChanged?.Invoke();
            return true;
        }

        return false;
    }

    public int HardDrop()
    {
        if (Status != GameStatus.Playing) return 0;
        int dropped = 0;
        while (IsValidPosition(_currentPiece, _currentRotation, _currentRow + 1, _currentCol))
        {
            _currentRow++;
            dropped++;
        }
        LockPiece();
        return dropped;
    }

    private void LockPiece()
    {
        var cells = Tetromino.Shapes[_currentPiece][_currentRotation];
        var color = Tetromino.Colors[_currentPiece];

        foreach (var cell in cells)
        {
            int r = _currentRow + cell[0];
            int c = _currentCol + cell[1];
            if (r >= 0 && r < Rows && c >= 0 && c < Cols)
                _grid[r, c] = color;
        }

        int lines = ClearLines();
        int scoreGained = ScoreTable[Math.Min(lines, 4)] * Level;
        Score += scoreGained;
        LinesCleared += lines;
        Level = (LinesCleared / 20) + 1;
        FallInterval = Math.Max(0.05, 0.8 - (Level - 1) * 0.07);

        bool isGameClear = false;
        if (Mode == GameMode.Stage && GemCount <= 0)
        {
            isGameClear = true;
            Status = GameStatus.GameClear;
            GameClearEvent?.Invoke();
        }

        var result = new LockResult
        {
            LinesCleared = lines,
            IsGameOver = false,
            IsGameClear = isGameClear,
            ScoreGained = scoreGained
        };
        PieceLocked?.Invoke(result);

        if (!isGameClear)
            SpawnNext();
    }

    private int ClearLines()
    {
        int cleared = 0;
        for (int r = Rows - 1; r >= 0; r--)
        {
            if (IsLineFull(r))
            {
                // Count gems in this line
                for (int c = 0; c < Cols; c++)
                {
                    if (_grid[r, c] == CellType.Gem)
                        GemCount--;
                }
                RemoveLine(r);
                cleared++;
                r++; // re-check this row
            }
        }
        return cleared;
    }

    private bool IsLineFull(int row)
    {
        for (int c = 0; c < Cols; c++)
            if (_grid[row, c] == CellType.Empty)
                return false;
        return true;
    }

    private void RemoveLine(int row)
    {
        for (int r = row; r > 0; r--)
            for (int c = 0; c < Cols; c++)
                _grid[r, c] = _grid[r - 1, c];
        for (int c = 0; c < Cols; c++)
            _grid[0, c] = CellType.Empty;
    }

    private bool IsValidPosition(int piece, int rotation, int row, int col)
    {
        var cells = Tetromino.Shapes[piece][rotation];
        foreach (var cell in cells)
        {
            int r = row + cell[0];
            int c = col + cell[1];
            if (r < 0 || r >= Rows || c < 0 || c >= Cols)
                return false;
            if (_grid[r, c] != CellType.Empty)
                return false;
        }
        return true;
    }

    private void LoadStage(int stageIndex)
    {
        if (stageIndex >= StageData.Stages.Length)
        {
            Status = GameStatus.GameClear;
            return;
        }
        GemCount = 0;
        var stage = StageData.Stages[stageIndex];
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                int val = stage[r, c];
                switch (val)
                {
                    case 0:
                        _grid[r, c] = CellType.Empty;
                        break;
                    case 1:
                        _grid[r, c] = CellType.J; // gray block
                        break;
                    case 2:
                        _grid[r, c] = CellType.Gem;
                        GemCount++;
                        break;
                }
            }
        }
    }

    public void AdvanceStage()
    {
        CurrentStage++;
        if (CurrentStage >= StageData.Stages.Length)
        {
            Status = GameStatus.GameClear;
            return;
        }
        Init();
    }

    public void Pause()
    {
        if (Status == GameStatus.Playing)
            Status = GameStatus.Paused;
    }

    public void Resume()
    {
        if (Status == GameStatus.Paused)
            Status = GameStatus.Playing;
    }
}
