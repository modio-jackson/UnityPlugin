using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace ModIO.UI
{
    /// <summary>Component responsible for management of the various views.</summary>
    public class ViewManager : MonoBehaviour
    {
        // ---------[ Constants ]---------
        /// <summary>The difference between the sorting order for each view on the view stack.</summary>
        public const int SORTORDER_SPACING = 2;

        // ---------[ Nested Data-Types ]---------
        /// <summary>Event for views changing.</summary>
        [System.Serializable]
        public class ViewChangeEvent : UnityEvent<IBrowserView> {}

        // ---------[ SINGLETON ]---------
        private static ViewManager _instance = null;
        public static ViewManager instance
        {
            get
            {
                if(ViewManager._instance == null)
                {
                    ViewManager._instance = UIUtilities.FindComponentInAllScenes<ViewManager>(true);

                    if(ViewManager._instance == null)
                    {
                        GameObject go = new GameObject("View Manager");
                        ViewManager._instance = go.AddComponent<ViewManager>();
                    }

                    ViewManager._instance.FindViews();
                }

                return ViewManager._instance;
            }
        }

        // ---------[ FIELDS ]---------
        private ExplorerView m_explorerView = null;
        private SubscriptionsView m_subscriptionsView = null;
        private InspectorView m_inspectorView = null;
        private LoginDialog m_loginDialog = null;
        private MessageDialog m_messageDialog = null;
        private bool m_viewsFound = false;

        /// <summary>Event callback for when a view is hidden.</summary>
        public ViewChangeEvent onBeforeHideView = new ViewChangeEvent();

        /// <summary>Event callback for when a view is shown.</summary>
        public ViewChangeEvent onBeforeShowView = new ViewChangeEvent();

        /// <summary>Event callback for when a view is defocused.</summary>
        public ViewChangeEvent onBeforeDefocusView = new ViewChangeEvent();

        /// <summary>Event callback for when a view is focused.</summary>
        public ViewChangeEvent onAfterFocusView = new ViewChangeEvent();

        /// <summary>View stack for all the currently open views.</summary>
        private List<IBrowserView> m_viewStack = new List<IBrowserView>();

        /// <summary>All found IBrowserView components.</summary>
        private IBrowserView[] m_views = null;

        /// <summary>Sorting order for the root view.</summary>
        private int m_rootViewSortOrder = 0;

        // --- Accessors ---
        /// <summary>Explorer View in the UI.</summary>
        public ExplorerView explorerView
        {
            get { return this.m_explorerView; }
        }
        /// <summary>Subscriptions View in the UI.</summary>
        public SubscriptionsView subscriptionsView
        {
            get { return this.m_subscriptionsView; }
        }
        /// <summary>Inspector View in the UI.</summary>
        public InspectorView inspectorView
        {
            get { return this.m_inspectorView; }
        }
        /// <summary>Login View in the UI</summary>
        public LoginDialog loginDialog
        {
            get { return this.m_loginDialog; }
        }
        /// <summary>Message View in the UI</summary>
        public MessageDialog messageDialog
        {
            get { return this.m_messageDialog; }
        }

        /// <summary>Currently focused view.</summary>
        public IBrowserView currentFocus
        {
            get { return this.m_viewStack[this.m_viewStack.Count-1]; }
        }

        // ---------[ INITIALIZATION ]---------
        /// <summary>Sets singleton instance.</summary>
        private void Awake()
        {
            if(ViewManager._instance == null)
            {
                ViewManager._instance = this;
            }
            #if DEBUG
            else if(ViewManager._instance != this)
            {
                Debug.LogWarning("[mod.io] Second instance of a ViewManager"
                                 + " component enabled simultaneously."
                                 + " Only one instance of a ViewManager"
                                 + " component should be active at a time.");
                this.enabled = false;
            }
            #endif
        }

        /// <summary>Gathers the views in the scene.</summary>
        private void Start()
        {
            this.FindViews();

            // - get the parent canvas for the views -
            IBrowserView firstView = this.m_views[0];
            Debug.Assert(firstView != null,
                         "[mod.io] No views found in the scene."
                         + " Please ensure the scene contains at least one IBrowserView component"
                         + " before using the ViewManager.", this);

            Transform firstViewParent = firstView.gameObject.transform.parent;
            Debug.Assert(firstViewParent != null,
                         "[mod.io] The first found view in the scene appears to be a root object."
                         + " ViewManager expects the views to be contained under a canvas object to"
                         + " function correctly.", firstView.gameObject);

            Canvas parentCanvas = firstViewParent.GetComponentInParent<Canvas>();
            Debug.Assert(parentCanvas != null,
                         "[mod.io] The first found view in the scene has no parent canvas component."
                         + " ViewManager expects the views to be contained under a canvas object to"
                         + " function correctly.", firstView.gameObject);

            #if UNITY_EDITOR
                if(this.m_views.Length > 1)
                {
                    foreach(IBrowserView view in this.m_views)
                    {
                        Transform parentTransform = view.gameObject.transform.parent;
                        if(parentTransform == null
                           || parentCanvas != parentTransform.GetComponentInParent<Canvas>())
                        {
                            Debug.LogError("[mod.io] All the views must have the same parent canvas"
                                           + " in order for the ViewManager to function correctly.", this);

                            this.enabled = false;
                            return;
                        }
                    }
                }
            #endif

            // set the sorting order base
            this.m_rootViewSortOrder = parentCanvas.sortingOrder + ViewManager.SORTORDER_SPACING;

            // add canvas + raycaster components to views
            foreach(IBrowserView view in this.m_views)
            {
                Canvas viewCanvas = view.gameObject.GetComponent<Canvas>();

                if(viewCanvas == null)
                {
                    viewCanvas = view.gameObject.AddComponent<Canvas>();
                    viewCanvas.overridePixelPerfect = false;
                    viewCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
                }
                viewCanvas.overrideSorting = true;
                viewCanvas.sortingOrder = this.m_rootViewSortOrder;

                GraphicRaycaster raycaster = view.gameObject.GetComponent<GraphicRaycaster>();

                if(raycaster == null)
                {
                    raycaster = view.gameObject.AddComponent<GraphicRaycaster>();
                    raycaster.ignoreReversedGraphics = true;
                    raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
                }
            }

            // - create the initial view stack -
            List<IBrowserView> initViewStack = new List<IBrowserView>();

            if(this.explorerView != null
               && this.explorerView.isActiveAndEnabled)
            {
                initViewStack.Add(this.explorerView);
            }

            if(this.subscriptionsView != null
               && this.subscriptionsView.isActiveAndEnabled)
            {
                if(initViewStack.Count == 1)
                {
                    initViewStack[0] = this.subscriptionsView;
                    this.explorerView.gameObject.SetActive(false);
                }
                else
                {
                    initViewStack.Add(this.subscriptionsView);
                }
            }

            if(initViewStack.Count == 0)
            {
                if(this.explorerView != null)
                {
                    initViewStack.Add(this.explorerView);

                    this.explorerView.gameObject.SetActive(true);
                }
                else if(this.subscriptionsView != null)
                {
                    initViewStack.Add(this.subscriptionsView);

                    this.subscriptionsView.gameObject.SetActive(true);
                }
                #if DEBUG
                    else
                    {
                        Debug.Log("[mod.io] No main view found in the scene."
                                  + " Please consider adding either an ExplorerView or"
                                  + " a SubscriptionsView to the scene.", this);
                    }
                #endif
            }

            if(this.inspectorView != null
               && this.inspectorView.isActiveAndEnabled)
            {
                initViewStack.Add(this.inspectorView);
            }

            if(this.loginDialog != null
               && this.loginDialog.isActiveAndEnabled)
            {
                initViewStack.Add(this.loginDialog);
            }

            this.StartCoroutine(DelayedViewFocusOnStart(initViewStack));
        }

        /// <summary>Sends events at the end of the frame.</summary>
        private System.Collections.IEnumerator DelayedViewFocusOnStart(List<IBrowserView> viewStack)
        {
            yield return null;

            if(this != null && viewStack != null && viewStack.Count > 0)
            {
                IBrowserView view = null;
                this.m_viewStack = viewStack;

                for(int i = 0; i < viewStack.Count-1; ++i)
                {
                    view = viewStack[i];

                    view.gameObject.GetComponent<Canvas>().sortingOrder
                        = this.m_rootViewSortOrder + i*ViewManager.SORTORDER_SPACING;
                    this.onBeforeDefocusView.Invoke(view);
                }

                view = viewStack[viewStack.Count-1];

                view.gameObject.GetComponent<Canvas>().sortingOrder
                    = this.m_rootViewSortOrder + (viewStack.Count - 1)*ViewManager.SORTORDER_SPACING;
                this.onAfterFocusView.Invoke(view);
            }
        }

        private void FindViews()
        {
            if(this.m_viewsFound) { return; }

            this.m_explorerView = GetComponentInChildren<ExplorerView>(true);
            this.m_subscriptionsView = GetComponentInChildren<SubscriptionsView>(true);
            this.m_inspectorView = GetComponentInChildren<InspectorView>(true);
            this.m_loginDialog = GetComponentInChildren<LoginDialog>(true);
            this.m_messageDialog = GetComponentInChildren<MessageDialog>(true);
            this.m_viewsFound = true;

            this.m_views = this.gameObject.GetComponentsInChildren<IBrowserView>(true);
        }

        // ---------[ VIEW MANAGEMENT ]---------
        public void InspectMod(int modId)
        {
            #if DEBUG
                if(this.m_inspectorView == null)
                {
                    Debug.Log("[mod.io] Inspector View not found.");
                }
            #endif

            if(this.m_inspectorView == null)
            {
                return;
            }

            this.m_inspectorView.modId = modId;

            this.FocusWindowedView(this.m_inspectorView);
        }

        public void ActivateExplorerView()
        {
            #if DEBUG
                if(this.m_explorerView == null)
                {
                    Debug.Log("[mod.io] Explorer View not found.");
                }
            #endif

            if(this.m_explorerView == null) { return; }

            this.FocusRootView(this.m_explorerView);
        }

        public void ActivateSubscriptionsView()
        {
            #if DEBUG
                if(this.m_subscriptionsView == null)
                {
                    Debug.Log("[mod.io] Subscriptions View not found.");
                }
            #endif

            if(this.m_subscriptionsView == null) { return; }

            this.FocusRootView(this.m_subscriptionsView);
        }

        public void ShowLoginDialog()
        {
            #if DEBUG
                if(this.m_loginDialog == null)
                {
                    Debug.Log("[mod.io] Login Dialog not found.");
                }
            #endif

            this.FocusWindowedView(this.m_loginDialog);
        }

        /// <summary>Shows the message dialog using the given settings.</summary>
        public void ShowMessageDialog(string header, string message,
                                      string highlightButton = null,
                                      string warningButton = null,
                                      string standardButton = null)
        {
            if(this.m_messageDialog == null) { return; }

            Debug.Assert(this.m_messageDialog.highlightedButton != null);
            Debug.Assert(this.m_messageDialog.warningButton != null);
            Debug.Assert(this.m_messageDialog.standardButton != null);

            this.m_messageDialog.headerText.text = header;
            this.m_messageDialog.messageText.text = message;

            if(string.IsNullOrEmpty(highlightButton))
            {
                this.m_messageDialog.highlightedButton.gameObject.SetActive(false);
            }
            else
            {
                this.m_messageDialog.highlightedButton.gameObject.SetActive(true);
                this.m_messageDialog.highlightedButtonText.text = highlightButton;
            }

            if(string.IsNullOrEmpty(warningButton))
            {
                this.m_messageDialog.warningButton.gameObject.SetActive(false);
            }
            else
            {
                this.m_messageDialog.warningButton.gameObject.SetActive(true);
                this.m_messageDialog.warningButtonText.text = warningButton;
            }

            if(string.IsNullOrEmpty(standardButton))
            {
                this.m_messageDialog.standardButton.gameObject.SetActive(false);
            }
            else
            {
                this.m_messageDialog.standardButton.gameObject.SetActive(true);
                this.m_messageDialog.standardButtonText.text = standardButton;
            }

            this.FocusWindowedView(this.m_messageDialog);
        }

        /// <summary>Clears the view stack and sets the view as the only view on the stack.</summary>
        public void FocusRootView(IBrowserView view)
        {
            if(view == null || view == this.currentFocus) { return; }

            while(this.m_viewStack.Count > 0
                  && this.currentFocus != view)
            {
                this.PopView(focusNextView: false);
            }

            if(this.m_viewStack.Count == 0)
            {
                this.PushView(view, defocusCurrentView: false);
            }
            else
            {
                Debug.Assert(this.currentFocus == view);

                this.onAfterFocusView.Invoke(view);
            }
        }

        /// <summary>Either adds the view to the stack, or removes any views above it on the stack.</summary>
        public void FocusWindowedView(IBrowserView view)
        {
            Debug.Assert(this.m_viewStack.Count > 0,
                         "[mod.io] Can only focus a stacked view if there is an existing view on the stack.");

            if(view == null || view == this.currentFocus) { return; }

            if(this.m_viewStack.Contains(view))
            {
                while(this.currentFocus != view)
                {
                    this.PopView(focusNextView: false);
                }

                this.onAfterFocusView.Invoke(view);
            }
            else
            {
                this.PushView(view, defocusCurrentView: true);
            }
        }

        /// <summary>Closes and hides a view.</summary>
        public void CloseWindowedView(IBrowserView view)
        {
            if(view == null) { return; }

            int viewIndex = this.m_viewStack.IndexOf(view);

            if(viewIndex >= 0)
            {
                if(this.currentFocus == view)
                {
                    this.PopView(focusNextView: true);
                }
                else
                {
                    this.onBeforeHideView.Invoke(view);
                    view.gameObject.SetActive(false);

                    this.m_viewStack.RemoveAt(viewIndex);

                    for(int i = viewIndex; i < this.m_viewStack.Count; ++i)
                    {
                        this.m_viewStack[i].gameObject.GetComponent<Canvas>().sortingOrder
                            = this.m_rootViewSortOrder + i*ViewManager.SORTORDER_SPACING;
                    }
                }
            }
        }

        /// <summary>Pushes a view to the stack and fires the necessary events.</summary>
        public void PushView(IBrowserView view, bool defocusCurrentView = true)
        {
            Debug.Assert(view != null);
            Debug.Assert(view.gameObject.GetComponent<Canvas>() != null);
            Debug.Assert(!this.m_viewStack.Contains(view));
            Debug.Assert(!(defocusCurrentView && this.m_viewStack.Count == 0),
                         "[mod.io] Cannot defocus if no views are on the stack.");

            if(defocusCurrentView)
            {
                this.onBeforeDefocusView.Invoke(this.currentFocus);
            }

            this.onBeforeShowView.Invoke(view);

            this.m_viewStack.Add(view);
            view.gameObject.GetComponent<Canvas>().sortingOrder
                = this.m_rootViewSortOrder + (this.m_viewStack.Count - 1)*ViewManager.SORTORDER_SPACING;
            view.gameObject.SetActive(true);

            this.onAfterFocusView.Invoke(view);
        }

        /// <summary>Pops a view from the stack and fires the necessary events.</summary>
        public void PopView(bool focusNextView = true)
        {
            Debug.Assert(this.m_viewStack.Count > 0);
            Debug.Assert(!(focusNextView && this.m_viewStack.Count == 1),
                         "[mod.io] Cannot focus the next view if there is only one view on the stack.");

            this.onBeforeDefocusView.Invoke(this.currentFocus);
            this.onBeforeHideView.Invoke(this.currentFocus);

            this.currentFocus.gameObject.SetActive(false);
            this.m_viewStack.RemoveAt(this.m_viewStack.Count-1);

            if(focusNextView)
            {
                this.onAfterFocusView.Invoke(this.currentFocus);
            }
        }
    }
}
