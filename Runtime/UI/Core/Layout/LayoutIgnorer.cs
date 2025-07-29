#nullable enable

namespace UnityEngine.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class LayoutIgnorer : MonoBehaviour
    {
        private void OnEnable() => LayoutRebuilder.SetDirty(this);
        private void OnDisable() => LayoutRebuilder.SetDirty(this);
    }
}