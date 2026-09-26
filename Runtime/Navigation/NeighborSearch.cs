using UnityEngine;

namespace Onion.UI.Navigation {
    /// <summary>
    /// Picks the neighbor in one direction among candidate rects, all expressed in the origin's local space.
    /// Shared by the runtime upgrader and the profile preview in the Editor, so both behave identically.
    /// </summary>
    internal struct NeighborSearch {
        private readonly Rect _from;
        private readonly Vector2 _direction;
        private readonly bool _horizontal;
        private readonly bool _wrap;
        private readonly float _maxAngle;
        private readonly float _alignmentPower;

        private int _best;
        private float _bestCost;
        private int _furthest;
        private float _furthestScore;

        internal NeighborSearch(Rect from, Vector2 direction, bool wrap, NavigationProfile profile)
            : this(from, direction, wrap, profile.directionTolerance, profile.alignmentPower) {
        }

        internal NeighborSearch(Rect from, Vector2 direction, bool wrap, float maxAngle, float alignmentPower) {
            _from = from;
            _direction = direction;
            _horizontal = direction.x != 0;
            _wrap = wrap;
            _maxAngle = maxAngle;
            _alignmentPower = alignmentPower;

            _best = -1;
            _bestCost = float.PositiveInfinity;
            _furthest = -1;
            _furthestScore = float.NegativeInfinity;
        }

        /// <summary>
        /// Index of the picked candidate, or -1 when there is none.
        /// </summary>
        internal int result => _best >= 0 ? _best : _furthest;

        /// <summary>
        /// Detection: the candidate's center must lie beyond the origin's leading edge, within the
        /// tolerance angle measured from the origin's rect (0° = center inside the origin's row/column).
        /// Scoring generalizes Unity's own: from the leading edge's center point to the candidate's
        /// center, cost = distance / cos(angle)^k. With k = 1 this orders candidates exactly like
        /// Unity's dot / distance² score.
        /// </summary>
        internal void Consider(int index, Rect to) {
            var offset = to.center - _from.center;
            float along = Vector2.Dot(offset, _direction);
            float halfMain = _horizontal ? _from.width * 0.5f : _from.height * 0.5f;
            float halfCross = _horizontal ? _from.height * 0.5f : _from.width * 0.5f;
            float crossOffset = Mathf.Abs(_horizontal ? offset.y : offset.x);
            float crossGap = Mathf.Max(0f, crossOffset - halfCross);

            // Relative to the center of the leading edge, like Unity.
            float ahead = along - halfMain;
            float sqrDistance = ahead * ahead + crossOffset * crossOffset;

            if (ahead > 0f) {
                if (!IsWithinTolerance(ahead, crossGap)) {
                    return;
                }

                // distance / cos^k == (distance² / ahead) * cos^(1 - k); k = 1 leaves Unity's term exactly.
                float cos = ahead / Mathf.Sqrt(sqrDistance);
                float cost = sqrDistance / ahead * Mathf.Pow(cos, 1f - _alignmentPower);
                if (cost < _bestCost) {
                    _best = index;
                    _bestCost = cost;
                }
            }
            else if (_wrap && ahead < 0f) {
                // Unity wraps to the furthest candidate behind (score = -dot * distance²); generalized
                // with the same cos^k alignment factor. Tolerance applies from the trailing edge.
                float behind = Mathf.Max(0f, -along - halfMain);
                if (!IsWithinTolerance(behind, crossGap)) {
                    return;
                }

                float cos = -ahead / Mathf.Sqrt(sqrDistance);
                float score = -ahead * sqrDistance * Mathf.Pow(cos, _alignmentPower - 1f);
                if (score > _furthestScore) {
                    _furthest = index;
                    _furthestScore = score;
                }
            }
        }

        private bool IsWithinTolerance(float mainGap, float crossGap) {
            return Mathf.Atan2(crossGap, mainGap) * Mathf.Rad2Deg <= _maxAngle;
        }
    }
}
