namespace TrickCore
{
    public interface IKeyExchange
    {
        /// <summary>
        /// Returns true if my shared key is equal to the targets shared key
        /// </summary>
        bool IsExchanged { get; }

        /// <summary>
        /// Gets if key share is finished.
        /// </summary>
        bool KeyShareFinished { get; }

        /// <summary>
        /// Key exchange init call
        /// </summary>
        void Initialize();

        /// <summary>
        /// Key exchange finish call. Shared key generation should be in here
        /// </summary>
        /// <param name="targetPublicKey"></param>
        void Finish(byte[] targetPublicKey);

        /// <summary>
        /// Set key share finished
        /// </summary>
        void SetKeyShareFinished();

        /// <summary>
        /// Reset the key exchange object
        /// </summary>
        void Reset();

        /// <summary>
        ///  Get my public key
        /// </summary>
        /// <returns></returns>
        byte[] GetMyPublicKey();
        
        /// <summary>
        ///  Get my shared key
        /// </summary>
        /// <returns></returns>
        byte[] GetMySharedKey();

        /// <summary>
        /// Sets the target shared key
        /// </summary>
        /// <param name="sharedKey">The shared key</param>
        void SetTargetSharedKey(byte[] sharedKey);


        byte[] DecryptMessage(byte[] sharedKey, byte[] encryptedMessage);

        void EncryptMessage(byte[] sharedKey, byte[] message, out byte[] encryptedMessage);
    }
}