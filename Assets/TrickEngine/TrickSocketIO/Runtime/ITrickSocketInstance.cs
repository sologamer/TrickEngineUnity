using System;
using System.Text;
using BestHTTP.SocketIO3;
using BestHTTP.SocketIO3.Events;

namespace TrickCore
{
    public interface ITrickSocketInstance
    {
        public Socket CurrentSocket { get; set; }
        public IKeyExchange KeyExchange { get; set; }

        /// <summary>
        /// Sends a message securely to the client
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="payload"></param>
        void SendMessageSecure(string eventName, object payload)
        {
            var message = new
            {
                eventName, payload
            };
            // Encrypt the message
            KeyExchange.EncryptMessage(KeyExchange.GetMySharedKey(), Encoding.UTF8.GetBytes(message.SerializeToJson(false, true)), out var encrypt);
            // Convert the encrypted message to base64, since it's a binary buffer.
            CurrentSocket.Emit("enc", Convert.ToBase64String(encrypt));
        }

        /// <summary>
        /// Gets the encrypted message as a byte array, null if message is failed or no key exchange failed.
        /// </summary>
        /// <param name="base64">The buffer encoded in base64</param>
        /// <returns>The json string of the payload</returns>
        byte[] GetMessageFromBase64AsBuffer(string base64) => KeyExchange.IsExchanged ? KeyExchange.DecryptMessage(KeyExchange.GetMySharedKey(), Convert.FromBase64String(base64)) : null;
        
        /// <summary>
        /// Gets the encrypted message as a string, null if message is failed or no key exchange failed.
        /// </summary>
        /// <param name="base64">The buffer encoded in base64</param>
        /// <returns>The json string of the payload</returns>
        string GetMessageFromBase64AsString(string base64) => GetMessageFromBase64AsBuffer(base64) is {} buffer ? Encoding.UTF8.GetString(buffer) : null;
        
        /// <summary>
        /// Gets the encrypted message, null if message is failed or no key exchange failed.
        /// </summary>
        /// <param name="base64">The buffer encoded in base64</param>
        /// <returns>A friendly object containing the event + the payload</returns>
        TrickSocketData GetMessageFromBase64(string base64) => GetMessageFromBase64AsString(base64)?.DeserializeJsonTryCatch<TrickSocketData>();
        
        void Register();
        void OnConnect(ConnectResponse connectResponse);
        void OnDisconnect();
    }
}