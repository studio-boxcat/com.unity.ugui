using System;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    /// <summary>
    /// Base class for effects that modify the generated Mesh.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    ///using UnityEngine;
    ///using UnityEngine.UI;
    ///
    ///public class PositionAsUV1 : BaseMeshEffect
    ///{
    ///    protected PositionAsUV1()
    ///    {}
    ///
    ///    public override void ModifyMesh(Mesh mesh)
    ///    {
    ///        if (!IsActive())
    ///            return;
    ///
    ///        var verts = mesh.vertices.ToList();
    ///        var uvs = ListPool<Vector2>.Get();
    ///
    ///        for (int i = 0; i < verts.Count; i++)
    ///        {
    ///            var vert = verts[i];
    ///            uvs.Add(new Vector2(verts[i].x, verts[i].y));
    ///            verts[i] = vert;
    ///        }
    ///        mesh.SetUVs(1, uvs);
    ///        ListPool<Vector2>.Release(uvs);
    ///    }
    ///}
    /// ]]>
    ///</code>
    ///</example>
    [ExecuteAlways]
    public abstract class BaseMeshEffect : UIBehaviour, IMeshModifier
    {
        [NonSerialized]
        Graphic m_Graphic;

        /// <summary>
        /// The graphic component that the Mesh Effect will aplly to.
        /// </summary>
        protected Graphic graphic => m_Graphic ??= GetComponent<Graphic>();

        protected virtual void OnEnable()
        {
            graphic.SetVerticesDirty();
        }

        protected virtual void OnDisable()
        {
            graphic.SetVerticesDirty();
        }

        /// <summary>
        /// Called from the native side any time a animation property is changed.
        /// </summary>
        protected virtual void OnDidApplyAnimationProperties()
        {
            graphic.SetVerticesDirty();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            graphic?.SetVerticesDirty();
        }
#endif

        /// <summary>
        /// Function that is called when the Graphic is populating the mesh.
        /// </summary>
        public abstract void ModifyMesh(MeshBuilder mb);
    }
}