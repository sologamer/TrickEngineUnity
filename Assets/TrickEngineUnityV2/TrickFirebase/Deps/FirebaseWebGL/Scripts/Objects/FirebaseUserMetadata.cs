using System;

namespace TrickCore
{
    [Serializable]
    public class FirebaseUserMetadata
    {
        public ulong lastSignInTimestamp;

        public ulong creationTimestamp;
    }
}
