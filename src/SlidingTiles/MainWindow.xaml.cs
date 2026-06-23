using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SlidingTiles;

/// <summary>
/// The view: turns the <see cref="PuzzleBoard"/> model into clickable, animated tiles on a Canvas.
/// </summary>
public partial class MainWindow : Window
{
    private const int N = 4;            // board side
    private const double TileSize = 84; // cell pitch in pixels
    private const double Gap = 4;       // visual gap between tiles

    private static readonly Duration SlideDuration = new(TimeSpan.FromMilliseconds(110));

    private readonly PuzzleBoard _board = new(N);
    private readonly Dictionary<int, Border> _tiles = new();
    private readonly Random _random = new();
    private int _moveCount;

    public MainWindow()
    {
        InitializeComponent();

        Board.Width = (N * TileSize) + Gap;
        Board.Height = (N * TileSize) + Gap;

        BuildTiles();
        SyncPositions(animate: false);
    }

    /// <summary>Creates one <see cref="Border"/> per tile value and wires up its click.</summary>
    private void BuildTiles()
    {
        for (var value = 1; value < N * N; value++)
        {
            var tile = new Border
            {
                Width = TileSize - Gap,
                Height = TileSize - Gap,
                CornerRadius = new CornerRadius(8),
                Background = TileBrush(value),
                Cursor = Cursors.Hand,
                Tag = value,
                Child = new TextBlock
                {
                    Text = value.ToString(),
                    Foreground = Brushes.White,
                    FontSize = 26,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };

            tile.MouseLeftButtonDown += OnTileClicked;
            _tiles[value] = tile;
            Board.Children.Add(tile);
        }
    }

    private void OnTileClicked(object sender, MouseButtonEventArgs e)
    {
        var value = (int)((Border)sender).Tag;
        var (row, column) = _board.LocationOf(value);

        if (!_board.TrySlide(row, column))
            return;

        _moveCount++;
        SyncPositions(animate: true);

        if (_board.IsSolved)
            Status.Text = $"Solved in {_moveCount} moves! 🎉";
        else
            Status.Text = $"Moves: {_moveCount}";
    }

    private void OnShuffle(object sender, RoutedEventArgs e)
    {
        _board.Shuffle(moves: 200, _random);
        _moveCount = 0;
        SyncPositions(animate: true);
        Status.Text = "Shuffled — slide the tiles back into order.";
    }

    /// <summary>Moves every tile to the pixel position implied by its cell in the model.</summary>
    private void SyncPositions(bool animate)
    {
        foreach (var (value, tile) in _tiles)
        {
            var (row, column) = _board.LocationOf(value);
            MoveTile(tile, (column * TileSize) + Gap, (row * TileSize) + Gap, animate);
        }
    }

    /// <summary>
    /// Slides a tile to (<paramref name="x"/>, <paramref name="y"/>). Animations use
    /// <see cref="FillBehavior.Stop"/> and write the final value on completion, so the Canvas attached
    /// property stays the single source of truth and successive slides never fight each other.
    /// </summary>
    private static void MoveTile(Border tile, double x, double y, bool animate)
    {
        if (!animate)
        {
            tile.BeginAnimation(Canvas.LeftProperty, null);
            tile.BeginAnimation(Canvas.TopProperty, null);
            Canvas.SetLeft(tile, x);
            Canvas.SetTop(tile, y);
            return;
        }

        Animate(tile, Canvas.LeftProperty, x);
        Animate(tile, Canvas.TopProperty, y);
    }

    private static void Animate(Border tile, DependencyProperty property, double to)
    {
        var from = (double)tile.GetValue(property);
        if (double.IsNaN(from))
            from = to;

        var animation = new DoubleAnimation(from, to, SlideDuration)
        {
            FillBehavior = FillBehavior.Stop,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };

        animation.Completed += (_, _) =>
        {
            tile.BeginAnimation(property, null);
            tile.SetValue(property, to);
        };

        tile.BeginAnimation(property, animation);
    }

    /// <summary>A cool-to-warm colour ramp across the tile numbers, just for legibility and charm.</summary>
    private static Brush TileBrush(int value)
    {
        var t = (value - 1) / (double)(N * N - 2);
        var from = Color.FromRgb(0x2F, 0x6F, 0xED);   // blue
        var to = Color.FromRgb(0x18, 0xA9, 0x99);     // teal
        var colour = Color.FromRgb(
            (byte)(from.R + ((to.R - from.R) * t)),
            (byte)(from.G + ((to.G - from.G) * t)),
            (byte)(from.B + ((to.B - from.B) * t)));
        return new SolidColorBrush(colour);
    }
}
