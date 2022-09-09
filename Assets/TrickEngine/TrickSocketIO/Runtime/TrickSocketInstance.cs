using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BestHTTP.SocketIO3;
using BestHTTP.SocketIO3.Events;
using UnityEngine;

namespace TrickCore
{
    /// <summary>
    /// The socket instance connected to a certain namespace on a socket.io host
    /// </summary>
    public abstract class TrickSocketInstance
    {
        public Dictionary<string, List<(object inst, MethodInfo info)>> RegisteredSocketEventInstances { get; set; } = new Dictionary<string, List<(object inst, MethodInfo info)>>();
        private HashSet<object> _registeredSocketEventVisitHashSet = new HashSet<object>();
        public Socket CurrentSocket { get; set; }
        public IKeyExchange KeyExchange { get; set; }

        /// <summary>
        /// Inject instances method (requires reflection to do so), it will be cached and will speed up the the event calls
        /// </summary>
        /// <param name="instances"></param>
        public void RegisterSocketEventInstances(params object[] instances)
        {
            foreach (object instance in instances)
            {
                if (_registeredSocketEventVisitHashSet.Contains(instance)) continue;
                _registeredSocketEventVisitHashSet.Add(instance);

                foreach (var pTuple in instance.GetType()
                             .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                             .Select(info => (instance, info, info.GetAttribute<SocketIOEventAttribute>()))
                             .Where(tuple => tuple.Item3 != null))
                {
                    if (!RegisteredSocketEventInstances.TryGetValue(pTuple.Item3.EventName, out var list))
                        RegisteredSocketEventInstances.Add(pTuple.Item3.EventName,
                            list = new List<(object inst, MethodInfo info)>());
                    list.Add((pTuple.info, pTuple.info));
                }
                
            }
        }
        
        /// <summary>
        /// Sends a message securely to the client
        /// </summary>
        /// <param name="eventName">The wrapped event name</param>
        /// <param name="payload">The payload data</param>
        /// <param name="rootEventName">The root event name, this is the socket.On('rootEventName', () => {action})</param>
        public void SendMessageSecure(string eventName, object payload, string rootEventName = nameof(TrickInternalSocketEventType.enc))
        {
            var message = new
            {
                eventName, payload
            };
            // Encrypt the message
            var sharedKey = KeyExchange.GetMySharedKey();
            if (sharedKey == null || sharedKey.Length == 0)
            {
                Debug.LogError("[SocketIO] Unable to send a secure message, no shared key generated yet!");
                return;
            }
            KeyExchange.EncryptMessage(sharedKey, Encoding.UTF8.GetBytes(message.SerializeToJson(false, true)), out var encrypt);
            // Convert the encrypted message to base64, since it's a binary buffer.
            CurrentSocket.Emit(rootEventName, Convert.ToBase64String(encrypt));
        }

        /// <summary>
        /// Gets the encrypted message as a byte array, null if message is failed or no key exchange failed.
        /// </summary>
        /// <param name="base64">The buffer encoded in base64</param>
        /// <param name="testKeyExchange">Test key exchange is required to decrypt the message</param>
        /// <returns>The json string of the payload</returns>
        public byte[] GetMessageFromBase64AsBuffer(string base64, bool testKeyExchange = true) => !testKeyExchange || KeyExchange.KeyShareFinished ? KeyExchange.DecryptMessage(KeyExchange.GetMySharedKey(), Convert.FromBase64String(base64)) : null;

        /// <summary>
        /// Gets the encrypted message as a string, null if message is failed or no key exchange failed.
        /// </summary>
        /// <param name="base64">The buffer encoded in base64</param>
        /// <param name="testKeyExchange">Test key exchange is required to decrypt the message</param>
        /// <returns>The json string of the payload</returns>
        public string GetMessageFromBase64AsString(string base64, bool testKeyExchange = true) => GetMessageFromBase64AsBuffer(base64, testKeyExchange) is {} buffer ? Encoding.UTF8.GetString(buffer) : null;

        /// <summary>
        /// Gets the encrypted message, null if message is failed or no key exchange failed.
        /// </summary>
        /// <param name="base64">The buffer encoded in base64</param>
        /// <param name="testKeyExchange">Test key exchange is required to decrypt the message</param>
        /// <returns>A friendly object containing the event + the payload</returns>
        public TrickSocketData GetMessageFromBase64(string base64, bool testKeyExchange = true) => GetMessageFromBase64AsString(base64, testKeyExchange)?.DeserializeJsonTryCatch<TrickSocketData>();
        
        public abstract void Register();
        public abstract void OnConnect(ConnectResponse connectResponse);
        public abstract void OnDisconnect();
    }
}