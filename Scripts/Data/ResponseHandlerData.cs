namespace Unity.Netcode.Insthync.ResquestResponse
{
    public struct ResponseHandlerData
    {
        public uint RequestId { get; private set; }
        public RequestResponseHandler ReqResHandler { get; private set; }
        public ulong ClientId { get; private set; }
        public FastBufferReader? Reader { get; private set; }

        public ResponseHandlerData(uint requestId, RequestResponseHandler reqResHandler, ulong clientId, FastBufferReader? reader)
        {
            RequestId = requestId;
            ReqResHandler = reqResHandler;
            ClientId = clientId;
            Reader = reader;
        }
    }
}
