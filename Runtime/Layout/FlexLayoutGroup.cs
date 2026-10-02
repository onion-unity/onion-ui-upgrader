using UnityEngine;
using UnityEngine.UI;

namespace Onion.UI.Layout {
    /// <summary>
    /// The axis a <see cref="FlexLayoutGroup"/> places its children along.
    /// </summary>
    public enum FlexDirection {
        Horizontal,
        Vertical,
    }

    /// <summary>
    /// How a <see cref="FlexLayoutGroup"/> distributes the space left over along its direction.
    /// </summary>
    public enum Justify {
        /// <summary>Uses Child Alignment, like Unity's layout groups.</summary>
        ChildAlignment,
        /// <summary>The first and last children touch the padding; the rest is split between the children.</summary>
        SpaceBetween,
        /// <summary>Each child gets equal space on both sides, so the edges get half a gap.</summary>
        SpaceAround,
        /// <summary>Every gap, including the edges, is equal.</summary>
        SpaceEvenly,
    }

    /// <summary>
    /// Replaces <see cref="HorizontalLayoutGroup"/> and <see cref="VerticalLayoutGroup"/>: the same settings give the same
    /// layout, and <see cref="justify"/> can also distribute the leftover space between the children.
    /// </summary>
    [AddComponentMenu("Onion/UI/Flex Layout Group")]
    // Not inherited from LayoutGroup; needed for the Edit Mode Update below.
    [ExecuteAlways]
    public class FlexLayoutGroup : LayoutGroup {
        [SerializeField] private FlexDirection _direction = FlexDirection.Horizontal;
        [SerializeField] private float _spacing = 0;

        [SerializeField]
        [Tooltip("How the leftover space along Direction is distributed between the children.\nWhen set to anything else, Child Force Expand along Direction is ignored, and Child Alignment only applies across Direction (except to a single child under Space Between). Has no effect when a child is flexible, since it takes the leftover.")]
        private Justify _justify = Justify.ChildAlignment;

        [SerializeField] private bool _reverseArrangement = false;
        [SerializeField] private bool _childControlWidth = false;
        [SerializeField] private bool _childControlHeight = false;
        [SerializeField] private bool _childScaleWidth = false;
        [SerializeField] private bool _childScaleHeight = false;
        // Off by default, unlike Unity's layout groups, so a new group leaves the leftover space to Child Alignment / Justify.
        [SerializeField] private bool _childForceExpandWidth = false;
        [SerializeField] private bool _childForceExpandHeight = false;

        public FlexDirection direction { get => _direction; set => SetProperty(ref _direction, value); }
        public float spacing { get => _spacing; set => SetProperty(ref _spacing, value); }
        public Justify justify { get => _justify; set => SetProperty(ref _justify, value); }
        public bool reverseArrangement { get => _reverseArrangement; set => SetProperty(ref _reverseArrangement, value); }
        public bool childControlWidth { get => _childControlWidth; set => SetProperty(ref _childControlWidth, value); }
        public bool childControlHeight { get => _childControlHeight; set => SetProperty(ref _childControlHeight, value); }
        public bool childScaleWidth { get => _childScaleWidth; set => SetProperty(ref _childScaleWidth, value); }
        public bool childScaleHeight { get => _childScaleHeight; set => SetProperty(ref _childScaleHeight, value); }
        public bool childForceExpandWidth { get => _childForceExpandWidth; set => SetProperty(ref _childForceExpandWidth, value); }
        public bool childForceExpandHeight { get => _childForceExpandHeight; set => SetProperty(ref _childForceExpandHeight, value); }

        private bool isVertical => _direction == FlexDirection.Vertical;

        /// <summary>
        /// Child Force Expand for <paramref name="axis"/>. Ignored along Direction while <see cref="justify"/> is set,
        /// since expanded children would take the leftover space Justify distributes.
        /// </summary>
        private bool ForceExpands(int axis) {
            if (_justify != Justify.ChildAlignment && axis == (isVertical ? 1 : 0)) return false;
            return axis == 0 ? _childForceExpandWidth : _childForceExpandHeight;
        }

        public override void CalculateLayoutInputHorizontal() {
            base.CalculateLayoutInputHorizontal();
            CalcAlongAxis(0);
        }

        public override void CalculateLayoutInputVertical() {
            CalcAlongAxis(1);
        }

        public override void SetLayoutHorizontal() {
            SetChildrenAlongAxis(0);
        }

        public override void SetLayoutVertical() {
            SetChildrenAlongAxis(1);
        }

        // The rest of this class follows HorizontalOrVerticalLayoutGroup, so the same settings give the same layout.

        private void CalcAlongAxis(int axis) {
            float combinedPadding = axis == 0 ? padding.horizontal : padding.vertical;
            bool controlSize = axis == 0 ? _childControlWidth : _childControlHeight;
            bool useScale = axis == 0 ? _childScaleWidth : _childScaleHeight;
            bool childForceExpandSize = ForceExpands(axis);

            float totalMin = combinedPadding;
            float totalPreferred = combinedPadding;
            float totalFlexible = 0;

            bool alongOtherAxis = isVertical ^ (axis == 1);
            for (int i = 0; i < rectChildren.Count; i++) {
                RectTransform child = rectChildren[i];
                GetChildSizes(child, axis, controlSize, childForceExpandSize, out float min, out float preferred, out float flexible);

                if (useScale) {
                    float scaleFactor = child.localScale[axis];
                    min *= scaleFactor;
                    preferred *= scaleFactor;
                    flexible *= scaleFactor;
                }

                if (alongOtherAxis) {
                    totalMin = Mathf.Max(min + combinedPadding, totalMin);
                    totalPreferred = Mathf.Max(preferred + combinedPadding, totalPreferred);
                    totalFlexible = Mathf.Max(flexible, totalFlexible);
                } else {
                    totalMin += min + _spacing;
                    totalPreferred += preferred + _spacing;
                    totalFlexible += flexible;
                }
            }

            if (!alongOtherAxis && rectChildren.Count > 0) {
                totalMin -= _spacing;
                totalPreferred -= _spacing;
            }
            totalPreferred = Mathf.Max(totalMin, totalPreferred);
            SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
        }

