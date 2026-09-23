# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

This is one package in the multi-package `Unity Package/` folder; the parent `../CLAUDE.md` covers the shared layout, the `_workspace` test project, and code conventions (K&R braces, `_camelCase` private fields, `Awaitable` async, `versionDefines`-gated optional deps). Read that first. This file only covers what is specific to this package.

## Build / test

There are no build scripts, tests, or CI. The package is compiled and verified by opening `../_workspace` (Unity 6000.0.58f2) in the Unity Editor, which references this package through a local `file:` path in `_workspace/Packages/manifest.json`. Git commands must run inside this folder (it is its own repo).

## Leftover template identifiers

The package was renamed from `onion-ui-tools` (`com.onion.uitools`) to `onion-ui-upgrader` (`com.onion.uiupgrader`). One template leftover remains:

- `Runtime/AssemblyInfo.cs` grants `InternalsVisibleTo("Onion.Template.Editor")` (left over from the template). The actual editor assembly is `Onion.UI.Editor`, so it can't see `internal` members such as `NavigationGroup.parent`/`children` until this is fixed.

## Assemblies and namespaces

- `Runtime/Onion.UI.Runtime.asmdef` and `Editor/Onion.UI.Editor.asmdef` (Editor-only, references Runtime). Both set `rootNamespace` to `Onion`, but code uses per-folder sub-namespaces, e.g. `Onion.UI.Navigation` for `Runtime/Navigation/`.

## NavigationGroup (early skeleton)

`Runtime/Navigation/NavigationGroup.cs` is a `UIBehaviour` that needs a `CanvasGroup` and forms a tree mirroring the transform hierarchy. On `Awake` each group finds the nearest ancestor `NavigationGroup` and registers with it as a child; groups without one are roots. It re-parents on `OnTransformParentChanged` and unregisters in `OnDestroy`. `Activate()`/`Deactivate()` are empty stubs, presumably for toggling the group's `CanvasGroup` and navigation focus.

Known gaps in the skeleton: `InitializeIfNeeded()` never sets `_isInitialized = true`, so the re-parent branch in `OnTransformParentChanged` never runs. Also, `Awake` doesn't call `base.Awake()`, unlike `OnDestroy`/`OnTransformParentChanged`.
