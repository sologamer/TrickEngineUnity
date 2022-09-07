using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BestHTTP.SocketIO3;
using BestHTTP.SocketIO3.Events;
using BestHTTP.SocketIO3.Parsers;
using Newtonsoft.Json;
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
        
            var autoRegister = instance.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(info => (info, info.GetAttribute<SocketIOEventAttribute>()))
                .Where(tuple => tuple.Item2 != null)
                .ToList();

            // exchange message, received from the SERVER
            socket.On<Dictionary<string,object>>(nameof(TrickInternalSocketEventType.exchange_start), dict =>
            {
                try
                {
                    if (!dict.TryGetValue("pub", out var keyObj) || keyObj is not string serverPub) return;
                    // Create a new key exchange instance, this is invoked whenever we connect to the server.
                    instance.KeyExchange = new KeyExchangeECDH();
                    instance.KeyExchange.Initialize();

                    var serverPubKey = Convert.FromBase64String(serverPub);
                    var myPublicKey = Convert.ToBase64String(instance.KeyExchange.GetMyPublicKey());
                    // We send our public key to the server, so they can create their shared secret too.
                    socket.Emit(nameof(TrickInternalSocketEventType.exchange_start), myPublicKey);
                    // Mark the key exchange finished, we now have a shared key
                    instance.KeyExchange.Finish(serverPubKey);

                    // For debugging purposes
                    //Debug.Log($"CLIENT PUB ({instance.KeyExchange.GetMyPublicKey().Length}): " + instance.KeyExchange.GetMyPublicKey().ByteArrayToHexViaLookup32());
                    //Debug.Log($"SERVER PUB ({serverPubKey.Length}): " + serverPubKey.ByteArrayToHexViaLookup32());
                    //Debug.Log($"SHARED ({instance.KeyExchange.GetMySharedKey().Length}): {instance.KeyExchange.GetMySharedKey().ByteArrayToHexViaLookup32()}");

                    // Encrypt the message, sending the socket id to the target + the shared secret.
                    // The opposite client will DECRYPT the message, compare their using their shared key, and equal test if these properties are equal.
                    // If yes, they continue communicating, otherwise abort connection. 
                    instance.SendMessageSecure(nameof(TrickInternalSocketEventType.exchange_finish), new
                    {
                        socketId = socket.Id,
                        sharedSecret = Convert.ToBase64String(instance.KeyExchange.GetMySharedKey()),
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            });
            
            // We have received something ENCRYPTED from the server, lets decrypt it here!
            socket.On<string>(nameof(TrickInternalSocketEventType.exchange_finish), base64 =>
            {
                try
                {
                    var message = instance.GetMessageFromBase64(base64);
                    if (message is { EventName: nameof(TrickInternalSocketEventType.exchange_finish) })
                    {
                        var payloadData = message.GetPayloadAs<Dictionary<string, object>>();
                        if (payloadData != null &&
                            payloadData.TryGetValue("socketId", out var socketId) &&
                            payloadData.TryGetValue("sharedSecret", out var sharedSecret))
                        {
                            var targetSharedKey = Convert.FromBase64String($"{sharedSecret}");
                        
                            // check if key exchange succeeded
                            if (Equals(socket.Id, socketId) &&
                                instance.KeyExchange.GetMySharedKey().SequenceEqual(targetSharedKey))
                            {
                                instance.KeyExchange.SetTargetSharedKey(targetSharedKey);
                                instance.KeyExchange.SetKeyShareFinished();
                                Debug.Log($"[SocketIO] Key exchange succeed (state={instance.KeyExchange.IsExchanged})");
                            }
                            else
                            {
                                Debug.Log("[SocketIO] Key exchange failed");
                            }
                        }
                    }
                    else
                    {
                        if (message != null) Debug.Log("[SocketIO] unhandled message: " + message.EventName);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            });
            
            // We have received something ENCRYPTED from the server, lets decrypt it here!
            socket.On<string>(nameof(TrickInternalSocketEventType.enc), base64 =>
            {
                try
                {
                    // The message is the stringified data
                    var message = instance.GetMessageFromBase64(base64);
                    Debug.Log("[SocketIO] Encrypted message: " + message);

                    if (autoRegister.Count > 0)
                    {
                        var validEvent = autoRegister.Find(tuple => tuple.Item2.EventName == message.EventName);
                        if (validEvent.info != null)
                        {
                            // Let's invoke it here
                            var parameters = validEvent.info.GetParameters();
                            if (parameters.Length == 0)
                                validEvent.info.Invoke(instance, null);
                            else if (parameters.Length == 1)
                                validEvent.info.Invoke(instance, new[] { message.Payload.DeserializeJson(parameters[0].ParameterType) });
                            else
                                Debug.LogError($"Event of name {message.EventName} not registered!");
                        }
                        else
                        {
                            Debug.LogError($"Event of name {message.EventName} not registered!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            });
            
            if (autoRegister.Count > 0)
            {
                if (typeof(Socket).GetField(nameof(TypedEventTable), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField)?.GetValue(socket) is TypedEventTable eventTable)
                {
                    // support auto register methods, for cleanliness, possible to optimize this
                    foreach (var (info, socketIOEventAttribute) in autoRegister)
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
}