        private void SetChildrenAlongAxis(int axis) {
            float size = rectTransform.rect.size[axis];
            bool controlSize = axis == 0 ? _childControlWidth : _childControlHeight;
            bool useScale = axis == 0 ? _childScaleWidth : _childScaleHeight;
            bool childForceExpandSize = ForceExpands(axis);
            float alignmentOnAxis = GetAlignmentOnAxis(axis);

            int count = rectChildren.Count;
            bool alongOtherAxis = isVertical ^ (axis == 1);
            if (alongOtherAxis) {
                float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);

                for (int k = 0; k < count; k++) {
                    RectTransform child = rectChildren[_reverseArrangement ? count - 1 - k : k];
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out float min, out float preferred, out float flexible);
                    float scaleFactor = useScale ? child.localScale[axis] : 1f;

                    float requiredSpace = Mathf.Clamp(innerSize, min, flexible > 0 ? size : preferred);
                    float startOffset = GetStartOffset(axis, requiredSpace * scaleFactor);
                    if (controlSize) {
                        SetChildAlongAxisWithScale(child, axis, startOffset, requiredSpace, scaleFactor);
                    } else {
                        float offsetInCell = (requiredSpace - child.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxisWithScale(child, axis, startOffset + offsetInCell, scaleFactor);
                    }
                }
            } else {
                float pos = axis == 0 ? padding.left : padding.top;
                float gap = _spacing;
                float itemFlexibleMultiplier = 0;
                float surplusSpace = size - GetTotalPreferredSize(axis);

                if (surplusSpace > 0) {
                    if (GetTotalFlexibleSize(axis) == 0) {
                        if (!TryJustify(surplusSpace, count, ref pos, ref gap))
                            pos = GetStartOffset(axis, GetTotalPreferredSize(axis) - (axis == 0 ? padding.horizontal : padding.vertical));
                    } else if (GetTotalFlexibleSize(axis) > 0) {
                        itemFlexibleMultiplier = surplusSpace / GetTotalFlexibleSize(axis);
                    }
                }

                float minMaxLerp = 0;
                if (GetTotalMinSize(axis) != GetTotalPreferredSize(axis))
                    minMaxLerp = Mathf.Clamp01((size - GetTotalMinSize(axis)) / (GetTotalPreferredSize(axis) - GetTotalMinSize(axis)));

                for (int k = 0; k < count; k++) {
                    RectTransform child = rectChildren[_reverseArrangement ? count - 1 - k : k];
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out float min, out float preferred, out float flexible);
                    float scaleFactor = useScale ? child.localScale[axis] : 1f;

                    float childSize = Mathf.Lerp(min, preferred, minMaxLerp);
                    childSize += flexible * itemFlexibleMultiplier;
                    if (controlSize) {
                        SetChildAlongAxisWithScale(child, axis, pos, childSize, scaleFactor);
                    } else {
                        float offsetInCell = (childSize - child.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxisWithScale(child, axis, pos + offsetInCell, scaleFactor);
                    }
                    pos += childSize * scaleFactor + gap;
                }
            }
        }

        // Moves the start and widens the gaps by the leftover space. False when Child Alignment decides instead.
        private bool TryJustify(float surplus, int count, ref float pos, ref float gap) {
            switch (_justify) {
                case Justify.SpaceBetween:
                    // A single child has nothing to space from.
                    if (count < 2) return false;
                    gap += surplus / (count - 1);
                    return true;
                case Justify.SpaceAround:
                    pos += surplus / count / 2;
                    gap += surplus / count;
                    return true;
                case Justify.SpaceEvenly:
                    pos += surplus / (count + 1);
                    gap += surplus / (count + 1);
                    return true;
                default:
                    return false;
            }
        }

        private static void GetChildSizes(RectTransform child, int axis, bool controlSize, bool childForceExpand,
                                          out float min, out float preferred, out float flexible) {
            if (!controlSize) {
                min = child.sizeDelta[axis];
                preferred = min;
                flexible = 0;
            } else {
                min = LayoutUtility.GetMinSize(child, axis);
                preferred = LayoutUtility.GetPreferredSize(child, axis);
                flexible = LayoutUtility.GetFlexibleSize(child, axis);
            }

            if (childForceExpand)
                flexible = Mathf.Max(flexible, 1);
        }

#if UNITY_EDITOR
        private Vector2[] _sizes = new Vector2[10];

        // Like HorizontalOrVerticalLayoutGroup: in Edit Mode, children resized by hand don't mark the layout dirty by themselves.
        protected virtual void Update() {
            if (Application.isPlaying) return;

            int count = transform.childCount;
            if (count > _sizes.Length)
                _sizes = new Vector2[Mathf.Max(count, _sizes.Length * 2)];

            bool dirty = false;
            for (int i = 0; i < count; i++) {
                if (transform.GetChild(i) is RectTransform t && t.sizeDelta != _sizes[i]) {
                    dirty = true;
                    _sizes[i] = t.sizeDelta;
                }
            }

            if (dirty)
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
#endif
    }
}
