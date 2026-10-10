using System.Collections.Generic;
using UnityEngine;

namespace Onion.UI.Selectables {
    /// <summary>
    /// How Selectables with a <see cref="SelectableUpgrader"/> look in each state: a list of effects, applied in order.
    /// </summary>
    [CreateAssetMenu(fileName = "SelectableStyle", menuName = "Onion/UI/Selectable Style")]
    public sealed class SelectableStyle : ScriptableObject {
        internal const string EffectsField = nameof(_effects);

        [SerializeReference]
        private List<SelectableEffect> _effects = new() { new SelectableTransitionEffect() };

        public IReadOnlyList<SelectableEffect> effects => _effects;
    }
}
