using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WindowsTetris.Game;

namespace WindowsTetris;

public partial class MainWindow : Window
{
    private const int CellSize = 30;
    private const int NextCellSize = 20;

    private readonly GameEngine _engine = new();
    private readonly DispatcherTimer _gameTimer = new();
    private readonly DispatcherTimer _autoRepeatLeft = new();
    private readonly DispatcherTimer _autoRepeatRight = new();

    private bool _softDropping;
    private CancellationTokenSource? _stageClearCts;

    private static readonly Dictionary<CellType, Color> CellColors = new()
    {
        { CellType.Empty, Color.FromRgb(22, 33, 62) },
        { CellType.I, Color.FromRgb(0, 240, 240) },
        { CellType.O, Color.FromRgb(240, 240, 0) },
        { CellType.T, Color.FromRgb(160, 0, 240) },
        { CellType.S, Color.FromRgb(0, 240, 0) },
        { CellType.Z, Color.FromRgb(240, 0, 0) },
        { CellType.J, Color.FromRgb(0, 0, 240) },
        { CellType.L, Color.FromRgb(240, 160, 0) },
        { CellType.Gem, Color.FromRgb(255, 165, 0) },
        { CellType.Ghost, Color.FromRgb(60, 60, 90) },
    };

    public MainWindow()
    {
        InitializeComponent();

        _gameTimer.Tick += GameTimer_Tick;

        _autoRepeatLeft.Interval = TimeSpan.FromMilliseconds(80);
        _autoRepeatLeft.Tick += (_, _) => { _engine.MoveLeft(); Render(); };

        _autoRepeatRight.Interval = TimeSpan.FromMilliseconds(80);
        _autoRepeatRight.Tick += (_, _) => { _engine.MoveRight(); Render(); };

        _engine.GameOverEvent += () => Dispatcher.Invoke(ShowGameOver);
        _engine.GameClearEvent += () => Dispatcher.Invoke(ShowGameClear);
    }

    private void StageMode_Click(object sender, RoutedEventArgs e)
    {
        StartGame(GameMode.Stage);
    }

    private void InfiniteMode_Click(object sender, RoutedEventArgs e)
    {
        StartGame(GameMode.Infinite);
    }

    private void StartGame(GameMode mode)
    {
        ModeSelectionPanel.Visibility = Visibility.Collapsed;
        GamePanel.Visibility = Visibility.Visible;
        OverlayPanel.Visibility = Visibility.Collapsed;

        GameModeLabel.Text = mode == GameMode.Stage ? "S T A G E  M O D E" : "I N F I N I T E  M O D E";
        StagePanel.Visibility = mode == GameMode.Stage ? Visibility.Visible : Visibility.Collapsed;

        _engine.StartNewGame(mode);
        UpdateTimerInterval();
        _gameTimer.Start();
        Render();
        Focus();
    }

    private void UpdateTimerInterval()
    {
        _gameTimer.Interval = TimeSpan.FromSeconds(
            _softDropping ? _engine.FallInterval / 10 : _engine.FallInterval);
    }

    private void GameTimer_Tick(object? sender, EventArgs e)
    {
        if (_engine.Status != GameStatus.Playing) return;

        _engine.PlayTimeSeconds += _gameTimer.Interval.TotalSeconds;
        _engine.MoveDown();
        UpdateTimerInterval();
        UpdateInfoPanel();
        Render();
    }

    private void Render()
    {
        GameCanvas.Children.Clear();

        // Draw grid lines
        for (int r = 0; r <= GameEngine.Rows; r++)
        {
            var line = new Line
            {
                X1 = 0, Y1 = r * CellSize,
                X2 = GameEngine.Cols * CellSize, Y2 = r * CellSize,
                Stroke = new SolidColorBrush(Color.FromRgb(30, 30, 50)),
                StrokeThickness = 0.5
            };
            GameCanvas.Children.Add(line);
        }
        for (int c = 0; c <= GameEngine.Cols; c++)
        {
            var line = new Line
            {
                X1 = c * CellSize, Y1 = 0,
                X2 = c * CellSize, Y2 = GameEngine.Rows * CellSize,
                Stroke = new SolidColorBrush(Color.FromRgb(30, 30, 50)),
                StrokeThickness = 0.5
            };
            GameCanvas.Children.Add(line);
        }

        // Draw locked cells
        for (int r = 0; r < GameEngine.Rows; r++)
        {
            for (int c = 0; c < GameEngine.Cols; c++)
            {
                var cell = _engine.GetCell(r, c);
                if (cell != CellType.Empty)
                {
                    DrawCell(r, c, CellColors[cell], cell == CellType.Gem);
                }
            }
        }

        if (_engine.Status == GameStatus.Playing)
        {
            // Draw ghost piece
            var ghostPositions = _engine.GetGhostPositions();
            foreach (var (gr, gc) in ghostPositions)
            {
                if (gr >= 0 && gr < GameEngine.Rows)
                    DrawCell(gr, gc, CellColors[CellType.Ghost], false, true);
            }

            // Draw current piece
            var cells = _engine.GetCurrentPieceCells();
            var color = CellColors[Tetromino.Colors[_engine.CurrentPiece]];
            foreach (var cell in cells)
            {
                int r = _engine.CurrentRow + cell[0];
                int c = _engine.CurrentCol + cell[1];
                if (r >= 0 && r < GameEngine.Rows)
                    DrawCell(r, c, color);
            }
        }

        RenderNextPiece();
        UpdateInfoPanel();
    }

