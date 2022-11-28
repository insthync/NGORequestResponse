namespace Unity.Netcode.Insthync.ResquestResponse
{
    public struct RequestHandlerData
    {
        public ushort RequestType { get; private set; }
        public uint RequestId { get; private set; }
        public RequestResponseHandler ReqResHandler { get; private set; }
        public ulong ClientId { get; private set; }
        public FastBufferReader? Reader { get; private set; }

        public RequestHandlerData(ushort requestType, uint requestId, RequestResponseHandler reqResHandler, ulong clientId, FastBufferReader? reader)
        {
            RequestType = requestType;
            RequestId = requestId;
            ReqResHandler = reqResHandler;
            ClientId = clientId;
            Reader = reader;
        }
    }
}
