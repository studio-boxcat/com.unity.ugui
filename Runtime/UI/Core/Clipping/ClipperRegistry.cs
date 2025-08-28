// #define VERBOSE
#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    /// <summary>
    /// Registry class to keep track of all IClippers that exist in the scene
    /// </summary>
    /// <remarks>
    /// This is used during the CanvasUpdate loop to cull clippable elements. The clipping is called after layout, but before Graphic update.
    /// </remarks>
    internal static class ClipperRegistry
    {
        private static readonly Dictionary<Clipper, List<Clippable>?> _clippers = new(RefComparer.Instance); // null means that the clipper is not resolved or has no clippables registered.
        private static readonly Dictionary<Clippable, Clipper?> _clippables = new(RefComparer.Instance); // null means that the clipper is not resolved or has no clipper on parents.
        private static readonly List<Clipper> _dirtyClippers = new(); // can contain duplicates, remove only occurs on Cull() call.
        private static readonly List<Clippable> _dirtyClippables = new(); // can contain duplicates, remove only occurs on Cull() call.
        private static readonly List<Clippable> _tempClippables = new(); // used to collect targets from clippers.

        public static void RegisterClipper(Clipper c)
        {
            V($"Registering clipper: {c}");
            _clippers.Add(c, null); // will be resolved on the next Cull() call. let it throw if the clipper is already registered.
            _dirtyClippers.Add(c);
        }

        public static void UnregisterClipper(Clipper c)
        {
            V($"Unregistering clipper: {c}");
            var result = _clippers.Remove(c, out var clippables);
            Assert.IsTrue(result, "Clipper not registered in ClipperRegistry. Clipper should be registered before it is unregistered.");
            if (clippables is null) return; // not resolved yet. (no need to restore cull state or whatever)
            V($"Unregistered clipper: {c}, restoring cull state for {clippables.Count} clippables: {string.Join(", ", clippables)}");
            _dirtyClippables.AddRange(clippables); // add only the clippables that were registered to this clipper.

            // set clippables to null, so they will be resolved on the next Cull() call.
            foreach (var cl in clippables)
            {
                Assert.IsTrue(_clippables.ContainsKey(cl),
                    "Clippable must be registered in ClipperRegistry before it can be unregistered.");
                _clippables[cl] = null;
            }
        }

        public static void RegisterClippable(Clippable c)
        {
            V($"Registering clippable: {c}");
            _clippables.Add(c, null); // will be resolved on the next Cull() call. let it throw if the clippable is already registered.
            _dirtyClippables.Add(c);
        }

        public static void UnregisterClippable(Clippable c)
        {
            V($"Unregistering clippable: {c}");
            var result = _clippables.Remove(c, out var clipper);
#if DEBUG
            if (result is false)
            {
                L.E("[ClipperRegistry] Clippable was not registered in ClipperRegistry: " + c);
                return;
            }
#endif

            // set dirty to restore cull state in Cull().
            _dirtyClippables.Add(c);

            // unregister from the Clipper immediately.
            if (clipper is not null // clippable has been resolved and has a clipper.
                && _clippers.TryGetValue(clipper, out var list) // registered
                && list is not null) // clipper has been resolved
            {
                var removed = list.Remove(c);
                Assert.IsTrue(removed, "Clippable was not registered in Clipper.");
            }
        }

        public static void ReparentClippable(Clippable c)
        {
            Assert.IsTrue(_clippables.ContainsKey(c), "Clippable must be registered in ClipperRegistry before it can be reparented.");
            _dirtyClippables.Add(c);
        }

        /// <summary>
        /// Perform the clipping on all registered IClipper
        /// </summary>
        public static void Cull()
        {
            // GetHashCode() is same as GetInstanceID(), but it doesn't check if it's main thread. (may be only for UnityEditor?)

            if (_dirtyClippers.NotEmpty())
            {
                // prune destroyed graphics
                _dirtyClippers.RemoveAll(static c => !c);

                // to ensure that we don't process the same clipper multiple times.
                _dirtyClippers.Sort(static (a, b) =>
                    a.GetHashCode().CompareTo(b.GetHashCode()));

                var prevHash = 0; // Unity ensures that GetHashCode() is unique for each object and not 0.
                foreach (var clipper in _dirtyClippers)
                {
                    var h = clipper.GetHashCode();
                    if (h == prevHash) continue; // skip duplicates.
                    prevHash = h;
                    // no need to add inactive children.
                    clipper.GetComponentsInChildren(includeInactive: false, _tempClippables); // _tempClippables will be cleared by GetComponentsInChildren().
                    _dirtyClippables.AddRange(_tempClippables);
                }
                _dirtyClippers.Clear();
            }

            if (_dirtyClippables.NotEmpty())
            {
                // prune destroyed graphics
                _dirtyClippables.RemoveAll(static c => !c.Graphic);

                // to ensure that we don't process the same clipper multiple times.
                _dirtyClippables.Sort(static (a, b) =>
                    a.Graphic.GetHashCode().CompareTo(b.Graphic.GetHashCode()));

                var prevHash = 0; // Unity ensures that GetHashCode() is unique for each object and not 0.
                foreach (var c in _dirtyClippables)
                {
                    var g = c.Graphic; // destroyed graphic is already pruned.
                    var h = g.GetHashCode();
                    if (h == prevHash) continue; // skip duplicates.
                    prevHash = h;

                    // if the graphic is no longer managed by ClipperRegistry, restore its cull state.
                    if (_clippables.TryGetValue(c, out var orgClipper) is false)
                    {
                        V($"Graphic {g} is not registered in ClipperRegistry, restoring cull state.");
                        RestoreCullState(g);
                        continue;
                    }

                    var newClipper = ActiveClipperFor(g);
                    if (newClipper.RefEq(orgClipper))
                    {
                        V($"Graphic {g} already has the same clipper {orgClipper}, no need to update.");
#if DEBUG
                        if (orgClipper is not null && (_clippers.TryGetValue(orgClipper, out var l) is false || l is null || l.ContainsRef(c) is false))
                            L.E($"[ClipperRegistry] Graphic {g} is already registered in ClipperRegistry, but not found in the clipper {orgClipper}. This is a bug.");
#endif
                        continue;
                    }

                    if (orgClipper is not null
                        && _clippers.TryGetValue(orgClipper, out var clippablesByOrgClipper) // could be not exist if the clipper is unregistered.
                        && clippablesByOrgClipper is not null) // could be null if the clipper is re-registered.
                    {
                        V($"Graphic {g} is reparented from clipper {orgClipper} to {newClipper}, removing from the old clipper.");
                        clippablesByOrgClipper.Remove(c);
                    }

                    // update the clipper for the clippable.
                    V($"Graphic {g} is reparented to clipper {newClipper}, updating clippable.");
                    _clippables[c] = newClipper;

                    if (newClipper is not null)
                    {
                        V($"Graphic {g} is now clipped by {newClipper}, updating cull state.");

                        newClipper.MarkNeedClip(); // will be used in PerformClipping().

                        // XXX: here, newClipper must be exists in the dictionary,
                        // as Cull() is called from Canvas.willRenderCanvases,
                        // which is later than OnEnable() of Clipper.
                        // also ActiveClipperFor() returns only enabled clippers,
                        // so we can safely assume that newClipper is not null here.
                        // but just in case, we check it again.
                        if (_clippers.TryGetValue(newClipper, out var list) is false || list is null) // could be null if this clippable is the first one for this clipper.
                            list = _clippers[newClipper] = new List<Clippable>();
                        list.Add(c); // add the clippable to the new clipper.
                    }
                    else
                    {
                        V($"Graphic {g} is not clipped by any clipper, restoring cull state.");
                        RestoreCullState(g); // no active clipper, restore cull state.
                    }
                }

                _dirtyClippables.Clear();
            }

            // Perform clipping.
            foreach (var (clipper, clippables) in _clippers)
            {
                Assert.IsTrue(clipper, "Clipper should not be destroyed.");
                if (clippables is null) continue; // no clippables at all.
                clipper.PerformClipping(clippables);
            }
        }

        private static Clipper? ActiveClipperFor(Graphic g)
        {
            // Don't use "isActiveAndEnabled" here, as it will return false when it called from OnEnable.
            // activeInHierarchy = true means all the parent GameObjects are active, only need to check component enabled state.
            Assert.IsTrue(g is { enabled: true, gameObject: { activeInHierarchy: true } },
                "Clippable must be active and enabled to get its active Clipper.");

            var t = g.transform;

            do
            {
                // get the nearest Clipper in the hierarchy.
                // Do not skip inactive, graphic is activeInHierarchy so Clipper (in parent) must be too.
                var clipper = t.GetComponentInParent<Clipper>(includeInactive: true);
                if (clipper is null) return null; // No mask at all.

                // if the nearest clipper is not on the same canvas as the graphic,
                // no need to check further.
                if (clipper.GetCanvas().RefNq(g.canvas)) return null;

                // if the clipper is enabled, return it.
                if (clipper.enabled)
                    return clipper; // only enabled clippers are returned.

                t = clipper.transform.parent; // climb up, null if reached the root.
            } while (t is not null);

            throw new System.InvalidOperationException();
        }

        internal static void RestoreCullState(Graphic g)
        {
            // L.I($"[ClipperRegistry] Restoring cull state for {g}");
            g.SetClipRect(new Rect(), validRect: false);
            g.UpdateCull(cull: false);
        }

        [Conditional("VERBOSE")]
        private static void V(string message) => L.I($"[ClipperRegistry] {message}");

#if UNITY_EDITOR
        public static Clipper? GetCachedClipper(Clippable c) => _clippables.GetValueOrDefault(c);
        public static List<Clippable>? GetCachedClippables(Clipper c) => _clippers.GetValueOrDefault(c);
#endif
    }
}