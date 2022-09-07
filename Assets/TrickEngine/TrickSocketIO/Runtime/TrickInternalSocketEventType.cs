namespace TrickCore
{
    /// <summary>
    /// Internal socket event types. use nameof(TrickInternalSocketEventType.x) to ensure the enum values are refactored correctly and no GC allocs.
    ///
    /// The names here are all lower case (javascript/typescript) naming style 
    /// </summary>
    internal enum TrickInternalSocketEventType
    {
        /// <summary>
        /// Called when we start with our key exchange.
        /// data: the public key of the other party.
        /// </summary>
        exchange_start,
        
        /// <summary>
        /// Called when we finish our key exchange. From now on both parties know how to communicate with each other encrypted.
        /// data: the socket id of the client, to ensure the exchange is succeeded
        /// </summary>
        exchange_finish,
        
        /// <summary>
        /// The encrypted/secure channel, if we receive something here, we know it's an encrypted message.
        /// </summary>
        enc,
        
        /// <summary>
        /// [not used] The decrypted/unsecure channel, if we receive something here, we know it's an just a message (not secure!).
        /// The message can still be encrypted in a less secure way to just make it harder for possible hackers.
        /// The encryption can be a secret on both client/server
        /// </summary>
        dec,
    }
}