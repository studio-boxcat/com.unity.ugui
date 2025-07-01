namespace UnityEngine.UI
{
    /// <summary>
    /// Interface which allows for the modification of vertices in a Graphic before they are passed to the CanvasRenderer.
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
        public static void GetComponentsAndModifyMesh(Graphic c, MeshBuilder mb)
        {
            using var _ = CompBuf.GetComponents(c, typeof(IMeshModifier), out var meshModifiers);

            foreach (IMeshModifier meshModifier in meshModifiers)
            {
                // check isActiveAndEnabled, since this method is called from Graphic.OnPopulateMesh, which is only called if the Graphic is active and enabled.
                if (meshModifier is Behaviour { enabled: false }) continue;
                meshModifier.ModifyMesh(mb);
            }
        }
    }
}