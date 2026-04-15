#nullable enable
using System;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;
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

        [SerializeField]
        private bool m_Horizontal = true;
        public bool horizontal => m_Horizontal;

        [SerializeField]
        private bool m_Vertical = true;
        public bool vertical => m_Vertical;

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

        [SerializeField]
        private Scrollbar? m_HorizontalScrollbar;
        [SerializeField]
        private Scrollbar? m_VerticalScrollbar;
        [SerializeField, ShowIf("m_HorizontalScrollbar")]
        private ScrollbarVisibility m_HorizontalScrollbarVisibility;
        [SerializeField, ShowIf("m_VerticalScrollbar")]
        private ScrollbarVisibility m_VerticalScrollbarVisibility;
        [SerializeField, ShowIf("m_HorizontalScrollbar")]
        private float m_HorizontalScrollbarSpacing;
        [SerializeField, ShowIf("m_VerticalScrollbar")]
        private float m_VerticalScrollbarSpacing;

        // The offset from handle position to mouse down position
        private Vector2 m_PointerStartLocalCursor = Vector2.zero;
        protected Vector2 m_ContentStartPosition = Vector2.zero;

        private Rect m_ContentBounds;
        private Rect m_ViewBounds;

        private Vector2 m_Velocity;
        public Vector2 velocity => m_Velocity;

        private bool m_Dragging;
        private bool m_Scrolling;

        private Vector2 m_PrevPosition = Vector2.zero;
        private Rect m_PrevContentBounds;
        private Rect m_PrevViewBounds;
        [NonSerialized]
        private bool m_HasRebuiltLayout = false;

        [NonSerialized] private RectTransform? m_Rect;
        private RectTransform rectTransform => m_Rect ??= (RectTransform)transform;

        void IPostLayoutRebuildCallback.PostLayoutRebuild()
        {
            UpdateBounds();
            UpdateScrollbars(Vector2.zero);
            UpdatePrevData();

            m_HasRebuiltLayout = true;
        }

        private void OnEnable()
        {
            if (m_Horizontal && m_HorizontalScrollbar)
                m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
            if (m_Vertical && m_VerticalScrollbar)
                m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);

            CanvasUpdateRegistry.QueueLayoutRebuildCallback(this);
            SetDirty();
        }

        private void OnDisable()
        {
            if (m_HorizontalScrollbar)
                m_HorizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
            if (m_VerticalScrollbar)
                m_VerticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);

            m_Dragging = false;
            m_Scrolling = false;
            m_HasRebuiltLayout = false;
            m_Velocity = Vector2.zero;
            LayoutRebuilder.SetDirty(rectTransform);
        }

        private void EnsureLayoutHasRebuilt()
        {
            if (!m_HasRebuiltLayout && CanvasUpdateRegistry.IsIdle())
                CanvasUpdateRegistry.PerformUpdate();
        }

        public virtual void StopMovement()
        {
            m_Velocity = Vector2.zero;
        }

        void IScrollHandler.OnScroll(PointerEventData data)
        {
            EnsureLayoutHasRebuilt();
            UpdateBounds();

            Vector2 delta = data.scrollDelta;
            // Down is positive for scroll events, while in UI system up is positive.
            delta.y *= -1;
            if (vertical && !horizontal)
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    delta.y = delta.x;
                delta.x = 0;
            }
            if (horizontal && !vertical)
            {
                if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    delta.x = delta.y;
                delta.y = 0;
            }

            if (data.IsScrolling())
                m_Scrolling = true;

            Vector2 position = m_Content.anchoredPosition;
            position += delta * m_ScrollSensitivity;
            if (m_MovementType == MovementType.Clamped)
                position += CalculateOffset(position - m_Content.anchoredPosition);

            SetContentAnchoredPosition(position);
            UpdateBounds();
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Velocity = Vector2.zero;
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            UpdateBounds();

            m_PointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out m_PointerStartLocalCursor);
            m_ContentStartPosition = m_Content.anchoredPosition;
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

            var pointerDelta = localCursor - m_PointerStartLocalCursor;
            Vector2 position = m_ContentStartPosition + pointerDelta;

            // Offset to get content into place in the view.
            var offset = CalculateOffset(position - m_Content.anchoredPosition);
            position += offset;
            if (m_MovementType == MovementType.Elastic)
            {
                if (offset.x != 0)
                    position.x -= RubberDelta(offset.x, m_ViewBounds.size.x);
                if (offset.y != 0)
                    position.y -= RubberDelta(offset.y, m_ViewBounds.size.y);
            }

            SetContentAnchoredPosition(position);
        }

        private void SetContentAnchoredPosition(Vector2 position)
        {
            if (!m_Horizontal)
                position.x = m_Content.anchoredPosition.x;
            if (!m_Vertical)
                position.y = m_Content.anchoredPosition.y;

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
            Vector2 offset = CalculateOffset(Vector2.zero);

            // Skip processing if deltaTime is invalid (0 or less) as it will cause inaccurate velocity calculations and a divide by zero error.
            if (deltaTime > 0.0f)
            {
                if (!m_Dragging && (offset != Vector2.zero || m_Velocity != Vector2.zero))
                {
                    Vector2 position = m_Content.anchoredPosition;
                    for (int axis = 0; axis < 2; axis++)
                    {
                        // Apply spring physics if movement is elastic and content has an offset from the view.
                        if (m_MovementType == MovementType.Elastic && offset[axis] != 0)
                        {
                            float speed = m_Velocity[axis];
                            float smoothTime = m_Elasticity;
                            if (m_Scrolling)
                                smoothTime *= 3.0f;
                            position[axis] = Mathf.SmoothDamp(m_Content.anchoredPosition[axis], m_Content.anchoredPosition[axis] + offset[axis], ref speed, smoothTime, Mathf.Infinity, deltaTime);
                            if (Mathf.Abs(speed) < 1)
                                speed = 0;
                            m_Velocity[axis] = speed;
                        }
                        // Decelerate via inertia.
                        else if (m_Inertia)
                        {
                            m_Velocity[axis] *= Mathf.Pow(m_DecelerationRate, deltaTime);
                            if (Mathf.Abs(m_Velocity[axis]) < 1)
                                m_Velocity[axis] = 0;
                            position[axis] += m_Velocity[axis] * deltaTime;
                        }
                        // No elasticity or friction — zero out velocity.
                        else
                        {
                            m_Velocity[axis] = 0;
                        }
                    }

                    if (m_MovementType == MovementType.Clamped)
                    {
                        offset = CalculateOffset(position - m_Content.anchoredPosition);
                        position += offset;
                    }

                    SetContentAnchoredPosition(position);
                }

                if (m_Dragging && m_Inertia)
                {
                    Vector3 newVelocity = (m_Content.anchoredPosition - m_PrevPosition) / deltaTime;
                    m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime * 10);
                }
            }

            if (m_ViewBounds != m_PrevViewBounds || m_ContentBounds != m_PrevContentBounds || m_Content.anchoredPosition != m_PrevPosition)
            {
                UpdateScrollbars(offset);
                UISystemProfilerApi.AddMarker("ScrollRect.value", this);
                UpdatePrevData();
            }
            UpdateScrollbarVisibility();
            m_Scrolling = false;
        }

        private void UpdatePrevData()
        {
            m_PrevPosition = m_Content.anchoredPosition;
            m_PrevViewBounds = m_ViewBounds;
            m_PrevContentBounds = m_ContentBounds;
        }

        private void UpdateScrollbars(Vector2 offset)
        {
            if (m_HorizontalScrollbar)
            {
                if (m_ContentBounds.size.x > 0)
                    m_HorizontalScrollbar.size = Mathf.Clamp01((m_ViewBounds.size.x - Mathf.Abs(offset.x)) / m_ContentBounds.size.x);
                else
                    m_HorizontalScrollbar.size = 1;

                m_HorizontalScrollbar.value = horizontalNormalizedPosition;
            }

            if (m_VerticalScrollbar)
            {
                if (m_ContentBounds.size.y > 0)
                    m_VerticalScrollbar.size = Mathf.Clamp01((m_ViewBounds.size.y - Mathf.Abs(offset.y)) / m_ContentBounds.size.y);
                else
                    m_VerticalScrollbar.size = 1;

                m_VerticalScrollbar.value = verticalNormalizedPosition;
            }
        }

        public Vector2 normalizedPosition
        {
            get
            {
                return new Vector2(horizontalNormalizedPosition, verticalNormalizedPosition);
            }
            set
            {
                SetNormalizedPosition(value.x, 0);
                SetNormalizedPosition(value.y, 1);
            }
        }

        public float horizontalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if ((m_ContentBounds.size.x <= m_ViewBounds.size.x) || Mathf.Approximately(m_ContentBounds.size.x, m_ViewBounds.size.x))
                    return (m_ViewBounds.min.x > m_ContentBounds.min.x) ? 1 : 0;
                return (m_ViewBounds.min.x - m_ContentBounds.min.x) / (m_ContentBounds.size.x - m_ViewBounds.size.x);
            }
            set
            {
                SetNormalizedPosition(value, 0);
            }
        }

        public float verticalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if ((m_ContentBounds.size.y <= m_ViewBounds.size.y) || Mathf.Approximately(m_ContentBounds.size.y, m_ViewBounds.size.y))
                    return (m_ViewBounds.min.y > m_ContentBounds.min.y) ? 1 : 0;

                return (m_ViewBounds.min.y - m_ContentBounds.min.y) / (m_ContentBounds.size.y - m_ViewBounds.size.y);
            }
            set
            {
                SetNormalizedPosition(value, 1);
            }
        }

        private void SetHorizontalNormalizedPosition(float value) { SetNormalizedPosition(value, 0); }
        private void SetVerticalNormalizedPosition(float value) { SetNormalizedPosition(value, 1); }

        private void SetNormalizedPosition(float value, int axis)
        {
            EnsureLayoutHasRebuilt();
            UpdateBounds();
            // How much the content is larger than the view.
            float hiddenLength = m_ContentBounds.size[axis] - m_ViewBounds.size[axis];
            // Where the position of the lower left corner of the content bounds should be, in the space of the view.
            float contentBoundsMinPosition = m_ViewBounds.min[axis] - value * hiddenLength;
            // The new content localPosition, in the space of the view.
            float newAnchoredPosition = m_Content.anchoredPosition[axis] + contentBoundsMinPosition - m_ContentBounds.min[axis];

            Vector3 anchoredPosition = m_Content.anchoredPosition;
            if (Mathf.Abs(anchoredPosition[axis] - newAnchoredPosition) > 0.01f)
            {
                anchoredPosition[axis] = newAnchoredPosition;
                m_Content.anchoredPosition = anchoredPosition;
                m_Velocity[axis] = 0;
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

        private bool hScrollingNeeded => m_ContentBounds.size.x > m_ViewBounds.size.x + 0.01f;
        private bool vScrollingNeeded => m_ContentBounds.size.y > m_ViewBounds.size.y + 0.01f;

        void ILayoutController.SetLayoutVertical()
        {
            m_ViewBounds = viewport.rect;
            m_ContentBounds = GetBounds();
        }

        private void UpdateScrollbarVisibility()
        {
            UpdateOneScrollbarVisibility(vScrollingNeeded, m_Vertical, m_VerticalScrollbarVisibility, m_VerticalScrollbar);
            UpdateOneScrollbarVisibility(hScrollingNeeded, m_Horizontal, m_HorizontalScrollbarVisibility, m_HorizontalScrollbar);
        }

        private static void UpdateOneScrollbarVisibility(bool xScrollingNeeded, bool xAxisEnabled, ScrollbarVisibility scrollbarVisibility, Scrollbar scrollbar)
        {
            if (scrollbar)
            {
                if (scrollbarVisibility == ScrollbarVisibility.Permanent)
                {
                    if (scrollbar.gameObject.activeSelf != xAxisEnabled)
                        scrollbar.gameObject.SetActive(xAxisEnabled);
                }
                else
                {
                    if (scrollbar.gameObject.activeSelf != xScrollingNeeded)
                        scrollbar.gameObject.SetActive(xScrollingNeeded);
                }
            }
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
                // Adjust content so that content bounds bottom (right side) is never higher (to the left) than the view bounds bottom (right side).
                // top (left side) is never lower (to the right) than the view bounds top (left side).
                // All this can happen if content has shrunk.
                // This works because content size is at least as big as view size (because of the call to AdjustBounds above).
                Vector2 delta = Vector2.zero;
                if (m_ViewBounds.max.x > m_ContentBounds.max.x)
                {
                    delta.x = Math.Min(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
                }
                else if (m_ViewBounds.min.x < m_ContentBounds.min.x)
                {
                    delta.x = Math.Max(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
                }

                if (m_ViewBounds.min.y < m_ContentBounds.min.y)
                {
                    delta.y = Math.Max(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
                }
                else if (m_ViewBounds.max.y > m_ContentBounds.max.y)
                {
                    delta.y = Math.Min(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
                }
                if (delta.sqrMagnitude > float.Epsilon)
                {
                    contentPos = m_Content.anchoredPosition + delta;
                    if (!m_Horizontal)
                        contentPos.x = m_Content.anchoredPosition.x;
                    if (!m_Vertical)
                        contentPos.y = m_Content.anchoredPosition.y;
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
        private Vector2 CalculateOffset(Vector2 delta)
        {
            if (m_MovementType == MovementType.Unrestricted)
                return default;

            Vector2 viewMin = m_ViewBounds.min;
            Vector2 viewMax = m_ViewBounds.max;
            Vector2 contentMin = m_ContentBounds.min;
            Vector2 contentMax = m_ContentBounds.max;

            // min/max offset extracted to check if approximately 0 and avoid recalculating layout every frame (case 1010178)

            return new Vector2(
                m_Horizontal ? Calculate(viewMin.x, viewMax.x, contentMin.x, contentMax.x, delta.x) : 0,
                m_Vertical ? Calculate(viewMin.y, viewMax.y, contentMin.y, contentMax.y, delta.y) : 0);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static float Calculate(float viewMin, float viewMax, float contentMin, float contentMax, float delta)
            {
                contentMin += delta;
                contentMax += delta;

                var maxOffset = viewMax - contentMax;
                var minOffset = viewMin - contentMin;

                if (minOffset < -0.001f)
                    return minOffset;
                if (maxOffset > 0.001f)
                    return maxOffset;
                return 0;
            }
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
