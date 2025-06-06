using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace OutbreakTracker2.Application.Utilities;

public static class ImageUtility
{
    public static CroppedBitmap GetCroppedBitmap(IImage imageSource, Rect rectSource)
    {
        PixelRect pixelRect = new((int)rectSource.X, (int)rectSource.Y, (int)rectSource.Width, (int)rectSource.Height);
        return new CroppedBitmap(imageSource, pixelRect);
    }
}