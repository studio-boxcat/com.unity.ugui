#nullable enable

using System.Collections.Generic;

namespace UnityEngine.UI
{
    public static class GraphicManipulator
    {
        private static readonly List<Graphic> _shared = new(16);

        public static List<Graphic> GetGraphicsInChildrenShared(this GameObject target, bool includeInactive = false)
        {
            // any existing values in the list are overwritten.
            // https://docs.unity3d.com/ScriptReference/Component.GetComponentsInChildren.html
            target.GetComponentsInChildren(includeInactive, _shared);
            return _shared;
        }

        public static List<Graphic> GetGraphicsInChildrenShared(this Component target, bool includeInactive = false)
        {
            // any existing values in the list are overwritten.
            // https://docs.unity3d.com/ScriptReference/Component.GetComponentsInChildren.html
            target.GetComponentsInChildren(includeInactive, _shared);
            return _shared;
        }

        public static void SetGraphicPropertyRecursive(this GameObject target, GraphicMaterialKind material, bool includeInactive = false)
        {
            var graphics = target.GetGraphicsInChildrenShared(includeInactive);
            foreach (var g in graphics) g.material = material;
        }

        public static void SetGraphicPropertyRecursive(this GameObject target, GraphicMaterialKind material, Color color, bool includeInactive = false)
        {
            var graphics = target.GetGraphicsInChildrenShared(includeInactive);
            foreach (var g in graphics)
            {
                g.material = material;
                g.color = color;
            }
        }
    }
}
