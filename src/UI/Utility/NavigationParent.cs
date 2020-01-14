using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    /// <summary>Acts as a parent selection object, delegating selection behaviour to the first available child.</summary>
    public class NavigationParent : Selectable
    {
        // ---------[ Fields ]---------
        /// <summary>Children assigned to this parent.</summary>
        public List<NavigationChild> children = new List<NavigationChild>();

        // ---------[ Overrides ]---------
        /// <summary>Overrides the OnSelect behaviour passing selection to a child.</summary>
        public override void OnSelect(BaseEventData eventData)
        {
            NavigationChild newSelection = this.GetHighestPriorityChild();

            if(newSelection != null
               && this.IsActive())
            {
                this.StartCoroutine(DelaySelectChild(newSelection));
            }
            else
            {
                base.OnSelect(eventData);
            }
        }

        /// <summary>Gets the highest selection priority child.</summary>
        public NavigationChild GetHighestPriorityChild()
        {
            NavigationChild child = null;

            foreach(var navChild in this.children)
            {
                if(child == null
                   || navChild.priority < child.priority)
                {
                    child = navChild;
                }
            }

            return child;
        }

        /// <summary>Executes a delayed reselection action.</summary>
        private System.Collections.IEnumerator DelaySelectChild(NavigationChild navChild)
        {
            Debug.Assert(navChild != null);

            GameObject newSelectionObject = navChild.gameObject;

            // bubble down as necessary
            NavigationParent childAsParent = newSelectionObject.GetComponent<NavigationParent>();
            while(childAsParent != null)
            {
                navChild = childAsParent.GetHighestPriorityChild();
                if(navChild != null)
                {
                    newSelectionObject = navChild.gameObject;
                    childAsParent = newSelectionObject.GetComponent<NavigationParent>();
                }
                else
                {
                    childAsParent = null;
                }
            }

            yield return null;

            EventSystem.current.SetSelectedGameObject(newSelectionObject);
        }

        // ---------[ Move calulcations ]---------
        /// <summary>Handles the navigation calculation on behalf of the given child.</summary>
        public void NavigateForChild(NavigationChild child, AxisEventData eventData)
        {
            Debug.Assert(child != null);
            Debug.Assert(child.transform as RectTransform != null);

            // Get new selection
            UIBehaviour newSelection = null;
            switch(eventData.moveDir)
            {
                case MoveDirection.Right:
                {
                    newSelection = FindBehaviourOnRight(child);
                }
                break;

                case MoveDirection.Up:
                {
                    newSelection = FindBehaviourOnUp(child);
                }
                break;

                case MoveDirection.Left:
                {
                    newSelection = FindBehaviourOnLeft(child);
                }
                break;

                case MoveDirection.Down:
                {
                    newSelection = FindBehaviourOnDown(child);
                }
                break;
            }

            // if no next child
            if(newSelection == null)
            {
                eventData.selectedObject = null;

                NavigationChild thisAsChild = this.gameObject.GetComponent<NavigationChild>();

                if(thisAsChild != null
                   && thisAsChild.IsActive())
                {
                    thisAsChild.OnMove(eventData);
                }
                else
                {
                    base.OnMove(eventData);
                }

                if(eventData.selectedObject == null)
                {
                    eventData.selectedObject = child.gameObject;
                }
            }
            else
            {
                eventData.selectedObject = newSelection.gameObject;
            }
        }

        /// <summary>Find the GameObject on the navigation network to the left of the given one.</summary>
        public UIBehaviour FindBehaviourOnLeft(NavigationChild child)
        {
            if (child.navigation.mode == Navigation.Mode.Explicit)
            {
                Selectable s = child.navigation.selectOnLeft;

                if(s != null && s.IsActive())
                {
                    return s;
                }
            }
            if ((child.navigation.mode & Navigation.Mode.Horizontal) != 0)
            {
                NavigationChild c = this.FindChildInDirection(child,
                                                              transform.rotation * Vector3.left);
                return c;
            }
            return null;
        }

        /// <summary>Find the GameObject on the navigation network to the right of the given one.</summary>
        public UIBehaviour FindBehaviourOnRight(NavigationChild child)
        {
            if (child.navigation.mode == Navigation.Mode.Explicit)
            {
                Selectable s = child.navigation.selectOnRight;

                if(s != null && s.IsActive())
                {
                    return s;
                }
            }
            if ((child.navigation.mode & Navigation.Mode.Horizontal) != 0)
            {
                NavigationChild c = this.FindChildInDirection(child,
                                                              transform.rotation * Vector3.right);
                return c;
            }
            return null;
        }

        /// <summary>Find the GameObject on the navigation network above the given one</summary>
        public UIBehaviour FindBehaviourOnUp(NavigationChild child)
        {
            if (child.navigation.mode == Navigation.Mode.Explicit)
            {
                Selectable s = child.navigation.selectOnUp;

                if(s != null && s.IsActive())
                {
                    return s;
                }
            }
            if ((child.navigation.mode & Navigation.Mode.Vertical) != 0)
            {
                NavigationChild c = this.FindChildInDirection(child,
                                                              transform.rotation * Vector3.up);
                return c;
            }
            return null;
        }

        /// <summary>Find the GameObject on the navigation network below the given one.</summary>
        public UIBehaviour FindBehaviourOnDown(NavigationChild child)
        {
            if (child.navigation.mode == Navigation.Mode.Explicit)
            {
                Selectable s = child.navigation.selectOnDown;

                if(s != null && s.IsActive())
                {
                    return s;
                }
            }
            if ((child.navigation.mode & Navigation.Mode.Vertical) != 0)
            {
                NavigationChild c = this.FindChildInDirection(child,
                                                              transform.rotation * Vector3.down);
                return c;
            }
            return null;
        }

        /// <summary>Finds the next NavigationChild in the direction from the child.</summary>
        public NavigationChild FindChildInDirection(NavigationChild child, Vector3 direction)
        {
            Debug.Assert(child != null);

            IList<NavigationChild> nextChildren = UIUtilities.OrderByNext(child.transform as RectTransform,
                                                                          this.children,
                                                                          direction);

            // select next appropriate child
            foreach(NavigationChild navChild in nextChildren)
            {
                if(navChild.IsActive()
                   && navChild.navigation.mode != Navigation.Mode.None)
                {
                    return navChild;
                }
            }

            return null;
        }
    }
}
