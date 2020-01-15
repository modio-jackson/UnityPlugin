using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    /// <summary>A component that can act as a child to a NavigationContainer.</summary>
    /// <remarks>It is important to the functionality of this component that it occurs in the Script
    /// Execution order after both the Selectable component. Consider this if altering the execution
    /// order, or if encountering unexpected behaviour.</remarks>
    /// <remarks>Furthermore, changes made to the sibling Selectable component's navigation data
    /// while this component is enabled will also cause unexpected behaviour.</remarks>
    [RequireComponent(typeof(Selectable))]
    [DisallowMultipleComponent]
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
        /// <summary>Selectable component.</summary>
        private Selectable m_selectable = null;

        /// <summary>Container.</summary>
        private NavigationContainer m_parentContainer = null;

        /// <summary>Navigation data copied from selectable.</summary>
        private Navigation m_navCopy = NavigationContainerElement.NAVIGATION_NONE;

        // ------[ Accessors ]------
        /// <summary>Selectable component.</summary>
        private Selectable selectable { get { return this.m_selectable; } }

        /// <summary>Navigation data copied from selectable.</summary>
        public Navigation navigation { get { return this.m_navCopy; } }

        // ---------[ Initialization ]---------
        protected override void Awake()
        {
            base.Awake();

            this.m_selectable = this.GetComponent<Selectable>();
        }

        protected override void OnEnable()
        {
            this.m_navCopy = NavigationContainerElement.NAVIGATION_NONE;

            // attempt to get parent container
            this.m_parentContainer = null;
            if(this.transform.parent != null)
            {
                this.m_parentContainer = this.transform.parent.gameObject.GetComponentInParent<NavigationContainer>();
            }

            // cancel enabling if there's no container
            if(this.m_parentContainer == null)
            {
                this.enabled = false;
                return;
            }

            // good to go
            base.OnEnable();

            this.m_parentContainer.children.Add(this);

            // copy over and clear navigation data
            if(!this.m_selectable.navigation.Equals(NavigationContainerElement.NAVIGATION_NONE))
            {
                this.m_navCopy = this.m_selectable.navigation;
                this.m_selectable.navigation = NavigationContainerElement.NAVIGATION_NONE;
            }
        }

        protected override void OnDisable()
        {
            if(this.m_parentContainer != null)
            {
                this.m_parentContainer.children.Remove(this);
            }

            if(EventSystem.current != null
               && EventSystem.current.currentSelectedGameObject == this.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(this.m_parentContainer.gameObject);
            }

            // restore navigation data
            if(this.m_selectable != null
               && this.m_selectable.navigation.Equals(NavigationContainerElement.NAVIGATION_NONE))
            {
                this.m_selectable.navigation = this.m_navCopy;
            }

            base.OnDisable();
        }

        // ---------[ IMoveHandler Interface ]---------
        /// <summary>Process the move event.</summary>
        public void OnMove(AxisEventData eventData)
        {
            Debug.Assert(this.m_selectable.navigation.Equals(NavigationContainerElement.NAVIGATION_NONE),
                         "[mod.io] The sibling Selectable component's navigation data has been"
                         + " altered during runtime. This will cause undesirable behaviour.");

            if(this.m_parentContainer == null) { return; }

            // calculate move
            this.m_parentContainer.NavigateForChildElement(this, eventData);
        }
    }
}
