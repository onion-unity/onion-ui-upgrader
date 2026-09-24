using System;
using UnityEngine;

namespace Onion.UI.Navigation {
    /// <summary>
    /// The navigation section of <see cref="UIUpgraderSettings"/>.
    /// </summary>
    [Serializable]
    internal sealed class NavigationSettings : UpgradeSettings {
        [Tooltip("Profile used to upgrade Selectable navigation.")]
        [SerializeField]
        internal NavigationProfile defaultProfile;

        /// <summary>
        /// The default profile, or null when the upgrade is off, no profile is assigned,
        /// or there are no settings. Null means Unity's default navigation is used.
        /// </summary>
        internal static NavigationProfile profile {
            get {
                var settings = UIUpgraderSettings.instance;
                if (settings == null || !settings.navigation.enabled) {
                    return null;
                }

                return settings.navigation.defaultProfile;
            }
        }
    }
}
