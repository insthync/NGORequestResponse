namespace Unity.Netcode.Insthync.ResquestResponse
{
    public struct ResponseMessage
    {
        public uint requestId;
        public AckResponseCode responseCode;
        public byte[] data;
    }
}