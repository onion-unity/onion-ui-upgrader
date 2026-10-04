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

        [SerializeField]
        [Tooltip("Moves children that don't fit along Direction to a new line. Each line is laid out on its own, and the lines are placed across Direction by Child Alignment.\nVertical: the preferred width follows the height of the last layout pass, so it lags when the height is driven by a layout.")]
        private bool _wrap = false;

        [SerializeField] private float _spacing = 0;

        [SerializeField]
        [Tooltip("Space between lines when Wrap is on.")]
        private float _lineSpacing = 0;

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
        public bool wrap { get => _wrap; set => SetProperty(ref _wrap, value); }
        public float spacing { get => _spacing; set => SetProperty(ref _spacing, value); }
        public float lineSpacing { get => _lineSpacing; set => SetProperty(ref _lineSpacing, value); }
        public Justify justify { get => _justify; set => SetProperty(ref _justify, value); }
        public bool reverseArrangement { get => _reverseArrangement; set => SetProperty(ref _reverseArrangement, value); }
        public bool childControlWidth { get => _childControlWidth; set => SetProperty(ref _childControlWidth, value); }
        public bool childControlHeight { get => _childControlHeight; set => SetProperty(ref _childControlHeight, value); }
        public bool childScaleWidth { get => _childScaleWidth; set => SetProperty(ref _childScaleWidth, value); }
        public bool childScaleHeight { get => _childScaleHeight; set => SetProperty(ref _childScaleHeight, value); }
        public bool childForceExpandWidth { get => _childForceExpandWidth; set => SetProperty(ref _childForceExpandWidth, value); }
        public bool childForceExpandHeight { get => _childForceExpandHeight; set => SetProperty(ref _childForceExpandHeight, value); }

        // Laid out children in arrangement order, and where each line ends (exclusive) in it.
        private readonly System.Collections.Generic.List<RectTransform> _order = new();
        private readonly System.Collections.Generic.List<int> _lineEnds = new();

        private bool isVertical => _direction == FlexDirection.Vertical;
        private int mainAxis => isVertical ? 1 : 0;

        /// <summary>
        /// Child Force Expand for <paramref name="axis"/>. Ignored along Direction while <see cref="justify"/> is set,
        /// since expanded children would take the leftover space Justify distributes.
        /// </summary>
        private bool ForceExpands(int axis) {
            if (_justify != Justify.ChildAlignment && axis == mainAxis) return false;
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
            // Vertical wrap: the lines placed horizontally before were broken by the previous height.
            if (_wrap && isVertical)
                SetChildrenAlongAxis(0);
        }

        // The rest of this class follows HorizontalOrVerticalLayoutGroup, so the same settings give the same layout.
        // Wrap only adds separate paths (see "Wrap" below).

        private void CalcAlongAxis(int axis) {
            if (_wrap && axis != mainAxis) {
                CalcAcrossLines(axis);
                return;
            }

            float combinedPadding = axis == 0 ? padding.horizontal : padding.vertical;
            bool controlSize = axis == 0 ? _childControlWidth : _childControlHeight;
            bool useScale = axis == 0 ? _childScaleWidth : _childScaleHeight;
            bool childForceExpandSize = ForceExpands(axis);

            float totalMin = combinedPadding;
            float totalPreferred = combinedPadding;
            float totalFlexible = 0;
            float largestMin = 0;

            bool alongOtherAxis = isVertical ^ (axis == 1);
            for (int i = 0; i < rectChildren.Count; i++) {
                RectTransform child = rectChildren[i];
                GetChildSizes(child, axis, controlSize, childForceExpandSize, out float min, out float preferred, out float flexible);
                if (alongOtherAxis && !controlSize && AlignOf(child) == AlignSelf.Stretch)
                    min = preferred = flexible = 0;

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
                    largestMin = Mathf.Max(largestMin, min);
                }
            }

            if (!alongOtherAxis && rectChildren.Count > 0) {
                totalMin -= _spacing;
                totalPreferred -= _spacing;
            }
            // Wrapped, a line can be as short as its largest child; the preferred size stays one line.
            if (_wrap && !alongOtherAxis)
                totalMin = combinedPadding + largestMin;
            totalPreferred = Mathf.Max(totalMin, totalPreferred);
            SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
        }

        private void SetChildrenAlongAxis(int axis) {
            if (_wrap) {
                FillOrder();
                Read(_mainSizes, mainAxis);
                BreakLines();
                if (axis == mainAxis) {
                    for (int line = 0, from = 0; line < _lineEnds.Count; from = _lineEnds[line++]) {
                        GetLineTotals(from, _lineEnds[line], out float lineMin, out float linePreferred, out float lineFlexible);
                        PlaceAlong(axis, from, _lineEnds[line], lineMin, linePreferred, lineFlexible);
                    }
                } else {
                    Read(_crossSizes, axis);
                    PlaceAcrossLines(axis);
                }
                return;
            }

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

                    AlignSelf align = AlignOf(child);
                    if (align != AlignSelf.Auto) {
                        PlaceSelf(child, axis, align, axis == 0 ? padding.left : padding.top, innerSize, size, min, preferred, flexible, scaleFactor, controlSize);
                        continue;
                    }

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
                FillOrder();
                Read(_mainSizes, axis);
                PlaceAlong(axis, 0, count, GetTotalMinSize(axis), GetTotalPreferredSize(axis), GetTotalFlexibleSize(axis));
            }
        }

        // Align Self of a child's active FlexItem.
        private static AlignSelf AlignOf(RectTransform child) {
            return child.TryGetComponent(out FlexItem item) && item.isActiveAndEnabled ? item.alignSelf : AlignSelf.Auto;
        }

        // Places a child with its own Align Self across the main axis, in `space` starting at `start`
        // (the group's inner size, or the line). Auto children keep the code ported from Unity instead.
        // `flexibleLimit` caps a flexible child like Auto does: Unity caps it at the group's full size, not the inner
        // size, which only differs with negative padding.
        private void PlaceSelf(RectTransform child, int axis, AlignSelf align, float start, float space, float flexibleLimit,
                               float min, float preferred, float flexible, float scaleFactor, bool controlSize) {
            float alignment;
            float requiredSpace;
            if (align == AlignSelf.Stretch) {
                // Fill the space; when the min size doesn't fit, overflow like the group's Child Alignment.
                alignment = GetAlignmentOnAxis(axis);
                float stretched = scaleFactor != 0 ? space / scaleFactor : space;
                requiredSpace = controlSize ? Mathf.Max(min, stretched) : stretched;
            } else {
                alignment = align == AlignSelf.Start ? 0 : align == AlignSelf.Center ? 0.5f : 1;
                requiredSpace = Mathf.Clamp(space, min, flexible > 0 ? flexibleLimit : preferred);
            }

            float startOffset = start + (space - requiredSpace * scaleFactor) * alignment;
            if (controlSize || align == AlignSelf.Stretch) {
                SetChildAlongAxisWithScale(child, axis, startOffset, requiredSpace, scaleFactor);
            } else {
                float offsetInCell = (requiredSpace - child.sizeDelta[axis]) * alignment;
                SetChildAlongAxisWithScale(child, axis, startOffset + offsetInCell, scaleFactor);
            }
        }

        // Places _order[from..to) along the main axis (sizes from _mainSizes), given their totals
        // (padding included, like the group's own).
        private void PlaceAlong(int axis, int from, int to, float totalMin, float totalPreferred, float totalFlexible) {
            float size = rectTransform.rect.size[axis];
            bool controlSize = axis == 0 ? _childControlWidth : _childControlHeight;
            float alignmentOnAxis = GetAlignmentOnAxis(axis);
            ChildSizes sizes = _mainSizes;

            float pos = axis == 0 ? padding.left : padding.top;
            float gap = _spacing;
            float itemFlexibleMultiplier = 0;
            float surplusSpace = size - totalPreferred;

            if (surplusSpace > 0) {
                if (totalFlexible == 0) {
                    if (!TryJustify(surplusSpace, to - from, ref pos, ref gap))
                        pos = GetStartOffset(axis, totalPreferred - (axis == 0 ? padding.horizontal : padding.vertical));
                } else if (totalFlexible > 0) {
                    itemFlexibleMultiplier = surplusSpace / totalFlexible;
                }
            }

            float minMaxLerp = 0;
            if (totalMin != totalPreferred)
                minMaxLerp = Mathf.Clamp01((size - totalMin) / (totalPreferred - totalMin));

            for (int k = from; k < to; k++) {
                RectTransform child = _order[k];
                float scaleFactor = sizes.scale[k];

                float childSize = Mathf.Lerp(sizes.min[k], sizes.preferred[k], minMaxLerp);
                childSize += sizes.flexible[k] * itemFlexibleMultiplier;
                if (controlSize) {
                    SetChildAlongAxisWithScale(child, axis, pos, childSize, scaleFactor);
                } else {
                    float offsetInCell = (childSize - child.sizeDelta[axis]) * alignmentOnAxis;
                    SetChildAlongAxisWithScale(child, axis, pos + offsetInCell, scaleFactor);
                }
                pos += childSize * scaleFactor + gap;
            }
        }

        private void FillOrder() {
            _order.Clear();
            int count = rectChildren.Count;
            for (int k = 0; k < count; k++)
                _order.Add(rectChildren[_reverseArrangement ? count - 1 - k : k]);
        }

        // Child sizes along one axis, in _order. Read once per pass, so line breaking, line sizes and placement
        // don't query LayoutUtility (a component search per call) for the same child again.
        private sealed class ChildSizes {
            public float[] min = new float[0], preferred = new float[0], flexible = new float[0], scale = new float[0];
            // Across the main axis only.
            public AlignSelf[] align = new AlignSelf[0];

            public void Fill(System.Collections.Generic.List<RectTransform> order, int axis, bool across, bool controlSize, bool forceExpand, bool useScale) {
                int count = order.Count;
                if (min.Length < count) {
                    int capacity = Mathf.Max(count, min.Length * 2);
                    min = new float[capacity];
                    preferred = new float[capacity];
                    flexible = new float[capacity];
                    scale = new float[capacity];
                    align = new AlignSelf[capacity];
                }
                for (int k = 0; k < count; k++) {
                    GetChildSizes(order[k], axis, controlSize, forceExpand, out min[k], out preferred[k], out flexible[k]);
                    scale[k] = useScale ? order[k].localScale[axis] : 1f;
                    if (!across) continue;
                    align[k] = AlignOf(order[k]);
                    // Its size is driven to the line, so it would hold the line at its last size.
                    if (align[k] == AlignSelf.Stretch && !controlSize)
                        min[k] = preferred[k] = flexible[k] = 0;
                }
            }
        }

        private readonly ChildSizes _mainSizes = new(), _crossSizes = new();

        private void Read(ChildSizes sizes, int axis) {
            sizes.Fill(_order, axis, axis != mainAxis, axis == 0 ? _childControlWidth : _childControlHeight, ForceExpands(axis),
                axis == 0 ? _childScaleWidth : _childScaleHeight);
        }

        // ---- Wrap ----

        // Breaks _order into lines (sizes from _mainSizes): a child starts a new line when its preferred size
        // (with scale) doesn't fit after the line so far. A line always holds at least one child.
        private void BreakLines() {
            _lineEnds.Clear();
            int axis = mainAxis;
            float innerSize = rectTransform.rect.size[axis] - (axis == 0 ? padding.horizontal : padding.vertical);

            float lineSize = 0;
            for (int k = 0; k < _order.Count; k++) {
                float preferred = _mainSizes.preferred[k] * _mainSizes.scale[k];

                // Small tolerance, so children that fit exactly don't wrap from rounding.
                if (k > 0 && lineSize + _spacing + preferred > innerSize + 0.001f) {
                    _lineEnds.Add(k);
                    lineSize = preferred;
                } else {
                    lineSize += (k > 0 ? _spacing : 0) + preferred;
                }
            }
            if (_order.Count > 0)
                _lineEnds.Add(_order.Count);
        }

        // Same sums as CalcAlongAxis along the main axis, for one line.
        private void GetLineTotals(int from, int to, out float totalMin, out float totalPreferred, out float totalFlexible) {
            ChildSizes sizes = _mainSizes;
            totalMin = totalPreferred = mainAxis == 0 ? padding.horizontal : padding.vertical;
            totalFlexible = 0;
            for (int k = from; k < to; k++) {
                float scaleFactor = sizes.scale[k];
                totalMin += sizes.min[k] * scaleFactor + _spacing;
                totalPreferred += sizes.preferred[k] * scaleFactor + _spacing;
                totalFlexible += sizes.flexible[k] * scaleFactor;
            }
            if (to > from) {
                totalMin -= _spacing;
                totalPreferred -= _spacing;
            }
            totalPreferred = Mathf.Max(totalMin, totalPreferred);
        }

        // Cross-axis size of one line: the largest child, like the cross axis of an unwrapped group.
        private void GetLineCross(int from, int to, out float min, out float preferred, out float flexible) {
            ChildSizes sizes = _crossSizes;
            min = preferred = flexible = 0;
            for (int k = from; k < to; k++) {
                float scaleFactor = sizes.scale[k];
                min = Mathf.Max(min, sizes.min[k] * scaleFactor);
                preferred = Mathf.Max(preferred, sizes.preferred[k] * scaleFactor);
                flexible = Mathf.Max(flexible, sizes.flexible[k] * scaleFactor);
            }
            preferred = Mathf.Max(min, preferred);
        }

        // The cross-axis input: the lines stacked with Line Spacing. Breaking needs the main-axis size, which is
        // already set for horizontal (the layout system sets widths before computing heights) and is the previous
        // pass's height for vertical.
        private void CalcAcrossLines(int axis) {
            FillOrder();
            Read(_mainSizes, mainAxis);
            BreakLines();
            Read(_crossSizes, axis);
            float combinedPadding = axis == 0 ? padding.horizontal : padding.vertical;
            float totalMin = combinedPadding, totalPreferred = combinedPadding, totalFlexible = 0;
            for (int line = 0, from = 0; line < _lineEnds.Count; from = _lineEnds[line++]) {
                GetLineCross(from, _lineEnds[line], out float min, out float preferred, out float flexible);
                float spacing = line > 0 ? _lineSpacing : 0;
                totalMin += min + spacing;
                totalPreferred += preferred + spacing;
                totalFlexible = Mathf.Max(totalFlexible, flexible);
            }
            totalPreferred = Mathf.Max(totalMin, totalPreferred);
            SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
        }

        // Places the lines across the main axis. Lines get their preferred size (shrunk toward their min size like the
        // main axis when the group is too small) and are packed by Child Alignment. Inside a line, each child is placed
        // like on the cross axis of an unwrapped group, with the line as the available space.
        private void PlaceAcrossLines(int axis) {
            float combinedPadding = axis == 0 ? padding.horizontal : padding.vertical;
            float innerSize = rectTransform.rect.size[axis] - combinedPadding;
            bool controlSize = axis == 0 ? _childControlWidth : _childControlHeight;
            float alignmentOnAxis = GetAlignmentOnAxis(axis);
            ChildSizes sizes = _crossSizes;

            float totalMin = 0, totalPreferred = 0;
            for (int line = 0, from = 0; line < _lineEnds.Count; from = _lineEnds[line++]) {
                GetLineCross(from, _lineEnds[line], out float min, out float preferred, out _);
                float spacing = line > 0 ? _lineSpacing : 0;
                totalMin += min + spacing;
                totalPreferred += preferred + spacing;
            }
            float minMaxLerp = totalMin != totalPreferred ? Mathf.Clamp01((innerSize - totalMin) / (totalPreferred - totalMin)) : 1;
            float used = Mathf.Lerp(totalMin, totalPreferred, minMaxLerp);

            float pos = axis == 0 ? padding.left : padding.top;
            if (innerSize > used)
                pos += (innerSize - used) * alignmentOnAxis;

            for (int line = 0, from = 0; line < _lineEnds.Count; from = _lineEnds[line++]) {
                GetLineCross(from, _lineEnds[line], out float lineMin, out float linePreferred, out _);
                float lineSize = Mathf.Lerp(lineMin, linePreferred, minMaxLerp);

                for (int k = from; k < _lineEnds[line]; k++) {
                    RectTransform child = _order[k];
                    float scaleFactor = sizes.scale[k];

                    if (sizes.align[k] != AlignSelf.Auto) {
                        PlaceSelf(child, axis, sizes.align[k], pos, lineSize, lineSize, sizes.min[k], sizes.preferred[k], sizes.flexible[k], scaleFactor, controlSize);
                        continue;
                    }

                    float requiredSpace = Mathf.Clamp(lineSize, sizes.min[k], sizes.flexible[k] > 0 ? lineSize : sizes.preferred[k]);
                    float startOffset = pos + (lineSize - requiredSpace * scaleFactor) * alignmentOnAxis;
                    if (controlSize) {
                        SetChildAlongAxisWithScale(child, axis, startOffset, requiredSpace, scaleFactor);
                    } else {
                        float offsetInCell = (requiredSpace - child.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxisWithScale(child, axis, startOffset + offsetInCell, scaleFactor);
                    }
                }
                pos += lineSize + _lineSpacing;
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
