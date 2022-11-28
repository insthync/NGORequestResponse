namespace Unity.Netcode.Insthync.ResquestResponse
{
    public struct EmptyMessage : INetworkSerializable
    {
        public static readonly EmptyMessage Value = new EmptyMessage();

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
        }
    }
}
