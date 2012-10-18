using System;

namespace CoAP
{
    public interface IResponseHandler
    {
        void HandleResponse(Response response);
    }
}
