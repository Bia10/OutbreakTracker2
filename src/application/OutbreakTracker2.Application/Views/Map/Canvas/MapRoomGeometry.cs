using System.Globalization;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal sealed class MapRoomGeometry(short roomId, string slug, MapRoomShape shape, string coords)
{
    public short RoomId { get; } = roomId;

    public string Slug { get; } = slug;

    public MapRoomShape Shape { get; } = shape;

    public int[] Coordinates { get; } = ParseCoordinates(coords);

    public bool Matches(short roomId, string roomSlug) =>
        RoomId == roomId || string.Equals(Slug, roomSlug, StringComparison.OrdinalIgnoreCase);

    private static int[] ParseCoordinates(string coords)
    {
        string[] values = coords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int[] parsedValues = new int[values.Length];

        for (int index = 0; index < values.Length; index++)
            parsedValues[index] = int.Parse(values[index], CultureInfo.InvariantCulture);

        return parsedValues;
    }
}
