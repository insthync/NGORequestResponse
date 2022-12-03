# NGOReqRes
A Request and response addon for Unity's Netcode for Game Object

## How to use it

Attach `RequestResponseManager` to any game object.

Register requests by uses functions from `RequestResponseManager` class, example:

```
// Request Data
using Unity.Netcode;

public struct TestReq : INetworkSerializable
{
    public int a;
    public int b;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref a);
        serializer.SerializeValue(ref b);
    }
}

// Response Data
using Unity.Netcode;

public struct TestRes : INetworkSerializable
{
    public int c;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref c);
    }
}

// Register class, attach this to the same game object with `RequestResponseManager`
using Unity.Netcode.Insthync.ResquestResponse;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRequestResponse : MonoBehaviour
{
    public RequestResponseManager reqResMgr;

    public int a = 0;
    public int b = 0;
    public bool sendUpdate;

    private void Start()
    {
        reqResMgr.RegisterRequestToServer<TestReq, TestRes>(1, RequestHandler, ResponseHandler);
    }

    public void Update()
    {
        if (sendUpdate)
        {
            sendUpdate = false;
            reqResMgr.ClientSendRequest(1, new TestReq()
            {
                a = a,
                b = b,
            });
        }
    }

    void RequestHandler(RequestHandlerData requestHandler, TestReq request, RequestProceedResultDelegate<TestRes> result)
    {
        Debug.Log("Request: " + request.a + ", " + request.b);
        result.Invoke(AckResponseCode.Success, new TestRes()
        {
            c = request.a + request.b
        });
    }

    void ResponseHandler(ResponseHandlerData requestHandler, AckResponseCode responseCode, TestRes response)
    {
        Debug.Log("Response: " + responseCode + " = " + response.c);
    }
}
```
