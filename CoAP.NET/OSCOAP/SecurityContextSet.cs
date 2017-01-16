using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoAP.OSCOAP
{
    public class SecurityContextSet
    {
        List<SecurityContext> _allContexts = new List<SecurityContext>();
        public static SecurityContextSet AllContexts = new SecurityContextSet();

        public int Count { get { return _allContexts.Count; } }

        public void Add(SecurityContext ctx)
        {
            _allContexts.Add(ctx);
        }

        public SecurityContext FindByKid(byte[] kid)
        {
            if (_allContexts.Count > 0) return _allContexts[0];
            return null;
        }
    }
}
