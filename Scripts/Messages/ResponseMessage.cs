namespace Unity.Netcode.Insthync.ResquestResponse
{
    public struct ResponseMessage : INetworkSerializable
    {
        public uint requestId;
        public AckResponseCode responseCode;
        public byte[] data;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref requestId);
            serializer.SerializeValue(ref responseCode);
            serializer.SerializeValue(ref data);
        }
    }
}