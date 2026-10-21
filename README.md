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

Requires Unity 6000.0 or later and uGUI (`com.unity.ugui`) 2.0.0 or later.

### Setup

Nothing to add to your scenes. The upgrade is **off** by default, so installing the package changes nothing until you turn it on.

1. Open **Project Settings > Onion > UI Upgrader**.
2. Check the box in the **Navigation** header.
3. Assign a Navigation Profile, or click **+** next to the profile field to create one.

On first load, the package creates `Assets/Settings/Onion_UIUpgraderSettings`, the project-wide settings for the whole package.

## How it works

At runtime, the currently selected Selectable gets its neighbors computed when it is selected and while move input is held, so nothing is computed while the UI sits idle (with a custom input module, whose move input can't be read, it is computed every frame). The result is written as Explicit navigation, and Unity performs the actual move as usual. When the selection moves away, the original navigation is restored. Scene data is never changed, and at most one Selectable is modified at a time. In Play Mode, the selected Selectable's inspector therefore shows Explicit navigation with the computed neighbors; that is expected.

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
| **Restore On Disable** | When the group is disabled in Play Mode while the selection is in it, what was selected just before the group was enabled is selected again on the next frame. Useful for popups opened over a menu that stays enabled: closing the popup returns to the button that opened it. Stacked popups return one level at a time. Nothing is restored when the group is destroyed or its scene unloaded, when that Selectable is no longer active and interactable (Selection Recovery then takes over), or when a script has already selected something outside the group. |
| **Boundary** | `Contain` (default): navigation stops at the group's edge. `Pass Through`: when nothing is found inside, the search continues in the parent group or the scene. |
| **Wrap Around** | `Horizontal` / `Vertical` / `Both`: moving past the last member on that axis wraps to the other side, in any navigation mode. This is added to each Selectable's own Wrap Around. |
| **Profile** | The Navigation Profile used inside the group. When empty, the parent group's profile is used, and at the root the project-wide one. |

When nested groups are entered at once, the outermost group is tried first. Wrap Around stays inside the current group, so a group never passes through on an axis it wraps on.

Groups only affect upgraded modes (Automatic / Horizontal / Vertical) and do nothing while the upgrade is off. Explicit slots can't target a group.

## Selection Recovery

With a keyboard or gamepad, uGUI stops responding once nothing is selected: after clicking empty space, or when the selected object is disabled or destroyed. While the Navigation upgrade is on, **Recover Selection** in the Navigation section fixes this. It is on by default; uncheck it to keep Unity's behavior.

When the selection is lost, the next move selects a usable Selectable instead of moving. The first one found wins:

1. The last selection, if it is still active and interactable.
2. Each [Navigation Group](#navigation-group) the last selection was in that is still enabled, innermost first: its entry (last selection when remembered, otherwise Default Selectable), else its member nearest to where the last selection was.
3. The Selectable nearest to where the last selection was.

Nothing is recovered until something has been selected once. The selection itself is left alone until the move, so `EventSystem.currentSelectedGameObject` stays `null` while waiting. Move input is read from `StandaloneInputModule` or `InputSystemUIInputModule`; custom input modules are not supported.

## Focus

uGUI handles hover and selection separately. After selecting A with the keyboard, hovering B highlights both, and a clicked button stays selected after the mouse leaves. The **Focus** section tracks which input the UI is being used with and settles on one focused Selectable. Hovering never changes the EventSystem's selection, so selection sounds, scrolling to the selection, and other selection-driven behavior don't react to hover. Check the box in the **Focus** header to turn it on. It is off by default.

| Input mode | Switched by | Focused |
| --- | --- | --- |
| **Navigation** (initial) | Move or Submit (and optionally Cancel) | The selection |
| **Pointer** | Moving the pointer, pressing a pointer button, or a touch | The hovered Selectable, when it is interactable. Otherwise nothing. |

A layout moving under a still cursor or the scroll wheel doesn't switch to Pointer mode. Clicking still selects as usual, so input fields, Cancel, and Navigation Group features keep working in Pointer mode.

| Setting | Description |
| --- | --- |
| **Continue From Hover** | When switching to Navigation, the hovered Selectable becomes the selection before the input is handled, so the move or submit starts from what was highlighted. Off: it starts from the existing selection, like Unity. |
| **Reveal On First Input** | When switching to Navigation and the selection wasn't highlighted, the first move or submit only shows the selection. A held move goes on after the usual repeat delay. When nothing is selected, Selection Recovery handles the first move instead. |
| **Cancel Switches Mode** | Cancel also switches to Navigation. Off by default, so closing a popup with Esc doesn't bring up a highlight for mouse users. |
| **Project Wide Style** | The [Selectable Style](#transition-upgrader) of Transition Upgraders that don't set their own. Click **+** to create one. |

Scripts read the result from `Onion.UI.Focus.UIFocus`: `mode`, `focused`, and the `modeChanged` / `focusChanged` events, updated once per frame in LateUpdate. While the Focus upgrade is off, `mode` is always `Navigation` and `focused` is the selection. The Focus section only decides what is focused; a [Transition Upgrader](#transition-upgrader) shows it. Input is read from `StandaloneInputModule` or `InputSystemUIInputModule`; custom input modules are not supported.

## Transition Upgrader

Add **Add Component > Onion > UI > Transition Upgrader** next to a Button, Toggle, or any other Selectable to show it in one of four states, so only the focused Selectable is highlighted. It needs the [Focus](#focus) section to be on; while it is off, the Selectable behaves as in Unity.

| State | When |
| --- | --- |
| **Disabled** | Not interactable (including through a CanvasGroup) |
| **Pressed** | The pointer is held down on it while it is hovered, or Submit is held while it is selected |
| **Focused** | It is the focus: hovered in Pointer mode, selected in Navigation mode |
| **Normal** | Otherwise |

How each state looks comes from a **Selectable Style** asset (**Assets > Create > Onion > UI > Selectable Style**), shared by every Selectable using it. A style is a list of effects, applied in order; click **+** to add one.

| Effect | Description |
| --- | --- |
| **Selectable Transition** | Uses the Selectable's own Transition settings (Color Tint, Sprite Swap or Animation). In a new style by default, so your existing setup keeps working with the problems fixed. |
| **Color Tint** / **Sprite Swap** / **Animation** | The same transitions with their values set in the style, one per slot (Normal, Highlighted, Pressed, Selected, Disabled). |

Focused uses the **Highlighted** slot in Pointer mode and the **Selected** slot in Navigation mode, so a keyboard or gamepad focus can look stronger than a hover. With Unity's default colors both look the same.

- While a Transition Upgrader with the Selectable Transition effect is active, the Selectable's **Transition** is set to None, so Unity doesn't apply it as well, and restored when it stops. Avoid changing `transition` from scripts meanwhile.
- Submit is still handled when pressed (`onClick` runs on key down); only the look is held until release.
- Your own effects show up in the style's **+** menu. When each state is just a different value, derive from `StateValueEffect<T>` and implement one method; the value for the state is picked for you:

  ```csharp
  using Onion.UI.Selectables;
  using UnityEngine;
  using UnityEngine.UI;

  [System.Serializable]
  public sealed class ScaleEffect : StateValueEffect<Vector3> {
      // Defaults for a newly added effect (otherwise every slot is zero).
      public ScaleEffect() {
          values = new StateValues<Vector3>(Vector3.one, Vector3.one * 1.05f, Vector3.one * 0.95f, Vector3.one * 1.05f, Vector3.one);
      }

      protected override void Apply(Selectable target, object data, Vector3 scale, bool instant) {
          target.transform.localScale = scale;
      }
  }
  ```

  For anything else, such as a sound when focused, derive from `SelectableEffect` and implement `Apply(target, data, state, mode, instant)`. A style is shared, so keep per-Selectable state in the object returned by `CreateData`, and undo your changes in `Release`.
- Scripts can read the current state from `TransitionUpgrader.state`.

## Hotkey Upgrader

Add **Add Component > Onion > UI > Hotkey Upgrader** (or **Upgrade > Add Hotkey Upgrader** in the context menu of a Button or Toggle) to press it with a key or gamepad button, such as B for Back or X for Sort. The hotkey sends Submit, so it acts exactly like pressing the Button or Toggle with Submit, and the selection doesn't move. It works without any settings section, independently of Navigation and Focus.

- **Binding:** with the Input System package, an Input Action: set the key right in the inspector, or reference an action from an asset. A referenced action that is off is enabled while a Hotkey Upgrader needs it. Without the Input System, a `KeyCode` or an Input Manager button name.
- **Same key on several:** only one reacts, the one enabled most recently that is active and interactable. A popup's Close button wins over the Back button behind it, and the Back button wins again once the popup closes. Pressing two different keys together presses both. If the enable order differs from what's on top (e.g. a button behind an open popup is disabled and enabled again), the wrong one wins; blocking the background with a CanvasGroup (not interactable) avoids it.
- **Ignored:** while an InputField or TMP_InputField is being edited, and when the same frame's Submit, Cancel or move would already be handled by the selection (e.g. Enter as a hotkey while another button is selected).
- **Pressed while held:** with a [Transition Upgrader](#transition-upgrader) on the same Selectable, it shows as Pressed until the key is released.
- Supported on Button and Toggle. InputField and Dropdown take the selection on Submit, so they aren't supported.

## Hold Button / Hold Toggle

For buttons that shouldn't be pressed by accident, such as Delete Save or Quit, use **Hold Button** or **Hold Toggle** (Add Component > Onion > UI). They click or switch only after being held for **Hold Duration** seconds (unscaled time); a shorter press does nothing. To change an existing one, use **Upgrade > Convert to Hold Button** in the context menu of a Button (**Convert to Hold Toggle** on a Toggle). They derive from Button and Toggle, so the values and every reference to the component are kept.

- **Holding:** a left pointer press holds while it stays down over the Selectable; moving out cancels, like Unity's click. Submit holds while the submit input is held and the Selectable stays selected. A [Hotkey Upgrader](#hotkey-upgrader) on it holds while its key is held, with nothing to set up.
- **It fires once the time is reached**, without waiting for the release, and not again until released and pressed again.
- **Gauge:** **On Hold Progress** is called with the progress from 0 to 1 while held, 1 when it fires, and 0 when released or cancelled. Hook up an Image's `fillAmount` to it in the inspector for a simple gauge. Scripts can also read `holdProgress`.
- **Limits:** these are subclasses, so a project's own Button subclass can't be combined with them. With a custom input module, Submit can't be read as held, so only the pointer and hotkeys work. A held Submit doesn't show Unity's Pressed flash; add a [Transition Upgrader](#transition-upgrader) to show Pressed while held.

## Turning it off

Uncheck the box in the **Navigation** header in **Project Settings > Onion > UI Upgrader**. Every Selectable then uses Unity's default navigation. The assigned profile is kept, so upgrading again restores the same behavior. Unchecking the **Focus** header likewise returns to Unity's hover and selection behavior.

## License

MIT. See [LICENSE](LICENSE).
