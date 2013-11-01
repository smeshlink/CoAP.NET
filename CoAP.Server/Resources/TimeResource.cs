using System;
using System.Threading;
using CoAP.EndPoint.Resources;

namespace CoAP.Examples.Resources
{
    class TimeResource : LocalResource
    {
        private Timer _timer;
        private DateTime _now;

        public TimeResource()
            : base("time")
        {
            Title = "GET the current time";
            ResourceType = "CurrentTime";
            Observable = true;

            _timer = new Timer(Timed, null, 0, 2000);
        }

        private void Timed(Object o)
        {
            _now = DateTime.Now;
            Changed();
        }

        public override void DoGet(Request request)
        {
            request.Respond(Code.Content, _now.ToString(), MediaType.TextPlain);
        }
    }
}
