using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Onion.UI.Editor {
    /// <summary>
    /// Makes sure the project has a UIUpgraderSettings asset, registered in EditorBuildSettings
    /// for the Editor and in Preloaded Assets for builds.
    /// </summary>
    [InitializeOnLoad]
    internal static class UIUpgraderSettingsProvider {
        internal const string SettingsDirectory = "Assets/Settings";
        private const string SettingsPath = SettingsDirectory + "/Onion_UIUpgraderSettings.asset";

        // The navigation-only settings asset that UIUpgraderSettings replaced.
        private const string LegacyNavigationConfigKey = "com.onion.uiupgrader.navigation";
        private const string LegacyNavigationSettingsPath = SettingsDirectory + "/Onion_NavigationSettings.asset";

        static UIUpgraderSettingsProvider() {
            // Never create assets on build servers.
            if (Application.isBatchMode) {
                return;
            }

            EditorApplication.delayCall += () => {
                RemoveLegacySettings();
                GetOrCreateSettings();
            };
        }

        internal static UIUpgraderSettings GetOrCreateSettings() {
            EditorBuildSettings.TryGetConfigObject(UIUpgraderSettings.ConfigKey, out UIUpgraderSettings settings);
            if (settings == null) {
                settings = FindSettings();
                if (settings == null) {
                    settings = CreateSettings();
                }

                EditorBuildSettings.AddConfigObject(UIUpgraderSettings.ConfigKey, settings, true);
            }

            RegisterToPreloadedAssets(settings);
            return settings;
        }

        private static void EnsureSettingsDirectory() {
            if (!AssetDatabase.IsValidFolder(SettingsDirectory)) {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
        }

        private static UIUpgraderSettings FindSettings() {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(UIUpgraderSettings)}");
            if (guids.Length == 0) {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (guids.Length > 1) {
                Debug.LogWarning($"Multiple {nameof(UIUpgraderSettings)} assets found. Using '{path}'.");
            }

            return AssetDatabase.LoadAssetAtPath<UIUpgraderSettings>(path);
        }

        private static UIUpgraderSettings CreateSettings() {
            EnsureSettingsDirectory();

            var settings = ScriptableObject.CreateInstance<UIUpgraderSettings>();
            AssetDatabase.CreateAsset(settings, AssetDatabase.GenerateUniqueAssetPath(SettingsPath));

            AssetDatabase.SaveAssets();
            return settings;
        }

        private static void RegisterToPreloadedAssets(UIUpgraderSettings settings) {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (Array.IndexOf(preloadedAssets, settings) >= 0) {
                return;
            }

            var assets = new List<Object>(preloadedAssets.Length + 1);
            foreach (var asset in preloadedAssets) {
                // Drop stale UIUpgraderSettings; leave everything else as is.
                if (asset is UIUpgraderSettings) {
                    continue;
                }

                assets.Add(asset);
            }

            assets.Add(settings);
            PlayerSettings.SetPreloadedAssets(assets.ToArray());
        }

        /// <summary>
        /// Deletes the old NavigationSettings asset and its registrations. Its settings aren't carried over.
        /// </summary>
        private static void RemoveLegacySettings() {
            string path = LegacyNavigationSettingsPath;
            if (EditorBuildSettings.TryGetConfigObject(LegacyNavigationConfigKey, out Object legacy) && legacy != null) {
                path = AssetDatabase.GetAssetPath(legacy);
            }

            EditorBuildSettings.RemoveConfigObject(LegacyNavigationConfigKey);
            if (!AssetDatabase.DeleteAsset(path)) {
                return;
            }

            Debug.Log($"Removed the old navigation settings asset '{path}'. The navigation upgrade is now in Project Settings > Onion > UI Upgrader.");

            // The deleted asset is left behind as a missing entry.
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            var assets = new List<Object>(preloadedAssets.Length);
            foreach (var asset in preloadedAssets) {
                if (asset != null) {
                    assets.Add(asset);
                }
            }

            PlayerSettings.SetPreloadedAssets(assets.ToArray());
        }
    }
}