    private void DrawCell(int row, int col, Color color, bool isGem = false, bool isGhost = false)
    {
        var rect = new Rectangle
        {
            Width = CellSize - 1,
            Height = CellSize - 1,
            RadiusX = 3,
            RadiusY = 3,
        };

        if (isGhost)
        {
            rect.Stroke = new SolidColorBrush(Color.FromArgb(120, 150, 150, 200));
            rect.StrokeThickness = 1.5;
            rect.Fill = new SolidColorBrush(Color.FromArgb(30, 150, 150, 200));
        }
        else if (isGem)
        {
            rect.Fill = new SolidColorBrush(Color.FromRgb(255, 165, 0));
            rect.Stroke = new SolidColorBrush(Color.FromRgb(255, 215, 0));
            rect.StrokeThickness = 2;
        }
        else
        {
            var lighter = Color.FromRgb(
                (byte)Math.Min(255, color.R + 40),
                (byte)Math.Min(255, color.G + 40),
                (byte)Math.Min(255, color.B + 40));
            rect.Fill = new LinearGradientBrush(lighter, color, 45);
            rect.Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
            rect.StrokeThickness = 0.5;
        }

        Canvas.SetLeft(rect, col * CellSize + 0.5);
        Canvas.SetTop(rect, row * CellSize + 0.5);
        GameCanvas.Children.Add(rect);
    }

    private void RenderNextPiece()
    {
        NextPieceCanvas.Children.Clear();
        var cells = Tetromino.Shapes[_engine.NextPiece][0];
        var color = CellColors[Tetromino.Colors[_engine.NextPiece]];

        // Center the piece in the preview
        int minR = int.MaxValue, maxR = int.MinValue, minC = int.MaxValue, maxC = int.MinValue;
        foreach (var cell in cells)
        {
            minR = Math.Min(minR, cell[0]);
            maxR = Math.Max(maxR, cell[0]);
            minC = Math.Min(minC, cell[1]);
            maxC = Math.Max(maxC, cell[1]);
        }

        double pieceWidth = (maxC - minC + 1) * NextCellSize;
        double pieceHeight = (maxR - minR + 1) * NextCellSize;
        double offsetX = (NextPieceCanvas.Width - pieceWidth) / 2;
        double offsetY = (NextPieceCanvas.Height - pieceHeight) / 2;

        foreach (var cell in cells)
        {
            var lighter = Color.FromRgb(
                (byte)Math.Min(255, color.R + 40),
                (byte)Math.Min(255, color.G + 40),
                (byte)Math.Min(255, color.B + 40));

            var rect = new Rectangle
            {
                Width = NextCellSize - 1,
                Height = NextCellSize - 1,
                RadiusX = 2,
                RadiusY = 2,
                Fill = new LinearGradientBrush(lighter, color, 45),
                Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                StrokeThickness = 0.5,
            };

            Canvas.SetLeft(rect, offsetX + (cell[1] - minC) * NextCellSize);
            Canvas.SetTop(rect, offsetY + (cell[0] - minR) * NextCellSize);
            NextPieceCanvas.Children.Add(rect);
        }
    }

