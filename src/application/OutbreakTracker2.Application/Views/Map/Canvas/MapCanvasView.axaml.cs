using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

// TODO: Map background images and a proper coordinate system are not yet available.
// Circles are placed by scaling raw game-world coordinates onto the canvas size.
// Self   = green  (player slot 0)
// Ally   = blue   (player slots 1-3, human characters)
// NPC    = yellow (player slots with non-zero NameId, i.e. friendly NPCs)
// Enemy  = red    (DecodedEnemy — position data not yet tracked; reserved for future use)
public partial class MapCanvasView : UserControl
{
    private const double CircleRadius = 5;
    private const double MaxMapCoordinate = 25000.0;

    private IDisposable? _playersSubscription;
    private DecodedInGamePlayer[]? _lastPlayers;

    public MapCanvasView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            _playersSubscription?.Dispose();

            if (DataContext is MapCanvasViewModel viewModel)
            {
                _playersSubscription = viewModel
                    .PlayersObservable.ObserveOn(SynchronizationContext.Current)
                    .Subscribe(DrawPlayers);
            }
        };

        GameMapCanvas.SizeChanged += (_, _) =>
        {
            if (_lastPlayers is not null)
                DrawPlayers(_lastPlayers);
        };
    }

    private void DrawPlayers(DecodedInGamePlayer[] players)
    {
        _lastPlayers = players;

        bool anyInGame = false;
        foreach (DecodedInGamePlayer p in players)
        {
            if (p.IsEnabled && p.IsInGame)
            {
                anyInGame = true;
                break;
            }
        }

        if (ViewModel is not null)
            ViewModel.IsInGame = anyInGame;

        GameMapCanvas.Children.Clear();

        if (!anyInGame)
            return;

        double canvasW =
            GameMapCanvas.Bounds.Width > 0 ? GameMapCanvas.Bounds.Width : ViewModel?.MapWidth ?? MaxMapCoordinate;
        double canvasH =
            GameMapCanvas.Bounds.Height > 0 ? GameMapCanvas.Bounds.Height : ViewModel?.MapHeight ?? MaxMapCoordinate;

        double scaleX = canvasW / MaxMapCoordinate;
        double scaleY = canvasH / MaxMapCoordinate;

        for (int i = 0; i < players.Length; i++)
        {
            DecodedInGamePlayer player = players[i];
            if (!player.IsEnabled || !player.IsInGame)
                continue;

            bool isSelf = i == 0;
            bool isFriendlyNpc = player.NameId != 0;

            IBrush fill =
                isSelf ? Brushes.Green
                : isFriendlyNpc ? Brushes.Yellow
                : Brushes.Blue;

            IBrush stroke =
                isSelf ? Brushes.DarkGreen
                : isFriendlyNpc ? Brushes.DarkGoldenrod
                : Brushes.DarkBlue;

            double cx = player.PositionX * scaleX;
            double cy = player.PositionY * scaleY;

            Ellipse circle = new()
            {
                Width = CircleRadius * 2,
                Height = CircleRadius * 2,
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = 1,
            };

            Avalonia.Controls.Canvas.SetLeft(circle, cx - CircleRadius);
            Avalonia.Controls.Canvas.SetTop(circle, cy - CircleRadius);

            TextBlock label = new()
            {
                Text = player.Name,
                Foreground = Brushes.White,
                FontSize = 9,
            };

            Avalonia.Controls.Canvas.SetLeft(label, cx + CircleRadius + 1);
            Avalonia.Controls.Canvas.SetTop(label, cy - 5);

            GameMapCanvas.Children.Add(circle);
            GameMapCanvas.Children.Add(label);
        }
    }

    protected void OnUnloaded()
    {
        _playersSubscription?.Dispose();
        _playersSubscription = null;
        _lastPlayers = null;

        base.OnUnloaded(null!);
    }

    private MapCanvasViewModel? ViewModel => DataContext as MapCanvasViewModel;
}
