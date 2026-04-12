using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using Material.Icons;
using OutbreakTracker2.Application.Converters;
using OutbreakTracker2.Application.Views.GameDock;
using OutbreakTracker2.Application.Views.GameDock.Converters;
using OutbreakTracker2.Outbreak.Enums.LobbySlot;

namespace OutbreakTracker2.UnitTests;

public sealed class ConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Test]
    public async Task BoolToColorConverter_ReturnsExpectedBrushes()
    {
        IValueConverter converter = BoolToColorConverter.EnemyFaction;

        object? active = converter.Convert(true, typeof(IBrush), null, Culture);
        object? inactive = converter.Convert(false, typeof(IBrush), null, Culture);

        await Assert.That(ReferenceEquals(active, Brushes.OrangeRed)).IsTrue();
        await Assert.That(ReferenceEquals(inactive, Brushes.White)).IsTrue();
    }

    [Test]
    public async Task BoolToOpacityConverter_ReturnsExpectedOpacityValues()
    {
        IValueConverter converter = BoolToOpacityConverter.InGame;

        double visible = (double)(
            converter.Convert(true, typeof(double), null, Culture) ?? throw new InvalidOperationException()
        );
        double dimmed = (double)(
            converter.Convert(false, typeof(double), null, Culture) ?? throw new InvalidOperationException()
        );

        await Assert.That(visible).IsEqualTo(1.0d);
        await Assert.That(dimmed).IsEqualTo(0.5d);
    }

    [Test]
    public async Task BoolToIconConverter_ReturnsConfiguredIcons_AndDoNothingForWrongType()
    {
        BoolToIconConverter converter = new(MaterialIconKind.Check, MaterialIconKind.Close);

        object trueValue = converter.Convert(true, typeof(MaterialIconKind), null, Culture);
        object falseValue = converter.Convert(false, typeof(MaterialIconKind), null, Culture);
        object invalidValue = converter.Convert("true", typeof(MaterialIconKind), null, Culture);

        await Assert.That((MaterialIconKind)trueValue).IsEqualTo(MaterialIconKind.Check);
        await Assert.That((MaterialIconKind)falseValue).IsEqualTo(MaterialIconKind.Close);
        await Assert.That(ReferenceEquals(invalidValue, BindingOperations.DoNothing)).IsTrue();
    }

    [Test]
    public async Task CollectionIsNullOrEmptyConverter_HandlesNullCollectionsAndInverse()
    {
        CollectionIsNullOrEmptyConverter converter = new();

        object nullValue = converter.Convert(null, typeof(bool), null, Culture);
        object emptyCollection = converter.Convert(Array.Empty<int>(), typeof(bool), null, Culture);
        object nonEmptyInverse = converter.Convert(new[] { 1 }, typeof(bool), "Inverse", Culture);
        object invalidValue = converter.Convert(123, typeof(bool), null, Culture);

        await Assert.That((bool)nullValue).IsTrue();
        await Assert.That((bool)emptyCollection).IsTrue();
        await Assert.That((bool)nonEmptyInverse).IsTrue();
        await Assert.That(ReferenceEquals(invalidValue, BindingOperations.DoNothing)).IsTrue();
    }

    [Test]
    public async Task ConditionToIconConverter_MapsKnownCondition_AndRejectsWrongType()
    {
        ConditionToIconConverter converter = new();

        object condition = converter.Convert("Fine", typeof(MaterialIconKind), null, Culture);
        object invalid = converter.Convert(123, typeof(MaterialIconKind), null, Culture);

        await Assert.That((MaterialIconKind)condition).IsEqualTo(MaterialIconKind.Success);
        await Assert.That(ReferenceEquals(invalid, BindingOperations.DoNothing)).IsTrue();
    }

    [Test]
    public async Task LobbySlotStatusToIconConverter_HandlesEnumAndJoinInAlias()
    {
        LobbySlotStatusToIconConverter converter = new();

        object direct = converter.Convert(SlotStatus.Full, typeof(MaterialIconKind), null, Culture);
        object alias = converter.Convert("Join in", typeof(MaterialIconKind), null, Culture);
        object invalid = converter.Convert(123, typeof(MaterialIconKind), null, Culture);

        await Assert.That((MaterialIconKind)direct).IsEqualTo(MaterialIconKind.AccountMultipleRemoveOutline);
        await Assert.That((MaterialIconKind)alias).IsEqualTo(MaterialIconKind.DoorOpen);
        await Assert.That(ReferenceEquals(invalid, BindingOperations.DoNothing)).IsTrue();
    }

    [Test]
    public async Task LobbyVersionToIconConverter_MapsKnownVersions_AndRejectsWrongType()
    {
        LobbyVersionToIconConverter converter = new();

        object dvd = converter.Convert("dvd-rom", typeof(MaterialIconKind), null, Culture);
        object hdd = converter.Convert("HDD", typeof(MaterialIconKind), null, Culture);
        object invalid = converter.Convert(123, typeof(MaterialIconKind), null, Culture);

        await Assert.That((MaterialIconKind)dvd).IsEqualTo(MaterialIconKind.Album);
        await Assert.That((MaterialIconKind)hdd).IsEqualTo(MaterialIconKind.Harddisk);
        await Assert.That(ReferenceEquals(invalid, BindingOperations.DoNothing)).IsTrue();
    }

    [Test]
    public async Task StatusToIconConverter_MapsKnownStatuses_AndRejectsWrongType()
    {
        StatusToIconConverter converter = new();

        object ok = converter.Convert("OK", typeof(MaterialIconKind), null, Culture);
        object down = converter.Convert("Down", typeof(MaterialIconKind), null, Culture);
        object invalid = converter.Convert(123, typeof(MaterialIconKind), null, Culture);

        await Assert.That((MaterialIconKind)ok).IsEqualTo(MaterialIconKind.Success);
        await Assert.That((MaterialIconKind)down).IsEqualTo(MaterialIconKind.Warning);
        await Assert.That(ReferenceEquals(invalid, BindingOperations.DoNothing)).IsTrue();
    }

    [Test]
    public async Task EitherNotNullConverter_ReturnsFirstNonNullValue_AndNullWhenAllValuesAreNull()
    {
        EitherNotNullConverter converter = new();

        object? firstValue = converter.Convert(
            new List<object?> { null, "fallback", 123 },
            typeof(object),
            null,
            Culture
        );
        object? allNull = converter.Convert(new List<object?> { null, null }, typeof(object), null, Culture);

        await Assert.That((string)firstValue!).IsEqualTo("fallback");
        await Assert.That(allNull).IsNull();
    }

    [Test]
    public async Task PinnedDockAlignmentConverter_ReturnsDockAlignment_AndLeftFallback()
    {
        GlobalToolDock dock = new() { Alignment = Alignment.Right };

        object aligned = PinnedDockAlignmentConverter.Instance.Convert(dock, typeof(Alignment), null, Culture);
        object fallback = PinnedDockAlignmentConverter.Instance.Convert("invalid", typeof(Alignment), null, Culture);

        await Assert.That((Alignment)aligned).IsEqualTo(Alignment.Right);
        await Assert.That((Alignment)fallback).IsEqualTo(Alignment.Left);
    }

    [Test]
    public async Task PinnedDockHasVisibleContentConverter_ReturnsTrueOnlyWhenDockContainsVisibleTool()
    {
        GlobalToolDock populatedDock = CreateDockWithVisibleTool();

        object empty = PinnedDockHasVisibleContentConverter.Instance.Convert("invalid", typeof(bool), null, Culture);
        object populated = PinnedDockHasVisibleContentConverter.Instance.Convert(
            populatedDock,
            typeof(bool),
            null,
            Culture
        );

        await Assert.That((bool)empty).IsFalse();
        await Assert.That((bool)populated).IsTrue();
    }

    private static GlobalToolDock CreateDockWithVisibleTool()
    {
        DummyTool tool = new();

        return new GlobalToolDock
        {
            VisibleDockables = new List<IDockable> { tool },
            ActiveDockable = tool,
        };
    }

    private sealed class DummyTool : Tool { }
}
