/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
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
    class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
    {
        private static readonly IEqualityComparer<T[]> defaultInstance = new ArrayEqualityComparer<T>();
        private readonly IEqualityComparer<T> _elementComparer;

        public static IEqualityComparer<T[]> Default
        {
            get { return defaultInstance; }
        }

        public ArrayEqualityComparer()
            : this(EqualityComparer<T>.Default)
        { }

        public ArrayEqualityComparer(IEqualityComparer<T> elementComparer)
        {
            _elementComparer = elementComparer;
        }

        public Boolean Equals(T[] x, T[] y)
        {
            if (Object.ReferenceEquals(x, y))
                return true;

            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            if (x.Length != y.Length)
                return false;

            for (Int32 i = 0; i < x.Length; i++)
            {
                if (!_elementComparer.Equals(x[i], y[i]))
                    return false;
            }

            return true;
        }

        public Int32 GetHashCode(T[] array)
        {
            if (array == null)
                return 0;
            
            Int32 hash = 17;
            foreach (T item in array)
            {
                hash = hash * 23 + _elementComparer.GetHashCode(item);
            }
            return hash;
        }
    }
}
