using System;
using System.Collections.Generic;
using System.Text;
using CoAP.Log;

namespace CoAP
{
    /// <summary>
    /// Class for linkformat attributes.
    /// </summary>
    public class LinkAttribute : IComparable<LinkAttribute>
    {
        private static ILogger log = LogManager.GetLogger(typeof(LinkAttribute));
        private String _name;
        private Object _value;

        /// <summary>
        /// Parses an attribute from a string.
        /// </summary>
        /// <param name="str">The string representation of an attribute</param>
        /// <returns></returns>
        public static LinkAttribute Parse(String str)
        {
            if (String.IsNullOrEmpty(str))
                return null;

            String[] tmp = str.Split('=');
            String name = tmp.Length > 0 ? tmp[0].Trim() : null;
            String value = tmp.Length > 1 ? tmp[1].Trim() : null;

            LinkAttribute attr = null;
            if (!String.IsNullOrEmpty(name))
            {
                Int32 val;
                attr = new LinkAttribute();
                attr._name = name;
                if (null == value)
                {
                    // flag attribute
                    attr._value = true;
                }
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
        public LinkAttribute()
        { }

        /// <summary>
        /// Initialize an attribute.
        /// </summary>
        public LinkAttribute(String name, Object value)
        {
            _name = name;
            _value = value;
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
                        builder.Append(_name);
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
                        if (log.IsErrorEnabled)
                            log.Error(String.Format("Serializing attribute of unexpected type: {0} ({1})", _name, _value.GetType().Name));
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

        public Int32 CompareTo(LinkAttribute other)
        {
            Int32 ret = _name.CompareTo(other.Name);
            if (ret == 0)
            {
                if (_value is String)
                    return StringValue.CompareTo(other.StringValue);
                else if (_value is Int32)
                    return IntValue.CompareTo(other.IntValue);
            }
            return ret;
        }
    }
}
