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

        private Bounds m_ContentBounds;
        private Bounds m_ViewBounds;

        private float m_Velocity;
        public float velocity => m_Velocity;

        private bool m_Dragging;
        private bool m_Scrolling;

        private float m_PrevPosition;
        private Bounds m_PrevContentBounds;
        private Bounds m_PrevViewBounds;
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
                position[di] -= RubberDelta(offset, m_ViewBounds.size);

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
            if (m_ContentBounds.size > 0)
                m_Scrollbar.size = Mathf.Clamp01((m_ViewBounds.size - Mathf.Abs(offset)) / m_ContentBounds.size);
            else
                m_Scrollbar.size = 1;
            m_Scrollbar.value = normalizedPosition;
        }

        public float normalizedPosition
        {
            get
            {
                UpdateBounds();
                return m_ViewBounds.NormalizedPosition(m_ContentBounds);
            }
            set => SetNormalizedPosition(value);
        }

        private void SetNormalizedPosition(float value)
        {
            EnsureLayoutHasRebuilt();
            UpdateBounds();
            var i = di;
            float hiddenLength = m_ContentBounds.size - m_ViewBounds.size;
            float contentBoundsMinPosition = m_ViewBounds.min - value * hiddenLength;
            float newAnchoredPosition = m_Content.anchoredPosition[i] + contentBoundsMinPosition - m_ContentBounds.min;

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

        private bool scrollingNeeded => m_ContentBounds.size > m_ViewBounds.size + 0.01f;

        void ILayoutController.SetLayoutVertical()
        {
            m_ViewBounds = Bounds.FromRect(viewport.rect, di);
            m_ContentBounds = GetContentBounds();
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
            var viewRect = viewport.rect;
            var i = di;
            m_ViewBounds = Bounds.FromRect(viewRect, i);

            if (m_Content == null)
                return;

            // Get content bounds in viewport-local space, expand to fill view on both axes.
            m_Content.CalcWorldCorners2D(default,
                out var p0, out var p1, out var p2, out var p3, out _);
            var m = viewport.worldToLocalMatrix;
            Vector2 v0 = m.MultiplyPoint2D(p0), v1 = m.MultiplyPoint2D(p1);
            Vector2 v2 = m.MultiplyPoint2D(p2), v3 = m.MultiplyPoint2D(p3);
            var cMin = Vector2.Min(Vector2.Min(v0, v1), Vector2.Min(v2, v3));
            var cMax = Vector2.Max(Vector2.Max(v0, v1), Vector2.Max(v2, v3));
            Vector2 contentSize = cMax - cMin;
            Vector2 contentPos = (cMin + cMax) * 0.5f;

            // Ensure content bounds are at least as large as view by expanding around the content pivot.
            var contentPivot = m_Content.pivot;
            Vector2 excess = viewRect.size - contentSize;
            if (excess.x > 0)
            {
                contentPos.x -= excess.x * (contentPivot.x - 0.5f);
                contentSize.x = viewRect.size.x;
            }
            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (contentPivot.y - 0.5f);
                contentSize.y = viewRect.size.y;
            }

            m_ContentBounds = Bounds.FromCenterSize(contentPos[i], contentSize[i]);

            if (movementType == MovementType.Clamped)
            {
                float clampDelta = m_ViewBounds.ClampDelta(m_ContentBounds);
                if (Mathf.Abs(clampDelta) > float.Epsilon)
                {
                    float newCenter = m_Content.anchoredPosition[i] + clampDelta;
                    m_ContentBounds = m_ViewBounds.Adjust(newCenter, contentSize[i], contentPivot[i]);
                }
            }
        }

        private float CalculateOffset(float delta)
        {
            if (m_MovementType == MovementType.Unrestricted)
                return 0;
            return m_ViewBounds.Offset(m_ContentBounds, delta);
        }

        // Used by SetLayoutVertical (before full UpdateBounds).
        private Bounds GetContentBounds()
        {
            if (m_Content == null)
                return default;
            m_Content.CalcWorldCorners2D(default,
                out var p0, out var p1, out var p2, out var p3, out _);
            var m = viewport.worldToLocalMatrix;
            var i = di;
            float a = m.MultiplyPoint2D(p0)[i], b = m.MultiplyPoint2D(p1)[i];
            float c = m.MultiplyPoint2D(p2)[i], d = m.MultiplyPoint2D(p3)[i];
            return new Bounds(
                Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d)),
                Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d)));
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

        /// <summary>1D interval [min, max] for single-axis scroll calculations.</summary>
        private struct Bounds : IEquatable<Bounds>
        {
            public float min, max;

            public Bounds(float min, float max) { this.min = min; this.max = max; }

            public float size => max - min;

            public static Bounds FromCenterSize(float center, float size)
            {
                float half = size * 0.5f;
                return new Bounds(center - half, center + half);
            }

            public static Bounds FromRect(Rect rect, int axis) => new(rect.min[axis], rect.max[axis]);

            /// <summary>
            /// How much content must shift (after applying delta) to stay within this (view) range.
            /// Returns 0 if within threshold. Threshold prevents jitter from micro-corrections.
            /// </summary>
            public float Offset(Bounds content, float delta = 0, float threshold = 0.001f)
            {
                float cMin = content.min + delta;
                float cMax = content.max + delta;
                float minOff = min - cMin;
                float maxOff = max - cMax;
                if (minOff < -threshold) return minOff;
                if (maxOff > threshold) return maxOff;
                return 0;
            }

            /// <summary>Normalized position of this (view) within content. 0 = start, 1 = end.</summary>
            public float NormalizedPosition(Bounds content)
            {
                if (content.size <= size || Mathf.Approximately(content.size, size))
                    return min > content.min ? 1 : 0;
                return (min - content.min) / (content.size - size);
            }

            /// <summary>
            /// Clamp delta: how much to shift content so it fills this (view) range.
            /// Assumes content.size >= view.size (guaranteed by Adjust).
            /// </summary>
            public float ClampDelta(Bounds content)
            {
                float minDiff = min - content.min;
                float maxDiff = max - content.max;
                if (maxDiff > 0) return Math.Min(minDiff, maxDiff);
                if (minDiff < 0) return Math.Max(minDiff, maxDiff);
                return 0;
            }

            /// <summary>Expand content range around pivot so it's at least as large as this (view) range.</summary>
            public Bounds Adjust(float contentCenter, float contentSize, float pivot)
            {
                float excess = size - contentSize;
                if (excess > 0)
                {
                    contentCenter -= excess * (pivot - 0.5f);
                    contentSize = size;
                }
                return FromCenterSize(contentCenter, contentSize);
            }

            public bool Equals(Bounds other) => min == other.min && max == other.max;
            public override bool Equals(object? obj) => obj is Bounds other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(min, max);
            public static bool operator ==(Bounds a, Bounds b) => a.min == b.min && a.max == b.max;
            public static bool operator !=(Bounds a, Bounds b) => a.min != b.min || a.max != b.max;
        }
    }
}
