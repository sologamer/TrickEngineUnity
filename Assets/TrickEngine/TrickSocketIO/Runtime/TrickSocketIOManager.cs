using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BestHTTP.SocketIO3;
using BestHTTP.SocketIO3.Events;
using BestHTTP.SocketIO3.Parsers;
using UnityEngine;

namespace TrickCore
{
    public class TrickSocketIOManager : MonoSingleton<TrickSocketIOManager>
    {
        private readonly IParser _defaultParser = new DefaultJsonParser();
        public SocketManager ActiveConnection { get; set; }

        protected override void Initialize()
        {
            base.Initialize();
        }

        public void Connect(Uri uri, SocketOptions options)
        {
            ActiveConnection = new SocketManager(uri, _defaultParser, options);
            if (ActiveConnection.State == SocketManager.States.Initial) ActiveConnection.Open();
        }

        public T RegisterSocketInstance<T>(string nsp) where T : ITrickSocketInstance, new()
        {
            var instance = new T();
            var socket = ActiveConnection.GetSocket(nsp);
            
            instance.CurrentSocket = socket;
            instance.Register();
        
            socket.On<ConnectResponse>(SocketIOEventTypes.Connect, instance.OnConnect);
            socket.On(SocketIOEventTypes.Disconnect, instance.OnDisconnect);
        
            // exchange message, received from the SERVER
            socket.On<Dictionary<string,object>>("exchange", dict =>
            {
                if (dict.TryGetValue("pub", out var keyObj) && keyObj is string serverPub)
                {
                    // Create a new key exchange instance, this is invoked whenever we connect to the server.
                    instance.KeyExchange = new KeyExchangeECDH();
                    instance.KeyExchange.Initialize();
                    Debug.Log($"CLIENT PUB ({instance.KeyExchange.GetMyPublicKey().Length}): " + instance.KeyExchange.GetMyPublicKey().ByteArrayToHexViaLookup32());
                    var serverPubKey = Convert.FromBase64String(serverPub);
                    var myPublicKey = Convert.ToBase64String(instance.KeyExchange.GetMyPublicKey());
                    Debug.Log($"SERVER PUB ({serverPubKey.Length}): " + serverPubKey.ByteArrayToHexViaLookup32());
                    // We send our public key to the server, so they can create their shared secret too.
                    socket.Emit("exchange", myPublicKey);
                    // Mark the key exchange finished, we now have a shared key
                    instance.KeyExchange.Finish(serverPubKey);
                    Debug.Log($"SHARED ({instance.KeyExchange.GetMySharedKey().Length}): {instance.KeyExchange.GetMySharedKey().ByteArrayToHexViaLookup32()}");
                    
                    // Test send encrypted message to the server, they can decrypt it now if they have generated their shared key
                    // The idea is that the message contains an eventName + the event's payload, so we can basically invoke 'functions' to the server securely.
                    var test = new
                    {
                        eventName = "random_event",
                        payload = new
                        {
                            a = 1,
                            b = "yo",
                        },
                    };
                    // Encrypt the message
                    instance.KeyExchange.EncryptMessage(instance.KeyExchange.GetMySharedKey(),
                        Encoding.UTF8.GetBytes(test.SerializeToJson(false, true)), out var encrypt);
                    // Convert the encrypted message to base64, since it's a binary buffer.
                    socket.Emit("enc", System.Convert.ToBase64String(encrypt));
                }
            });
            // We have received something ENCRYPTED from the server, lets decrypt it here!
            socket.On<string>("enc", base64 =>
            {
                var decrypted = instance.KeyExchange.DecryptMessage(instance.KeyExchange.GetMySharedKey(), Convert.FromBase64String(base64));
                Debug.Log(Encoding.UTF8.GetString(decrypted));
            });
            
            socket.On<Dictionary<string,object>>("req", dict =>
            {
                if (dict.TryGetValue("payload", out var payloadObj) && payloadObj is string payload)
                {
                    Debug.Log("REQ PAYLOAD: " + payload);
                    //Debug.Log("DECRYPT: " + TrickAES.Decrypt(payload, instance.AesKey));
                }
            });
            
            var autoRegister = instance.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(info => info.GetAttribute<SocketIOEventAttribute>() != null)
                .ToList();

            if (autoRegister.Count > 0)
            {
                if (typeof(Socket).GetField(nameof(TypedEventTable), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField)?.GetValue(socket) is TypedEventTable eventTable)
                {
                    // support auto register methods, for cleanliness, possible to optimize this
                    foreach (MethodInfo info in autoRegister)
                    {
                        Debug.Log("Auto register method: " + info.Name);
                        var attribute = info.GetAttribute<SocketIOEventAttribute>();
                        var parameters = info.GetParameters();
                        eventTable.Register(attribute.EventName, parameters.Select(parameterInfo => parameterInfo.ParameterType).ToArray(),
                            objects =>
                            {
                                info.Invoke(instance, objects);
                            });
                    }
                }
                else
                {
                    Debug.LogError("Unable to find the TypedEventTable. Is it being stripped away?");
                }
            }
        
            return instance;
        }
    }

    public class SocketIOEventAttribute : Attribute
    {
        public string EventName { get; }

        public SocketIOEventAttribute(string eventName)
        {
            EventName = eventName;
        }
    }

    public interface ITrickSocketInstance
    {
        public Socket CurrentSocket { get; set; }
        public IKeyExchange KeyExchange { get; set; }
        void Register();
        void OnConnect(ConnectResponse connectResponse);
        void OnDisconnect();
    }
}