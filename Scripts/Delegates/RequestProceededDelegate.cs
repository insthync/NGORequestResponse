namespace Unity.Netcode.Insthync.ResquestResponse
{
    public delegate void RequestProceededDelegate<TResponse>(ulong clientId, uint requestId, AckResponseCode responseCode, TResponse response, SerializerDelegate extraResponseSerializer);
}
