// ReSharper disable InconsistentNaming

#nullable enable
using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    public class Clippable : MonoBehaviour, IClippable
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField, Required, ChildGameObjectsOnly, HideIf("@m_Graphic")]
        private Graphic m_Graphic = null!;

        private void OnEnable()
        {
#if UNITY_EDITOR
            EnabledMemory.Mark(this);
#endif
            ClipperRegistry.RegisterClippable(this);
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!EnabledMemory.Erase(this)) return;
#endif
            ClipperRegistry.UnregisterClippable(this);
        }

        private void OnTransformParentChanged()
        {
            if (isActiveAndEnabled)
                ClipperRegistry.ReparentClippable(this);
        }

        private void OnCanvasHierarchyChanged() => OnTransformParentChanged();

        Graphic IClippable.Graphic => m_Graphic;

#if UNITY_EDITOR
        [ShowInInspector, MultiLineProperty, HideLabel]
        private string _infoMessage
        {
            get
            {
                var sb = SbPool.Rent();

                var cr = m_Graphic.canvasRenderer;
                sb.AppendLine($"hasRectClipping: {cr.hasRectClipping}");
                var clipper = ClipperRegistry.GetCachedClipper(this);
                sb.AppendLine($"clipper: {clipper.SafeName()}");

                return SbPool.Return(sb);
            }
        }

        private void Reset() => m_Graphic = GetComponent<Graphic>();
        private void OnValidate() => m_Graphic ??= GetComponent<Graphic>(); // ensure m_Graphic is set in the editor

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (this.CountComponents<IClippable>() > 1)
                result.AddError("Multiple IClippable components found on the same GameObject. Only one is allowed.");
        }
#endif
    }
}