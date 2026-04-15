#nullable enable
using System;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
// ReSharper disable InconsistentNaming

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Scroll Rect", 37)]
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class ScrollRect : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler, IPostLayoutRebuildCallback, ILayoutGroup
    {
        public enum MovementType
        {
            Unrestricted,
            Elastic,
            Clamped,
        }

        public enum ScrollbarVisibility
        {
            Permanent,
            AutoHide,
        }

        [SerializeField, Required]
        private RectTransform m_Content;
        public RectTransform content => m_Content;

        [SerializeField, FormerlySerializedAs("m_Vertical")]
        private Axis m_Direction = Axis.Y;
        public Axis direction => m_Direction;

        [SerializeField]
        private MovementType m_MovementType = MovementType.Elastic;
        public MovementType movementType { get => m_MovementType; set => m_MovementType = value; }

        [SerializeField]
        private float m_Elasticity = 0.1f;
        [SerializeField]
        private bool m_Inertia = true;
        public bool inertia { set => m_Inertia = value; }

        [SerializeField]
        private float m_DecelerationRate = 0.135f; // Only used when inertia is enabled
        [SerializeField]
        private float m_ScrollSensitivity = 1.0f;

        [SerializeField, Required]
        private RectTransform m_Viewport;
        public RectTransform viewport => m_Viewport;

        [SerializeField, FormerlySerializedAs("m_VerticalScrollbar")]
        private Scrollbar? m_Scrollbar;
        [SerializeField, FormerlySerializedAs("m_VerticalScrollbarVisibility"), ShowIf("m_Scrollbar")]
        private ScrollbarVisibility m_ScrollbarVisibility;
        [SerializeField, FormerlySerializedAs("m_VerticalScrollbarSpacing"), ShowIf("m_Scrollbar")]
        private float m_ScrollbarSpacing;

        private float m_PointerStartLocalCursor;
        private float m_ContentStartPosition;

        private Rect m_ContentBounds;
        private Rect m_ViewBounds;

        private float m_Velocity;
        public float velocity => m_Velocity;

        private bool m_Dragging;
        private bool m_Scrolling;

        private float m_PrevPosition;
        private Rect m_PrevContentBounds;
        private Rect m_PrevViewBounds;
        [NonSerialized]
        private bool m_HasRebuiltLayout = false;

        [NonSerialized] private RectTransform? m_Rect;
        private RectTransform rectTransform => m_Rect ??= (RectTransform)transform;

        private int di => m_Direction.Idx();   // scroll axis index
        private int li => 1 - di;              // locked axis index

        void IPostLayoutRebuildCallback.PostLayoutRebuild()
        {
            UpdateBounds();
            UpdateScrollbar(0);
            UpdatePrevData();

            m_HasRebuiltLayout = true;
        }

        private void OnEnable()
        {
            if (m_Scrollbar)
                m_Scrollbar.onValueChanged.AddListener(SetNormalizedPosition);

            CanvasUpdateRegistry.QueueLayoutRebuildCallback(this);
            SetDirty();
        }

        private void OnDisable()
        {
            if (m_Scrollbar)
                m_Scrollbar.onValueChanged.RemoveListener(SetNormalizedPosition);

            m_Dragging = false;
            m_Scrolling = false;
            m_HasRebuiltLayout = false;
            m_Velocity = 0;
            LayoutRebuilder.SetDirty(rectTransform);
        }

        private void EnsureLayoutHasRebuilt()
        {
            if (!m_HasRebuiltLayout && CanvasUpdateRegistry.IsIdle())
                CanvasUpdateRegistry.PerformUpdate();
        }

        public virtual void StopMovement()
        {
            m_Velocity = 0;
        }

        void IScrollHandler.OnScroll(PointerEventData data)
        {
            EnsureLayoutHasRebuilt();
            UpdateBounds();

            Vector2 delta = data.scrollDelta;
            // Down is positive for scroll events, while in UI system up is positive.
            delta.y *= -1;

            // Route cross-axis scroll input to the scroll direction.
            float scrollDelta = delta[di];
            if (Mathf.Abs(delta[li]) > Mathf.Abs(scrollDelta))
                scrollDelta = delta[li];

            if (data.IsScrolling())
                m_Scrolling = true;

            Vector2 position = m_Content.anchoredPosition;
            position[di] += scrollDelta * m_ScrollSensitivity;
            if (m_MovementType == MovementType.Clamped)
                position[di] += CalculateOffset(position[di] - m_Content.anchoredPosition[di]);

            SetContentAnchoredPosition(position);
            UpdateBounds();
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Velocity = 0;
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            UpdateBounds();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out var startCursor);
            m_PointerStartLocalCursor = startCursor[di];
            m_ContentStartPosition = m_Content.anchoredPosition[di];
            m_Dragging = true;
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Dragging = false;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!m_Dragging)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    viewport, eventData.position, eventData.pressEventCamera, out var localCursor))
                return;

            UpdateBounds();

            float pointerDelta = localCursor[di] - m_PointerStartLocalCursor;
            Vector2 position = m_Content.anchoredPosition;
            position[di] = m_ContentStartPosition + pointerDelta;

            // Offset to get content into place in the view.
            float offset = CalculateOffset(position[di] - m_Content.anchoredPosition[di]);
            position[di] += offset;
            if (m_MovementType == MovementType.Elastic && offset != 0)
                position[di] -= RubberDelta(offset, m_ViewBounds.size[di]);

            SetContentAnchoredPosition(position);
        }

        private void SetContentAnchoredPosition(Vector2 position)
        {
            // Lock the non-scroll axis.
            position[li] = m_Content.anchoredPosition[li];

            if (position != m_Content.anchoredPosition)
            {
                m_Content.anchoredPosition = position;
                UpdateBounds();
            }
        }

        private void LateUpdate()
        {
            if (!m_Content)
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();
            float deltaTime = Time.unscaledDeltaTime;
            float offset = CalculateOffset(0);

            // Skip processing if deltaTime is invalid (0 or less) as it will cause inaccurate velocity calculations and a divide by zero error.
            if (deltaTime > 0.0f)
            {
                if (!m_Dragging && (offset != 0 || m_Velocity != 0))
                {
                    Vector2 position = m_Content.anchoredPosition;

                    // Apply spring physics if movement is elastic and content has an offset from the view.
                    if (m_MovementType == MovementType.Elastic && offset != 0)
                    {
                        float speed = m_Velocity;
                        float smoothTime = m_Elasticity;
                        if (m_Scrolling)
                            smoothTime *= 3.0f;
                        position[di] = Mathf.SmoothDamp(m_Content.anchoredPosition[di], m_Content.anchoredPosition[di] + offset, ref speed, smoothTime, Mathf.Infinity, deltaTime);
                        if (Mathf.Abs(speed) < 1)
                            speed = 0;
                        m_Velocity = speed;
                    }
                    // Decelerate via inertia.
                    else if (m_Inertia)
                    {
                        m_Velocity *= Mathf.Pow(m_DecelerationRate, deltaTime);
                        if (Mathf.Abs(m_Velocity) < 1)
                            m_Velocity = 0;
                        position[di] += m_Velocity * deltaTime;
                    }
                    // No elasticity or friction — zero out velocity.
                    else
                    {
                        m_Velocity = 0;
                    }

                    if (m_MovementType == MovementType.Clamped)
                    {
                        offset = CalculateOffset(position[di] - m_Content.anchoredPosition[di]);
                        position[di] += offset;
                    }

                    SetContentAnchoredPosition(position);
                }

                if (m_Dragging && m_Inertia)
                {
                    float newVelocity = (m_Content.anchoredPosition[di] - m_PrevPosition) / deltaTime;
                    m_Velocity = Mathf.Lerp(m_Velocity, newVelocity, deltaTime * 10);
                }
            }

            if (m_ViewBounds != m_PrevViewBounds || m_ContentBounds != m_PrevContentBounds || m_Content.anchoredPosition[di] != m_PrevPosition)
            {
                UpdateScrollbar(offset);
                UISystemProfilerApi.AddMarker("ScrollRect.value", this);
                UpdatePrevData();
            }
            UpdateScrollbarVisibility();
            m_Scrolling = false;
        }

        private void UpdatePrevData()
        {
            m_PrevPosition = m_Content.anchoredPosition[di];
            m_PrevViewBounds = m_ViewBounds;
            m_PrevContentBounds = m_ContentBounds;
        }

        private void UpdateScrollbar(float offset)
        {
            if (!m_Scrollbar) return;
            var i = di;
            if (m_ContentBounds.size[i] > 0)
                m_Scrollbar.size = Mathf.Clamp01((m_ViewBounds.size[i] - Mathf.Abs(offset)) / m_ContentBounds.size[i]);
            else
                m_Scrollbar.size = 1;
            m_Scrollbar.value = normalizedPosition;
        }

        public float normalizedPosition
        {
            get
            {
                UpdateBounds();
                var i = di;
                if (m_ContentBounds.size[i] <= m_ViewBounds.size[i] || Mathf.Approximately(m_ContentBounds.size[i], m_ViewBounds.size[i]))
                    return m_ViewBounds.min[i] > m_ContentBounds.min[i] ? 1 : 0;
                return (m_ViewBounds.min[i] - m_ContentBounds.min[i]) / (m_ContentBounds.size[i] - m_ViewBounds.size[i]);
            }
            set => SetNormalizedPosition(value);
        }

        private void SetNormalizedPosition(float value)
        {
            EnsureLayoutHasRebuilt();
            UpdateBounds();
            var i = di;
            // How much the content is larger than the view.
            float hiddenLength = m_ContentBounds.size[i] - m_ViewBounds.size[i];
            // Where the position of the lower left corner of the content bounds should be, in the space of the view.
            float contentBoundsMinPosition = m_ViewBounds.min[i] - value * hiddenLength;
            // The new content localPosition, in the space of the view.
            float newAnchoredPosition = m_Content.anchoredPosition[i] + contentBoundsMinPosition - m_ContentBounds.min[i];

            Vector3 anchoredPosition = m_Content.anchoredPosition;
            if (Mathf.Abs(anchoredPosition[i] - newAnchoredPosition) > 0.01f)
            {
                anchoredPosition[i] = newAnchoredPosition;
                m_Content.anchoredPosition = anchoredPosition;
                m_Velocity = 0;
                UpdateBounds();
            }
        }

        private static float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
        }

        protected virtual void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        private bool scrollingNeeded => m_ContentBounds.size[di] > m_ViewBounds.size[di] + 0.01f;

        void ILayoutController.SetLayoutVertical()
        {
            m_ViewBounds = viewport.rect;
            m_ContentBounds = GetBounds();
        }

        private void UpdateScrollbarVisibility()
        {
            if (!m_Scrollbar) return;
            bool shouldBeActive = m_ScrollbarVisibility == ScrollbarVisibility.Permanent || scrollingNeeded;
            if (m_Scrollbar.gameObject.activeSelf != shouldBeActive)
                m_Scrollbar.gameObject.SetActive(shouldBeActive);
        }

        protected void UpdateBounds()
        {
            m_ViewBounds = viewport.rect;
            m_ContentBounds = GetBounds();

            if (m_Content == null)
                return;

            Vector2 contentSize = m_ContentBounds.size;
            Vector2 contentPos = m_ContentBounds.center;
            var contentPivot = m_Content.pivot;
            AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
            m_ContentBounds.size = contentSize;
            m_ContentBounds.center = contentPos;

            if (movementType == MovementType.Clamped)
            {
                // Clamp scroll axis so content never leaves a gap inside the view.
                // AdjustBounds above guarantees contentSize >= viewSize, so at most one side overflows.
                var i = di;
                float minDiff = m_ViewBounds.min[i] - m_ContentBounds.min[i];
                float maxDiff = m_ViewBounds.max[i] - m_ContentBounds.max[i];

                float clampDelta = 0;
                if (maxDiff > 0)
                    clampDelta = Math.Min(minDiff, maxDiff);
                else if (minDiff < 0)
                    clampDelta = Math.Max(minDiff, maxDiff);

                if (Mathf.Abs(clampDelta) > float.Epsilon)
                {
                    contentPos = m_Content.anchoredPosition;
                    contentPos[i] += clampDelta;
                    AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
                }
            }
        }

        // Ensure content bounds are at least as large as view by expanding around the content pivot.
        // E.g. if pivot is at top, bounds expand downwards. Works well with ContentSizeFitter.
        internal static void AdjustBounds(ref Rect viewBounds, ref Vector2 contentPivot, ref Vector2 contentSize, ref Vector2 contentPos)
        {
            Vector3 excess = viewBounds.size - contentSize;
            if (excess.x > 0)
            {
                contentPos.x -= excess.x * (contentPivot.x - 0.5f);
                contentSize.x = viewBounds.size.x;
            }
            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (contentPivot.y - 0.5f);
                contentSize.y = viewBounds.size.y;
            }
        }

        private static readonly Vector3[] s_TempCorners = new Vector3[4];
        private Rect GetBounds()
        {
            if (m_Content == null)
                return new Rect();
            m_Content.GetWorldCorners(s_TempCorners);
            var viewWorldToLocalMatrix = viewport.worldToLocalMatrix;
            return InternalGetBounds(s_TempCorners, ref viewWorldToLocalMatrix);
        }

        private static Rect InternalGetBounds(Vector3[] corners, ref Matrix4x4 viewWorldToLocalMatrix)
        {
            var vMin = new Vector2(float.MaxValue, float.MaxValue);
            var vMax = new Vector2(float.MinValue, float.MinValue);

            for (int j = 0; j < 4; j++)
            {
                Vector2 v = viewWorldToLocalMatrix.MultiplyPoint2D(corners[j]);
                vMin = Vector2.Min(v, vMin);
                vMax = Vector2.Max(v, vMax);
            }

            return new Rect(vMin, vMax - vMin);
        }

        // Calculates how much content should move to stay within view bounds.
        // The 0.001 threshold prevents tiny adjustments that would trigger unnecessary layout recalculations.
        private float CalculateOffset(float delta)
        {
            if (m_MovementType == MovementType.Unrestricted)
                return 0;

            var i = di;
            float contentMin = m_ContentBounds.min[i] + delta;
            float contentMax = m_ContentBounds.max[i] + delta;
            float minOffset = m_ViewBounds.min[i] - contentMin;
            float maxOffset = m_ViewBounds.max[i] - contentMax;

            if (minOffset < -0.001f)
                return minOffset;
            if (maxOffset > 0.001f)
                return maxOffset;
            return 0;
        }

        private void SetDirty()
        {
            if (!isActiveAndEnabled)
                return;

            LayoutRebuilder.SetDirty(rectTransform);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            m_Viewport = (RectTransform)transform;
        }
#endif
    }
}
