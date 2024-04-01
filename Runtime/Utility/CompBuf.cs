using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace UnityEngine
{
    public readonly struct CompBufScope : IDisposable
    {
        readonly List<Component> _buf;

        public CompBufScope(List<Component> buf)
        {
            _buf = buf;
        }

        public void Dispose()
        {
            CompBuf.Release(_buf);
        }
    }

    public static class CompBuf
    {
        static readonly List<List<Component>> _buffers = new();
        static int _available;

        static CompBufScope Rent(out List<Component> buffer)
        {
            if (_available == 0)
            {
                buffer = new List<Component>();
                return new CompBufScope(buffer);
            }

            var index = --_available;
            buffer = _buffers[index];
            _buffers.RemoveAt(index);
            return new CompBufScope(buffer);
        }

        public static void Release(List<Component> buffer)
        {
            _available++;
            _buffers.Add(buffer);
        }

        [MustUseReturnValue]
        public static CompBufScope GetComponents(GameObject target, Type type, out List<Component> components)
        {
            var scope = Rent(out components);
            target.GetComponents(type, components);
            return scope;
        }

        [MustUseReturnValue]
        public static CompBufScope GetComponents(Component target, Type type, out List<Component> components)
        {
            var scope = Rent(out components);
            target.GetComponents(type, components);
            return scope;
        }

        [MustUseReturnValue]
        public static CompBufScope GetEnabledComponents(GameObject target, Type type, out List<Component> components)
        {
            var scope = GetComponents(target, type, out components);
            RemoveDisabledComponents(components);
            return scope;
        }

        [MustUseReturnValue]
        public static CompBufScope GetEnabledComponents(Component target, Type type, out List<Component> components)
        {
            var scope = GetComponents(target, type, out components);
            RemoveDisabledComponents(components);
            return scope;
        }

        static void RemoveDisabledComponents(List<Component> components)
        {
            for (var i = components.Count - 1; i >= 0; i--)
            {
                var component = components[i];
                if (component is Behaviour {isActiveAndEnabled: false})
                    components.RemoveAt(i);
            }
        }
    }
}