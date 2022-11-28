namespace Unity.Netcode.Insthync.ResquestResponse
{
    public interface IRequestInvoker
    {
        void InvokeRequest(RequestHandlerData requestHandler);
    }

    public class RequestInvoker<TRequest, TResponse> : IRequestInvoker
        where TRequest : INetworkSerializable, new()
        where TResponse : INetworkSerializable, new()
    {
        private RequestResponseHandler handler;
        private RequestDelegate<TRequest, TResponse> requestHandler;

        public RequestInvoker(RequestResponseHandler handler, RequestDelegate<TRequest, TResponse> requestHandler)
        {
            this.handler = handler;
            this.requestHandler = requestHandler;
        }

        public void InvokeRequest(RequestHandlerData requestHandlerData)
        {
            TRequest request = new TRequest();
            if (requestHandlerData.Reader.HasValue)
                requestHandlerData.Reader.Value.ReadNetworkSerializable(out request);
            if (requestHandler != null)
                requestHandler.Invoke(requestHandlerData, request, (responseCode, response, extraResponseSerializer) => RequestProceeded(requestHandlerData.ClientId, requestHandlerData.RequestId, responseCode, response, extraResponseSerializer));
        }

        /// <summary>
        /// Send response to the requester
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="requestId"></param>
        /// <param name="responseCode"></param>
        /// <param name="response"></param>
        /// <param name="extraResponseSerializer"></param>
        private void RequestProceeded(ulong clientId, uint requestId, AckResponseCode responseCode, TResponse response, SerializerDelegate extraResponseSerializer)
        {
            // Write response
            handler.Writer.Truncate();
            handler.Writer.WriteNetworkSerializable(response);
            if (extraResponseSerializer != null)
                extraResponseSerializer.Invoke(handler.Writer);
            ResponseMessage responseMessage = new ResponseMessage()
            {
                requestId = requestId,
                responseCode = responseCode,
                data = handler.Writer.ToArray(),
            };
            handler.Writer.Truncate();
            handler.Writer.WriteNetworkSerializable(responseMessage);
            // Send response
            handler.Manager.NetworkManager.CustomMessagingManager.SendNamedMessage(handler.Manager.ResponseMessageName, clientId, handler.Writer);
        }
    }
}