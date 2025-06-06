using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using OutbreakTracker2.Outbreak.Models;
using R3;
using System;
using System.Threading;

namespace OutbreakTracker2.App.Views.Map.Canvas;

// TODO: Well need to get the map img data then resolving proper scaling onto the canvas
// question remains wherever we will stitch together the map room images or simply use one map image
public partial class MapCanvasView : UserControl
{
    private const double PlayerCircleRadius = 4;
    private const double MaxMapCoordinate = 25000.00000;
    private IDisposable? _playersSubscription;

    public MapCanvasView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            _playersSubscription?.Dispose();

            if (DataContext is MapCanvasViewModel viewModel)
            {
                _playersSubscription = viewModel.PlayersObservable
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(DrawPlayers);
            }
        };
    }

    private void DrawPlayers(DecodedInGamePlayer[] players)
    {
        GameMapCanvas.Children.Clear();

        double scaleX = GameMapCanvas.Width / MaxMapCoordinate;
        double scaleY = GameMapCanvas.Height / MaxMapCoordinate;

        if (GameMapCanvas.Width == 0 || GameMapCanvas.Height == 0)
        {
            scaleX = ViewModel?.MapWidth / MaxMapCoordinate ?? 1;
            scaleY = ViewModel?.MapHeight / MaxMapCoordinate ?? 1;
        }

        foreach (DecodedInGamePlayer? player in players)
        {
            if (player is null) continue;
            if (!player.IsEnabled || !player.IsInGame) continue;

            double canvasX = player.PositionX * scaleX;
            double canvasY = player.PositionY * scaleY;

            Ellipse playerCircle = new()
            {
                Width = PlayerCircleRadius * 2,
                Height = PlayerCircleRadius * 2,
                Fill = Brushes.Blue,
                Stroke = Brushes.DarkBlue,
                StrokeThickness = 1
            };

            Avalonia.Controls.Canvas.SetLeft(playerCircle, canvasX - PlayerCircleRadius);
            Avalonia.Controls.Canvas.SetTop(playerCircle, canvasY - PlayerCircleRadius);

            TextBlock playerInfoText = new()
            {
                Text = $"{player.Name} ({player.PositionX:F0}, {player.PositionY:F0})",
                Foreground = Brushes.White,
                FontSize = 10,
                TextAlignment = TextAlignment.Center
            };

            Avalonia.Controls.Canvas.SetLeft(playerInfoText, canvasX - (PlayerCircleRadius * 2));
            Avalonia.Controls.Canvas.SetTop(playerInfoText, canvasY - (PlayerCircleRadius * 2));

            GameMapCanvas.Children.Add(playerCircle);
            GameMapCanvas.Children.Add(playerInfoText);
        }
    }

    protected void OnUnloaded()
    {
        _playersSubscription?.Dispose();
        _playersSubscription = null;

        base.OnUnloaded(null!);
    }

    private MapCanvasViewModel? ViewModel => DataContext as MapCanvasViewModel;
}