    private void UpdateInfoPanel()
    {
        ScoreText.Text = _engine.Score.ToString();
        LevelText.Text = _engine.Level.ToString();
        LinesText.Text = _engine.LinesCleared.ToString();
        StageText.Text = (_engine.CurrentStage + 1).ToString();

        var ts = TimeSpan.FromSeconds(_engine.PlayTimeSeconds);
        TimeText.Text = $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}:{(ts.Milliseconds / 10):D2}";
    }

    private void ShowGameOver()
    {
        _gameTimer.Stop();
        _autoRepeatLeft.Stop();
        _autoRepeatRight.Stop();
        OverlayPanel.Visibility = Visibility.Visible;
        OverlayText.Text = "GAME OVER";
        OverlayButton.Visibility = Visibility.Visible;
        OverlayButton.Content = "RESTART";
    }

    private async void ShowGameClear()
    {
        _gameTimer.Stop();
        _autoRepeatLeft.Stop();
        _autoRepeatRight.Stop();

        if (_engine.CurrentStage + 1 >= StageData.Stages.Length)
        {
            OverlayPanel.Visibility = Visibility.Visible;
            OverlayText.Text = "ALL CLEAR!";
            OverlayButton.Visibility = Visibility.Visible;
            OverlayButton.Content = "BACK TO MENU";
            return;
        }

        _stageClearCts?.Cancel();
        _stageClearCts = new CancellationTokenSource();
        var token = _stageClearCts.Token;

        try
        {
            OverlayPanel.Visibility = Visibility.Visible;
            OverlayButton.Visibility = Visibility.Collapsed;
            OverlayText.Text = "STAGE CLEAR!";
            await Task.Delay(500, token);
            OverlayText.Text = "3";
            await Task.Delay(500, token);
            OverlayText.Text = "2";
            await Task.Delay(500, token);
            OverlayText.Text = "1";
            await Task.Delay(500, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        OverlayPanel.Visibility = Visibility.Collapsed;
        _engine.AdvanceStage();
        UpdateTimerInterval();
        _gameTimer.Start();
        Render();
    }

    private void Restart_Click(object sender, RoutedEventArgs e)
    {
        OverlayPanel.Visibility = Visibility.Collapsed;
        if ((string)OverlayButton.Content == "BACK TO MENU")
        {
            BackToMenu();
            return;
        }
        _softDropping = false;
        _engine.StartNewGame(_engine.Mode);
        UpdateTimerInterval();
        _gameTimer.Start();
        Render();
        Focus();
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        if (_engine.Status == GameStatus.Playing)
        {
            _engine.Pause();
            _gameTimer.Stop();
            _autoRepeatLeft.Stop();
            _autoRepeatRight.Stop();
            PauseButton.Content = "RESUME";
            OverlayPanel.Visibility = Visibility.Visible;
            OverlayText.Text = "PAUSED";
            OverlayButton.Visibility = Visibility.Collapsed;
        }
        else if (_engine.Status == GameStatus.Paused)
        {
            _engine.Resume();
            UpdateTimerInterval();
            _gameTimer.Start();
            PauseButton.Content = "PAUSE";
            OverlayPanel.Visibility = Visibility.Collapsed;
        }
        Focus();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        BackToMenu();
    }

    private void BackToMenu()
    {
        _gameTimer.Stop();
        _autoRepeatLeft.Stop();
        _autoRepeatRight.Stop();
        _softDropping = false;
        _stageClearCts?.Cancel();
        GamePanel.Visibility = Visibility.Collapsed;
        OverlayPanel.Visibility = Visibility.Collapsed;
        ModeSelectionPanel.Visibility = Visibility.Visible;
        PauseButton.Content = "PAUSE";
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (_engine.Status == GameStatus.Paused && e.Key == Key.P)
        {
            Pause_Click(sender, e);
            return;
        }

        if (_engine.Status != GameStatus.Playing) return;

        switch (e.Key)
        {
            case Key.Left:
                if (!_autoRepeatLeft.IsEnabled)
                {
                    _engine.MoveLeft();
                    Render();
                    _autoRepeatLeft.Start();
                }
                break;
            case Key.Right:
                if (!_autoRepeatRight.IsEnabled)
                {
                    _engine.MoveRight();
                    Render();
                    _autoRepeatRight.Start();
                }
                break;
            case Key.Down:
                if (!_softDropping)
                {
                    _softDropping = true;
                    UpdateTimerInterval();
                }
                break;
            case Key.Up:
                _engine.Rotate();
                Render();
                break;
            case Key.Space:
                _engine.HardDrop();
                Render();
                break;
            case Key.P:
                Pause_Click(sender, e);
                break;
        }
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                _autoRepeatLeft.Stop();
                break;
            case Key.Right:
                _autoRepeatRight.Stop();
                break;
            case Key.Down:
                _softDropping = false;
                UpdateTimerInterval();
                break;
        }
    }
}
