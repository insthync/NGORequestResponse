namespace Unity.Netcode.Insthync.ResquestResponse
{
    public delegate void RequestProceedResultDelegate<TResponse>(AckResponseCode responseCode, TResponse response, SerializerDelegate responseExtraSerializer = null);
}
