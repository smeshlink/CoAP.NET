using System;
using System.Collections.Generic;

namespace CoAP.Util
{
    class HashMap<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private IDictionary<TKey, TValue> _inner = new Dictionary<TKey, TValue>();

        public void Add(TKey key, TValue value)
        {
            _inner.Add(key, value);
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
            return _inner.ContainsKey(key) ? _inner.Remove(key) : false;
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
                    _inner.Add(key, value);
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _inner.Add(item);
        }

        public void Clear()
        {
            _inner.Clear();
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
            return _inner.Remove(item);
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
