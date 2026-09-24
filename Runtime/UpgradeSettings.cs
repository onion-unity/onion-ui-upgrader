using System;
using UnityEngine;

namespace Onion.UI {
    /// <summary>
    /// Base of every section in <see cref="UIUpgraderSettings"/>: whether that upgrade replaces Unity's default behavior.
    /// </summary>
    [Serializable]
    internal abstract class UpgradeSettings {
        [Tooltip("Whether this upgrade is applied. Off keeps Unity's default behavior.")]
        [SerializeField]
        internal bool enabled;
    }
}
