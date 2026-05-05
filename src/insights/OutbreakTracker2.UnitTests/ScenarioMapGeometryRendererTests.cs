using OutbreakTracker2.Application.Views.Map.Canvas;

namespace OutbreakTracker2.UnitTests;

public sealed class ScenarioMapGeometryRendererTests
{
    [Test]
    public async Task TryResolveGeometry_UsesFullUmbrellaSectionGeometry_ForWaitingRoom()
    {
        bool resolved = ScenarioMapGeometryRenderer.TryResolveGeometry(
            "End of the road",
            Path.Combine("Assets", "Maps", "end-of-the-road", "default.png"),
            roomId: 1,
            roomName: "Waiting room",
            out MapSectionGeometry? section,
            out MapRoomGeometry? highlightedRoom
        );

        await Assert.That(resolved).IsTrue();
        await Assert.That(section).IsNotNull();
        await Assert.That(section!.Width).IsEqualTo(725);
        await Assert.That(section.Height).IsEqualTo(694);
        await Assert.That(highlightedRoom).IsNotNull();
        await Assert.That(highlightedRoom!.RoomId).IsEqualTo((short)1);
    }

    [Test]
    public async Task TryResolveGeometry_UsesFullWaterTreatmentSectionGeometry_ForMaintenancePassage1()
    {
        bool resolved = ScenarioMapGeometryRenderer.TryResolveGeometry(
            "End of the road",
            Path.Combine("Assets", "Maps", "end-of-the-road", "r0062800.png"),
            roomId: 28,
            roomName: "Maintenance Passage 1",
            out MapSectionGeometry? section,
            out MapRoomGeometry? highlightedRoom
        );

        await Assert.That(resolved).IsTrue();
        await Assert.That(section).IsNotNull();
        await Assert.That(section!.Width).IsEqualTo(504);
        await Assert.That(section.Height).IsEqualTo(747);
        await Assert.That(highlightedRoom).IsNotNull();
        await Assert.That(highlightedRoom!.RoomId).IsEqualTo((short)28);
    }

    [Test]
    public async Task TryResolveGeometry_UsesRoomNameFallback_ForMissingHighwayRooftopEnum()
    {
        bool resolved = ScenarioMapGeometryRenderer.TryResolveGeometry(
            "End of the road",
            Path.Combine("Assets", "Maps", "end-of-the-road", "r0066300.png"),
            roomId: -1,
            roomName: "Rooftop",
            out MapSectionGeometry? section,
            out MapRoomGeometry? highlightedRoom
        );

        await Assert.That(resolved).IsTrue();
        await Assert.That(section).IsNotNull();
        await Assert.That(section!.Width).IsEqualTo(327);
        await Assert.That(section.Height).IsEqualTo(473);
        await Assert.That(highlightedRoom).IsNotNull();
        await Assert.That(highlightedRoom!.Slug).IsEqualTo("rooftop");
    }

    [Test]
    public async Task TryResolveGeometry_ExposesWebsiteDerivedTransitionLinks_ForUmbrellaFacility()
    {
        bool resolved = ScenarioMapGeometryRenderer.TryResolveGeometry(
            "End of the road",
            Path.Combine("Assets", "Maps", "end-of-the-road", "default.png"),
            roomId: 1,
            roomName: "Waiting room",
            out MapSectionGeometry? section,
            out _
        );

        await Assert.That(resolved).IsTrue();
        await Assert.That(section).IsNotNull();
        await Assert.That(section!.Links.Count).IsGreaterThan(0);
        await Assert
            .That(section.Links.Any(link => link.SourceSlug == "waiting-room" && link.TargetSlug == "examination-room"))
            .IsTrue();
        await Assert
            .That(
                section.Links.Any(link =>
                    link.SourceSlug == "east-passage-1" && link.TargetSlug == "special-research-room"
                )
            )
            .IsTrue();
    }

    [Test]
    public async Task ShouldDrawSyntheticTransitions_IsFalseForSectionsWithAuthoritativeDetailBucket()
    {
        bool resolved = ScenarioMapGeometryRenderer.TryResolveGeometry(
            "End of the road",
            Path.Combine("Assets", "Maps", "end-of-the-road", "default.png"),
            roomId: 1,
            roomName: "Waiting room",
            out MapSectionGeometry? section,
            out _
        );

        await Assert.That(resolved).IsTrue();
        await Assert.That(section).IsNotNull();
        await Assert.That(section!.DetailAssetBucket).IsNotNull();
        await Assert.That(ScenarioMapGeometryRenderer.ShouldDrawSyntheticTransitions(section)).IsFalse();
    }

    [Test]
    public async Task TryResolveGeometry_ExposesAuthoritativeDoorAndLadderDetails_ForUmbrellaFacility()
    {
        bool resolved = ScenarioMapGeometryRenderer.TryResolveGeometry(
            "End of the road",
            Path.Combine("Assets", "Maps", "end-of-the-road", "default.png"),
            roomId: 1,
            roomName: "Waiting room",
            out MapSectionGeometry? section,
            out _
        );

        await Assert.That(resolved).IsTrue();
        await Assert.That(section).IsNotNull();
        await Assert.That(section!.Details.Count(detail => detail.Kind == MapSectionDetailKind.Door)).IsGreaterThan(20);
        await Assert
            .That(
                section.Details.Any(detail => detail.Kind is MapSectionDetailKind.Ladder or MapSectionDetailKind.Stairs)
            )
            .IsTrue();
    }

    [Test]
    public async Task TryResolveGeometry_ExposesVerticalLadderDetail_ForWaterTreatmentBasement2()
    {
        bool resolved = ScenarioMapGeometryRenderer.TryResolveGeometry(
            "End of the road",
            Path.Combine("Assets", "Maps", "end-of-the-road", "r0064500.png"),
            roomId: 46,
            roomName: "Maintenance passage 3",
            out MapSectionGeometry? section,
            out MapRoomGeometry? highlightedRoom
        );

        await Assert.That(resolved).IsTrue();
        await Assert.That(section).IsNotNull();
        await Assert.That(highlightedRoom).IsNotNull();
        await Assert.That(section!.Details.Any(detail => detail.Kind == MapSectionDetailKind.Ladder)).IsTrue();
        await Assert
            .That(
                section.Details.Any(detail =>
                    detail.Kind == MapSectionDetailKind.Ladder
                    && detail.Orientation == MapSectionDetailOrientation.Vertical
                )
            )
            .IsTrue();
    }
}
