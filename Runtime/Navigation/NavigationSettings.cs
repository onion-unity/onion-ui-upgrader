using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Onion.UI.Navigation {
    /// <summary>
    /// Project-wide navigation settings. Included in builds through Preloaded Assets;
    /// in the Editor it is looked up through EditorBuildSettings.
    /// </summary>
    internal sealed class NavigationSettings : ScriptableObject {
        internal const string ConfigKey = "com.onion.uiupgrader.navigation";

        private static NavigationSettings _instance;

        [Tooltip("Profile used to upgrade Selectable navigation. Leave empty to turn the upgrade off and keep Unity's default navigation.")]
        [SerializeField]
        internal NavigationProfile defaultProfile;

        internal static NavigationSettings instance {
            get {
#if UNITY_EDITOR
                if (_instance == null) {
                    EditorBuildSettings.TryGetConfigObject(ConfigKey, out _instance);
                }
#endif
                return _instance;
            }
        }

        /// <summary>
        /// The default profile, or null when none is assigned (or there are no settings),
        /// which means the upgrade is off.
        /// </summary>
        internal static NavigationProfile profile {
            get {
                var settings = instance;
                return settings != null ? settings.defaultProfile : null;
            }
        }

#if !UNITY_EDITOR
        private void OnEnable() {
            _instance = this;
        }
#endif
    }
}
