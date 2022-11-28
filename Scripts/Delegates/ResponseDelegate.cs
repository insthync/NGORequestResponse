namespace Unity.Netcode.Insthync.ResquestResponse
{
    public delegate void ResponseDelegate<TResponse>(ResponseHandlerData requestHandler, AckResponseCode responseCode, TResponse response);
}
