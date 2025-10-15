// ReSharper disable InconsistentNaming

#nullable enable
using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    /// <summary>
    /// Base class for all UI components that should be derived from when creating new Graphic types.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public abstract class Graphic : UIBehaviour
    {
        private static Material? s_DefaultUI;
        public static Material defaultGraphicMaterial => s_DefaultUI ??= Canvas.GetDefaultCanvasMaterial();
        private static Texture2D? s_WhiteTexture;
        protected static Texture2D whiteTexture => s_WhiteTexture ??= Texture2D.whiteTexture;

        // Cached and saved values
        [FormerlySerializedAs("m_Mat")]
        [ShowIf("@CanShow(GraphicPropertyFlag.Material)"), PropertyOrder(GraphicPropOrder.Material), OnValueChanged("OnInspectorMaterialChanged")]
        [SerializeField] protected Material? m_Material;

        [ShowIf("@CanShow(GraphicPropertyFlag.Color)"), PropertyOrder(GraphicPropOrder.Color), OnValueChanged("SetVerticesDirty"), DontValidate]
        [SerializeField]
        private Color m_Color = Color.white;

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

        [SerializeField, ShowIf("@CanShow(GraphicPropertyFlag.Raycast)"), OnValueChanged("SetRaycastDirty")]
        [FoldoutGroup("Advanced", order: GraphicPropOrder.Advanced), PropertyOrder(GraphicPropOrder.Advanced_RaycastTarget)]
        private bool m_RaycastTarget;

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

        [SerializeField, HideInInspector] // padding is edited by handles.
        private Vector4 m_RaycastPadding;

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

        [NonSerialized] private RectTransform? m_RectTransform;
        [NonSerialized] private CanvasRenderer? m_CanvasRenderer;
        [NonSerialized] private Canvas? m_Canvas;

        [NonSerialized] private bool m_VertsDirty;
        [NonSerialized] private bool m_MaterialDirty;

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

            LayoutRebuilder.SetDirty(rectTransform);
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
            CanvasUpdateRegistry.QueueGraphic(this);
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
            CanvasUpdateRegistry.QueueGraphic(this);
        }

        public void SetRaycastDirty()
        {
            m_RaycastRegisterLink.Reset(canvas, this);
        }

        protected virtual void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
            {
                SetVerticesDirty();

                // prevent double dirtying...
                if (CanvasUpdateRegistry.IsRebuildingLayout() is false)
                    SetLayoutDirty();
            }
        }

        protected virtual void OnBeforeTransformParentChanged()
        {
            if (!IsActive())
                return;

            m_RaycastRegisterLink.TryUnlink(this);
            LayoutRebuilder.SetDirty(rectTransform);
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
                if (!m_Canvas) CacheCanvas();
                return m_Canvas!; // must be exists
            }
        }

        private void CacheCanvas()
        {
            m_Canvas = ComponentSearch.NearestUpwards_GOAnyAndCompEnabled<Canvas>(this);
#if DEBUG
            if (!m_Canvas && Editing.No(this))
                L.E("[Graphic] No canvas found for the graphic: " + this);
#endif
        }

        /// <summary>
        /// A reference to the CanvasRenderer populated by this Graphic.
        /// </summary>
        // The CanvasRenderer is a required component that must not be destroyed. Based on this assumption, a null-reference check is sufficient.
        public CanvasRenderer canvasRenderer => m_CanvasRenderer ??= GetComponent<CanvasRenderer>();

        /// <summary>
        /// The Material set by the user
        /// </summary>
        public virtual Material material
        {
            get => m_Material ? m_Material! : defaultGraphicMaterial;
            set
            {
                if (m_Material.RefEq(value)) return;
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
        public virtual Texture mainTexture => s_WhiteTexture!;

        /// <summary>
        /// Mark the Graphic and the canvas as having been changed.
        /// </summary>
        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            EnabledMemory.Mark(this);
            GraphicRebuildTracker.TrackGraphic(this);
#endif

            CacheCanvas();
            m_RaycastRegisterLink.Reset(m_Canvas, this);

            s_WhiteTexture ??= Texture2D.whiteTexture;

            SetAllDirty();
        }

        /// <summary>
        /// Clear references.
        /// </summary>
        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            if (!EnabledMemory.Erase(this)) return; // OnDisable() is called without OnEnable(), mostly domain reload.
            GraphicRebuildTracker.UnTrackGraphic(this);
#endif

            m_RaycastRegisterLink.TryUnlink(this);

            canvasRenderer.Clear();

            LayoutRebuilder.SetDirty(rectTransform);
        }

        protected virtual void OnCanvasHierarchyChanged()
        {
            // Clear the cached canvas. Will be fetched below if active.
            m_Canvas = null;

            if (!IsActive())
            {
                // XXX: As we already called unregister in OnDisable(), we should not call it again here.
                // RaycastableRegistry.UnregisterGraphicForCanvas(currentCanvas, this);
                return;
            }

            CacheCanvas();
            m_RaycastRegisterLink.Reset(m_Canvas, this);
        }

        /// <summary>
        /// See IClippable.Cull
        /// </summary>
        public void Cull(Rect clipRect, bool validRect)
        {
            if (validRect is false)
            {
                UpdateCull(true); // true = don't draw
                return;
            }

            var graphicRect = CanvasUtils.BoundingRect(rectTransform, canvas);
            var cull = !clipRect.Overlaps(graphicRect);
            UpdateCull(cull);
        }

        internal void UpdateCull(bool cull)
        {
            var cr = canvasRenderer;
            if (cr.cull == cull) return;

            cr.cull = cull;
            // When we were culled, we potentially skipped calls to Rebuild.
            if (!cull && (m_VertsDirty || m_MaterialDirty))
                CanvasUpdateRegistry.QueueGraphic(this);
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
        /// Rebuilds the graphic geometry and its material on the Pre cycle.
        /// </summary>
        /// <remarks>
        /// See CanvasUpdateRegistry for more details on the canvas update cycle.
        /// </remarks>
        public virtual void Rebuild()
        {
            if (canvasRenderer.cull is false) // skip if culled
            {
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
            }
        }

        /// <summary>
        /// Call to update the Material of the graphic onto the CanvasRenderer.
        /// </summary>
        protected virtual void UpdateMaterial()
        {
            if (!IsActive())
                return;

            var cr = canvasRenderer;
            cr.materialCount = 1;
            cr.SetMaterial(materialForRendering, 0);
            cr.SetTexture(mainTexture);
        }

        /// <summary>
        /// Call to update the geometry of the Graphic onto the CanvasRenderer.
        /// </summary>
        protected virtual void UpdateGeometry()
        {
            // Populate the mesh builder with the vertices of the graphic.
            using var _ = MeshBuilderPool.Rent(out var mb); // automatically returns the MeshBuilder to the pool
#if UNITY_EDITOR
            try // XXX: never shouldn't be happening, but just in case.
#endif
            {
                OnPopulateMesh(mb);
            }
#if UNITY_EDITOR
            catch (Exception e)
            {
                L.E($"OnPopulateMesh failed for {name}", this);
                L.E(e);
                throw;
            }
#endif

            // When no vertices are generated, SetMesh with an empty mesh.
            // If we call canvasRenderer.Clear() to clear the mesh,
            // it will also remove the material, which is not what we want.
            if (mb.HasSetUp() is false)
            {
                canvasRenderer.SetMesh(MeshPool.Empty);
                return;
            }


            // modify the mesh.
            MeshModifierUtils.GetComponentsAndModifyMesh(this, mb);


            // set the mesh to the CanvasRenderer
            mb.SetMeshAndInvalidate(canvasRenderer);
        }

        public virtual void ForceUpdateGeometry() => UpdateGeometry();

        /// <summary>
        /// Callback function when a UI element needs to generate vertices. Fills the vertex buffer data.
        /// </summary>
        /// <remarks>
        /// Used by Text, UI.Image, and RawImage for example to generate vertices specific to their use case.
        /// </remarks>
        protected virtual void OnPopulateMesh(MeshBuilder mb)
        {
#if UNITY_EDITOR
            L.E("[Graphic] OnPopulateMesh not implemented: " + GetType().Name);
#endif
        }

        // Call from unity if animation properties have changed

        protected virtual void OnDidApplyAnimationProperties()
        {
            SetAllDirty();
        }

        /// <summary>
        /// Returns a pixel perfect Rect closest to the Graphic RectTransform.
        /// </summary>
        /// <remarks>
        /// Note: This is only accurate if the Graphic root Canvas is in Screen Space.
        /// </remarks>
        /// <returns>A Pixel perfect Rect.</returns>
        protected Rect GetPixelAdjustedRect() => rectTransform.rect;

#if UNITY_EDITOR
        [BoxGroup("Advanced/Top", showLabel: false, order: GraphicPropOrder.Advanced_Info)]
        [ShowInInspector, HideLabel, MultiLineProperty(4)]
        private string _infoMessage
        {
            get
            {
                var cr = canvasRenderer;
                if (!cr)
                    return "CanvasRenderer is not available.";

                var sb = SbPool.Rent();
                sb
                    .Append("canvas: \"").Append(canvas.SafeName()).Append("\", ")
                    .Append("cull: ").Append(cr.cull).Append(", ")
                    .Append("inheritedAlpha: ").Append((int) (cr.GetInheritedAlpha() * 255)).Append(", ")
                    ;

                // for deactivated graphics, the mesh is not built.
                var mesh = cr.GetMesh();
                if (mesh)
                {
                    sb.Append("vertices: ").Append(mesh.vertexCount).Append(", ")
                        .Append("indices: ").Append(mesh.GetIndexCount(0)).Append(", ");
                }

                sb.Length -= 2; // remove last ", "
                return SbPool.Return(sb);
            }
        }

        protected virtual void OnInspectorMaterialChanged() => SetMaterialDirty();

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
            SetAllDirty();
            m_SkipLayoutUpdate = false;
        }

        [UsedImplicitly]
        private bool CanShow(GraphicPropertyFlag flag) => GraphicPropertyVisible.IsVisible(GetType(), flag);

        [ContextMenu("Toggle Raycast Target _r")]
        private void ToggleRaycastTarget()
        {
            UnityEditor.Undo.RecordObject(this, "Toggle Raycast Target");
            raycastTarget = !raycastTarget;
            raycastPadding = Vector4.zero;
        }
#endif
    }
}