# 🧅 Onion.UI.Upgrader

Upgrades uGUI `Selectable` navigation, with no subclassing required.

Button, Toggle, Slider, and every other uGUI control keep working as they are. The package only changes **which neighbor gets selected** when the player moves with a keyboard or gamepad.

## Why

Unity's built-in navigation has a few rough edges:

- **Explicit** navigation is fixed. It doesn't adapt when objects are added or removed at runtime.
- **Automatic / Horizontal / Vertical** navigation looks at every Selectable in the scene and can pick targets in unintended directions, such as a nearby diagonal button instead of the one in the same row.

Onion.UI.Upgrader keeps Unity's automatic modes and makes their target selection configurable.

## Getting started

### Installation

Install via Unity Package Manager (UPM).

`https://github.com/onion-unity/onion-ui-upgrader.git`

Requires Unity 6000.0 or later and uGUI (`com.unity.ugui`) 2.0.0.

### Setup

Nothing to add to your scenes. The upgrade is **off** by default, so installing the package changes nothing until you turn it on.

1. Open **Project Settings > Onion > UI Upgrader**.
2. Check the box in the **Navigation** header.
3. Assign a Navigation Profile, or click **+** next to the profile field to create one.

On first load, the package creates `Assets/Settings/Onion_UIUpgraderSettings`, the project-wide settings for the whole package.

## How it works

At runtime, the currently selected Selectable gets its neighbors computed every frame. The result is written as Explicit navigation, and Unity performs the actual move as usual. When the selection moves away, the original navigation is restored. Scene data is never changed, and at most one Selectable is modified at a time.

Which Selectables are affected depends on their own **Navigation** mode, so the Inspector setting you already know works as the switch:

| Navigation mode | Behavior |
| --- | --- |
| Automatic / Horizontal / Vertical | Upgraded: neighbors are chosen by the profile |
| Explicit | Untouched: your assigned targets are used |
| None | Untouched |

Other behaviors:

- Sliders and Scrollbars in Automatic mode still change their value along their own axis.
- Wrap Around in Horizontal/Vertical mode is supported.

## Navigation Profile

Create additional profiles from **Assets > Create > Onion > UI > Navigation Profile**, and assign them to a [Navigation Group](#navigation-group) to tune one area of the UI differently.

| Setting | Description |
| --- | --- |
| **Direction Tolerance** (0–90°) | How far a candidate may deviate from the move direction. `0` = same row/column only, `90` = anything ahead. |
| **Alignment Bias** (0–1) | `0` = nearest candidate first, `0.25` = same as Unity, `1` = strongly prefer the same row/column. |

- **Reset** sets the profile to Unity's built-in behavior (`90°`, `0.25`).
- **Preview** shows an example layout and which button gets selected in each direction. Hover a side of the preview to focus that direction.
- **Visualize** draws the upgraded navigation arrows in the Scene view, like Unity's own Visualize button. Turning it on hides Unity's arrows so the two don't overlap. Navigation Groups related to the selection are outlined, and a move that enters a group points at the group.

## Navigation Group

By default, upgraded navigation considers every Selectable in the scene. Add **Add Component > Onion > UI > Navigation Group** to a UI object to limit navigation to the Selectables under it, such as a side menu, a popup, or an inventory grid.

- A Selectable belongs to the nearest enabled group on itself or its parents. Moving from it only looks at Selectables in the same group, including those inside nested groups.
- From outside, a group's members are candidates as usual. Picking one **enters** the group.

| Setting | Description |
| --- | --- |
| **Default Selectable** | Selected when navigation enters the group. When empty, or when it is inactive or not interactable, the member picked by the move is selected. |
| **Remember Last Selection** | Entering the group selects the member that was selected last, before Default Selectable. Useful for tabs and panels you leave and come back to. |
| **Select On Enable** | When the group is enabled in Play Mode, its entry (the last selection when remembered, otherwise Default Selectable) is selected on the next frame. Useful for popups. If several groups are enabled at once, the one enabled last wins. |
| **Boundary** | `Contain` (default): navigation stops at the group's edge. `Pass Through`: when nothing is found inside, the search continues in the parent group or the scene. |
| **Wrap Around** | `Horizontal` / `Vertical` / `Both`: moving past the last member on that axis wraps to the other side, in any navigation mode. This is added to each Selectable's own Wrap Around. |
| **Profile** | The Navigation Profile used inside the group. When empty, the parent group's profile is used, and at the root the project-wide one. |

When nested groups are entered at once, the outermost group is tried first. Wrap Around stays inside the current group, so a group never passes through on an axis it wraps on.

Groups only affect upgraded modes (Automatic / Horizontal / Vertical) and do nothing while the upgrade is off. Explicit slots can't target a group.

## Turning it off

Uncheck the box in the **Navigation** header in **Project Settings > Onion > UI Upgrader**. Every Selectable then uses Unity's default navigation. The assigned profile is kept, so upgrading again restores the same behavior.
