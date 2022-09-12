using System;

namespace TrickCore
{
    [Serializable]
    public sealed class FirebaseUserProvider
    {
        public string displayName;

        public string email;

        public string photoUrl;

        public string providerId;

        public string userId;
    }
}