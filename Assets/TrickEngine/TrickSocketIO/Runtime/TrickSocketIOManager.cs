#if TRICK_SOCKET_IO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BeauRoutine;
using BestHTTP.SocketIO3;
using BestHTTP.SocketIO3.Events;
using BestHTTP.SocketIO3.Parsers;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace TrickCore
{
    public sealed class TrickSocketIOManager : MonoSingleton<TrickSocketIOManager>
    {
        /// <summary>
        /// The amount of times we poll per second locally for state changes.
        /// </summary>
        public float ConnectionStatePollRate = 10.0f;

        private readonly IParser _defaultParser = new DefaultJsonParser();
        private Routine _connectionUpdater;
        private SocketManager.States? _lastState = null;

        public UnityEvent<(SocketManager.States previousState, SocketManager.States newState)> StateChangeEvent { get; } = new UnityEvent<(SocketManager.States previousState, SocketManager.States newState)>();
        public SocketManager ActiveConnection { get; set; }

        /// <summary>
        /// Event invoked whenever we receive a message. A way to listen to messages
        /// </summary>
        public UnityEvent<TrickSocketData> OnMessageReceived { get; } = new UnityEvent<TrickSocketData>();
        
        protected override void Initialize()
        {
            base.Initialize();
        }

        public void Connect(Uri uri, SocketOptions options)
        {
            ActiveConnection = new SocketManager(uri, _defaultParser, options);
            if (ActiveConnection.State == SocketManager.States.Initial) ActiveConnection.Open();

            _connectionUpdater.Replace(Routine.PerSecond(() =>
            {
                if (ActiveConnection != null)
                {
                    if (_lastState != ActiveConnection.State)
                    {
                        StateChangeEvent?.Invoke((_lastState.GetValueOrDefault(SocketManager.States.Initial), ActiveConnection.State));
                        _lastState = ActiveConnection.State;
                    }
                }
                else
                {
                    _connectionUpdater.Stop();
                }
            }, ConnectionStatePollRate));
        }

        public void Close()
        {
            ActiveConnection?.Close();
        }

        /// <summary>
        /// Registers a socket instance
        /// </summary>
        /// <param name="nsp">The socket namespace</param>
        /// <param name="handshakeCompleteAction">A callback of the current created instance and a result of the handshake completion</param>
        /// <param name="socketEventInjectInstanceList">Additional instances of classes where we want to inject/scan the socket events for</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T RegisterSocketInstance<T>([NotNull] string nsp, Action<T, bool> handshakeCompleteAction, List<object> socketEventInjectInstanceList = null) where T : TrickSocketInstance, new()
        {
            if (ActiveConnection == null) throw new ArgumentNullException(nameof(ActiveConnection));
            
            var instance = new T();
            var socket = ActiveConnection.GetSocket(nsp);
            
            instance.CurrentSocket = socket;
            instance.Register();
        
            socket.On<ConnectResponse>(SocketIOEventTypes.Connect, instance.OnConnect);
            socket.On(SocketIOEventTypes.Disconnect, instance.OnDisconnect);

            socketEventInjectInstanceList ??= new List<object>();
            socketEventInjectInstanceList.Add(instance);

            instance.RegisterSocketEventInstances(socketEventInjectInstanceList);
            
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
                    }, nameof(TrickInternalSocketEventType.exchange_finish));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            });
            
            // We have received something ENCRYPTED from the server, lets decrypt it here!
            socket.On<string>(nameof(TrickInternalSocketEventType.exchange_finish), base64 =>
            {
                // Ignore any further key exchange calls.
                if (instance.KeyExchange.KeyShareFinished)
                {
                    Debug.LogError($"[SocketIO] Key exchange already done!");
                    return;
                }
                try
                {
                    var message = instance.GetMessageFromBase64(base64, false);
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
                                
                                // from now on we can read the encrypted messages quickly
                                instance.KeyExchange.SetKeyShareFinished();
                                Debug.Log($"[SocketIO] Key exchange succeed (state={instance.KeyExchange.IsExchanged})");
                                handshakeCompleteAction?.Invoke(instance, true);
                            }
                            else
                            {
                                Debug.Log("[SocketIO] Key exchange failed");
                                handshakeCompleteAction?.Invoke(instance, false);
                            }
                        }
                    }
                    else
                    {
                        if (message != null) Debug.Log("[SocketIO] unhandled message: " + message.EventName);
                        handshakeCompleteAction?.Invoke(instance, false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    handshakeCompleteAction?.Invoke(instance, false);
                }
            });
            
            // We have received something ENCRYPTED from the server, lets decrypt it here!
            socket.On<string>(nameof(TrickInternalSocketEventType.enc), base64 =>
            {
                try
                {
                    // The message is the stringified data
                    var message = instance.GetMessageFromBase64(base64);
                    if (message == null) return;
                    OnMessageReceived?.Invoke(message);
                    if (instance.RegisteredSocketEventInstances.Count > 0)
                    {
                        if (instance.RegisteredSocketEventInstances.TryGetValue(message.EventName, out var list))
                        {
                            list.ForEach(tuple =>
                            {
                                try
                                {
                                    // Let's invoke it here
                                    var parameters = tuple.info.GetParameters();
                                    if (parameters.Length == 0)
                                        tuple.info.Invoke(tuple.inst, null);
                                    else if (parameters.Length == 1)
                                    {
                                        var type = parameters[0].ParameterType;
                                    
                                        if (type == typeof(string))
                                        {
                                            JToken.Parse(message.Payload);                                        
                                        }
                                        if (type == typeof(string))
                                        {
                                            if (!(message.Payload.StartsWith("{") && message.Payload.EndsWith("}")))
                                                tuple.info.Invoke(tuple.inst,
                                                    new[] { message.Payload?.Trim('\"') as object });
                                        }
                                        else 
                                            tuple.info.Invoke(tuple.inst,
                                                new[] { message.Payload.DeserializeJson(parameters[0].ParameterType) });
                                    }
                                    else
                                        Debug.LogError(
                                            $"[SocketIO] Event of name '{message.EventName}' only supports one payload (parameter).");
                                }
                                catch (Exception e)
                                {
                                    Debug.LogException(e);
                                }
                            });
                        }
                        else
                        {
                            if (OnMessageReceived == null || OnMessageReceived.GetPersistentEventCount() == 0) Debug.LogError($"[SocketIO] Event of name '{message.EventName}' not registered!");
                        }
                    }
                    else
                    {
                        if (OnMessageReceived == null || OnMessageReceived.GetPersistentEventCount() == 0) Debug.LogError($"[SocketIO] Event of name '{message.EventName}' not registered!");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            });
            
            return instance;
        }
    }
}
#endif