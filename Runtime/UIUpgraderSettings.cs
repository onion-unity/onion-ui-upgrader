using Onion.UI.Navigation;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Onion.UI {
    /// <summary>
    /// Project-wide settings for the whole package, one section per upgrade. Included in builds
    /// through Preloaded Assets; in the Editor it is looked up through EditorBuildSettings.
    /// </summary>
    internal sealed class UIUpgraderSettings : ScriptableObject {
        internal const string ConfigKey = "com.onion.uiupgrader";

        private static UIUpgraderSettings _instance;

        [SerializeField]
        internal NavigationSettings navigation = new NavigationSettings();

        internal static UIUpgraderSettings instance {
            get {
#if UNITY_EDITOR
                if (_instance == null) {
                    EditorBuildSettings.TryGetConfigObject(ConfigKey, out _instance);
                }
#endif
                return _instance;
            }
        }

#if !UNITY_EDITOR
        private void OnEnable() {
            _instance = this;
        }
#endif
    }
}
