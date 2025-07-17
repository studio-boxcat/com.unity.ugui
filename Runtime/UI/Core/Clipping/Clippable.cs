// ReSharper disable InconsistentNaming

#nullable enable
using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    internal class Clippable : MonoBehaviour, IClippable
    {
        [SerializeField, Required, ChildGameObjectsOnly, HideIf("@m_Graphic")]
        private Graphic m_Graphic = null!;

        private void OnEnable() => ClipperRegistry.RegisterTarget(this);
        private void OnDisable() => ClipperRegistry.UnregisterTarget(this);

        private void OnTransformParentChanged()
        {
            if (isActiveAndEnabled)
                ClipperRegistry.ReparentTarget(this);
        }

        private void OnCanvasHierarchyChanged() => OnTransformParentChanged();

        Graphic IClippable.Graphic => m_Graphic;

#if UNITY_EDITOR
        private void Reset() => m_Graphic = GetComponent<Graphic>();
        private void OnValidate() => m_Graphic ??= GetComponent<Graphic>(); // ensure m_Graphic is set in the editor
#endif
    }
}