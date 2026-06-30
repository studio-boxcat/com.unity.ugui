#nullable enable

using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    internal sealed class UIImageSpritePreview : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField, Required]
        private Sprite _sprite;

        private void OnValidate()
        {
            if (Editing.Yes(this) && TryGetComponent<UIImageBase>(out var img))
                img.PreviewSprite(_sprite);
        }
#endif
    }
}
