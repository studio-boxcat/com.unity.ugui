using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    public interface IMaterialModifier
    {
        Material? GetModifiedMaterial(GraphicMaterialKey key);
    }

    [ExecuteAlways]
    public abstract class MaterialModifierBase : MonoBehaviour, IMaterialModifier
    {
        [SerializeField, Required, ChildGameObjectsOnly]
        [HideIf("_graphic_HideIf")]
        private Graphic _graphic;
        public Graphic Graphic => _graphic;

        protected virtual void Awake() => _graphic ??= GetComponent<Graphic>();
        protected virtual void OnEnable() => SetMaterialDirty();
        protected virtual void OnDisable() => SetMaterialDirty();
        public void SetMaterialDirty() => _graphic.SetMaterialDirty();
        public abstract Material? GetModifiedMaterial(GraphicMaterialKey key);

#if UNITY_EDITOR
        protected virtual void Reset() => _graphic = GetComponent<Graphic>();
        private bool _graphic_HideIf() => _graphic && _graphic.gameObject.RefEq(gameObject);
#endif
    }
}
