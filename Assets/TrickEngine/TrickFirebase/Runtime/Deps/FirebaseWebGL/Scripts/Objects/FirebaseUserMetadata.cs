using System;

namespace TrickCore
{
    [Serializable]
    public sealed class FirebaseUserMetadata
    {
        public ulong lastSignInTimestamp;

        public ulong creationTimestamp;
    }
}
