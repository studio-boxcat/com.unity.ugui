// ReSharper disable InconsistentNaming

#nullable enable
using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEngine.Experimental.Rendering;
#endif

namespace UnityEngine.UI
{
    /// <summary>
    /// Base class for all UI components that should be derived from when creating new Graphic types.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public abstract class Graphic : UIBehaviour
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        private static Material? s_DefaultUI;
        public static Material defaultGraphicMaterial => s_DefaultUI ??= Canvas.GetDefaultCanvasMaterial();
        private static Texture2D? s_WhiteTexture;
        protected static Texture2D whiteTexture => s_WhiteTexture ??= Texture2D.whiteTexture;

        [ShowIf("@CanShow(GraphicPropertyFlag.Material)"), PropertyOrder(GraphicPropOrder.Material), OnValueChanged("OnInspectorMaterialChanged")]
        [SerializeField] private GraphicMaterialKind m_Material;
        public GraphicMaterialKind material
        {
            get => m_Material;
            set
            {
                if (m_Material == value) return;
                m_Material = value;
                SetMaterialDirty();
            }
        }

        [ShowIf("@CanShow(GraphicPropertyFlag.Color)"), PropertyOrder(GraphicPropOrder.Color), OnValueChanged("SetVerticesDirty"), DontValidate]
        [SerializeField]
        private Color m_Color = Color.white;
        public virtual Color color
        {
            get => m_Color;
            set
            {
                if (SetPropertyUtility.SetColor(ref m_Color, value))
                    SetVerticesDirty();
            }
        }

        [SerializeField, ShowIf("@CanShow(GraphicPropertyFlag.Raycast)"), OnValueChanged("SetRaycastDirty")]
        [FoldoutGroup(GraphicEditorConst.Advanced, order: GraphicPropOrder.Advanced), PropertyOrder(GraphicPropOrder.Advanced_RaycastTarget)]
        private bool m_RaycastTarget;
        protected RaycastRegisterLink m_RaycastRegisterLink;
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

        // padding is edited by handles.
        [SerializeField, HideInInspector, FormerlySerializedAs("m_RaycastPadding")]
        private Vector4 m_RaycastInset;

        /// <summary>
        /// Inset to shrink the raycast area from each edge.
        /// X = Left, Y = Bottom, Z = Right, W = Top
        /// </summary>
        public Vector4 raycastInset
        {
            get => m_RaycastInset;
            set => m_RaycastInset = value;
        }

        [NonSerialized] private RectTransform? m_RectTransform;
        // The RectTransform is a required component that must not be destroyed. Based on this assumption, a
        // null-reference check is sufficient.
        public RectTransform rectTransform => m_RectTransform ??= (RectTransform)transform;

        [NonSerialized] private CanvasRenderer? m_CanvasRenderer;
        // The CanvasRenderer is a required component that must not be destroyed. Based on this assumption, a null-reference check is sufficient.
        public CanvasRenderer canvasRenderer => m_CanvasRenderer ??= GetComponent<CanvasRenderer>();

        [NonSerialized] private Canvas? m_Canvas;
        public Canvas canvas
        {
            get
            {
                if (!m_Canvas) CacheCanvas();
                return m_Canvas!; // must be exists
            }
        }

        [NonSerialized] private bool m_VertsDirty;
        [NonSerialized] private bool m_MaterialDirty;
        [NonSerialized] private bool m_SkipLayoutUpdate;
        [NonSerialized] private bool m_SkipMaterialUpdate;

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
        public virtual void SetLayoutDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.SetDirty(rectTransform);
        }

        public virtual void SetVerticesDirty()
        {
            m_VertsDirty = true;
            if (isActiveAndEnabled)
                CanvasUpdateRegistry.QueueGraphic(this);
        }

        public virtual void SetMaterialDirty()
        {
            m_MaterialDirty = true;
            if (isActiveAndEnabled)
                CanvasUpdateRegistry.QueueGraphic(this);
        }

        public void SetVisualDirty()
        {
            SetVerticesDirty();
            SetMaterialDirty();
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

        private void CacheCanvas()
        {
            m_Canvas = ComponentSearch.NearestUpwards_GOAnyAndCompEnabled<Canvas>(this);
#if DEBUG
            if (!m_Canvas && Editing.No(this))
                L.E("[Graphic] No canvas found for the graphic: " + this);
#endif
        }

        public virtual Texture mainTexture => s_WhiteTexture!;

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

        // Notified by CanvasRenderer.UpdateCull after the cull flag flips. When we become un-culled we
        // may have skipped Rebuild calls while culled, so re-queue if verts/material are still dirty.
        internal void OnUncull()
        {
            if ((m_VertsDirty || m_MaterialDirty))
                CanvasUpdateRegistry.QueueGraphic(this);
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

            canvasRenderer.SetMaterialSingle(GraphicMaterialResolver.ResolveRender(this), mainTexture);
        }

        /// <summary>
        /// Call to update the geometry of the Graphic onto the CanvasRenderer.
        /// </summary>
        protected virtual void UpdateGeometry()
        {
            // Populate the mesh builder with the vertices of the graphic.
            using var _ = MeshBuilderPool.Rent(out var mb); // automatically returns the MeshBuilder to the pool
#if UNITY_EDITOR
            try
#endif
            {
                var color = m_Color;
                this.OverlayColorToRender(ref color);
                OnPopulateMesh(color, mb);

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
#if UNITY_EDITOR
            catch (Exception e)
            {
                L.E($"UpdateGeometry failed for {name}", this);
                L.E(e);
                mb.Invalidate(); // keep the pool consistent so Return() doesn't mask this exception
                throw;
            }
#endif
        }

        /// <summary>
        /// Callback function when a UI element needs to generate vertices. Fills the vertex buffer data.
        /// </summary>
        /// <remarks>
        /// Used by Text, UI.Image, and RawImage for example to generate vertices specific to their use case.
        /// </remarks>
        protected virtual void OnPopulateMesh(Color color, MeshBuilder mb)
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
                    .Append("inheritedAlpha: ").Append((int)(cr.GetInheritedAlpha() * 255)).Append(", ")
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
            raycastInset = Vector4.zero;
        }

        [ContextMenu("Reset Raycast Inset _p")]
        private void ResetRaycastInset()
        {
            UnityEditor.Undo.RecordObject(this, "Reset Raycast Inset");
            raycastInset = Vector4.zero;
        }

        public virtual void Validate(SelfValidationResult result)
        {
            if (GetComponents<IMaterialModifier>().Length > 1)
                result.AddError("Multiple IMaterialModifier components on the same GameObject are not supported.");

            if (m_Material is GraphicMaterialKind.Additive)
            {
                var tex = mainTexture;
                if (tex && tex.graphicsFormat is GraphicsFormat.R8_UNorm or GraphicsFormat.R16_UNorm or (GraphicsFormat)54 /* A8 */)
                    result.AddError("Additive material with R8 or A8 texture.");
            }

            var useCustomMaterial = m_Material is GraphicMaterialKind.Custom;
            if (useCustomMaterial && this.NoComponent<ICustomMaterialProvider>())
                result.AddError("GraphicMaterialKind.Custom requires an ICustomMaterialProvider component.");
        }
#endif
    }
}
