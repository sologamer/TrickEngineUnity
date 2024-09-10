using System;
using System.Text;
using Newtonsoft.Json;

namespace TrickCore
{
    [JsonObject]
    public class PackedGamePatchData
    {
        [JsonProperty("patchData")] public byte[] PatchData { get; set; }
        [JsonProperty("hash")] public string Hash { get; set; }
        [JsonProperty("patchVersion")] public int PatchVersion { get; set; }

        /// <summary>
        /// Checks if the patch data is empty
        /// </summary>
        public bool IsEmpty()
        {
            return PatchData == null || PatchData.Length == 0;
        }

        /// <summary>
        /// Checks if the patch data is valid
        /// </summary>
        public bool IsPatchDataValid()
        {
            return !string.IsNullOrEmpty(Hash) && PatchData != null && PatchData.Length > 0 && GetPatchDataHash() == Hash;
        }

        /// <summary>
        /// Unpacks the patch data using the pack processor if it is not null, otherwise it will use the default json base64 unpacking
        /// </summary>
        /// <param name="packProcessor"></param>
        /// <typeparam name="TPatch"></typeparam>
        /// <returns></returns>
        public TPatch UnpackPatchData<TPatch>(IPackProcessor<TPatch> packProcessor) where TPatch : IGamePatch, new()
        {
            if (packProcessor != null) return packProcessor.UnpackToObject(PatchData);
            
            var bytes = PatchData;
            return bytes == null ? default : Convert.ToBase64String(bytes).DeserializeJsonBase64<TPatch>();
        }

        /// <summary>
        /// Gets the hash of the patch data (SHA256). Note that this is not the hash of the patch data itself, but the hash of the string representation of the patch data
        /// </summary>
        /// <returns></returns>
        public string GetPatchDataHash()
        {
            return HashUtil.CreateSHA256(PatchData == null ? string.Empty : Encoding.UTF8.GetString(PatchData));
        }
    }
}