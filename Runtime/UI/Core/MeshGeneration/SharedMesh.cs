#nullable enable

using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    // Single-ownership mesh.
    public static class SharedMesh
    {
        private static Mesh?[] _shared = new Mesh?[2];
        private static Mesh? _empty;
        private static uint _usage; // 0b01 for mesh 1, 0b10 for mesh 2, 0b11 for both meshes.

        public static Mesh Claim(out uint token)
        {
            // both mesh will be used when font rebuilt while OnPopulateMesh() is running on Text or LText.
            // Text.OnPopulateMesh()
            // -> TextGenerator.TextPopulateWithErrors()
            // -> Font.textureRebuilt
            // -> FontUpdateTracker.FontTextureChanged()
            // -> Graphic.UpdateGeometry() (other graphics, self graphic will be skipped by m_DisableFontTextureRebuiltCallback or _isPopulatingMesh)

            // both mesh is in use, so we create a temporary mesh.
            if (_usage is 0b11)
            {
                L.W("[SharedMesh] Create temp mesh");
                token = 0;
                return CreateDynamicMesh(debugName: "Temp");
            }

            // one of the meshes must be available.
            var index = _usage is 0b01 ? 1 : 0; // if usage is 0b01, switch to mesh 2, else switch to mesh 1.
            ref var mesh = ref _shared[index];
            mesh ??= CreateDynamicMesh(); // create the mesh if it is not already created.

            // mark the mesh as in use.
            token = 1u << index; // set the mask for the mesh.
            _usage |= token; // set the usage bit for the mesh.

            return mesh;
        }

        public static void Release(Mesh mesh, uint token)
        {
            Assert.IsTrue(token is 0b01 or 0b10, "Invalid token for SharedMesh.Release. Must be 0b01 or 0b10.");

            if (token is not 0)
            {
                _usage &= ~token; // clear the usage bit for the mesh.
                mesh.Clear(); // clear the mesh to reuse it.
            }
            else // rare-case
            {
                L.W("[SharedMesh] Releasing temporary mesh.");
                Object.Destroy(mesh);
            }
        }

        public static Mesh Empty => _empty ??= CreateDynamicMesh("Empty");

        public static Mesh CreateDynamicMesh(string debugName = "")
        {
            var mesh = new Mesh();
            mesh.EditorHideAndDontSaveFlag(); // XXX: To prevent destroying the mesh after exiting play mode.
            mesh.SetNameDebug(debugName);
            return mesh;
        }
    }
}