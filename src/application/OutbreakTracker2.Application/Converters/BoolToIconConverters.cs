using Material.Icons;

namespace OutbreakTracker2.Application.Converters;

public static class BoolToIconConverters
{
    public static readonly BoolToIconConverter Password = new(MaterialIconKind.Lock, MaterialIconKind.Unlocked);
    public static readonly BoolToIconConverter WindowLock = new(
        MaterialIconKind.Lock,
        MaterialIconKind.LockOpenVariant
    );
    public static readonly BoolToIconConverter Visibility = new(MaterialIconKind.Eye, MaterialIconKind.EyeOffOutline);
}
