using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    /// <summary>
    /// Interface which allows for the modification of verticies in a Graphic before they are passed to the CanvasRenderer.
    /// When a Graphic generates a list of vertices they are passed (in order) to any components on the GameObject that implement IMeshModifier. This component can modify the given Mesh.
    /// </summary>
    public interface IMeshModifier
    {
        /// <summary>
        /// Call used to modify mesh.
        /// Place any custom mesh processing in this function.
        /// </summary>
        void ModifyMesh(MeshBuilder mb);
    }

    public static class MeshModifierUtils
    {
        public static void GetComponentsAndModifyMesh(Component c, MeshBuilder mb)
        {
            using var _ = CompBuf.GetComponents(c, typeof(IMeshModifier), out var meshModifiers);

            foreach (IMeshModifier meshModifier in meshModifiers)
            {
                if (meshModifier is Behaviour {isActiveAndEnabled: false})
                    continue;
                meshModifier.ModifyMesh(mb);
            }
        }
    }
}