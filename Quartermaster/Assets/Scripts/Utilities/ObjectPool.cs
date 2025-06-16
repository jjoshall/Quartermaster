using System;
using System.Collections.Generic;

namespace Quartermaster.Utilities
{
    public interface IPool<T>
    {
        T Get();
        void Return(T obj);
    }

    public class ObjectPool<T> : IPool<T> where T : class
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly Func<T> _factory;
        private readonly Action<T> _reset;

        public ObjectPool(Func<T> factory, int initialSize = 0, Action<T> reset = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset   = reset;
            for (int i = 0; i < initialSize; i++)
                _queue.Enqueue(_factory());
        }

        public T Get()
        {
            return _queue.Count > 0 ? _queue.Dequeue() : _factory();
        }

        public void Return(T obj)
        {
            _reset?.Invoke(obj);
            _queue.Enqueue(obj);
        }
    }
}
