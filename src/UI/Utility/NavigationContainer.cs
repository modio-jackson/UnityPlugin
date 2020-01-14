using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    /// <summary>Acts as a container for navigation elements lower in the hierachy.</summary>
    public class NavigationContainer : Selectable
    {
        // ---------[ Fields ]---------
        /// <summary>Prioritized list to pass of elements to pass selection to.</summary>
        public Selectable[] selectionPriority = new Selectable[0];

        /// <summary>Children assigned to this container.</summary>
        [System.NonSerialized]
        public List<NavigationContainerElement> children = new List<NavigationContainerElement>();

        // ---------[ Initialization ]---------
        protected override void Awake()
        {
            Selectable[] childSelectables = gameObject.GetComponentsInChildren<Selectable>(true);

            foreach(var selectable in childSelectables)
            {
                if(selectable.gameObject == this.gameObject) { continue; }

                NavigationContainerElement element = selectable.gameObject.GetComponent<NavigationContainerElement>();
                if(element == null)
                {
                    selectable.gameObject.AddComponent<NavigationContainerElement>();
                }
            }
        }

        // ---------[ Overrides ]---------
        /// <summary>Overrides the OnSelect behaviour passing selection to a child.</summary>
        public override void OnSelect(BaseEventData eventData)
        {
            NavigationContainerElement newSelection = this.GetHighestPriorityChildElement();

            if(newSelection != null
               && this.IsActive())
            {
                this.StartCoroutine(DelayChildSelection(newSelection));
            }
            else
            {
                base.OnSelect(eventData);
            }
        }

        /// <summary>Gets the highest selection priority child element.</summary>
        public NavigationContainerElement GetHighestPriorityChildElement()
        {
            // check priority list
            for(int i = 0; i < this.selectionPriority.Length; ++i)
            {
                Selectable childElement = this.selectionPriority[i];

                if(childElement != null
                   && childElement.IsActive())
                {
                    return childElement.gameObject.GetComponent<NavigationContainerElement>();
                }
            }

            // check remaining children
            foreach(NavigationContainerElement childElement in this.children)
            {
                if(childElement != null
                   && childElement.IsActive())
                {
                    return childElement;
                }
            }

            return null;
        }

        /// <summary>Executes a delayed reselection action.</summary>
        private System.Collections.IEnumerator DelayChildSelection(NavigationContainerElement childElement)
        {
            Debug.Assert(childElement != null);

            GameObject newSelectionObject = childElement.gameObject;

            // bubble down as necessary
            NavigationContainer childAsContainer = newSelectionObject.GetComponent<NavigationContainer>();
            while(childAsContainer != null)
            {
                childElement = childAsContainer.GetHighestPriorityChildElement();
                if(childElement != null)
                {
                    newSelectionObject = childElement.gameObject;
                    childAsContainer = newSelectionObject.GetComponent<NavigationContainer>();
                }
                else
                {
                    childAsContainer = null;
                }
            }

            yield return null;

            EventSystem.current.SetSelectedGameObject(newSelectionObject);
        }

        // ---------[ Move calulcations ]---------
        /// <summary>Handles the navigation calculation on behalf of the given child element.</summary>
        public void NavigateForChildElement(NavigationContainerElement childElement, AxisEventData eventData)
        {
            Debug.Assert(childElement != null);
            Debug.Assert(childElement.transform as RectTransform != null);

            // Get new selection
            UIBehaviour newSelection = null;
            switch(eventData.moveDir)
            {
                case MoveDirection.Right:
                {
                    newSelection = FindBehaviourOnRight(childElement);
                }
                break;

                case MoveDirection.Up:
                {
                    newSelection = FindBehaviourOnUp(childElement);
                }
                break;

                case MoveDirection.Left:
                {
                    newSelection = FindBehaviourOnLeft(childElement);
                }
                break;

                case MoveDirection.Down:
                {
                    newSelection = FindBehaviourOnDown(childElement);
                }
                break;
            }

            // if no next child
            if(newSelection == null)
            {
                eventData.selectedObject = null;

                NavigationContainerElement thisAsChild = this.gameObject.GetComponent<NavigationContainerElement>();

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
                    eventData.selectedObject = childElement.gameObject;
                }
            }
            else
            {
                eventData.selectedObject = newSelection.gameObject;
            }
        }

        /// <summary>Find the GameObject on the navigation network to the left of the given one.</summary>
        public UIBehaviour FindBehaviourOnLeft(NavigationContainerElement childElement)
        {
            if (childElement.navigation.mode == Navigation.Mode.Explicit)
            {
                Selectable s = childElement.navigation.selectOnLeft;

                if(s != null && s.IsActive())
                {
                    return s;
                }
            }
            if ((childElement.navigation.mode & Navigation.Mode.Horizontal) != 0)
            {
                NavigationContainerElement c = this.FindChildInDirection(childElement,
                                                                         transform.rotation * Vector3.left);
                return c;
            }
            return null;
        }

        /// <summary>Find the GameObject on the navigation network to the right of the given one.</summary>
        public UIBehaviour FindBehaviourOnRight(NavigationContainerElement childElement)
        {
            if (childElement.navigation.mode == Navigation.Mode.Explicit)
            {
                Selectable s = childElement.navigation.selectOnRight;

                if(s != null && s.IsActive())
                {
                    return s;
                }
            }
            if ((childElement.navigation.mode & Navigation.Mode.Horizontal) != 0)
            {
                NavigationContainerElement c = this.FindChildInDirection(childElement,
                                                                         transform.rotation * Vector3.right);
                return c;
            }
            return null;
        }

        /// <summary>Find the GameObject on the navigation network above the given one</summary>
        public UIBehaviour FindBehaviourOnUp(NavigationContainerElement childElement)
        {
            if (childElement.navigation.mode == Navigation.Mode.Explicit)
            {
                Selectable s = childElement.navigation.selectOnUp;

                if(s != null && s.IsActive())
                {
                    return s;
                }
            }
            if ((childElement.navigation.mode & Navigation.Mode.Vertical) != 0)
            {
                NavigationContainerElement c = this.FindChildInDirection(childElement,
                                                                         transform.rotation * Vector3.up);
                return c;
            }
            return null;
        }

        /// <summary>Find the GameObject on the navigation network below the given one.</summary>
        public UIBehaviour FindBehaviourOnDown(NavigationContainerElement childElement)
        {
            if (childElement.navigation.mode == Navigation.Mode.Explicit)
            {
                Selectable s = childElement.navigation.selectOnDown;

                if(s != null && s.IsActive())
                {
                    return s;
                }
            }
            if ((childElement.navigation.mode & Navigation.Mode.Vertical) != 0)
            {
                NavigationContainerElement c = this.FindChildInDirection(childElement,
                                                                         transform.rotation * Vector3.down);
                return c;
            }
            return null;
        }

        /// <summary>Finds the next NavigationContainerElement in the direction from given the element.</summary>
        public NavigationContainerElement FindChildInDirection(NavigationContainerElement childElement, Vector3 direction)
        {
            Debug.Assert(childElement != null);

            var nextChildren = UIUtilities.OrderByNext(childElement.transform as RectTransform,
                                                       this.children,
                                                       direction);

            // select next appropriate child
            foreach(NavigationContainerElement containerElement in nextChildren)
            {
                if(containerElement.IsActive()
                   && containerElement.navigation.mode != Navigation.Mode.None)
                {
                    return containerElement;
                }
            }

            return null;
        }
    }
}
