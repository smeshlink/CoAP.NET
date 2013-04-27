using System;
using System.Text;

namespace CoAP.Util
{
    static class ByteArrayUtils
    {
        /// <summary>
        /// Returns a hex string representation of the given bytes array.
        /// </summary>
        public static String ToHexString(Byte[] data)
        {
            const String digits = "0123456789ABCDEF";
            if (data != null && data.Length > 0)
            {
                StringBuilder builder = new StringBuilder(data.Length * 3);
                for (Int32 i = 0; i < data.Length; i++)
                {
                    builder.Append(digits[(data[i] >> 4) & 0xF]);
                    builder.Append(digits[data[i] & 0xF]);
                    if (i < data.Length - 1)
                        builder.Append(' ');
                }
                return builder.ToString();
            }
            else
            {
                return "--";
            }
        }
    }
}
