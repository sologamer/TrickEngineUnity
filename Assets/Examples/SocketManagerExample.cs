
using System;
using BestHTTP.SocketIO3;
using BestHTTP.SocketIO3.Events;
using TrickCore;
using UnityEngine;

public class SocketManagerExample : MonoBehaviour
{
    public string HostUrl = "http://127.0.0.1:3000";
    public void Start()
    {
        TrickSocketIOManager.Instance.Connect(new Uri(HostUrl), new SocketOptions()
        {
            Timeout = TimeSpan.FromSeconds(10),
            AutoConnect = true,
        });

        TrickSocketIOManager.Instance.RegisterSocketInstance<SampleTrickSocketInstance>("/");
    }
}


public class SampleTrickSocketInstance : ITrickSocketInstance
{
    public Socket CurrentSocket { get; set; }
    public IKeyExchange KeyExchange { get; set; }

    public void Register()
    {
        CurrentSocket.On("Hello", () =>
        {
            Debug.Log("Hey");
        });
    }

    public void OnConnect(ConnectResponse connectResponse)
    {
        Debug.Log("Socket connected: " + connectResponse.sid);
    }

    public void OnDisconnect()
    {
        Debug.Log("Socket disconnected");
    }

    [SocketIOEvent(nameof(TestOneArg))]
    public void TestOneArg(string arg)
    {
        Debug.Log($"[{nameof(TestOneArg)}] Does this work? value = {arg}");
    }

    [SocketIOEvent(nameof(TestTwoArgs))]
    public void TestTwoArgs(string arg, string arg2)
    {
        Debug.Log($"[{nameof(TestTwoArgs)}] Does this work? value = {arg} / {arg2}");
    }
}
