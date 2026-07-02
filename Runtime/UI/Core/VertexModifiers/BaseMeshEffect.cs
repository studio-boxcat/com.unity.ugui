#nullable enable
using System;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    public abstract class BaseMeshEffect : UIBehaviour, IMeshModifier
    {
        [NonSerialized] Graphic? m_Graphic;
        protected Graphic graphic => m_Graphic ??= GetComponent<Graphic>();

        protected virtual void OnEnable() => graphic.SetVerticesDirty();
        protected virtual void OnDisable() => graphic.SetVerticesDirty();
        protected virtual void OnDidApplyAnimationProperties() => graphic.SetVerticesDirty();

        public abstract void ModifyMesh(MeshBuilder mb);
    }
}
