namespace UnityEngine
{
    public class UIBehaviour : MonoBehaviour
    {
        public bool IsActive() => isActiveAndEnabled;
        public bool IsDestroyed() => this == null;
    }
}