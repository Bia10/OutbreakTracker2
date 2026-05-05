using System.Collections.Concurrent;
using System.Globalization;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal static class ScenarioMapGeometryRenderer
{
    private static readonly byte[] BackgroundColor = [0, 0, 0, 255];
    private static readonly byte[] RoomFillColor = [28, 28, 28, 255];
    private static readonly byte[] RoomOutlineColor = [215, 215, 215, 255];
    private static readonly byte[] ActiveRoomFillColor = [0, 128, 0, 255];
    private static readonly byte[] DetailDarkColor = [0, 51, 0, 255];
    private static readonly byte[] DetailLightColor = [153, 153, 153, 255];
    private static readonly byte[] DoorFillColor = [51, 153, 204, 255];
    private static readonly byte[] TransitionMarkerOutlineColor = [24, 24, 24, 255];
    private static readonly byte[] TransitionMarkerColor = [255, 208, 64, 255];
    private static readonly IReadOnlyDictionary<string, MapSectionGeometry> Sections =
        EndOfTheRoadMapGeometry.CreateSections();
    private static readonly ConcurrentDictionary<string, Bitmap?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static bool TryLoadBitmap(
        string scenarioName,
        string? relativePath,
        short roomId,
        string roomName,
        out Bitmap? bitmap
    )
    {
        if (!TryResolveGeometry(scenarioName, relativePath, roomId, roomName, out MapSectionGeometry? section, out _))
        {
            bitmap = null;
            return false;
        }

        string roomSlug = MapAssetNameUtility.Slugify(roomName);
        string groupKey = ResolveGroupKey(scenarioName, relativePath, roomId)!;
        string cacheKey = string.Concat(groupKey, "|", roomId.ToString(CultureInfo.InvariantCulture), "|", roomSlug);
        if (Cache.TryGetValue(cacheKey, out bitmap))
            return bitmap is not null;

        bitmap = RenderSection(scenarioName, section!, roomId, roomSlug);
        Cache[cacheKey] = bitmap;
        return bitmap is not null;
    }

    internal static bool TryResolveGeometry(
        string scenarioName,
        string? relativePath,
        short roomId,
        string roomName,
        out MapSectionGeometry? section,
        out MapRoomGeometry? highlightedRoom
    )
    {
        section = null;
        string? groupKey = ResolveGroupKey(scenarioName, relativePath, roomId);
        if (string.IsNullOrWhiteSpace(groupKey) || !Sections.TryGetValue(groupKey, out section))
        {
            highlightedRoom = null;
            return false;
        }

        string roomSlug = MapAssetNameUtility.Slugify(roomName);
        highlightedRoom = section.Rooms.FirstOrDefault(room => room.Matches(roomId, roomSlug));
        return true;
    }

    private static string? ResolveGroupKey(string scenarioName, string? relativePath, short roomId)
    {
        if (
            roomId >= 0
            && MapAssetNameUtility.TryGetRoomAssetBaseName(scenarioName, roomId, out string roomAssetBaseName)
        )
        {
            string scenarioSlug = MapAssetNameUtility.GetScenarioSlug(scenarioName);
            string roomRelativePath = Path.Combine("Assets", "Maps", scenarioSlug, $"{roomAssetBaseName}.png");
            string roomGroupKey = MapProjectionCalibrationStore.ResolveCalibrationTargetKey(
                scenarioName,
                roomRelativePath
            );
            if (Sections.ContainsKey(roomGroupKey))
                return roomGroupKey;
        }

        if (!string.IsNullOrWhiteSpace(relativePath))
        {
            string groupKey = MapProjectionCalibrationStore.ResolveCalibrationTargetKey(scenarioName, relativePath);
            if (Sections.ContainsKey(groupKey))
                return groupKey;
        }

        return null;
    }

    private static Bitmap RenderSection(
        string scenarioName,
        MapSectionGeometry section,
        short currentRoomId,
        string currentRoomSlug
    )
    {
        byte[] pixels = new byte[section.Width * section.Height * 4];
        Clear(pixels, BackgroundColor);

        foreach (MapRoomGeometry room in section.Rooms)
        {
            byte[] fillColor = room.Matches(currentRoomId, currentRoomSlug) ? ActiveRoomFillColor : RoomFillColor;

            if (room.Shape is MapRoomShape.Rectangle)
                DrawRectangle(pixels, section.Width, section.Height, room.Coordinates, fillColor, RoomOutlineColor);
            else
                DrawPolygon(pixels, section.Width, section.Height, room.Coordinates, fillColor, RoomOutlineColor);
        }

        DrawGeometryDetails(pixels, section);
        DrawRoomDetailOverlays(pixels, section, scenarioName);
        DrawRoomOutlines(pixels, section);

        if (ShouldDrawSyntheticTransitions(section))
            DrawTransitions(pixels, section);

        return CreateBitmap(section.Width, section.Height, pixels);
    }

    internal static bool ShouldDrawSyntheticTransitions(MapSectionGeometry section) =>
        string.IsNullOrWhiteSpace(section.DetailAssetBucket);

    private static void DrawGeometryDetails(byte[] pixels, MapSectionGeometry section)
    {
        foreach (MapSectionDetailGeometry detail in section.Details)
        {
            switch (detail.Kind)
            {
                case MapSectionDetailKind.Door:
                    DrawRectangle(
                        pixels,
                        section.Width,
                        section.Height,
                        detail.X,
                        detail.Y,
                        detail.Right,
                        detail.Bottom,
                        DoorFillColor,
                        DetailDarkColor
                    );
                    break;

                case MapSectionDetailKind.Stairs:
                    DrawHatchedDetail(pixels, section.Width, section.Height, detail, drawRails: false);
                    break;

                case MapSectionDetailKind.Ladder:
                    DrawHatchedDetail(pixels, section.Width, section.Height, detail, drawRails: true);
                    break;
            }
        }
    }

    private static void DrawHatchedDetail(
        byte[] pixels,
        int width,
        int height,
        MapSectionDetailGeometry detail,
        bool drawRails
    )
    {
        DrawRectangleOutline(pixels, width, height, detail.X, detail.Y, detail.Right, detail.Bottom, DetailDarkColor);

        switch (detail.Orientation)
        {
            case MapSectionDetailOrientation.Vertical:
                DrawVerticalHatch(pixels, width, height, detail, drawRails);
                break;

            case MapSectionDetailOrientation.Horizontal:
                DrawHorizontalHatch(pixels, width, height, detail, drawRails);
                break;

            case MapSectionDetailOrientation.DiagonalDown:
                DrawDiagonalHatch(pixels, width, height, detail, drawRails, diagonalDown: true);
                break;

            case MapSectionDetailOrientation.DiagonalUp:
                DrawDiagonalHatch(pixels, width, height, detail, drawRails, diagonalDown: false);
                break;
        }
    }

    private static void DrawVerticalHatch(
        byte[] pixels,
        int width,
        int height,
        MapSectionDetailGeometry detail,
        bool drawRails
    )
    {
        int left = detail.X + 1;
        int right = detail.Right - 1;
        int top = detail.Y + 1;
        int bottom = detail.Bottom - 1;
        if (left > right || top > bottom)
            return;

        if (drawRails)
        {
            DrawLine(pixels, width, height, left, top, left, bottom, DetailLightColor);
            DrawLine(pixels, width, height, right, top, right, bottom, DetailLightColor);
        }

        int startY = top + (drawRails ? 2 : 1);
        for (int y = startY; y <= bottom; y += 4)
        {
            DrawLine(pixels, width, height, left, y, right, y, DetailLightColor);
        }
    }

    private static void DrawHorizontalHatch(
        byte[] pixels,
        int width,
        int height,
        MapSectionDetailGeometry detail,
        bool drawRails
    )
    {
        int left = detail.X + 1;
        int right = detail.Right - 1;
        int top = detail.Y + 1;
        int bottom = detail.Bottom - 1;
        if (left > right || top > bottom)
            return;

        if (drawRails)
        {
            DrawLine(pixels, width, height, left, top, right, top, DetailLightColor);
            DrawLine(pixels, width, height, left, bottom, right, bottom, DetailLightColor);
        }

        int startX = left + (drawRails ? 2 : 1);
        for (int x = startX; x <= right; x += 4)
        {
            DrawLine(pixels, width, height, x, top, x, bottom, DetailLightColor);
        }
    }

    private static void DrawDiagonalHatch(
        byte[] pixels,
        int width,
        int height,
        MapSectionDetailGeometry detail,
        bool drawRails,
        bool diagonalDown
    )
    {
        int inset = drawRails ? 2 : 1;
        int left = detail.X + inset;
        int right = detail.Right - inset;
        int top = detail.Y + inset;
        int bottom = detail.Bottom - inset;
        if (left > right || top > bottom)
            return;

        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
                int relativeX = x - left;
                int relativeY = y - top;
                int pattern = diagonalDown ? relativeX - relativeY : relativeX + relativeY;
                if (Math.Abs(pattern % 4) == 0)
                    SetPixel(pixels, width, height, x, y, DetailLightColor);
            }
        }
    }

    private static void DrawRoomDetailOverlays(byte[] pixels, MapSectionGeometry section, string scenarioName)
    {
        if (ScenarioMapDetailAssetResolver.TryLoadSectionDetail(scenarioName, section, out var sectionDetailBitmap))
        {
            DrawFullCanvasDetailOverlay(pixels, section.Width, section.Height, sectionDetailBitmap!);
            return;
        }

        for (int roomIndex = 0; roomIndex < section.Rooms.Count; roomIndex++)
        {
            if (
                !ScenarioMapDetailAssetResolver.TryLoadRoomDetail(
                    scenarioName,
                    section,
                    roomIndex + 1,
                    out ScenarioMapDetailAssetResolver.DetailBitmapData? detailBitmap
                )
            )
                continue;

            if (detailBitmap!.Width == section.Width && detailBitmap.Height == section.Height)
            {
                DrawFullCanvasDetailOverlay(pixels, section.Width, section.Height, detailBitmap);
                continue;
            }

            DrawRoomDetailOverlay(pixels, section.Width, section.Height, section.Rooms[roomIndex], detailBitmap);
        }
    }

    private static void DrawFullCanvasDetailOverlay(
        byte[] pixels,
        int width,
        int height,
        ScenarioMapDetailAssetResolver.DetailBitmapData detailBitmap
    )
    {
        for (int destY = 0; destY < height; destY++)
        {
            int sourceY = detailBitmap.Height == 1 ? 0 : (destY * (detailBitmap.Height - 1)) / Math.Max(1, height - 1);

            for (int destX = 0; destX < width; destX++)
            {
                int sourceX = detailBitmap.Width == 1 ? 0 : (destX * (detailBitmap.Width - 1)) / Math.Max(1, width - 1);
                int sourceIndex = ((sourceY * detailBitmap.Width) + sourceX) * 4;
                BlendPixel(
                    pixels,
                    width,
                    height,
                    destX,
                    destY,
                    detailBitmap.Pixels[sourceIndex],
                    detailBitmap.Pixels[sourceIndex + 1],
                    detailBitmap.Pixels[sourceIndex + 2],
                    detailBitmap.Pixels[sourceIndex + 3]
                );
            }
        }
    }

    private static void DrawRoomOutlines(byte[] pixels, MapSectionGeometry section)
    {
        foreach (MapRoomGeometry room in section.Rooms)
        {
            if (room.Shape is MapRoomShape.Rectangle)
            {
                int[] coords = room.Coordinates;
                int left = Math.Min(coords[0], coords[2]);
                int top = Math.Min(coords[1], coords[3]);
                int right = Math.Max(coords[0], coords[2]);
                int bottom = Math.Max(coords[1], coords[3]);

                DrawLine(pixels, section.Width, section.Height, left, top, right, top, RoomOutlineColor);
                DrawLine(pixels, section.Width, section.Height, right, top, right, bottom, RoomOutlineColor);
                DrawLine(pixels, section.Width, section.Height, right, bottom, left, bottom, RoomOutlineColor);
                DrawLine(pixels, section.Width, section.Height, left, bottom, left, top, RoomOutlineColor);
                continue;
            }

            DrawPolygonOutline(pixels, section.Width, section.Height, room.Coordinates, RoomOutlineColor);
        }
    }

    private static void DrawRoomDetailOverlay(
        byte[] pixels,
        int width,
        int height,
        MapRoomGeometry room,
        ScenarioMapDetailAssetResolver.DetailBitmapData detailBitmap
    )
    {
        (int left, int top, int right, int bottom) = GetBounds(room.Coordinates);
        int overlayWidth = Math.Max(1, right - left + 1);
        int overlayHeight = Math.Max(1, bottom - top + 1);

        for (int destY = 0; destY < overlayHeight; destY++)
        {
            int canvasY = top + destY;
            if (canvasY < 0 || canvasY >= height)
                continue;

            int sourceY =
                detailBitmap.Height == 1 ? 0 : (destY * (detailBitmap.Height - 1)) / Math.Max(1, overlayHeight - 1);

            for (int destX = 0; destX < overlayWidth; destX++)
            {
                int canvasX = left + destX;
                if (canvasX < 0 || canvasX >= width || !RoomContainsPoint(room, canvasX, canvasY))
                    continue;

                int sourceX =
                    detailBitmap.Width == 1 ? 0 : (destX * (detailBitmap.Width - 1)) / Math.Max(1, overlayWidth - 1);
                int sourceIndex = ((sourceY * detailBitmap.Width) + sourceX) * 4;
                BlendPixel(
                    pixels,
                    width,
                    height,
                    canvasX,
                    canvasY,
                    detailBitmap.Pixels[sourceIndex],
                    detailBitmap.Pixels[sourceIndex + 1],
                    detailBitmap.Pixels[sourceIndex + 2],
                    detailBitmap.Pixels[sourceIndex + 3]
                );
            }
        }
    }

    private static void DrawTransitions(byte[] pixels, MapSectionGeometry section)
    {
        foreach (MapRoomLink link in section.Links)
        {
            MapRoomGeometry? sourceRoom = section.Rooms.FirstOrDefault(room =>
                string.Equals(room.Slug, link.SourceSlug, StringComparison.OrdinalIgnoreCase)
            );
            MapRoomGeometry? targetRoom = section.Rooms.FirstOrDefault(room =>
                string.Equals(room.Slug, link.TargetSlug, StringComparison.OrdinalIgnoreCase)
            );
            if (sourceRoom is null || targetRoom is null)
                continue;

            if (
                !TryGetTransitionMarker(
                    sourceRoom.Coordinates,
                    targetRoom.Coordinates,
                    out int markerX,
                    out int markerY,
                    out bool drawVertical
                )
            )
                continue;

            DrawTransitionMarker(pixels, section.Width, section.Height, markerX, markerY, drawVertical);
        }
    }

    private static void DrawTransitionMarker(
        byte[] pixels,
        int width,
        int height,
        int centerX,
        int centerY,
        bool drawVertical
    )
    {
        const int markerHalfLength = 5;

        if (drawVertical)
        {
            DrawLine(
                pixels,
                width,
                height,
                centerX - 1,
                centerY - markerHalfLength - 1,
                centerX - 1,
                centerY + markerHalfLength + 1,
                TransitionMarkerOutlineColor
            );
            DrawLine(
                pixels,
                width,
                height,
                centerX + 1,
                centerY - markerHalfLength - 1,
                centerX + 1,
                centerY + markerHalfLength + 1,
                TransitionMarkerOutlineColor
            );
            DrawLine(
                pixels,
                width,
                height,
                centerX,
                centerY - markerHalfLength,
                centerX,
                centerY + markerHalfLength,
                TransitionMarkerColor
            );
            return;
        }

        DrawLine(
            pixels,
            width,
            height,
            centerX - markerHalfLength - 1,
            centerY - 1,
            centerX + markerHalfLength + 1,
            centerY - 1,
            TransitionMarkerOutlineColor
        );
        DrawLine(
            pixels,
            width,
            height,
            centerX - markerHalfLength - 1,
            centerY + 1,
            centerX + markerHalfLength + 1,
            centerY + 1,
            TransitionMarkerOutlineColor
        );
        DrawLine(
            pixels,
            width,
            height,
            centerX - markerHalfLength,
            centerY,
            centerX + markerHalfLength,
            centerY,
            TransitionMarkerColor
        );
    }

    private static bool TryGetTransitionMarker(
        int[] sourceCoordinates,
        int[] targetCoordinates,
        out int markerX,
        out int markerY,
        out bool drawVertical
    )
    {
        const int adjacencyTolerance = 12;
        const int maxFallbackGap = 96;

        (int left, int top, int right, int bottom) = GetBounds(sourceCoordinates);
        (int otherLeft, int otherTop, int otherRight, int otherBottom) = GetBounds(targetCoordinates);

        int verticalOverlapTop = Math.Max(top, otherTop);
        int verticalOverlapBottom = Math.Min(bottom, otherBottom);
        if (verticalOverlapBottom > verticalOverlapTop)
        {
            if (Math.Abs(right - otherLeft) <= adjacencyTolerance)
            {
                markerX = (right + otherLeft) / 2;
                markerY = (verticalOverlapTop + verticalOverlapBottom) / 2;
                drawVertical = true;
                return true;
            }

            if (Math.Abs(otherRight - left) <= adjacencyTolerance)
            {
                markerX = (left + otherRight) / 2;
                markerY = (verticalOverlapTop + verticalOverlapBottom) / 2;
                drawVertical = true;
                return true;
            }
        }

        int horizontalOverlapLeft = Math.Max(left, otherLeft);
        int horizontalOverlapRight = Math.Min(right, otherRight);
        if (horizontalOverlapRight > horizontalOverlapLeft)
        {
            if (Math.Abs(bottom - otherTop) <= adjacencyTolerance)
            {
                markerX = (horizontalOverlapLeft + horizontalOverlapRight) / 2;
                markerY = (bottom + otherTop) / 2;
                drawVertical = false;
                return true;
            }

            if (Math.Abs(otherBottom - top) <= adjacencyTolerance)
            {
                markerX = (horizontalOverlapLeft + horizontalOverlapRight) / 2;
                markerY = (top + otherBottom) / 2;
                drawVertical = false;
                return true;
            }
        }

        double sourceCenterX = (left + right) / 2.0;
        double sourceCenterY = (top + bottom) / 2.0;
        double targetCenterX = (otherLeft + otherRight) / 2.0;
        double targetCenterY = (otherTop + otherBottom) / 2.0;

        (double sourcePointX, double sourcePointY) = GetBoundaryPoint(
            left,
            top,
            right,
            bottom,
            targetCenterX,
            targetCenterY
        );
        (double targetPointX, double targetPointY) = GetBoundaryPoint(
            otherLeft,
            otherTop,
            otherRight,
            otherBottom,
            sourceCenterX,
            sourceCenterY
        );

        double deltaX = targetPointX - sourcePointX;
        double deltaY = targetPointY - sourcePointY;
        if (Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY)) > maxFallbackGap)
        {
            markerX = 0;
            markerY = 0;
            drawVertical = false;
            return false;
        }

        markerX = (int)Math.Round((sourcePointX + targetPointX) / 2.0);
        markerY = (int)Math.Round((sourcePointY + targetPointY) / 2.0);
        drawVertical = Math.Abs(deltaX) > Math.Abs(deltaY);
        return true;
    }

    private static (double x, double y) GetBoundaryPoint(
        int left,
        int top,
        int right,
        int bottom,
        double targetX,
        double targetY
    )
    {
        double centerX = (left + right) / 2.0;
        double centerY = (top + bottom) / 2.0;
        double deltaX = targetX - centerX;
        double deltaY = targetY - centerY;

        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
        {
            return deltaX >= 0 ? (right, Math.Clamp(targetY, top, bottom)) : (left, Math.Clamp(targetY, top, bottom));
        }

        return deltaY >= 0 ? (Math.Clamp(targetX, left, right), bottom) : (Math.Clamp(targetX, left, right), top);
    }

    private static (int left, int top, int right, int bottom) GetBounds(int[] coordinates)
    {
        int left = int.MaxValue;
        int top = int.MaxValue;
        int right = int.MinValue;
        int bottom = int.MinValue;

        for (int index = 0; index < coordinates.Length; index += 2)
        {
            left = Math.Min(left, coordinates[index]);
            right = Math.Max(right, coordinates[index]);
            top = Math.Min(top, coordinates[index + 1]);
            bottom = Math.Max(bottom, coordinates[index + 1]);
        }

        return (left, top, right, bottom);
    }

    private static bool RoomContainsPoint(MapRoomGeometry room, int x, int y)
    {
        if (room.Shape is MapRoomShape.Rectangle)
        {
            int[] rectangle = room.Coordinates;
            int left = Math.Min(rectangle[0], rectangle[2]);
            int top = Math.Min(rectangle[1], rectangle[3]);
            int right = Math.Max(rectangle[0], rectangle[2]);
            int bottom = Math.Max(rectangle[1], rectangle[3]);
            return x >= left && x <= right && y >= top && y <= bottom;
        }

        return IsPointInsidePolygon(room.Coordinates, x, y);
    }

    private static bool IsPointInsidePolygon(int[] coordinates, int x, int y)
    {
        bool inside = false;
        int pointCount = coordinates.Length / 2;

        for (int i = 0, j = pointCount - 1; i < pointCount; j = i++)
        {
            int xi = coordinates[i * 2];
            int yi = coordinates[(i * 2) + 1];
            int xj = coordinates[j * 2];
            int yj = coordinates[(j * 2) + 1];

            bool intersects = ((yi > y) != (yj > y)) && (x < ((double)(xj - xi) * (y - yi) / (yj - yi)) + xi);
            if (intersects)
                inside = !inside;
        }

        return inside;
    }

    private static void BlendPixel(
        byte[] pixels,
        int width,
        int height,
        int x,
        int y,
        byte sourceBlue,
        byte sourceGreen,
        byte sourceRed,
        byte sourceAlpha
    )
    {
        if (sourceAlpha == 0 || x < 0 || x >= width || y < 0 || y >= height)
            return;

        int index = ((y * width) + x) * 4;
        int inverseAlpha = 255 - sourceAlpha;

        pixels[index] = (byte)(((sourceBlue * sourceAlpha) + (pixels[index] * inverseAlpha)) / 255);
        pixels[index + 1] = (byte)(((sourceGreen * sourceAlpha) + (pixels[index + 1] * inverseAlpha)) / 255);
        pixels[index + 2] = (byte)(((sourceRed * sourceAlpha) + (pixels[index + 2] * inverseAlpha)) / 255);
        pixels[index + 3] = 255;
    }

    private static void Clear(byte[] pixels, byte[] color)
    {
        for (int index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = color[0];
            pixels[index + 1] = color[1];
            pixels[index + 2] = color[2];
            pixels[index + 3] = color[3];
        }
    }

    private static Bitmap CreateBitmap(int width, int height, byte[] pixels)
    {
        WriteableBitmap bitmap = new(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormats.Bgra8888,
            AlphaFormat.Opaque
        );

        unsafe
        {
            using ILockedFramebuffer framebuffer = bitmap.Lock();
            fixed (byte* source = pixels)
            {
                Buffer.MemoryCopy(source, (void*)framebuffer.Address, pixels.Length, pixels.Length);
            }
        }

        return bitmap;
    }

    private static void DrawRectangle(
        byte[] pixels,
        int width,
        int height,
        int[] coords,
        byte[] fillColor,
        byte[] outlineColor
    )
    {
        int left = Math.Min(coords[0], coords[2]);
        int top = Math.Min(coords[1], coords[3]);
        int right = Math.Max(coords[0], coords[2]);
        int bottom = Math.Max(coords[1], coords[3]);

        DrawRectangle(pixels, width, height, left, top, right, bottom, fillColor, outlineColor);
    }

    private static void DrawRectangle(
        byte[] pixels,
        int width,
        int height,
        int left,
        int top,
        int right,
        int bottom,
        byte[] fillColor,
        byte[] outlineColor
    )
    {
        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
                SetPixel(pixels, width, height, x, y, fillColor);
        }

        DrawRectangleOutline(pixels, width, height, left, top, right, bottom, outlineColor);
    }

    private static void DrawRectangleOutline(
        byte[] pixels,
        int width,
        int height,
        int left,
        int top,
        int right,
        int bottom,
        byte[] outlineColor
    )
    {
        DrawLine(pixels, width, height, left, top, right, top, outlineColor);
        DrawLine(pixels, width, height, right, top, right, bottom, outlineColor);
        DrawLine(pixels, width, height, right, bottom, left, bottom, outlineColor);
        DrawLine(pixels, width, height, left, bottom, left, top, outlineColor);
    }

    private static void DrawPolygon(
        byte[] pixels,
        int width,
        int height,
        int[] coords,
        byte[] fillColor,
        byte[] outlineColor
    )
    {
        FillPolygon(pixels, width, height, coords, fillColor);
        DrawPolygonOutline(pixels, width, height, coords, outlineColor);
    }

    private static void FillPolygon(byte[] pixels, int width, int height, int[] coords, byte[] fillColor)
    {
        int pointCount = coords.Length / 2;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        for (int index = 1; index < coords.Length; index += 2)
        {
            minY = Math.Min(minY, coords[index]);
            maxY = Math.Max(maxY, coords[index]);
        }

        minY = Math.Max(minY, 0);
        maxY = Math.Min(maxY, height - 1);

        double[] intersections = new double[pointCount];

        for (int y = minY; y <= maxY; y++)
        {
            double scanY = y + 0.5;
            int count = 0;

            for (int i = 0, j = pointCount - 1; i < pointCount; j = i++)
            {
                int xi = coords[i * 2];
                int yi = coords[(i * 2) + 1];
                int xj = coords[j * 2];
                int yj = coords[(j * 2) + 1];

                if ((yi > scanY) == (yj > scanY) || yi == yj)
                    continue;

                intersections[count++] = xi + ((scanY - yi) * (xj - xi) / (double)(yj - yi));
            }

            Array.Sort(intersections, 0, count);

            for (int index = 0; index + 1 < count; index += 2)
            {
                int start = (int)Math.Ceiling(intersections[index]);
                int end = (int)Math.Floor(intersections[index + 1]);
                for (int x = start; x <= end; x++)
                    SetPixel(pixels, width, height, x, y, fillColor);
            }
        }
    }

    private static void DrawPolygonOutline(byte[] pixels, int width, int height, int[] coords, byte[] outlineColor)
    {
        int pointCount = coords.Length / 2;
        for (int index = 0; index < pointCount; index++)
        {
            int nextIndex = (index + 1) % pointCount;
            DrawLine(
                pixels,
                width,
                height,
                coords[index * 2],
                coords[(index * 2) + 1],
                coords[nextIndex * 2],
                coords[(nextIndex * 2) + 1],
                outlineColor
            );
        }
    }

    private static void DrawLine(byte[] pixels, int width, int height, int x0, int y0, int x1, int y1, byte[] color)
    {
        int deltaX = Math.Abs(x1 - x0);
        int deltaY = Math.Abs(y1 - y0);
        int stepX = x0 < x1 ? 1 : -1;
        int stepY = y0 < y1 ? 1 : -1;
        int error = deltaX - deltaY;

        while (true)
        {
            SetPixel(pixels, width, height, x0, y0, color);
            if (x0 == x1 && y0 == y1)
                return;

            int doubledError = error * 2;
            if (doubledError > -deltaY)
            {
                error -= deltaY;
                x0 += stepX;
            }

            if (doubledError < deltaX)
            {
                error += deltaX;
                y0 += stepY;
            }
        }
    }

    private static void SetPixel(byte[] pixels, int width, int height, int x, int y, byte[] color)
    {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height)
            return;

        int index = ((y * width) + x) * 4;
        pixels[index] = color[0];
        pixels[index + 1] = color[1];
        pixels[index + 2] = color[2];
        pixels[index + 3] = color[3];
    }
}
