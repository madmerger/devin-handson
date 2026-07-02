namespace WindowsTetris.Game;

public static class Tetromino
{
    // Each piece: array of rotations, each rotation is array of (row, col) offsets
    // 7 standard tetrominoes: I, O, T, S, Z, J, L
    public static readonly int[][][][] Shapes =
    {
        // I
        new[]
        {
            new[] { new[] {0,0}, new[] {0,1}, new[] {0,2}, new[] {0,3} },
            new[] { new[] {0,0}, new[] {1,0}, new[] {2,0}, new[] {3,0} },
            new[] { new[] {0,0}, new[] {0,1}, new[] {0,2}, new[] {0,3} },
            new[] { new[] {0,0}, new[] {1,0}, new[] {2,0}, new[] {3,0} },
        },
        // O
        new[]
        {
            new[] { new[] {0,0}, new[] {0,1}, new[] {1,0}, new[] {1,1} },
            new[] { new[] {0,0}, new[] {0,1}, new[] {1,0}, new[] {1,1} },
            new[] { new[] {0,0}, new[] {0,1}, new[] {1,0}, new[] {1,1} },
            new[] { new[] {0,0}, new[] {0,1}, new[] {1,0}, new[] {1,1} },
        },
        // T
        new[]
        {
            new[] { new[] {0,1}, new[] {1,0}, new[] {1,1}, new[] {1,2} },
            new[] { new[] {0,0}, new[] {1,0}, new[] {1,1}, new[] {2,0} },
            new[] { new[] {0,0}, new[] {0,1}, new[] {0,2}, new[] {1,1} },
            new[] { new[] {0,1}, new[] {1,0}, new[] {1,1}, new[] {2,1} },
        },
        // S
        new[]
        {
            new[] { new[] {0,1}, new[] {0,2}, new[] {1,0}, new[] {1,1} },
            new[] { new[] {0,0}, new[] {1,0}, new[] {1,1}, new[] {2,1} },
            new[] { new[] {0,1}, new[] {0,2}, new[] {1,0}, new[] {1,1} },
            new[] { new[] {0,0}, new[] {1,0}, new[] {1,1}, new[] {2,1} },
        },
        // Z
        new[]
        {
            new[] { new[] {0,0}, new[] {0,1}, new[] {1,1}, new[] {1,2} },
            new[] { new[] {0,1}, new[] {1,0}, new[] {1,1}, new[] {2,0} },
            new[] { new[] {0,0}, new[] {0,1}, new[] {1,1}, new[] {1,2} },
            new[] { new[] {0,1}, new[] {1,0}, new[] {1,1}, new[] {2,0} },
        },
        // J
        new[]
        {
            new[] { new[] {0,0}, new[] {1,0}, new[] {1,1}, new[] {1,2} },
            new[] { new[] {0,0}, new[] {0,1}, new[] {1,0}, new[] {2,0} },
            new[] { new[] {0,0}, new[] {0,1}, new[] {0,2}, new[] {1,2} },
            new[] { new[] {0,1}, new[] {1,1}, new[] {2,0}, new[] {2,1} },
        },
        // L
        new[]
        {
            new[] { new[] {0,2}, new[] {1,0}, new[] {1,1}, new[] {1,2} },
            new[] { new[] {0,0}, new[] {1,0}, new[] {2,0}, new[] {2,1} },
            new[] { new[] {0,0}, new[] {0,1}, new[] {0,2}, new[] {1,0} },
            new[] { new[] {0,0}, new[] {0,1}, new[] {1,1}, new[] {2,1} },
        },
    };

    public static readonly CellType[] Colors =
    {
        CellType.I, // cyan
        CellType.O, // yellow
        CellType.T, // purple
        CellType.S, // green
        CellType.Z, // red
        CellType.J, // blue
        CellType.L, // orange
    };

    public const int Count = 7;
}
