namespace SlidingTiles;

/// <summary>
/// The pure model of an <c>n × n</c> sliding-tile puzzle — no WPF, no pixels, just where each tile
/// sits. The view reads this to lay tiles out and animate them; keeping it UI-free makes the rules
/// easy to reason about and to test.
/// </summary>
/// <remarks>
/// Cells are stored row-major in a flat array. Each cell holds a tile number 1…n²-1, except for the
/// single blank, which holds <see cref="Blank"/> (0). A click slides a whole line of tiles toward the
/// blank, so a tile anywhere in the blank's row or column is a legal move — not just an adjacent one.
/// </remarks>
public sealed class PuzzleBoard
{
    /// <summary>The value stored in the empty cell.</summary>
    public const int Blank = 0;

    private readonly int[] _cells;

    /// <summary>Creates a solved board of the given side length (default 4 → the classic 15-puzzle).</summary>
    public PuzzleBoard(int size = 4)
    {
        Size = size;
        _cells = new int[size * size];
        for (var i = 0; i < _cells.Length - 1; i++)
            _cells[i] = i + 1;
        _cells[^1] = Blank;   // blank starts bottom-right
    }

    /// <summary>The side length of the board.</summary>
    public int Size { get; }

    /// <summary>The tile number at <paramref name="row"/>, <paramref name="column"/> (or <see cref="Blank"/>).</summary>
    public int this[int row, int column] => _cells[(row * Size) + column];

    /// <summary>True when every tile is back in its home cell.</summary>
    public bool IsSolved
    {
        get
        {
            for (var i = 0; i < _cells.Length - 1; i++)
                if (_cells[i] != i + 1)
                    return false;
            return _cells[^1] == Blank;
        }
    }

    /// <summary>Finds the (row, column) of a tile value.</summary>
    public (int Row, int Column) LocationOf(int value)
    {
        var index = Array.IndexOf(_cells, value);
        return (index / Size, index % Size);
    }

    /// <summary>
    /// Attempts to slide the tile at <paramref name="row"/>, <paramref name="column"/> toward the
    /// blank. Legal only when the tile shares the blank's row or column; the whole line between them
    /// shifts one step. Returns <see langword="true"/> if the board changed.
    /// </summary>
    public bool TrySlide(int row, int column)
    {
        var (blankRow, blankColumn) = LocationOf(Blank);

        if (row == blankRow)
        {
            ShiftLine(blankColumn, column, c => Index(row, c));
            return column != blankColumn;
        }

        if (column == blankColumn)
        {
            ShiftLine(blankRow, row, r => Index(r, column));
            return row != blankRow;
        }

        return false;   // not in line with the blank — ignored
    }

    /// <summary>
    /// Produces a guaranteed-solvable scramble by making <paramref name="moves"/> random legal slides
    /// from the current position. (Random *placements* can be unsolvable; random *moves* never are.)
    /// </summary>
    public void Shuffle(int moves, Random random)
    {
        for (var i = 0; i < moves; i++)
        {
            var (blankRow, blankColumn) = LocationOf(Blank);

            // Pick a random tile sharing the blank's row or column (never the blank itself).
            if (random.Next(2) == 0)
            {
                var c = random.Next(Size);
                if (c != blankColumn)
                    TrySlide(blankRow, c);
            }
            else
            {
                var r = random.Next(Size);
                if (r != blankRow)
                    TrySlide(r, blankColumn);
            }
        }
    }

    private int Index(int row, int column) => (row * Size) + column;

    /// <summary>
    /// Pulls tiles one step from <paramref name="from"/> toward <paramref name="to"/>, leaving the
    /// blank at <paramref name="to"/>. <paramref name="cellAt"/> maps a line coordinate to a flat index
    /// so the same logic serves both rows and columns.
    /// </summary>
    private void ShiftLine(int from, int to, Func<int, int> cellAt)
    {
        var step = Math.Sign(from - to);   // walk from the blank toward the clicked cell
        for (var i = from; i != to; i -= step)
            _cells[cellAt(i)] = _cells[cellAt(i - step)];
        _cells[cellAt(to)] = Blank;
    }
}
