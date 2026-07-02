#nullable enable

using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    internal sealed class UITexturePreview : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField, Required]
        private Texture _texture;

        private void OnValidate()
        {
            if (Editing.Yes(this) && TryGetComponent<UITextureImageBase>(out var img))
                img.PreviewTexture(_texture);
        }
#endif
    }
}
