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
    }
}
