namespace Unity.Netcode.Insthync.ResquestResponse
{
    public struct RequestMessage
    {
        public ushort requestType;
        public uint requestId;
        public byte[] data;
    }
}