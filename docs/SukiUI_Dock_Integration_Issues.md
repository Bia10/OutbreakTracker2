# SukiUI.Dock Integration Issues — Upstream Report

## Summary

Integrating SukiUI.Dock 6.0.3 with Dock.Avalonia 11.3.11.22 requires extensive
workarounds due to API drift, stale compiled templates, and cross-assembly
ControlTheme resolution failures. This document catalogues every issue
encountered so it can be raised with the upstream projects
([SukiUI.Dock](https://github.com/kikipoulet/SukiUI),
[Dock](https://github.com/wieslawsoltes/Dock)).

---

## 1. SukiUI.Dock's `Index.axaml` can't load — TypeLoadException

**What happens:**
`<StyleInclude Source="avares://SukiUI.Dock/Index.axaml" />` immediately throws a
`TypeLoadException` at startup.

**Root cause:**
`Index.axaml` creates an instance of `DockFluentTheme` and expects to find it in
the `Dock.Avalonia` assembly (where it lived in older versions). In
Dock.Avalonia 11.3.x, `DockFluentTheme` was moved to the separate
`Dock.Avalonia.Themes.Fluent` assembly. SukiUI.Dock was compiled against the
old layout and its embedded BAML hard-codes the wrong assembly-qualified type
reference.

**Impact:** The single intended entry-point for the theme package is completely
non-functional. Consumers must manually include each individual resource
dictionary and separately register `DockFluentTheme`.

**Fix required (SukiUI.Dock):** Rebuild against Dock.Avalonia ≥ 11.3.x so
`DockFluentTheme` resolves from `Dock.Avalonia.Themes.Fluent`.

---

## 2. Compiled DockTarget template references deleted PNG assets

**What happens:**
Dragging any dockable crashes with `FileNotFoundException` for
`avares://Dock.Avalonia/Assets/DockAnchorableTop.png` (and Bottom, Left, Right,
`DockDocumentInside.png`).

**Root cause:**
`DockFluentTheme` (from the Dock.Avalonia.Themes.Fluent 11.3.11.22 NuGet) has a
compiled `DockTarget` ControlTheme whose `Image.Source` setters still reference
PNG assets at `avares://Dock.Avalonia/Assets/DockAnchorable*.png`. Those assets
were **removed** from the `Dock.Avalonia` package in the same release cycle,
but the compiled theme was not updated to stop referencing them.

SukiUI.Dock provides an override `DockTarget.axaml` that comments out the
`Source` setters — but because of issue #3 below, that override never wins.

**Impact:** Drag-and-dock is broken out of the box for any consumer of
Dock.Avalonia.Themes.Fluent 11.3.11.22.

**Fix required (Dock.Avalonia.Themes.Fluent):** Either re-add the PNG assets,
switch to `DrawingImage`/`GeometryDrawing` icons, or remove the `Source`
setters (as SukiUI.Dock did). The GitHub
[`master` source](https://github.com/wieslawsoltes/Dock/blob/master/src/Dock.Avalonia.Themes.Fluent/Controls/DockTarget.axaml)
already contains a fix with `DockProperties.IndicatorDockOperation` attached
properties, but the v11.3.11.22 NuGet was published **before** that fix landed.

---

## 3. Cross-assembly ControlTheme overrides don't reliably win

**What happens:**
SukiUI.Dock's resource dictionaries (loaded via `ResourceInclude` into
`Application.Resources`) should override `DockFluentTheme`'s compiled
ControlThemes (registered via `Application.Styles`). In practice the compiled
key from `DockFluentTheme` is resolved instead of the override for certain
controls, particularly `DockTarget` and `HostWindow`.

**Root cause:**
Avalonia's ControlTheme lookup first checks the control's own `Theme` property
(if explicitly set), then walks the logical tree's `Resources`, then finally
`Styles`. The issue is that controls created dynamically at runtime (e.g.
`HostWindow` via `HostWindowLocator`, `DockTarget` via the adorner layer,
pinned dock controls via auto-hide) are not part of the normal logical tree
when their theme is resolved. The compiled ControlTheme key (registered by
`DockFluentTheme` in `Application.Styles`) wins because the cross-assembly
`ResourceInclude` override doesn't propagate to the adorner/overlay visual
tree.

**Impact:** Every ControlTheme that targets a dynamically-created control must
be vendored locally into the consuming application's project. In our case: 5
AXAML files (`DockTarget`, `HostWindow`, `PinnedDockControl`,
`ToolPinnedControl`, `ToolPinItemControl`).

**Fix required (SukiUI.Dock + Dock.Avalonia):** Either:
- Dock.Avalonia should respect `Application.Resources` ControlTheme overrides
  for adorner-layer controls, or
- SukiUI.Dock should document that consumers must vendor the dynamically-resolved
  themes locally, or
- SukiUI.Dock should register its themes via `Application.Styles` (as child
  styles of `DockFluentTheme`) rather than through `ResourceInclude` merging.

---

## 4. NuGet vs GitHub source mismatch (ghost API)

**What happens:**
The v11.3.11.22 **tag** on GitHub contains code not present in the **NuGet**:

| Feature | In GitHub source | In NuGet DLL |
|---|---|---|
| `DockProperties.IndicatorDockOperation` | Yes (on indicators/selectors) | String exists but NOT used in compiled themes |
| `ShowVerticalTargets` / `ShowHorizontalTargets` bindings | Yes (on DockTarget template) | Property exists on class but NOT bound in compiled template |
| `ShowIndicatorsOnly` pseudo-class logic | Yes | Partial |
| `:documentwindow` pseudo-class | Yes (in HostWindow theme) | **Does not exist** |
| `PinnedDockDisplayMode` on `PinnedDockControl` | Yes (in GitHub source) | **Not a StyledProperty** — only a string reference in code |
| `FocusedDockable.Title` binding on HostWindow | Yes | Property exists on `IDock` but old NuGet template uses `ActiveDockable.Title` |
| `OverlayHost` wrapper in HostWindow | Yes (in GitHub source) | Class exists but old NuGet template doesn't use it |

**Impact:** Anyone reading the GitHub source to understand how to write a custom
theme will use API that doesn't exist in the published NuGet. Bindings silently
fail, pseudo-class selectors never match, and template structure is wrong.

**Fix required (Dock.Avalonia):** Publish a new NuGet that matches the GitHub
source, or tag the commit that corresponds to the actual NuGet contents.

---

## 5. SukiUI.Dock targets netstandard2.0 — no Avalonia source generator support

**What happens:**
SukiUI.Dock compiles to `netstandard2.0`. Avalonia's XAML source generators
require `net6.0+`. This means all AXAML in SukiUI.Dock is compiled via the
legacy XAML compiler path, which has different (and less reliable) type
resolution behavior for cross-assembly references.

**Impact:** ControlTheme keys, `x:Type` markup extensions, and compiled bindings
are resolved differently than in a `net6.0+` assembly. This contributes to
issue #3 (theme overrides not winning) and makes debugging extremely difficult.

**Fix required (SukiUI.Dock):** Multi-target or drop `netstandard2.0`, matching
Dock.Avalonia's own target of `net6.0`/`net8.0`/`net10.0`.

---

## 6. DockTarget selector Images have no Source — inner zones untargetable

**What happens:**
SukiUI.Dock's `DockTarget.axaml` comments out the `Image.Source` setters on all
five selector Images (PART_TopSelector, PART_BottomSelector, etc.) to avoid the
missing-PNG crash from issue #2. But `Image` controls without a `Source` have
zero rendered size and don't participate in hit-testing.

**Result:** Dragging a dockable over a dock panel only allows docking to the
**outer** edges (the indicator Panels, which have a Background brush and
therefore render). The inner directional zones (top/bottom/left/right/center of
a specific panel) cannot be targeted because the selector Images are invisible.

**Fix required (SukiUI.Dock):** Set `Image.Source` to a `DrawingImage` with a
`GeometryDrawing` icon (triangle for directional, square for center) so the
selectors are rendered and hit-testable.

---

## 7. EitherNotNullConverter is internal — consumers must duplicate it

**What happens:**
SukiUI.Dock's `PinnedDockControl.axaml` references
`EitherNotNullConverter.Instance` via `{x:Static}`. When vendoring the template
locally (required due to issue #3), this converter is not accessible because
it's `internal` to the SukiUI.Dock assembly.

**Impact:** Every consumer who vendors `PinnedDockControl.axaml` must also
re-implement `EitherNotNullConverter`.

**Fix required (SukiUI.Dock):** Make `EitherNotNullConverter` public, or provide
the converter as a resource in a merged dictionary that consumers can reference.

---

## 8. ToolTabStripItem context menu compiled bindings silently fail

**What happens:**
Right-clicking a docked tool tab and selecting "Float" creates a broken
HostWindow (wrong title, standard decorations, original tool not removed from
source dock). Selecting "Dock" on an already-docked tool auto-hides it instead
of being a no-op.

**Root cause:**
SukiUI.Dock's `ToolTabStripItem.axaml` defines its context menu with
`x:CompileBindings="True"` and `x:DataType="dmc:IToolDock"`. The command
binding path `{Binding Owner.Factory.FloatDockable}` is compiled against
`IToolDock`, whose `Owner` property returns `IDockable`. `Factory` lives on
`IDock` (a sub-interface), not on `IDockable` — so the compiled accessor can
miss the intermediate cast. Because SukiUI.Dock targets `netstandard2.0`
(issue #5), the XAML compiler sometimes degrades to reflection, causing
non-deterministic failures.

**Impact:** The Float/Dock/Close context menu on docked tool tabs is unreliable.
Float may leave the original tool in place while spawning a broken floating
window. Dock may auto-hide a tool that is already at its home dock.

**Fix required (SukiUI.Dock):** Use `ReflectionBinding` for all command bindings
in the ToolTabStripItem context menu, or switch to `x:DataType="core:IDockable"`
which has a shorter (and correct) interface path.

---

## Currently Required Workarounds

To use SukiUI.Dock 6.0.3 with Dock.Avalonia 11.3.11.22, consumers must:

1. **Skip `Index.axaml`** — do not use `<StyleInclude Source="avares://SukiUI.Dock/Index.axaml" />`.
2. **Register `DockFluentTheme` directly** in `Application.Styles`.
3. **Include SukiUI.Dock resources individually** via 9 separate `ResourceInclude` entries in `Application.Resources`.
4. **Vendor 5 AXAML files locally** (DockTarget, HostWindow, PinnedDockControl, ToolPinnedControl, ToolPinItemControl) because cross-assembly ControlTheme resolution fails for dynamically-created controls.
5. **Re-implement `EitherNotNullConverter`** locally.
6. **Add `DrawingImage` sources** to the vendored DockTarget's selector Images.
7. **Use `ReflectionBinding`** instead of `CompiledBinding` in vendored HostWindow templates because the DataContext type (`IRootDock`) requires walking interface hierarchies that compiled bindings can't always resolve in a cross-assembly AXAML context.
8. **Avoid referencing features from the GitHub tag source** that are not in the NuGet (`:documentwindow`, `PinnedDockDisplayMode`, `IndicatorDockOperation` bindings).
9. **Move all dock-tool `DataTemplate`s to `Application.DataTemplates`** because floating HostWindows are top-level OS windows that can't see UserControl-scoped templates.
10. **Vendor `ToolTabStripItem.axaml` locally** with `ReflectionBinding` for all context menu command bindings to fix Float/Dock/Close actions on docked tool tabs.

---

## Recommended Upstream Actions

### For SukiUI.Dock (kikipoulet/SukiUI)
- Rebuild against Dock.Avalonia ≥ 11.3.x (fix #1)
- Multi-target `net6.0`+ instead of `netstandard2.0` (fix #5)
- Add `DrawingImage` sources to DockTarget selector Images (fix #6)
- Make `EitherNotNullConverter` public (fix #7)
- Use `ReflectionBinding` in ToolTabStripItem context menu, or fix `x:DataType` (fix #8)
- Document that dynamically-created control themes must be vendored locally, or find a way to register them so they win (fix #3)

### For Dock.Avalonia (wieslawsoltes/Dock)
- Re-add or replace the deleted DockAnchorable PNG assets in the compiled DockTarget template (fix #2)
- Publish a NuGet that matches the tagged GitHub source (fix #4)
- Consider making ControlTheme resolution for adorner/overlay controls respect `Application.Resources` (fix #3)
