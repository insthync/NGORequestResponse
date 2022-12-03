using Unity.Collections;

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
            FastBufferWriter writer;
            ResponseMessage responseMessage;
            using (writer = new FastBufferWriter(1300, Allocator.Temp, 4096000))
            {
                writer.WriteNetworkSerializable(response);
                if (extraResponseSerializer != null)
                    extraResponseSerializer.Invoke(writer);
                responseMessage = new ResponseMessage()
                {
                    requestId = requestId,
                    responseCode = responseCode,
                    data = writer.ToArray(),
                };
            }

            using (writer = new FastBufferWriter(1300, Allocator.Temp, 4096000))
            {
                writer.WriteNetworkSerializable(responseMessage);
                // Send response
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(handler.Manager.ResponseMessageName, clientId, writer);
            }
        }
    }
}