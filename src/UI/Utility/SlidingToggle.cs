using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Allows a Toggle component to present as a slide.</summary>
    [RequireComponent(typeof(StateToggle))]
    public class SlidingToggle : Toggle, UnityEngine.EventSystems.IPointerExitHandler
    {
        public enum SlideAxis
        {
            Horizontal,
            Vertical,
        }

        // ---------[ FIELDS ]---------
        /// <summary>Event triggered if the toggled is clicked while on.</summary>
        public UnityEvent onClickedWhileOn = new UnityEvent();
        /// <summary>Event triggered if the toggled is clicked while off.</summary>
        public UnityEvent onClickedWhileOff = new UnityEvent();

        [Header("Slide Settings")]
        [SerializeField] private RectTransform m_content = null;
        [Tooltip("When enabled, the isOn value is not toggled via a click/submit action.")]
        [SerializeField] private bool m_disableAutoToggle = false;
        [SerializeField] private SlideAxis m_slideAxis = SlideAxis.Horizontal;
        [SerializeField] private float m_slideDuration = 0.15f;
        [Tooltip("Set duration to block clicks for after the slide animation")]
        [SerializeField] private float m_reactivateDelay = 0.05f;

        // --- RUNTIME DATA ---
        private Coroutine m_animation = null;

        // --- ACCESSORS ---
        public SlideAxis slideAxis
        {
            get { return m_slideAxis; }
            set
            {
                if(m_slideAxis != value)
                {
                    m_slideAxis = value;
                    UpdateContentPosition(true);
                }
            }
        }

        public bool isAnimating
        {
            get { return m_animation != null; }
        }

        // ---------[ INITIALIZATION ]---------
        protected override void Awake()
        {
            // add listener to event
            this.onValueChanged.AddListener((b) => this.UpdateContentPosition(true));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            StartCoroutine(LateEnable());
        }
        private System.Collections.IEnumerator LateEnable()
        {
            yield return null;
            UpdateContentPosition(false);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        private void UpdateContentPosition(bool animate)
        {
            if(this.m_content == null) { return; }

            Vector2 startPos;
            Vector2 targetPos;
            if(m_slideAxis == SlideAxis.Horizontal)
            {
                if(this.isOn)
                {
                    startPos = SlidingToggle.GetLeftPos(this.m_content);
                    targetPos = SlidingToggle.GetRightPos(this.m_content);
                }
                else
                {
                    startPos = SlidingToggle.GetRightPos(this.m_content);
                    targetPos = SlidingToggle.GetLeftPos(this.m_content);
                }
            }
            else
            {
                if(this.isOn)
                {
                    startPos = SlidingToggle.GetBottomPos(this.m_content);
                    targetPos = SlidingToggle.GetTopPos(this.m_content);
                }
                else
                {
                    startPos = SlidingToggle.GetTopPos(this.m_content);
                    targetPos = SlidingToggle.GetBottomPos(this.m_content);
                }
            }


            animate &= (this.isActiveAndEnabled && m_slideDuration > 0f);
            if(animate)
            {
                if(m_animation != null)
                {
                    StopCoroutine(m_animation);
                }

                this.m_animation = StartCoroutine(AnimateScroll(startPos, targetPos));
            }
            else
            {
                this.m_content.anchoredPosition = targetPos;
            }
        }

        private System.Collections.IEnumerator AnimateScroll(Vector2 startPos, Vector2 targetPos)
        {
            Vector2 currentPos = this.m_content.anchoredPosition;

            float elapsed = 0f;
            float distance = Vector2.Distance(startPos, targetPos);
            float factoredDuration = (Vector2.Distance(currentPos, targetPos) / distance) * m_slideDuration;

            while(elapsed < factoredDuration)
            {
                currentPos = Vector2.LerpUnclamped(startPos, targetPos, elapsed / factoredDuration);
                this.m_content.anchoredPosition = currentPos;
                elapsed += Time.unscaledDeltaTime;

                yield return null;
            }

            this.m_content.anchoredPosition = targetPos;

            // delay enabling buttons
            yield return new WaitForSecondsRealtime(m_reactivateDelay);

            m_animation = null;
        }

        // ---------[ UTILITY ]---------
        private static Vector2 GetLeftPos(RectTransform content)
        {
            Rect pDim = content.parent.GetComponent<RectTransform>().rect;

            // placement of offsetMin.x to left-align
            float offsetPos = -content.anchorMin.x * pDim.width;

            // offset -> pivot
            float pivotDiff = content.anchoredPosition.x - content.offsetMin.x;

            Vector2 pos = new Vector2(offsetPos + pivotDiff, content.anchoredPosition.y);
            return pos;
        }

        private static Vector2 GetRightPos(RectTransform content)
        {
            Rect pDim = content.parent.GetComponent<RectTransform>().rect;

            // placement of offsetMax.x to right-align
            float offsetPos = (1f-content.anchorMax.x) * pDim.width;

            // offset -> pivot
            float pivotDiff = content.anchoredPosition.x - content.offsetMax.x;

            Vector2 pos = new Vector2(offsetPos + pivotDiff, content.anchoredPosition.y);
            return pos;
        }

        private static Vector2 GetBottomPos(RectTransform content)
        {
            Rect pDim = content.parent.GetComponent<RectTransform>().rect;

            // placement of offsetMin.y to bottom-align
            float offsetPos = -content.anchorMin.y * pDim.height;

            // offset -> pivot
            float pivotDiff = content.anchoredPosition.y - content.offsetMin.y;

            Vector2 pos = new Vector2(content.anchoredPosition.x, offsetPos + pivotDiff);
            return pos;
        }

        private static Vector2 GetTopPos(RectTransform content)
        {
            Rect pDim = content.parent.GetComponent<RectTransform>().rect;

            // placement of offsetMax.y to top-align
            float offsetPos = (1f-content.anchorMax.y) * pDim.height;

            // offset -> pivot
            float pivotDiff = content.anchoredPosition.y - content.offsetMax.y;

            Vector2 pos = new Vector2(content.anchoredPosition.x, offsetPos + pivotDiff);
            return pos;
        }

        // ---------[ EVENTS ]---------
        /// <summary>Overrides click event.</summary>
        public override void OnPointerClick(PointerEventData eventData)
        {
            if(eventData.button != PointerEventData.InputButton.Left)
                return;

            if(this.isOn)
            {
                this.onClickedWhileOn.Invoke();
            }
            else
            {
                this.onClickedWhileOff.Invoke();
            }

            if(!this.m_disableAutoToggle)
            {
                base.OnPointerClick(eventData);
            }
        }


        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null)
                {
                    UpdateContentPosition(false);
                }
            };
        }
        #endif

    }
}
