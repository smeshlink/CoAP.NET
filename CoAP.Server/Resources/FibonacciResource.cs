using System;
using CoAP.Server.Resources;

namespace CoAP.Examples.Resources
{
    /// <summary>
    /// This resource computes the Fibonacci numbers and therefore needs
    /// a lot of computing power to respond to a request. Use the query ?n=20 to
    /// compute the 20. Fibonacci number, e.g.: coap://localhost:5683/fibonacci?n=20.
    /// </summary>
    class FibonacciResource : Resource
    {
        public FibonacciResource(String name)
            : base(name)
        { }

        protected override void DoGet(CoapExchange exchange)
        {
            Int32? n = null;
            foreach (String query in exchange.Request.UriQueries)
            {
                String[] tmp = query.Split('=');
                if (tmp.Length != 2 || tmp[0] != "n")
                    continue;
                n = Int32.Parse(tmp[1]);
            }
            if (n.HasValue)
                exchange.Respond("Fibonacci(" + n.Value + ") = " + Fibonacci(n.Value));
            else
                exchange.Respond("Missing n in query");
        }

        private UInt64 Fibonacci(Int32 n)
        {
            return Fibs(n)[1];
        }

        private UInt64[] Fibs(Int32 n)
        {
            if (n == 1)
                return new[] { 0UL, 1UL };
            UInt64[] fibs = Fibs(n - 1);
            return new[] { fibs[1], fibs[0] + fibs[1] };
        }
    }
}
