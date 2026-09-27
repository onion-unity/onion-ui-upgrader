using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Onion.UI.Navigation {
    /// <summary>
    /// The navigation section of <see cref="UIUpgraderSettings"/>.
    /// </summary>
    [Serializable]
    internal sealed class NavigationSettings : UpgradeSettings {
        /// <summary>
        /// The project-wide navigation profile.
        /// </summary>
        [Tooltip("Profile used to upgrade Selectable navigation.")]
        [SerializeField]
        [FormerlySerializedAs("defaultProfile")]
        internal NavigationProfile projectWideProfile;

        /// <summary>
        /// Whether a lost selection is recovered on the next move.
        /// </summary>
        [Tooltip("When nothing usable is selected, the next move selects a Selectable instead of doing nothing.")]
        [SerializeField]
        internal bool recoverSelection = true;

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

                return settings.navigation.projectWideProfile;
            }
        }

        /// <summary>
        /// Whether lost selections are recovered. Only while the upgrade is on.
        /// </summary>
        internal static bool recoversSelection => profile != null && UIUpgraderSettings.instance.navigation.recoverSelection;
    }
}
