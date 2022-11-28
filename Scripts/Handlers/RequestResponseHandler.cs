using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Netcode.Insthync.ResquestResponse
{
    // TODO: Implement better logger
    public class RequestResponseHandler
    {
        public readonly RequestResponseManager Manager;
        public readonly FastBufferWriter Writer = new FastBufferWriter();
        protected readonly Dictionary<ushort, IRequestInvoker> requestInvokers = new Dictionary<ushort, IRequestInvoker>();
        protected readonly Dictionary<ushort, IResponseInvoker> responseInvokers = new Dictionary<ushort, IResponseInvoker>();
        protected readonly ConcurrentDictionary<uint, RequestCallback> requestCallbacks = new ConcurrentDictionary<uint, RequestCallback>();
        protected uint nextRequestId;

        public RequestResponseHandler(RequestResponseManager manager)
        {
            this.Manager = manager;
        }

        /// <summary>
        /// Create new request callback with a new request ID
        /// </summary>
        /// <param name="responseInvoker"></param>
        /// <param name="responseHandler"></param>
        /// <returns></returns>
        private uint CreateRequest(IResponseInvoker responseInvoker, ResponseDelegate<object> responseHandler)
        {
            uint requestId = nextRequestId++;
            // Get response callback by request type
            requestCallbacks.TryAdd(requestId, new RequestCallback(requestId, this, responseInvoker, responseHandler));
            return requestId;
        }

        /// <summary>
        /// Delay and do something when request timeout
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="millisecondsTimeout"></param>
        private async void HandleRequestTimeout(uint requestId, int millisecondsTimeout)
        {
            if (millisecondsTimeout > 0)
            {
                await Task.Delay(millisecondsTimeout);
                if (requestCallbacks.TryRemove(requestId, out RequestCallback callback))
                    callback.ResponseTimeout();
            }
        }

        /// <summary>
        /// Create a new request and send to target
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <param name="clientId"></param>
        /// <param name="requestType"></param>
        /// <param name="request"></param>
        /// <param name="extraRequestSerializer"></param>
        /// <param name="responseHandler"></param>
        /// <param name="millisecondsTimeout"></param>
        /// <returns></returns>
        public bool CreateAndSendRequest<TRequest>(
            ulong clientId,
            ushort requestType,
            TRequest request,
            SerializerDelegate extraRequestSerializer,
            ResponseDelegate<object> responseHandler,
            int millisecondsTimeout)
            where TRequest : INetworkSerializable, new()
        {
            if (!responseInvokers.ContainsKey(requestType))
            {
                responseHandler.Invoke(new ResponseHandlerData(nextRequestId++, this, NetworkManager.ServerClientId, null), AckResponseCode.Unimplemented, EmptyMessage.Value);
                Debug.LogError($"Cannot create request. Request type: {requestType} not registered.");
                return false;
            }
            if (!responseInvokers[requestType].IsRequestTypeValid(typeof(TRequest)))
            {
                responseHandler.Invoke(new ResponseHandlerData(nextRequestId++, this, NetworkManager.ServerClientId, null), AckResponseCode.Unimplemented, EmptyMessage.Value);
                Debug.LogError($"Cannot create request. Request type: {requestType}, {typeof(TRequest)} is not valid message type.");
                return false;
            }
            // Create request
            uint requestId = CreateRequest(responseInvokers[requestType], responseHandler);
            HandleRequestTimeout(requestId, millisecondsTimeout);
            // Write request
            Writer.Truncate();
            Writer.WriteNetworkSerializable(request);
            if (extraRequestSerializer != null)
                extraRequestSerializer.Invoke(Writer);
            RequestMessage requestMessage = new RequestMessage()
            {
                requestType = requestType,
                requestId = requestId,
                data = Writer.ToArray(),
            };
            Writer.Truncate();
            Writer.WriteNetworkSerializable(requestMessage);
            // Send request
            Manager.NetworkManager.CustomMessagingManager.SendNamedMessage(Manager.RequestMessageName, clientId, Writer);
            return true;
        }

        /// <summary>
        /// Proceed request which reveived from server or client
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="requestMessage"></param>
        public void ProceedRequest(ulong clientId, RequestMessage requestMessage)
        {
            ushort requestType = requestMessage.requestType;
            uint requestId = requestMessage.requestId;
            if (!requestInvokers.ContainsKey(requestType))
            {
                // No request-response handler
                ResponseMessage responseMessage = new ResponseMessage()
                {
                    requestId = requestId,
                    responseCode = AckResponseCode.Unimplemented,
                };
                Writer.Truncate();
                Writer.WriteNetworkSerializable(responseMessage);
                // Send response
                Manager.NetworkManager.CustomMessagingManager.SendNamedMessage(Manager.ResponseMessageName, clientId, Writer);
                Debug.LogError($"Cannot proceed request {requestType} not registered.");
                return;
            }
            // Invoke request and create response
            requestInvokers[requestType].InvokeRequest(new RequestHandlerData(requestType, requestId, this, clientId, new FastBufferReader(requestMessage.data, Collections.Allocator.Temp)));
        }

        /// <summary>
        /// Proceed response which reveived from server or client
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="responseMessage"></param>
        public void ProceedResponse(ulong clientId, ResponseMessage responseMessage)
        {
            uint requestId = responseMessage.requestId;
            AckResponseCode responseCode = responseMessage.responseCode;
            if (requestCallbacks.ContainsKey(requestId))
            {
                requestCallbacks[requestId].Response(clientId, new FastBufferReader(responseMessage.data, Collections.Allocator.Temp), responseCode);
                requestCallbacks.TryRemove(requestId, out _);
            }
        }

        /// <summary>
        /// Register request handler which will read request message and response to requester peer
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="requestType"></param>
        /// <param name="handlerDelegate"></param>
        public void RegisterRequestHandler<TRequest, TResponse>(
            ushort requestType,
            RequestDelegate<TRequest, TResponse> handlerDelegate)
            where TRequest : INetworkSerializable, new()
            where TResponse : INetworkSerializable, new()
        {
            requestInvokers[requestType] = new RequestInvoker<TRequest, TResponse>(this, handlerDelegate);
        }

        public void UnregisterRequestHandler(ushort requestType)
        {
            requestInvokers.Remove(requestType);
        }

        /// <summary>
        /// Register response handler which will read response message and do something by requester
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="requestType"></param>
        /// <param name="handlerDelegate"></param>
        public void RegisterResponseHandler<TRequest, TResponse>(
            ushort requestType,
            ResponseDelegate<TResponse> handlerDelegate = null)
            where TRequest : INetworkSerializable, new()
            where TResponse : INetworkSerializable, new()
        {
            responseInvokers[requestType] = new ResponseInvoker<TRequest, TResponse>(handlerDelegate);
        }

        public void UnregisterResponseHandler(ushort requestType)
        {
            responseInvokers.Remove(requestType);
        }
    }
}
