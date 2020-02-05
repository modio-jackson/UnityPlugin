using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    /// <summary>Component responsible for managing the navigation of the UI.</summary>
    /// <remarks>This component needs to be Updated after the EventSystem/InputModule has Updated
    /// to ensure that the selection storage functions as expected.</remarks>
    public class NavigationManager : MonoBehaviour
    {
        // ---------[ Singleton ]---------
        /// <summary>Singleton instance.</summary>
        private static NavigationManager _instance = null;

        /// <summary>Singleton instance.</summary>
        public static NavigationManager instance
        {
            get
            {
                if(NavigationManager._instance == null)
                {
                    NavigationManager._instance = UIUtilities.FindComponentInAllScenes<NavigationManager>(true);

                    if(NavigationManager._instance == null)
                    {
                        GameObject go = new GameObject("Navigation Manager");
                        NavigationManager._instance = go.AddComponent<NavigationManager>();
                    }
                }

                return NavigationManager._instance;
            }
        }

        // ---------[ Fields ]---------
        /// <summary>The menu bar that is always shown.</summary>
        public CanvasGroup menuBar = null;

        /// <summary>Selections to remember for when a view is refocused.</summary>
        public Dictionary<IBrowserView, GameObject> m_selectionMap = new Dictionary<IBrowserView, GameObject>();

        // ---------[ Initialization ]---------
        /// <summary>Sets singleton instance.</summary>
        private void Awake()
        {
            if(NavigationManager._instance == null)
            {
                NavigationManager._instance = this;
            }
            #if DEBUG
            else if(NavigationManager._instance != this)
            {
                Debug.LogWarning("[mod.io] Second instance of a NavigationManager"
                                 + " component enabled simultaneously."
                                 + " Only one instance of a NavigationManager"
                                 + " component should be active at a time.");
                this.enabled = false;
            }
            #endif
        }

        /// <summary>Links with View Manager.</summary>
        private void Start()
        {
            ViewManager.instance.onBeforeDefocusView.AddListener(this.OnDefocusView);
            ViewManager.instance.onAfterFocusView.AddListener(this.OnFocusView);
        }

        // ---------[ Update ]---------
        /// <summary>Catches and resets the selection if currently unavailable.</summary>
        private void Update()
        {
            if(ViewManager.instance.currentFocus == null) { return; }

            if(Input.GetAxis("Horizontal") != 0f || Input.GetAxis("Vertical") != 0f)
            {
                GameObject currentSelection = EventSystem.current.currentSelectedGameObject;

                // on controller/keyboard input reset selection
                if(currentSelection == null || !currentSelection.activeInHierarchy)
                {
                    IBrowserView view = ViewManager.instance.currentFocus;
                    if(view != null)
                    {
                        EventSystem.current.SetSelectedGameObject(NavigationManager.GetPrimarySelection(view));
                    }
                }
                else
                {
                    this.m_selectionMap[ViewManager.instance.currentFocus] = currentSelection;
                }
            }
            //if mouse has moved clear selection
            else if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        // ---------[ Event Handlers ]---------
        /// <summary>Stores the selection and makes the view uninteractable.</summary>
        public void OnDefocusView(IBrowserView view)
        {
            view.canvasGroup.interactable = false;
        }

        /// <summary>Set the selection for a change in focus.</summary>
        public void OnFocusView(IBrowserView view)
        {
            view.canvasGroup.interactable = true;

            EventSystem.current.SetSelectedGameObject(NavigationManager.GetPrimarySelection(view));

            this.menuBar.interactable = (view is ExplorerView || view is SubscriptionsView);
        }

        /// <summary>Gets the primary selection element for a given view.</summary>
        public static GameObject GetPrimarySelection(IBrowserView view)
        {
            int primaryPriority = -1;
            GameObject primarySelection = null;

            foreach(var selectionPriority in view.gameObject.GetComponentsInChildren<SelectionFocusPriority>())
            {
                if(selectionPriority.gameObject.activeSelf
                   && selectionPriority.priority > primaryPriority)
                {
                    primarySelection = selectionPriority.gameObject;
                    primaryPriority = selectionPriority.priority;
                }
            }

            if(primarySelection != null)
            {
                return primarySelection;
            }

            foreach(var sel in view.gameObject.GetComponentsInChildren<Selectable>())
            {
                if(sel.IsActive() && sel.interactable)
                {
                    return sel.gameObject;
                }
            }

            return null;
        }
    }
}
