/*
 * Copyright (c) 2011-2013, Longxiang He <helongxiang@smeshlink.com>,
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
    class Sort
    {
        public static void InsertionSort<T>(List<T> list, Comparison<T> comparison)
        {
            for (Int32 i = 1; i < list.Count; i++)
            {
                Int32 j;
                T temp = list[i];
                for (j = i; j > 0; j--)
                {
                    if (comparison(list[j - 1], temp) > 0)
                    {
                        list[j] = list[j - 1];
                    }
                    else
                    {
                        break;
                    }
                }
                if (i != j)
                    list[j] = temp;
            }
        }

        public static Boolean IsSequenceEqualTo<T>(IEnumerable<T> obj, IEnumerable<T> other)
        {
            if (obj == null && other == null)
                return true;
            else if (obj != null && other != null)
            {
                IEnumerator<T> it1 = obj.GetEnumerator();
                IEnumerator<T> it2 = other.GetEnumerator();
                while (it1.MoveNext() && it2.MoveNext())
                {
                    if (!Object.Equals(it1.Current, it2.Current))
                        return false;
                }
                if (it1.MoveNext() || it2.MoveNext())
                    return false;
                return true;
            }
            else
                return false;
        }
    }
}
