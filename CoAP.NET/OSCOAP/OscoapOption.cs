using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CoAP;

namespace CoAP.OSCOAP
{
#if INCLUDE_OSCOAP
    public class OscoapOption : Option
    {
        public OscoapOption() : base(OptionType.Oscoap)
        {

        }

        public void Set(byte[] value) { RawValue = value; }

        public override string ToString()
        {
            if (this.RawValue == null) return "** InPayload";
            return String.Format("** Length={0}", this.RawValue.Length);
        }
    }
#endif
}
