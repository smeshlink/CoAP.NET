/*
 * Copyright (c) 2011-2012, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;

namespace CoAP.Util
{
    class HashMap<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private Int32 _size = 0;
        private IDictionary<TKey, TValue> _inner = new Dictionary<TKey, TValue>();
        private LinkedList<TKey> _keyQueue;

        public HashMap()
        { }

        public HashMap(Int32 size)
        {
            _size = size;
            if (_size > 0)
            {
                _keyQueue = new LinkedList<TKey>();
            }
        }

        public void Add(TKey key, TValue value)
        {
            _inner.Add(key, value);

            if (_keyQueue != null)
            {
                if (_keyQueue.Count == _size)
                {
                    _inner.Remove(_keyQueue.First.Value);
                    _keyQueue.RemoveFirst();
                }
                _keyQueue.AddLast(key);
            }
        }

        public Boolean ContainsKey(TKey key)
        {
            return _inner.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return _inner.Keys; ; }
        }

        public Boolean Remove(TKey key)
        {
            if (_inner.Remove(key))
            {
                if (_keyQueue != null)
                    _keyQueue.Remove(key);
                return true;
            }
            else
                return false;
        }

        public Boolean TryGetValue(TKey key, out TValue value)
        {
            return _inner.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return _inner.Values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                return _inner.ContainsKey(key) ? _inner[key] : default(TValue);
            }
            set
            {
                if (_inner.ContainsKey(key))
                    _inner[key] = value;
                else
                    Add(key, value);
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _inner.Add(item);

            if (_keyQueue != null)
            {
                if (_keyQueue.Count == _size)
                {
                    _inner.Remove(_keyQueue.First.Value);
                    _keyQueue.RemoveFirst();
                }
                _keyQueue.AddLast(item.Key);
            }
        }

        public void Clear()
        {
            _inner.Clear();
            if (_keyQueue != null)
                _keyQueue.Clear();
        }

        public Boolean Contains(KeyValuePair<TKey, TValue> item)
        {
            return _inner.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, Int32 arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        public Int32 Count
        {
            get { return _inner.Count; }
        }

        public Boolean IsReadOnly
        {
            get { return _inner.IsReadOnly; }
        }

        public Boolean Remove(KeyValuePair<TKey, TValue> item)
        {
            if (_inner.Remove(item))
            {
                if (_keyQueue != null)
                    _keyQueue.Remove(item.Key);
                return true;
            }
            else
                return false;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }
    }
}
