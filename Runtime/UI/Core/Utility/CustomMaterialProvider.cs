#nullable enable
using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    public interface ICustomMaterialProvider
    {
        Material? ProvideMaterial();
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class CustomMaterialProvider : MonoBehaviour, ICustomMaterialProvider
    {
        [SerializeField, Required]
        private Material _material = null!;

        public Material Material
        {
            get => _material;
            set
            {
                if (_material.RefEq(value)) return;
                _material = value;
                GetComponent<Graphic>().SetMaterialDirty();
            }
        }

        public Material ProvideMaterial() => _material;

        public static void Attach(Graphic graphic, Material material)
        {
            graphic.material = GraphicMaterialKind.Custom;
            graphic.gameObject.CheckAndAddComponent<CustomMaterialProvider>().Material = material;
        }
    }
}
