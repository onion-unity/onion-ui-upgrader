using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Onion.UI.Layout {
    /// <summary>
    /// How one child of a <see cref="FlexLayoutGroup"/> is placed across the group's direction.
    /// </summary>
    public enum AlignSelf {
        /// <summary>Uses the group's Child Alignment.</summary>
        Auto,
        Start,
        Center,
        End,
        /// <summary>Sized to the line (or the group's inner size without Wrap), even when the group doesn't control child sizes.</summary>
        Stretch,
    }

    /// <summary>
    /// Per-child settings for a <see cref="FlexLayoutGroup"/> parent. Does nothing under other parents.
    /// </summary>
    [AddComponentMenu("Onion/UI/Flex Item")]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class FlexItem : UIBehaviour {
        [SerializeField]
        [Tooltip("Placement across the parent's direction. Auto uses its Child Alignment.\nStretch sizes this child to the line even when Control Child Size is off; then this child's own size doesn't count toward the line's size.")]
        private AlignSelf _alignSelf = AlignSelf.Auto;

        public AlignSelf alignSelf {
            get => _alignSelf;
            set {
                if (_alignSelf == value) return;
                _alignSelf = value;
                SetDirty();
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable() {
            SetDirty();
            base.OnDisable();
        }

        protected override void OnTransformParentChanged() {
            SetDirty();
        }

        protected override void OnBeforeTransformParentChanged() {
            SetDirty();
        }

        protected override void OnDidApplyAnimationProperties() {
            SetDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate() {
            SetDirty();
        }
#endif

        // Unlike LayoutElement, also while being disabled, so the parent lays this child out without it.
        private void SetDirty() {
            if (gameObject.activeInHierarchy)
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }
    }
}
