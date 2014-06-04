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
using System.Text;

namespace CoAP.Util
{
    /// <summary>
    /// Utility methods.
    /// </summary>
    public static class Utils
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

        public static Boolean AreSequenceEqualTo<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            return AreSequenceEqualTo<T>(first, second, null);
        }

        public static Boolean AreSequenceEqualTo<T>(IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T> comparer)
        {
            if (first == null && second == null)
                return true;
            else if (first != null && second != null)
            {
                if (comparer == null)
                    comparer = EqualityComparer<T>.Default;

                using (IEnumerator<T> it1 = first.GetEnumerator())
                using (IEnumerator<T> it2 = second.GetEnumerator())
                {
                    while (it1.MoveNext() && it2.MoveNext())
                    {
                        if (!comparer.Equals(it1.Current, it2.Current))
                            return false;
                    }
                    if (it1.MoveNext() || it2.MoveNext())
                        return false;
                    return true;
                }
            }
            else
                return false;
        }

        public static String ToString(Message msg)
        {
            StringBuilder sb = new StringBuilder();
            String kind = "Message", code = "Code";
            if (msg.IsRequest)
            {
                kind = "Request";
                code = "Method";
            }
            else if (msg.IsResponse)
            {
                kind = "Response";
                code = "Status";
            }
            sb.AppendFormat("==[ COAP {0} ]============================================\n", kind)
                .AppendFormat("ID     : {0}\n", msg.ID)
                .AppendFormat("Type   : {0}\n", msg.Type)
                .AppendFormat("Token  : {0}\n", msg.TokenString)
                .AppendFormat("{1:7}: {0}\n", CoAP.Code.ToString(msg.Code), code)
                .AppendFormat("Source : {0}", msg.Source)
                .AppendFormat("Dest   : {0}", msg.Destination);

            sb.AppendFormat("Payload: {0} Bytes\n", msg.PayloadSize);
            if (msg.PayloadSize > 0 && MediaType.IsPrintable(msg.ContentType))
            {
                sb.AppendLine("---------------------------------------------------------------");
                sb.AppendLine(msg.PayloadString);
            }
            sb.AppendLine("===============================================================");

            return sb.ToString();
        }
    }
}
