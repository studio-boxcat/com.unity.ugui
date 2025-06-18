using System.Collections.Generic;
using UnityEngine.UI;

namespace UnityEngine
{
    public static class GraphicManipulator
    {
        public static readonly List<Graphic> Instance = new(16);

        public static List<Graphic> GetGraphicsInChildrenShared(this GameObject target, bool includeInactive = false)
        {
            // any existing values in the list are overritten.
            // https://docs.unity3d.com/ScriptReference/Component.GetComponentsInChildren.html
            target.GetComponentsInChildren(includeInactive, Instance);
            return Instance;
        }

        public static List<Graphic> GetGraphicsInChildrenShared(this Component target, bool includeInactive = false)
        {
            // any existing values in the list are overritten.
            // https://docs.unity3d.com/ScriptReference/Component.GetComponentsInChildren.html
            target.GetComponentsInChildren(includeInactive, Instance);
            return Instance;
        }

        public static void SetGraphicPropertyRecursive(this GameObject target, Material material, bool includeInactive = false)
        {
            var graphics = target.GetGraphicsInChildrenShared(includeInactive);
            foreach (var g in graphics) g.material = material;
        }

        public static void SetGraphicPropertyRecursive(this GameObject target, Material material, Color color, bool includeInactive = false)
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