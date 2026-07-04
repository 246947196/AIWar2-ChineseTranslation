using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{    
    public class Pooler
    {
        private readonly List<IPool> _pools;
 
        public Pooler()
        {
            _pools = new List<IPool>();
        }

        public void DefineType<T>(int initialCapacity, Pool<T>.Create createHandler, Pool<T>.Reset resetHandler)
        {
            var p = new Pool<T>(initialCapacity, createHandler, resetHandler);
            _pools.Add(p);
        }

        public T Get<T>()
        {
            var p = GetPool(typeof (T));
            if (p == null)
                throw new Exception(string.Format("Pooler.Get<{0}>() failed; there is no pool for that type.", typeof(T)));

            return ((Pool<T>)p).Get();
        }

        public void Return(object item)
        {
            var p = GetPool(item.GetType());
            if (p == null)
                throw new Exception(string.Format("Pooler.Get<{0}>() failed; there is no pool for that type.", item.GetType()));

            p.Return(item);            
        }

        private IPool GetPool(Type itemType)
        {
            foreach (var p in _pools)
            {
                if (p.ItemType == itemType)
                {
                    return p;
                }
            }

            return null;
        }
    }

    public interface IPool
    {
        Type ItemType { get; }
        void Return(object item);
    }

    /// <summary>
    /// A pool of items of the same type.
    /// 
    /// Items are taken and then later returned to the pool (generally for reference types) to avoid allocations and
    /// the resulting garbage generation.
    /// 
    /// Any pool must have a way to 'reset' returned items to a canonical state.
    /// This class delegates that work to the allocator (literally, with a delegate) who probably knows more about the type being pooled.
    /// </summary>    
    public class Pool<T> : IPool
    {
        public delegate T Create();
        public readonly Create HandleCreate;

        public delegate void Reset(ref T item);
        public readonly Reset HandleReset;

        private readonly List<T> _in;

#if !SHIPPING
        private readonly List<T> _out;
#endif

        public Type ItemType
        {
            get
            {
                return typeof (T);   
            }            
        }

        public Pool(int initialCapacity, Create createMethod, Reset resetMethod)
        {
            HandleCreate = createMethod;
            HandleReset = resetMethod;

            _in = new List<T>(initialCapacity);            
            for (var i = 0; i < initialCapacity; i++)
            {
                _in.Add(HandleCreate());
            }
#if !SHIPPING
            _out = new List<T>();            
#endif
        }

        public T Get()
        {
            if (_in.Count == 0)
            {
                _in.Add(HandleCreate());
            }

            var item = _in.PopLast();
#if !SHIPPING
            _out.Add(item);
#endif
            return item;
        }

        public void Return( T item )
        {
            HandleReset(ref item);
#if !SHIPPING
            Debug.Assert(!_in.Contains(item), "Returning an Item we already have.");
            Debug.Assert(_out.Contains(item), "Returning an Item we never gave out.");
            _out.Remove(item);
#endif
            _in.Add(item);
        }

        public void Return( object item )
        {
            Return((T) item);
        }

#if !SHIPPING
        public void Validate()
        {
            Debug.Assert(_out.Count == 0, "An Item was not returned.");
        }
#endif
    }
}
