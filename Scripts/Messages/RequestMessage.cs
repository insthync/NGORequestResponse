namespace Unity.Netcode.Insthync.ResquestResponse
{
    public struct RequestMessage : INetworkSerializable
    {
        public ushort requestType;
        public uint requestId;
        public byte[] data;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref requestType);
            serializer.SerializeValue(ref requestId);
            serializer.SerializeValue(ref data);
        }
    }
}