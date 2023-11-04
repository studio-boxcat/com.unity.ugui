using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using System.Reflection;
#endif
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    /// <summary>
    /// Base class for all UI components that should be derived from when creating new Graphic types.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    /// <summary>
    ///   Base class for all visual UI Component.
    ///   When creating visual UI components you should inherit from this class.
    /// </summary>
    /// <example>
    /// Below is a simple example that draws a colored quad inside the Rect Transform area.
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.UI;
    ///
    /// [ExecuteInEditMode]
    /// public class SimpleImage : Graphic
    /// {
    ///     protected override void OnPopulateMesh(VertexHelper vh)
    ///     {
    ///         Vector2 corner1 = Vector2.zero;
    ///         Vector2 corner2 = Vector2.zero;
    ///
    ///         corner1.x = 0f;
    ///         corner1.y = 0f;
    ///         corner2.x = 1f;
    ///         corner2.y = 1f;
    ///
    ///         corner1.x -= rectTransform.pivot.x;
    ///         corner1.y -= rectTransform.pivot.y;
    ///         corner2.x -= rectTransform.pivot.x;
    ///         corner2.y -= rectTransform.pivot.y;
    ///
    ///         corner1.x *= rectTransform.rect.width;
    ///         corner1.y *= rectTransform.rect.height;
    ///         corner2.x *= rectTransform.rect.width;
    ///         corner2.y *= rectTransform.rect.height;
    ///
    ///         vh.Clear();
    ///
    ///         UIVertex vert = UIVertex.simpleVert;
    ///
    ///         vert.position = new Vector2(corner1.x, corner1.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vert.position = new Vector2(corner1.x, corner2.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vert.position = new Vector2(corner2.x, corner2.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vert.position = new Vector2(corner2.x, corner1.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vh.AddTriangle(0, 1, 2);
    ///         vh.AddTriangle(2, 3, 0);
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public abstract class Graphic
        : UIBehaviour,
            ICanvasElement
    {
        protected static Material s_DefaultUI = null;
        protected static Texture2D s_WhiteTexture = null;

        /// <summary>
        /// Default material used to draw UI elements if no explicit material was specified.
        /// </summary>
        public static Material defaultGraphicMaterial => s_DefaultUI ??= Canvas.GetDefaultCanvasMaterial();

        // Cached and saved values
        [FormerlySerializedAs("m_Mat")]
        [ShowIf("m_Material_ShowIf")]
        [SerializeField] protected Material m_Material;

        [ShowIf("m_Color_ShowIf")]
        [SerializeField] private Color m_Color = Color.white;

        [NonSerialized] protected bool m_SkipLayoutUpdate;
        [NonSerialized] protected bool m_SkipMaterialUpdate;

        /// <summary>
        /// Base color of the Graphic.
        /// </summary>
        /// <remarks>
        /// The builtin UI Components use this as their vertex color. Use this to fetch or change the Color of visual UI elements, such as an Image.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// //Place this script on a GameObject with a Graphic component attached e.g. a visual UI element (Image).
        ///
        /// using UnityEngine;
        /// using UnityEngine.UI;
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     Graphic m_Graphic;
        ///     Color m_MyColor;
        ///
        ///     void Start()
        ///     {
        ///         //Fetch the Graphic from the GameObject
        ///         m_Graphic = GetComponent<Graphic>();
        ///         //Create a new Color that starts as red
        ///         m_MyColor = Color.red;
        ///         //Change the Graphic Color to the new Color
        ///         m_Graphic.color = m_MyColor;
        ///     }
        ///
        ///     // Update is called once per frame
        ///     void Update()
        ///     {
        ///         //When the mouse button is clicked, change the Graphic Color
        ///         if (Input.GetKey(KeyCode.Mouse0))
        ///         {
        ///             //Change the Color over time between blue and red while the mouse button is pressed
        ///             m_MyColor = Color.Lerp(Color.red, Color.blue, Mathf.PingPong(Time.time, 1));
        ///         }
        ///         //Change the Graphic Color to the new Color
        ///         m_Graphic.color = m_MyColor;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual Color color { get { return m_Color; } set { if (SetPropertyUtility.SetColor(ref m_Color, value)) SetVerticesDirty(); } }

        [SerializeField, ShowIf("m_RaycastTarget_ShowIf")] private bool m_RaycastTarget = false;

        protected RaycastRegisterLink m_RaycastRegisterLink;

        /// <summary>
        /// Should this graphic be considered a target for raycasting?
        /// </summary>
        public virtual bool raycastTarget
        {
            get
            {
                return m_RaycastTarget;
            }
            set
            {
                m_RaycastTarget = value;

                if (value)
                {
                    m_RaycastRegisterLink.Reset(canvas, this);
                }
                else
                {
                    m_RaycastRegisterLink.TryUnlink(this);
                }
            }
        }

        [SerializeField, ShowIf("m_RaycastPadding_ShowIf")]
        private Vector4 m_RaycastPadding = new Vector4();

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

        [NonSerialized] private RectTransform m_RectTransform;
        [NonSerialized] private CanvasRenderer m_CanvasRenderer;
        [NonSerialized] private Canvas m_Canvas;

        [NonSerialized] private bool m_VertsDirty;
        [NonSerialized] private bool m_MaterialDirty;

        [NonSerialized] protected static Mesh s_Mesh;

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
        /// Absolute depth of the graphic, used by rendering and events -- lowest to highest.
        /// </summary>
        /// <example>
        /// The depth is relative to the first root canvas.
        ///
        /// Canvas
        ///  Graphic - 1
        ///  Graphic - 2
        ///  Nested Canvas
        ///     Graphic - 3
        ///     Graphic - 4
        ///  Graphic - 5
        ///
        /// This value is used to determine draw and event ordering.
        /// </example>
        public int depth { get { return canvasRenderer.absoluteDepth; } }

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

        private void CacheCanvas()
        {
            m_Canvas = ComponentSearch.SearchActiveAndEnabledParentOrSelfComponent<Canvas>(this);
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
            get
            {
                return (m_Material != null) ? m_Material : defaultGraphicMaterial;
            }
            set
            {
                if (ReferenceEquals(m_Material, value))
                    return;

                m_Material = value;
                SetMaterialDirty();
            }
        }

        static readonly List<IMaterialModifier> _materialModifierBuf = new();

        /// <summary>
        /// The material that will be sent for Rendering (Read only).
        /// </summary>
        /// <remarks>
        /// This is the material that actually gets sent to the CanvasRenderer. By default it's the same as [[Graphic.material]]. When extending Graphic you can override this to send a different material to the CanvasRenderer than the one set by Graphic.material. This is useful if you want to modify the user set material in a non destructive manner.
        /// </remarks>
        public virtual Material materialForRendering
        {
            get
            {
                var currentMat = material;
                GetComponents(_materialModifierBuf);
                var count = _materialModifierBuf.Count;
                for (var i = 0; i < count; i++)
                    currentMat = _materialModifierBuf[i].GetModifiedMaterial(currentMat);
                return currentMat;
            }
        }

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

#if DEBUG
            try
#endif
            {
                OnPopulateMesh(mb);
                Assert.IsTrue(mb.CheckPrepared());
                MeshModifierUtils.GetComponentsAndModifyMesh(this, mb);
            }
#if DEBUG
            catch (Exception e)
            {
                Debug.LogException(e, this);
                mb.Invalidate();
                canvasRenderer.Clear();
                return;
            }
#endif

            var mesh = workerMesh;
            mesh.Clear();

#if DEBUG
            try
#endif
            {
                mb.FillMesh(mesh);
            }
#if DEBUG
            catch (Exception e)
            {
                Debug.LogException(e, this);
                mb.Invalidate();
                canvasRenderer.Clear();
                return;
            }
#endif

            mb.Invalidate();
            canvasRenderer.SetMesh(mesh);
        }

        protected static Mesh workerMesh
        {
            get
            {
                return s_Mesh ??= new Mesh
                {
                    name = "Shared UI Mesh",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
        }

        /// <summary>
        /// Callback function when a UI element needs to generate vertices. Fills the vertex buffer data.
        /// </summary>
        /// <remarks>
        /// Used by Text, UI.Image, and RawImage for example to generate vertices specific to their use case.
        /// </remarks>
        protected virtual void OnPopulateMesh(MeshBuilder mb) { }

#if UNITY_EDITOR
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

        protected virtual void Reset()
        {
            SetAllDirty();
        }

#endif

        // Call from unity if animation properties have changed

        protected virtual void OnDidApplyAnimationProperties()
        {
            SetAllDirty();
        }

        /// <summary>
        /// Make the Graphic have the native size of its content.
        /// </summary>
        public virtual void SetNativeSize() { }

        static readonly List<ICanvasRaycastFilter> _raycastFilterBuf = new();

        /// <summary>
        /// When a GraphicRaycaster is raycasting into the scene it does two things. First it filters the elements using their RectTransform rect. Then it uses this Raycast function to determine the elements hit by the raycast.
        /// </summary>
        /// <param name="sp">Screen point being tested</param>
        /// <param name="eventCamera">Camera that is being used for the testing.</param>
        /// <returns>True if the provided point is a valid location for GraphicRaycaster raycasts.</returns>
        public bool Raycast(Vector2 sp, Camera eventCamera)
        {
            Assert.IsTrue(isActiveAndEnabled);

            var t = transform;
            var ignoreParentGroups = false;
            while (t is not null)
            {
                // For most cases, there's no ICanvasRaycastFilter so we can avoid the GetComponents call.
                if (t.TryGetComponent(out ICanvasRaycastFilter _))
                {
                    t.GetComponents(_raycastFilterBuf);
                    foreach (var filter in _raycastFilterBuf)
                    {
                        // If the filter is disabled, skip it.
                        if (filter is Behaviour {enabled: false})
                            continue;

                        // Skip if we've set ignoreParentGroups to true.
                        if (filter is CanvasGroup group)
                        {
                            if (ignoreParentGroups)
                                continue;
                            if (group.ignoreParentGroups)
                                ignoreParentGroups = true;
                        }

                        // If any filter says it's not valid, return false.
                        if (filter.IsRaycastLocationValid(sp, eventCamera) == false)
                            return false;
                    }
                }


                // If canvas.overrideSorting is set, raycast traversal should stop at the canvas.
                if (t.TryGetComponent(out Canvas canvas) && canvas.overrideSorting)
                    break;


                // Go up the hierarchy.
                t = t.parent;
            }

            return true;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            SetAllDirty();
        }

#endif

        ///<summary>
        ///Adjusts the given pixel to be pixel perfect.
        ///</summary>
        ///<param name="point">Local space point.</param>
        ///<returns>Pixel perfect adjusted point.</returns>
        ///<remarks>
        ///Note: This is only accurate if the Graphic root Canvas is in Screen Space.
        ///</remarks>
        public Vector2 PixelAdjustPoint(Vector2 point)
        {
            if (!canvas || canvas.renderMode == RenderMode.WorldSpace || canvas.scaleFactor == 0.0f || !canvas.pixelPerfect)
                return point;
            else
            {
                return RectTransformUtility.PixelAdjustPoint(point, transform, canvas);
            }
        }

        /// <summary>
        /// Returns a pixel perfect Rect closest to the Graphic RectTransform.
        /// </summary>
        /// <remarks>
        /// Note: This is only accurate if the Graphic root Canvas is in Screen Space.
        /// </remarks>
        /// <returns>A Pixel perfect Rect.</returns>
        public Rect GetPixelAdjustedRect()
        {
            if (!canvas || canvas.renderMode == RenderMode.WorldSpace || canvas.scaleFactor == 0.0f || !canvas.pixelPerfect)
                return rectTransform.rect;
            else
                return RectTransformUtility.PixelAdjustRect(rectTransform, canvas);
        }

#if UNITY_EDITOR
        protected virtual bool m_Material_ShowIf() => true;
        protected virtual bool m_Color_ShowIf() => true;
        protected virtual bool m_RaycastTarget_ShowIf() => true;
        protected virtual bool m_RaycastPadding_ShowIf() => true;
#endif
    }
}