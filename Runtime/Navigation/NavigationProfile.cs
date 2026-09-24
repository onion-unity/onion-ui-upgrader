using UnityEngine;

namespace Onion.UI.Navigation {
    /// <summary>
    /// Parameters that decide which Selectable is picked as the neighbor in each direction.
    /// </summary>
    [CreateAssetMenu(fileName = "NavigationProfile", menuName = "Onion/UI/Navigation Profile")]
    public sealed class NavigationProfile : ScriptableObject {
        // alignmentBias × this = exponent on cos(angle); 0.25 → 1, which is Unity's own scoring.
        private const float MaxAlignmentPower = 4f;

        [Tooltip("How far a candidate may deviate from the move direction, in degrees.\n0 = same row/column only, 90 = anything ahead (same as Unity).")]
        [Range(0f, 90f)]
        public float directionTolerance = 90f;

        [Tooltip("0 = pick the nearest candidate, 0.25 = same as Unity, 1 = strongly prefer the one in the same row/column.")]
        [Range(0f, 1f)]
        public float alignmentBias = 0.25f;

        // Exponent k in cost = distance / cos(angle)^k; 0 = distance only, 1 = Unity.
        internal float alignmentPower => alignmentBias * MaxAlignmentPower;
    }
}
