using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Onion.UI.Navigation {
    [AddComponentMenu("Onion/UI/Navigation Group")]
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public sealed class NavigationGroup : UIBehaviour {
        private const int InitialGroupCapacity = 4;
        
        private CanvasGroup _canvasGroup;

        internal bool isRoot { get; private set; } = false;
        internal NavigationGroup parent { get; private set; }
        internal List<NavigationGroup> children { get; private set; }
        private bool _isInitialized = false;

        protected override void Awake() {
            _canvasGroup = GetComponent<CanvasGroup>();

            InitializeIfNeeded();
        }

        protected override void OnDestroy() {
            base.OnDestroy();

            if (parent != null) {
                parent.Unregister(this);
            }
        }

        private void InitializeIfNeeded() {
            if (_isInitialized) {
                return;
            }

            children = new(InitialGroupCapacity);
            if (transform.parent != null) {
                parent = transform.parent.GetComponentInParent<NavigationGroup>();

                if (parent != null) {
                    parent.Register(this);

                    isRoot = false;
                }
                else {
                    isRoot = true;
                }
            }
            else {
                isRoot = true;
            }
        }

        protected override void OnTransformParentChanged() {
            base.OnTransformParentChanged();

            if (_isInitialized) {
                if (parent != null) {
                    parent.Unregister(this);
                }

                _isInitialized = false;
                InitializeIfNeeded();
            }
        }

        private void Register(NavigationGroup child) {
            if (!children.Contains(child)) {
                children.Add(child);
            }
        }

        private void Unregister(NavigationGroup child) {
            if (children.Contains(child)) {
                children.Remove(child);
            }
        }

        public void Activate() {
            
        }

        public void Deactivate() {

        }
    }
}