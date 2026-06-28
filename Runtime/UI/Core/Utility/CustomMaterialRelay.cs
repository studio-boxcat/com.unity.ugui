#nullable enable

using Sirenix.OdinInspector;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    internal sealed class CustomMaterialRelay : MonoBehaviour, ICustomMaterialProvider
    {
        [SerializeField, Required, InstanceOf(typeof(ICustomMaterialProvider))]
        private MonoBehaviour _materialProvider;

        Material? ICustomMaterialProvider.ProvideMaterial()
        {
            Assert.IsTrue(this.RefNq(_materialProvider));
            return ((ICustomMaterialProvider)_materialProvider).ProvideMaterial();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            // no self-component
            _materialProvider = (MonoBehaviour)transform.parent.GetComponentInParent(typeof(ICustomMaterialProvider));
        }
#endif
    }
}
