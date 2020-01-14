using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    /// <summary>A component that can act as a child to a NavigationContainer.</summary>
    [RequireComponent(typeof(Selectable))]
    public class NavigationContainerElement : UIBehaviour, IMoveHandler
    {
        // ---------[ Constants & Statics ]---------
        /// <summary>None navigation to be assigned to the Selectable component.</summary>
        private static Navigation NAVIGATION_NONE = new Navigation()
        {
            mode = Navigation.Mode.None,
            selectOnRight = null,
            selectOnUp = null,
            selectOnLeft = null,
            selectOnDown = null,
        };

        // ---------[ Fields ]---------
        /// <summary>Selection priority for receiving selection when parent would be selected.</summary>
        public int priority = 0;

        /// <summary>Selectable component.</summary>
        private Selectable m_selectable = null;

        /// <summary>Container.</summary>
        private NavigationContainer m_container = null;

        /// <summary>Navigation data copied from selectable.</summary>
        private Navigation m_navCopy = NavigationContainerElement.NAVIGATION_NONE;

        // ------[ Accessors ]------
        /// <summary>Selectable component.</summary>
        private Selectable selectable { get { return this.m_selectable; } }

        /// <summary>Navigation data copied from selectable.</summary>
        public Navigation navigation
        {
            get
            {
                // copy over and clear navigation data
                if(this.IsActive()
                   && !this.m_selectable.navigation.Equals(NavigationContainerElement.NAVIGATION_NONE))
                {
                    this.m_navCopy = this.m_selectable.navigation;
                    this.m_selectable.navigation = NavigationContainerElement.NAVIGATION_NONE;
                }

                return this.m_navCopy;
            }
        }

        // ---------[ Initialization ]---------
        protected override void Awake()
        {
            base.Awake();

            this.m_selectable = this.GetComponent<Selectable>();
            this.m_container = this.GetComponentInParent<NavigationContainer>();
        }

        protected override void OnEnable()
        {
            // copy over and clear navigation data
            if(!this.m_selectable.navigation.Equals(NavigationContainerElement.NAVIGATION_NONE))
            {
                this.m_navCopy = this.m_selectable.navigation;
                this.m_selectable.navigation = NavigationContainerElement.NAVIGATION_NONE;
            }

            if(this.m_container != null)
            {
                this.m_container.children.Add(this);
            }
        }

        protected override void OnDisable()
        {
            // restore navigation data
            if(this.m_selectable.navigation.Equals(NavigationContainerElement.NAVIGATION_NONE))
            {
                this.m_selectable.navigation = this.m_navCopy;
            }

            if(this.m_container != null)
            {
                this.m_container.children.Remove(this);
            }
        }

        // ---------[ IMoveHandler Interface ]---------
        /// <summary>Process the move event.</summary>
        public void OnMove(AxisEventData eventData)
        {
            if(this.m_container == null) { return; }

            // already moved?
            if(eventData.selectedObject != this.gameObject)
            {
                NavigationContainerElement navSibling = eventData.selectedObject.GetComponent<NavigationContainerElement>();

                // check if new selection is the correct result
                if(navSibling != null
                   && navSibling.m_container != this.m_container)
                {
                    return;
                }
            }
            // eventData.selectedObject = this.gameObject;

            // calculate move
            this.m_container.NavigateForChildElement(this, eventData);
        }
    }
}
