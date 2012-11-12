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
    class HashSet<T> : ICollection<T>
    {
        private static readonly Object dummy = new Object();
        private SortedDictionary<T, Object> _inner = new SortedDictionary<T, Object>();

        public void Add(T item)
        {
            _inner.Add(item, dummy);
        }

        public void Clear()
        {
            _inner.Clear();
        }

        public Boolean Contains(T item)
        {
            return _inner.ContainsKey(item);
        }

        public void CopyTo(T[] array, Int32 arrayIndex)
        {
            _inner.Keys.CopyTo(array, arrayIndex);
        }

        public Int32 Count
        {
            get { return _inner.Count; }
        }

        public Boolean IsReadOnly
        {
            get { return false; }
        }

        public Boolean Remove(T item)
        {
            return _inner.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _inner.Keys.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
