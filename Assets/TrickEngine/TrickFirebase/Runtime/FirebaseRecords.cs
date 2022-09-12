using System.Collections.Generic;
using Newtonsoft.Json;

namespace TrickCore
{
    public sealed class FirebaseRecords
    {

    }

    public sealed class AdditionalUserInfo
    {
        [JsonProperty("providerId")] public string ProviderId;
        [JsonProperty("isNewUser")] public bool IsNewUser;
    }

    public sealed class MultiFactor
    {
        [JsonProperty("enrolledFactors")] public List<string> EnrolledFactors;
    }

    public sealed class ProviderDatum
    {
        [JsonProperty("uid")] public string Uid;
        [JsonProperty("displayName")] public string DisplayName;
        [JsonProperty("photoURL")] public string PhotoURL;
        [JsonProperty("email")] public string Email;
        [JsonProperty("phoneNumber")] public string PhoneNumber;
        [JsonProperty("providerId")] public string ProviderId;
    }

    public sealed class FirebaseUserCredential
    {
        [JsonProperty("user")] public FirebaseUser User;
        [JsonProperty("credential")] public string Credential;
        [JsonProperty("additionalUserInfo")] public AdditionalUserInfo AdditionalUserInfo;
        [JsonProperty("operationType")] public string OperationType;
    }

    public sealed class StsTokenManager
    {
        [JsonProperty("apiKey")] public string ApiKey;
        [JsonProperty("refreshToken")] public string RefreshToken;
        [JsonProperty("accessToken")] public string AccessToken;
        [JsonProperty("expirationTime")] public long ExpirationTime;
    }

    public sealed class FirebaseUser
    {
        [JsonProperty("uid")] public string Uid;
        [JsonProperty("displayName")] public string DisplayName;
        [JsonProperty("photoURL")] public string PhotoURL;
        [JsonProperty("email")] public string Email;
        [JsonProperty("emailVerified")] public bool EmailVerified;
        [JsonProperty("phoneNumber")] public string PhoneNumber;
        [JsonProperty("isAnonymous")] public bool IsAnonymous;
        [JsonProperty("tenantId")] public string TenantId;
        [JsonProperty("providerData")] public List<ProviderDatum> ProviderData;
        [JsonProperty("apiKey")] public string ApiKey;
        [JsonProperty("appName")] public string AppName;
        [JsonProperty("authDomain")] public string AuthDomain;
        [JsonProperty("stsTokenManager")] public StsTokenManager StsTokenManager;
        [JsonProperty("redirectEventId")] public string RedirectEventId;
        [JsonProperty("lastLoginAt")] public string LastLoginAt;
        [JsonProperty("createdAt")] public string CreatedAt;
        [JsonProperty("multiFactor")] public MultiFactor MultiFactor;

        [JsonProperty("userId")] public string RedirectUserId;

        public string GetUserId()
        {
            return string.IsNullOrEmpty(Uid) ? RedirectUserId : Uid;
        }
    }
}