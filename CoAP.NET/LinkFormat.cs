/*
 * Copyright (c) 2011, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Text;
using System.Text.RegularExpressions;
using CoAP.Util;

namespace CoAP
{
    /// <summary>
    /// This class provides link format definitions as specified in
    /// draft-ietf-core-link-format-06
    /// </summary>
    public static class LinkFormat
    {
        /// <summary>
        /// Name of the attribute Resource Type
        /// </summary>
        public static readonly String ResourceType = "rt";
        /// <summary>
        /// Name of the attribute Interface Description
        /// </summary>
        public static readonly String InterfaceDescription = "if";
        /// <summary>
        /// Name of the attribute Content Type
        /// </summary>
        public static readonly String ContentType = "ct";
        /// <summary>
        /// Name of the attribute Max Size Estimate
        /// </summary>
        public static readonly String MaxSizeEstimate = "sz";
        /// <summary>
        /// Name of the attribute Title
        /// </summary>
        public static readonly String Title = "title";
        /// <summary>
        /// Name of the attribute Observable
        /// </summary>
        public static readonly String Observable = "obs";

        /// <summary>
        /// The string as the delimiter between resources
        /// </summary>
        public static readonly String Delimiter = ",";
        /// <summary>
        /// The string to separate attributes
        /// </summary>
        public static readonly String Separator = ";";

        public static readonly Regex AttributeNameRegex = new Regex("</.*?>");
        public static readonly Regex QuotedString = new Regex("\\G\".*?\"");
        public static readonly Regex Cardinal = new Regex("\\G\\d+");

        /// <summary>
        /// Class for linkformat attributes.
        /// </summary>
        public class Attribute
        {
            private String _name;
            private Object _value;

            /// <summary>
            /// Parses an attribute from a string.
            /// </summary>
            /// <param name="str">The string representation of an attribute</param>
            /// <returns></returns>
            public static Attribute Parse(String str)
            {
                if (String.IsNullOrEmpty(str))
                    return null;

                String[] tmp = str.Split('=');
                String name = tmp.Length > 0 ? tmp[0].Trim() : null;
                String value = tmp.Length > 1 ? tmp[1].Trim() : null;

                Attribute attr = null;
                if (!String.IsNullOrEmpty(name))
                {
                    Int32 val;
                    attr = new Attribute();
                    attr._name = name;
                    if (null == value)
                    { }
                    else if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        // trim " "
                        attr._value = value.Substring(1, value.Length - 2);
                    }
                    else if (Int32.TryParse(value, out val))
                    {
                        attr._value = val;
                    }
                    else
                    {
                        attr._value = value;
                    }
                }

                return attr;
            }

            /// <summary>
            /// Initialize an attribute.
            /// </summary>
            public Attribute()
            { }

            /// <summary>
            /// Initialize an attribute.
            /// </summary>
            public Attribute(String name, Object value)
            {
                _name = name;
                _value = value;
            }

            /// <summary>
            /// Serializes this attribute into its string representation.
            /// </summary>
            /// <param name="builder"></param>
            public void Serialize(StringBuilder builder)
            {
                // check if there's something to write
                if (_name != null && _value != null)
                {
                    if (_value is Boolean)
                    {
                        // flag attribute
                        if ((Boolean)_value)
                        {
                            builder.Append(_name);
                        }
                    }
                    else
                    {
                        // name-value-pair
                        builder.Append(_name);
                        builder.Append('=');
                        if (_value is String)
                        {
                            builder.Append('"');
                            builder.Append((String)_value);
                            builder.Append('"');
                        }
                        else if (_value is Int32)
                        {
                            builder.Append(((Int32)_value));
                        }
                        else
                        {
                            if (Log.IsErrorEnabled)
                                Log.Error(this, "Serializing attribute of unexpected type: {0} ({1})", _name, _value.GetType().Name);
                            builder.Append(_value);
                        }
                    }
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override String ToString()
            {
                return String.Format("name: {0} value: {1}", _name, _value);
            }

            /// <summary>
            /// Gets the name of this attribute.
            /// </summary>
            public String Name
            {
                get { return _name; }
            }

            /// <summary>
            /// Gets the value of this attribute.
            /// </summary>
            public Object Value
            {
                get { return _value; }
            }

            /// <summary>
            /// Gets the int value of this attribute.
            /// </summary>
            public Int32 IntValue
            {
                get { return (_value is Int32) ? (Int32)_value : -1; }
            }

            /// <summary>
            /// Gets the string value of this attribute.
            /// </summary>
            public String StringValue
            {
                get { return (_value is String) ? (String)_value : null; }
            }
        }
    }
}
