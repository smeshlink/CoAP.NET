using System;

namespace CoAP.Observe
{
    /// <summary>
    /// Represents an event when a observing request is reregistering.
    /// </summary>
    public class ReregisterEventArgs : EventArgs
    {
        readonly Request _refreshRequest;

        /// <summary>
        /// Instantiates.
        /// </summary>
        public ReregisterEventArgs(Request refresh)
        {
            _refreshRequest = refresh;
        }

        /// <summary>
        /// Gets the request sent to refresh an observation.
        /// </summary>
        public Request RefreshRequest
        {
            get { return _refreshRequest; }
        }
    }
}
