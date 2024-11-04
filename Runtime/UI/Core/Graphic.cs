using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using System.Reflection;
#endif
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    /// <summary>
    /// Base class for all UI components that should be derived from when creating new Graphic types.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public abstract class Graphic
        : UIBehaviour,
            ICanvasElement
    {
        static Material s_DefaultUI = null;
        public static Material defaultGraphicMaterial => s_DefaultUI ??= Canvas.GetDefaultCanvasMaterial();
        static Texture2D s_WhiteTexture = null;
        protected static Texture2D whiteTexture => s_WhiteTexture ??= Texture2D.whiteTexture;

        // Cached and saved values
        [FormerlySerializedAs("m_Mat")]
        [ShowIf("@CanShow(GraphicPropertyFlag.Material)"), PropertyOrder(501)]
        [SerializeField] protected Material m_Material;

        [ShowIf("@CanShow(GraphicPropertyFlag.Color)"), PropertyOrder(500)]
        [SerializeField] Color m_Color = Color.white;

        [NonSerialized] protected bool m_SkipLayoutUpdate;
        [NonSerialized] protected bool m_SkipMaterialUpdate;

        public virtual Color color
        {
            get => m_Color;
            set
            {
                if (SetPropertyUtility.SetColor(ref m_Color, value)) SetVerticesDirty();
            }
        }

        [SerializeField, ShowIf("@CanShow(GraphicPropertyFlag.Raycast)")]
        [FoldoutGroup("Advanced", order: 600)]
        [HorizontalGroup("Advanced/RaycastTarget", DisableAutomaticLabelWidth = true)]
        bool m_RaycastTarget = false;

        protected RaycastRegisterLink m_RaycastRegisterLink;

        /// <summary>
        /// Should this graphic be considered a target for raycasting?
        /// </summary>
        public bool raycastTarget
        {
            get => m_RaycastTarget;
            set
            {
                m_RaycastTarget = value;
                if (value) m_RaycastRegisterLink.Reset(canvas, this);
                else m_RaycastRegisterLink.TryUnlink(this);
            }
        }

        [SerializeField, HideLabel, ShowIf("@CanShow(GraphicPropertyFlag.Raycast) && m_RaycastTarget")]
        [FoldoutGroup("Advanced"), HorizontalGroup("Advanced/RaycastTarget", DisableAutomaticLabelWidth = true)]
        Vector4 m_RaycastPadding;

        /// <summary>
        /// Padding to be applied to the masking
        /// X = Left
        /// Y = Bottom
        /// Z = Right
        /// W = Top
        /// </summary>
        public Vector4 raycastPadding
        {
            get => m_RaycastPadding;
            set => m_RaycastPadding = value;
        }

        [NonSerialized] RectTransform m_RectTransform;
        [NonSerialized] CanvasRenderer m_CanvasRenderer;
        [NonSerialized] Canvas m_Canvas;

        [NonSerialized] bool m_VertsDirty;
        [NonSerialized] bool m_MaterialDirty;

        /// <summary>
        /// Set all properties of the Graphic dirty and needing rebuilt.
        /// Dirties Layout, Vertices, and Materials.
        /// </summary>
        public virtual void SetAllDirty()
        {
            // Optimization: Graphic layout doesn't need recalculation if
            // the underlying Sprite is the same size with the same texture.
            // (e.g. Sprite sheet texture animation)

            if (m_SkipLayoutUpdate)
            {
                m_SkipLayoutUpdate = false;
            }
            else
            {
                SetLayoutDirty();
            }

            if (m_SkipMaterialUpdate)
            {
                m_SkipMaterialUpdate = false;
            }
            else
            {
                SetMaterialDirty();
            }

            SetVerticesDirty();
            SetRaycastDirty();
        }

        /// <summary>
        /// Mark the layout as dirty and needing rebuilt.
        /// </summary>
        /// <remarks>
        /// Send a OnDirtyLayoutCallback notification if any elements are registered. See RegisterDirtyLayoutCallback
        /// </remarks>
        public virtual void SetLayoutDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        /// <summary>
        /// Mark the vertices as dirty and needing rebuilt.
        /// </summary>
        /// <remarks>
        /// Send a OnDirtyVertsCallback notification if any elements are registered. See RegisterDirtyVerticesCallback
        /// </remarks>
        public virtual void SetVerticesDirty()
        {
            if (!IsActive())
                return;

            m_VertsDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }

        /// <summary>
        /// Mark the material as dirty and needing rebuilt.
        /// </summary>
        /// <remarks>
        /// Send a OnDirtyMaterialCallback notification if any elements are registered. See RegisterDirtyMaterialCallback
        /// </remarks>
        public virtual void SetMaterialDirty()
        {
            if (!IsActive())
                return;

            m_MaterialDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }

        public void SetRaycastDirty()
        {
            m_RaycastRegisterLink.Reset(canvas, this);
        }

        protected virtual void OnRectTransformDimensionsChange()
        {
            if (gameObject.activeInHierarchy)
            {
                // prevent double dirtying...
                if (CanvasUpdateRegistry.IsRebuildingLayout())
                    SetVerticesDirty();
                else
                {
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        protected virtual void OnBeforeTransformParentChanged()
        {
            if (!IsActive())
                return;

            m_RaycastRegisterLink.TryUnlink(this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        protected virtual void OnTransformParentChanged()
        {
            m_Canvas = null;

            if (!IsActive())
                return;

            CacheCanvas();
            m_RaycastRegisterLink.Reset(m_Canvas, this);
            SetAllDirty();
        }

        /// <summary>
        /// The RectTransform component used by the Graphic. Cached for speed.
        /// </summary>
        public RectTransform rectTransform
        {
            get
            {
                // The RectTransform is a required component that must not be destroyed. Based on this assumption, a
                // null-reference check is sufficient.
                return m_RectTransform ??= (RectTransform) transform;
            }
        }

        /// <summary>
        /// A reference to the Canvas this Graphic is rendering to.
        /// </summary>
        /// <remarks>
        /// In the situation where the Graphic is used in a hierarchy with multiple Canvases, the Canvas closest to the root will be used.
        /// </remarks>
        public Canvas canvas
        {
            get
            {
                if (m_Canvas == null)
                    CacheCanvas();
                return m_Canvas;
            }
        }

        void CacheCanvas()
        {
            m_Canvas = ComponentSearch.SearchEnabledParentOrSelfComponent<Canvas>(this);
        }

        /// <summary>
        /// A reference to the CanvasRenderer populated by this Graphic.
        /// </summary>
        // The CanvasRenderer is a required component that must not be destroyed. Based on this assumption, a null-reference check is sufficient.
        public CanvasRenderer canvasRenderer => m_CanvasRenderer ??= (GetComponent<CanvasRenderer>() ?? gameObject.AddComponent<CanvasRenderer>());

        /// <summary>
        /// The Material set by the user
        /// </summary>
        public virtual Material material
        {
            get => m_Material != null ? m_Material : defaultGraphicMaterial;
            set
            {
                if (ReferenceEquals(m_Material, value))
                    return;

                m_Material = value;
                SetMaterialDirty();
            }
        }

        /// <summary>
        /// The material that will be sent for Rendering (Read only).
        /// </summary>
        /// <remarks>
        /// This is the material that actually gets sent to the CanvasRenderer. By default it's the same as [[Graphic.material]]. When extending Graphic you can override this to send a different material to the CanvasRenderer than the one set by Graphic.material. This is useful if you want to modify the user set material in a non destructive manner.
        /// </remarks>
        public virtual Material materialForRendering => MaterialModifierUtils.ResolveMaterialForRendering(this, material);

        /// <summary>
        /// The graphic's texture. (Read Only).
        /// </summary>
        /// <remarks>
        /// This is the Texture that gets passed to the CanvasRenderer, Material and then Shader _MainTex.
        ///
        /// When implementing your own Graphic you can override this to control which texture goes through the UI Rendering pipeline.
        ///
        /// Bear in mind that Unity tries to batch UI elements together to improve performance, so its ideal to work with atlas to reduce the number of draw calls.
        /// </remarks>
        public virtual Texture mainTexture => s_WhiteTexture;

        /// <summary>
        /// Mark the Graphic and the canvas as having been changed.
        /// </summary>
        protected virtual void OnEnable()
        {
            CacheCanvas();
            m_RaycastRegisterLink.Reset(m_Canvas, this);

#if UNITY_EDITOR
            GraphicRebuildTracker.TrackGraphic(this);
#endif
            if (s_WhiteTexture == null)
                s_WhiteTexture = Texture2D.whiteTexture;

            SetAllDirty();
        }

        /// <summary>
        /// Clear references.
        /// </summary>
        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            GraphicRebuildTracker.UnTrackGraphic(this);
#endif
            m_RaycastRegisterLink.TryUnlink(this);
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            if (canvasRenderer != null)
                canvasRenderer.Clear();

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        protected virtual void OnCanvasHierarchyChanged()
        {
            // Clear the cached canvas. Will be fetched below if active.
            m_Canvas = null;

            if (!IsActive())
            {
                // XXX: As we already called unregister in OnDisable(), we should not call it again here.
                // GraphicRegistry.UnregisterGraphicForCanvas(currentCanvas, this);
                return;
            }

            CacheCanvas();
            m_RaycastRegisterLink.Reset(m_Canvas, this);
        }

        /// <summary>
        /// This method must be called when <c>CanvasRenderer.cull</c> is modified.
        /// </summary>
        /// <remarks>
        /// This can be used to perform operations that were previously skipped because the <c>Graphic</c> was culled.
        /// </remarks>
        protected void OnCullingChanged()
        {
            if (!canvasRenderer.cull && (m_VertsDirty || m_MaterialDirty))
            {
                /// When we were culled, we potentially skipped calls to <c>Rebuild</c>.
                CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
            }
        }

        /// <summary>
        /// See IClippable.Cull
        /// </summary>
        public virtual void Cull(Rect clipRect, bool validRect)
        {
            if (validRect is false)
            {
                UpdateCull(true);
                return;
            }

            var graphicRect = CanvasUtils.BoundingRect(rectTransform, canvas);
            var cull = !clipRect.Overlaps(graphicRect);
            UpdateCull(cull);
        }

        protected void UpdateCull(bool cull)
        {
            if (canvasRenderer.cull != cull)
            {
                canvasRenderer.cull = cull;
                UISystemProfilerApi.AddMarker("MaskableGraphic.cullingChanged", this);
                OnCullingChanged();
            }
        }

        /// <summary>
        /// See IClippable.SetClipRect
        /// </summary>
        public void SetClipRect(Rect clipRect, bool validRect)
        {
            if (validRect)
                canvasRenderer.EnableRectClipping(clipRect);
            else
                canvasRenderer.DisableRectClipping();
        }

        public void SetClipSoftness(Vector2 clipSoftness)
        {
            canvasRenderer.clippingSoftness = clipSoftness;
        }

        /// <summary>
        /// Rebuilds the graphic geometry and its material on the PreRender cycle.
        /// </summary>
        /// <param name="update">The current step of the rendering CanvasUpdate cycle.</param>
        /// <remarks>
        /// See CanvasUpdateRegistry for more details on the canvas update cycle.
        /// </remarks>
        public virtual void Rebuild(CanvasUpdate update)
        {
            if (canvasRenderer is null || canvasRenderer.cull)
                return;

            switch (update)
            {
                case CanvasUpdate.PreRender:
                    if (m_VertsDirty)
                    {
                        UpdateGeometry();
                        m_VertsDirty = false;
                    }
                    if (m_MaterialDirty)
                    {
                        UpdateMaterial();
                        m_MaterialDirty = false;
                    }
                    break;
            }
        }

        /// <summary>
        /// Call to update the Material of the graphic onto the CanvasRenderer.
        /// </summary>
        protected virtual void UpdateMaterial()
        {
            if (!IsActive())
                return;

            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(materialForRendering, 0);
            canvasRenderer.SetTexture(mainTexture);
        }

        /// <summary>
        /// Call to update the geometry of the Graphic onto the CanvasRenderer.
        /// </summary>
        protected virtual void UpdateGeometry()
        {
            using var _ = MeshBuilderPool.Rent(out var mb);

            OnPopulateMesh(mb);

            // When no vertices are generated, SetMesh with an empty mesh.
            // If we call canvasRenderer.Clear() to clear the mesh,
            // somehow it will prevent further graphic rendering.
            var posCount = mb.Poses.Count;
            if (posCount is MeshBuilder.Invalid)
            {
                canvasRenderer.SetMesh(SharedMesh.Empty);
                return;
            }

            mb.AssertPrepared();

            MeshModifierUtils.GetComponentsAndModifyMesh(this, mb);

            var mesh = SharedMesh.Claim();
            mesh.Clear();
            mb.FillMeshAndInvalidate(mesh);
            canvasRenderer.SetMesh(mesh);
            SharedMesh.Release(mesh);
        }

        public virtual void ForceUpdateGeometry() => UpdateGeometry();

        /// <summary>
        /// Callback function when a UI element needs to generate vertices. Fills the vertex buffer data.
        /// </summary>
        /// <remarks>
        /// Used by Text, UI.Image, and RawImage for example to generate vertices specific to their use case.
        /// </remarks>
        protected virtual void OnPopulateMesh(MeshBuilder mb) { }

        // Call from unity if animation properties have changed

        protected virtual void OnDidApplyAnimationProperties()
        {
            SetAllDirty();
        }

        /// <summary>
        /// Make the Graphic have the native size of its content.
        /// </summary>
        public virtual void SetNativeSize() { }

        /// <summary>
        /// Returns a pixel perfect Rect closest to the Graphic RectTransform.
        /// </summary>
        /// <remarks>
        /// Note: This is only accurate if the Graphic root Canvas is in Screen Space.
        /// </remarks>
        /// <returns>A Pixel perfect Rect.</returns>
        protected Rect GetPixelAdjustedRect() => rectTransform.rect;

#if UNITY_EDITOR
        protected virtual void OnValidate() => SetAllDirty();
        protected virtual void Reset() => SetAllDirty();

        /// <summary>
        /// Editor-only callback that is issued by Unity if a rebuild of the Graphic is required.
        /// Currently sent when an asset is reimported.
        /// </summary>
        public virtual void OnRebuildRequested()
        {
            // when rebuild is requested we need to rebuild all the graphics /
            // and associated components... The correct way to do this is by
            // calling OnValidate... Because MB's don't have a common base class
            // we do this via reflection. It's nasty and ugly... Editor only.
            m_SkipLayoutUpdate = true;
            var mbs = gameObject.GetComponents<MonoBehaviour>();
            foreach (var mb in mbs)
            {
                if (mb == null)
                    continue;
                var methodInfo = mb.GetType().GetMethod("OnValidate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (methodInfo != null)
                    methodInfo.Invoke(mb, null);
            }
            m_SkipLayoutUpdate = false;
        }

        [UsedImplicitly]
        bool CanShow(GraphicPropertyFlag flag) => GraphicPropertyVisible.IsVisible(GetType(), flag);
#endif
    }
}