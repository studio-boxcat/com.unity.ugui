namespace UnityEngine.UI
{
    public class LayoutIgnorer : MonoBehaviour, ILayoutIgnorer
    {
        public bool ignoreLayout => true;
    }
}