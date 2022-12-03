using UnityEngine;

namespace Unity.Netcode.Insthync.ResquestResponse
{
    public class RequestResponseManager : MonoBehaviour
    {
        [SerializeField]
        private string requestMessageName = "REQ";
        public string RequestMessageName => requestMessageName;

        [SerializeField]
        private string responseMessageName = "RES";
        public string ResponseMessageName => responseMessageName;

        [SerializeField]
        private bool autoSetupByNetworkState = true;

        public int clientRequestTimeoutInMilliseconds = 30000;
        public int serverRequestTimeoutInMilliseconds = 30000;

        private bool _alreadySetup = false;
        private RequestResponseHandler _serverReqResHandler;
        private RequestResponseHandler _clientReqResHandler;

        private void Awake()
        {
            _serverReqResHandler = new RequestResponseHandler(this);
            _clientReqResHandler = new RequestResponseHandler(this);
        }

        private void Update()
        {
            if (!autoSetupByNetworkState || !NetworkManager.Singleton)
                return;

            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsConnectedClient)
            {
                if (!_alreadySetup)
                {
                    _alreadySetup = true;
                    Setup();
                }
            }
            else
            {
                _alreadySetup = false;
            }
        }

        public void Setup()
        {
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(RequestMessageName, RequestCallback);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(ResponseMessageName, ResponseCallback);
        }

        private void RequestCallback(ulong clientId, FastBufferReader messagePayload)
        {
            messagePayload.ReadNetworkSerializable(out RequestMessage msg);
            if (clientId != NetworkManager.ServerClientId)
                _serverReqResHandler.ProceedRequest(clientId, msg);
            else
                _clientReqResHandler.ProceedRequest(clientId, msg);
        }

        private void ResponseCallback(ulong clientId, FastBufferReader messagePayload)
        {
            messagePayload.ReadNetworkSerializable(out ResponseMessage msg);
            if (clientId != NetworkManager.ServerClientId)
                _serverReqResHandler.ProceedResponse(clientId, msg);
            else
                _clientReqResHandler.ProceedResponse(clientId, msg);
        }

        public bool ServerSendRequest<TRequest>(
            ulong clientId,
            ushort requestType,
            TRequest request,
            SerializerDelegate extraRequestSerializer = null,
            ResponseDelegate<object> responseHandler = null)
            where TRequest : INetworkSerializable, new()
        {
            return _serverReqResHandler.CreateAndSendRequest(clientId, requestType, request, extraRequestSerializer, responseHandler, serverRequestTimeoutInMilliseconds);
        }

        public bool ClientSendRequest<TRequest>(
            ushort requestType,
            TRequest request,
            SerializerDelegate extraRequestSerializer = null,
            ResponseDelegate<object> responseHandler = null)
            where TRequest : INetworkSerializable, new()
        {
            return _clientReqResHandler.CreateAndSendRequest(NetworkManager.ServerClientId, requestType, request, extraRequestSerializer, responseHandler, clientRequestTimeoutInMilliseconds);
        }

        public void RegisterRequestToServer<TRequest, TResponse>(ushort reqType, RequestDelegate<TRequest, TResponse> requestHandler, ResponseDelegate<TResponse> responseHandler = null)
            where TRequest : INetworkSerializable, new()
            where TResponse : INetworkSerializable, new()
        {
            _serverReqResHandler.RegisterRequestHandler(reqType, requestHandler);
            _clientReqResHandler.RegisterResponseHandler<TRequest, TResponse>(reqType, responseHandler);
        }

        public void UnregisterRequestToServer(ushort reqType)
        {
            _serverReqResHandler.UnregisterRequestHandler(reqType);
            _clientReqResHandler.UnregisterResponseHandler(reqType);
        }

        public void RegisterRequestToClient<TRequest, TResponse>(ushort reqType, RequestDelegate<TRequest, TResponse> requestHandler, ResponseDelegate<TResponse> responseHandler = null)
            where TRequest : INetworkSerializable, new()
            where TResponse : INetworkSerializable, new()
        {
            _clientReqResHandler.RegisterRequestHandler(reqType, requestHandler);
            _serverReqResHandler.RegisterResponseHandler<TRequest, TResponse>(reqType, responseHandler);
        }

        public void UnregisterRequestToClient(ushort reqType)
        {
            _clientReqResHandler.UnregisterRequestHandler(reqType);
            _serverReqResHandler.UnregisterResponseHandler(reqType);
        }
    }
}
