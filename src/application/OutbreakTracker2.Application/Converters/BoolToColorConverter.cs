using Avalonia.Data.Converters;
using Avalonia.Media;

namespace OutbreakTracker2.Application.Converters;

public static class BoolToColorConverter
{
    /// <summary>
    /// Returns <see cref="Brushes.OrangeRed"/> when the bound bool is true (enemy faction NPC),
    /// <see cref="Brushes.White"/> when false.
    /// </summary>
    public static readonly FuncValueConverter<bool, IBrush> EnemyFaction = new(isEnemy =>
        isEnemy ? Brushes.OrangeRed : Brushes.White
    );
}
