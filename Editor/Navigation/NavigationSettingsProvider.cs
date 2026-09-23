using System;
using System.Collections.Generic;
using Onion.UI.Navigation;
using UnityEditor;
using UnityEngine;

namespace Onion.UI.Editor {
    /// <summary>
    /// Makes sure the project has a NavigationSettings asset (with a default profile),
    /// registered in EditorBuildSettings for the Editor and in Preloaded Assets for builds.
    /// </summary>
    [InitializeOnLoad]
    internal static class NavigationSettingsProvider {
        private const string SettingsDirectory = "Assets/Settings";
        private const string SettingsPath = SettingsDirectory + "/Onion_NavigationSettings.asset";
        private const string ProfilePath = SettingsDirectory + "/Onion_DefaultNavigationProfile.asset";

        static NavigationSettingsProvider() {
            // Never create assets on build servers.
            if (Application.isBatchMode) {
                return;
            }

            EditorApplication.delayCall += () => GetOrCreateSettings();
        }

        internal static NavigationSettings GetOrCreateSettings() {
            EditorBuildSettings.TryGetConfigObject(NavigationSettings.ConfigKey, out NavigationSettings settings);
            if (settings == null) {
                settings = FindSettings();
                if (settings == null) {
                    settings = CreateSettings();
                }

                EditorBuildSettings.AddConfigObject(NavigationSettings.ConfigKey, settings, true);
            }

            RegisterToPreloadedAssets(settings);
            return settings;
        }

        private static NavigationSettings FindSettings() {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(NavigationSettings)}");
            if (guids.Length == 0) {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (guids.Length > 1) {
                Debug.LogWarning($"Multiple {nameof(NavigationSettings)} assets found. Using '{path}'.");
            }

            return AssetDatabase.LoadAssetAtPath<NavigationSettings>(path);
        }

        private static NavigationSettings CreateSettings() {
            if (!AssetDatabase.IsValidFolder(SettingsDirectory)) {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            var profile = ScriptableObject.CreateInstance<NavigationProfile>();
            AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(ProfilePath));

            var settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.defaultProfile = profile;
            AssetDatabase.CreateAsset(settings, AssetDatabase.GenerateUniqueAssetPath(SettingsPath));

            AssetDatabase.SaveAssets();
            return settings;
        }

        private static void RegisterToPreloadedAssets(NavigationSettings settings) {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (Array.IndexOf(preloadedAssets, settings) >= 0) {
                return;
            }

            var assets = new List<UnityEngine.Object>(preloadedAssets.Length + 1);
            foreach (var asset in preloadedAssets) {
                // Drop stale NavigationSettings; leave everything else as is.
                if (asset is NavigationSettings) {
                    continue;
                }

                assets.Add(asset);
            }

            assets.Add(settings);
            PlayerSettings.SetPreloadedAssets(assets.ToArray());
        }
    }
}
