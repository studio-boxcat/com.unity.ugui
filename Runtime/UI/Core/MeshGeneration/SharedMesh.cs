using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    // Single-ownership mesh.
    public static class SharedMesh
    {
        private static Mesh _shared;
        private static Mesh _empty;
        private static bool _claimed;

        private const string _debugName = "MwgKvNSc";

        // Mesh must be cleared on the calling site.
        public static Mesh Claim()
        {
            if (_claimed) // rare-case
            {
                L.E("[SharedMesh] Already claimed.");
                _shared = null;
            }

            if (_shared is null)
            {
                _shared = CreateDynamicMesh(_debugName);
                _shared.MarkDynamic(); // Optimize for frequent updates.
            }

            return _shared;
        }

        public static void Release(Mesh mesh)
        {
            if (mesh.RefEq(_shared))
            {
                _claimed = false;
            }
            else // rare-case
            {
                L.W("[SharedMesh] Releasing non-shared mesh.");
#if DEBUG
                Assert.IsTrue(mesh.name == _debugName, "Mesh must be shared.");
#endif
                Object.Destroy(mesh);
            }
        }

        public static Mesh Empty => _empty ??= CreateDynamicMesh("Empty");

        public static Mesh CreateDynamicMesh(string debugName)
        {
            var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave }; // XXX: To prevent destroying the mesh after exiting play mode.
            mesh.SetNameDebug(debugName);
            return mesh;
        }
    }
}