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
3. Assign a Navigation Profile, or click **New** next to the profile field to create one.

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

Create additional profiles from **Assets > Create > Onion > UI > Navigation Profile**.

| Setting | Description |
| --- | --- |
| **Direction Tolerance** (0–90°) | How far a candidate may deviate from the move direction. `0` = same row/column only, `90` = anything ahead. |
| **Alignment Bias** (0–1) | `0` = nearest candidate first, `0.25` = same as Unity, `1` = strongly prefer the same row/column. |

- **Reset** sets the profile to Unity's built-in behavior (`90°`, `0.25`).
- **Preview** shows an example layout and which button gets selected in each direction. Hover a side of the preview to focus that direction.
- **Visualize** draws the upgraded navigation arrows in the Scene view, like Unity's own Visualize button. Turning it on hides Unity's arrows so the two don't overlap.

## Turning it off

Uncheck the box in the **Navigation** header in **Project Settings > Onion > UI Upgrader**. Every Selectable then uses Unity's default navigation. The assigned profile is kept, so upgrading again restores the same behavior.
