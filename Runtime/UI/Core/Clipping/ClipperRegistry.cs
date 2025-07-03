#nullable enable
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    /// <summary>
    /// Registry class to keep track of all IClippers that exist in the scene
    /// </summary>
    /// <remarks>
    /// This is used during the CanvasUpdate loop to cull clippable elements. The clipping is called after layout, but before Graphic update.
    /// </remarks>
    public static class ClipperRegistry
    {
        private static readonly Dictionary<Clipper, List<IClippable>?> _clippers = new(RefComparer.Instance);
        private static readonly Dictionary<IClippable, Clipper?> _targets = new(RefComparer.Instance);
        private static readonly List<Clipper> _dirtyClippers = new(); // can contain duplicates.
        private static readonly List<IClippable> _dirtyTargets = new(); // can contain duplicates.
        private static readonly List<IClippable> _tempTargets = new(); // used to collect targets from clippers.

        public static void RegisterClipper(Clipper c)
        {
            _clippers.Add(c, null);
            _dirtyClippers.Add(c);
        }

        public static void UnregisterClipper(Clipper c)
        {
            var result = _clippers.Remove(c, out var targets);
            Assert.IsTrue(result, "Clipper not registered in ClipperRegistry. Clipper should be registered before it is unregistered.");
            if (targets is null) return; // not resolved yet. (no need to restore cull state or whatever)

            _dirtyTargets.AddRange(targets); // add only the targets that were registered to this clipper.

            // _clippers and _targets must be in sync
            foreach (var t in targets)
            {
                RestoreCullState(t.Graphic);
                _targets[t] = null; // remove the target from the registry.
            }
        }

        public static void RegisterTarget(IClippable c)
        {
            _targets.Add(c, null);
            _dirtyTargets.Add(c);
        }

        public static void UnregisterTarget(IClippable c)
        {
            var result = _targets.Remove(c, out var clipper);
            Assert.IsTrue(result, "Graphic not registered in ClipperRegistry. Graphic should be registered before it is unregistered.");
            // don't removed from _dirtyTargets, we will check if this item is in the _targets anyway.

            if (clipper is not null)
            {
                result = _clippers[clipper]!.Remove(c); // if the clipper is registered on _targets, it must be registered in _clippers as well.
                Assert.IsTrue(result, "Graphic not registered in Clipper. Graphic should be registered before it is unregistered from Clipper.");
            }
        }

        public static void ReparentTarget(IClippable c)
        {
            Assert.IsTrue(_targets.ContainsKey(c), "Clippable must be registered in ClipperRegistry before it can be reparented.");
            _dirtyTargets.Add(c);
        }

        /// <summary>
        /// Perform the clipping on all registered IClipper
        /// </summary>
        public static void Cull()
        {
            // GetHashCode() is same as GetInstanceID(), but it doesn't check if it's main thread. (may be only for UnityEditor?)

            // collect all dirty graphics.
            _dirtyClippers.Sort(static (a, b) =>
                a.GetHashCode().CompareTo(b.GetHashCode()));
            var prevHash = 0; // Unity ensures that GetHashCode() is unique for each object and not 0.
            foreach (var clipper in _dirtyClippers)
            {
                var h = clipper.GetHashCode();
                if (h == prevHash) continue; // skip duplicates.
                prevHash = h;

                if (!clipper) continue; // skip destroyed clippers.
                clipper.GetComponentsInChildren(includeInactive: false, _tempTargets);
                _dirtyTargets.AddRange(_tempTargets);
            }
            _dirtyClippers.Clear();

            // update targets for all clippers.
            _dirtyTargets.Sort(static (a, b) =>
                a.Graphic.GetHashCode().CompareTo(b.Graphic.GetHashCode()));
            prevHash = 0;
            foreach (var t in _dirtyTargets)
            {
                var g = t.Graphic;
                var h = g.GetHashCode();
                if (h == prevHash) continue; // skip duplicates.
                prevHash = h;

                if (_targets.TryGetValue(t, out var orgClipper) is false) continue; // skip unregistered target.

                var newClipper = ActiveClipperFor(g);
                if (ReferenceEquals(newClipper, orgClipper)) continue; // no change in clipper.

                if (orgClipper is not null)
                {
                    // remove the clippable from the old clipper.
                    var removed = _clippers[orgClipper]!.Remove(t);
                    Assert.IsTrue(removed, "Clippable not registered in Clipper. Clippable should be registered before it is unregistered from Clipper.");
                }

                // update the clipper for the clippable.
                _targets[t] = newClipper;

                if (newClipper is not null)
                {
                    newClipper.MarkNeedClip();
                    var list = (_clippers[newClipper] ??= new List<IClippable>()); // could be null if the clipper is not resolved yet.
                    list.Add(t); // add the clippable to the new clipper.
                }
                else
                {
                    RestoreCullState(g);
                }
            }
            _dirtyTargets.Clear();

            // Perform clipping.
            foreach (var (clipper, targets) in _clippers)
            {
                Assert.IsTrue(clipper, "Clipper should not be destroyed.");
                if (targets is null) continue; // no target graphics at all.
                clipper.PerformClipping(targets);
            }
        }

        private static Clipper? ActiveClipperFor(Graphic g)
        {
            // Don't use "isActiveAndEnabled" here, as it will return false when it called from OnEnable.
            // activeInHierarchy = true means all the parent GameObjects are active, only need to check component enabled state.
            Assert.IsTrue(g is { enabled: true, gameObject: { activeInHierarchy: true } },
                "Clippable must be active and enabled to get its active Clipper.");

            var t = g.transform;

            // Handle most common cases.
            {
                // No mask at all.
                var clipper = t.GetComponentInParent<Clipper>(true); // Do not skip inactive.
                if (clipper is null) return null;

                // There is a enabled mask, and it's located on the same canvas with the graphic.
                if (clipper.enabled && ReferenceEquals(clipper.GetCanvas(), g.canvas))
                    return clipper; // only enabled clippers are returned.
            }

            // Going up the hierarchy to find first mask.
            do
            {
                if (t.TryGetComponent(out Clipper clipper) && clipper.enabled) return clipper; // only enabled clippers are returned.
                if (CanvasUtils.IsRenderRoot(t)) return null; // stop climbing if we reach a render root.
                t = t.parent;
            } while (t is not null);

            return null;
        }

        private static void RestoreCullState(Graphic target)
        {
            target.SetClipRect(new Rect(), validRect: false);
            target.UpdateCull(cull: false);
        }
    }
}