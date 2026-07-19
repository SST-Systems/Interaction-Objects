using System;
using System.Collections.Generic;
using UnityEngine;

namespace SST.InteractionObjects
{
    /// <summary>
    /// Casts a ray each tick and reports when a component of type <typeparamref name="T"/>
    /// enters or leaves the crosshair via <see cref="OnSelected"/> / <see cref="OnDeselected"/>.
    /// </summary>
    /// <remarks>
    /// Resolved colliders are kept in a small fixed-size cache so that repeatedly
    /// looking at the same object does not call <see cref="Component.TryGetComponent{T}"/>
    /// on every tick.
    /// </remarks>
    /// <typeparam name="T">Component type that marks an object as selectable.</typeparam>
    public class ObjectRaycaster<T> where T : MonoBehaviour
    {
        private const int CacheSize = 10;

        private sealed class CacheEntry
        {
            public int Key { get; private set; }
            public T Value { get; private set; }

            public void Set(int key, T value)
            {
                Key = key;
                Value = value;
            }
        }

        public event Action<T> OnSelected = delegate { };
        public event Action<T> OnDeselected = delegate { };

        private readonly List<CacheEntry> _cache = new(CacheSize);
        private int _writeIndex;

        private T _selected;

        public ObjectRaycaster()
        {
            for (int i = 0; i < CacheSize; i++)
                _cache.Add(new CacheEntry());
        }

        public void CheckObjectInRay(Vector3 origin, Vector3 direction, float maxDistance)
        {
            if (Physics.Raycast(origin, direction, out var hit, maxDistance))
            {
                var hitObject = ResolveObject(hit.collider);

                if (hitObject != null)
                {
                    if (_selected != null && _selected != hitObject)
                        OnDeselected.Invoke(_selected);

                    _selected = hitObject;
                    AdvanceWriteIndex();

                    OnSelected.Invoke(_selected);
                    return;
                }
            }

            if (_selected != null)
                OnDeselected.Invoke(_selected);

            _selected = null;
        }

        private T ResolveObject(Collider collider)
        {
            var key = collider.gameObject.GetHashCode();

            foreach (var entry in _cache)
            {
                if (entry.Key == key)
                    return entry.Value;
            }

            if (collider.TryGetComponent(out T component))
            {
                _cache[_writeIndex].Set(key, component);
                return component;
            }

            return null;
        }

        private void AdvanceWriteIndex()
        {
            _writeIndex = _writeIndex < _cache.Count - 1 ? _writeIndex + 1 : 0;
        }
    }
}