using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    // Single-ownership mesh.
    public static class SharedMesh
    {
        static Mesh _shared;
        static Mesh _empty;
        static bool _claimed;

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
                _shared = Create("Shared");
                _shared.MarkDynamic(); // Optimize for frequent updates.
            }

            return _shared;
        }

        public static void Release(Mesh mesh)
        {
            if (ReferenceEquals(mesh, _shared))
            {
                _claimed = false;
            }
            else // rare-case
            {
                L.W("[SharedMesh] Releasing non-shared mesh.");
                Assert.AreEqual("Shared", mesh.name, "Mesh must be shared.");
                Object.Destroy(mesh);
            }
        }

        public static Mesh Empty => _empty ??= Create("Empty");

        static Mesh Create(string name)
        {
            return new Mesh
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave // XXX: To prevent destroying the mesh after exiting play mode.
            };
        }
    }
}