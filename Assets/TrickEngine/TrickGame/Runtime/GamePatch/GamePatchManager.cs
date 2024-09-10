using System;
using UnityEngine;

namespace TrickCore
{
    public abstract class GamePatchManager<TInstance, TPatch> : MonoSingleton<TInstance> where
        TInstance : GamePatchManager<TInstance, TPatch>, new() where
        TPatch : IGamePatch, new()
    {
        [field: SerializeField] public TPatch DefaultPatch { get; private set; } = new TPatch();

        public TPatch Current { get; protected set; } = new TPatch();

        /// <summary>
        /// A function to unpack the patch data from bytes. This is used when applying a patch file
        /// </summary>
        public Func<byte[], TPatch> UnpackPatchFunc { get; set; } = DefaultUnpacker;

        /// <summary>
        /// A function to pack the patch data to bytes. This is used when creating a patch file
        /// </summary>
        public Func<TPatch, int, byte[]> PackPatchToBytesFunc { get; set; } = DefaultPacker;

        /// <summary>
        /// The pack processor to use for packing and unpacking the patch data (lz4, zlib, etc).
        /// Defaults to null, which will use the default json base64 packing and unpacking
        /// </summary>
        public IPackProcessor<TPatch> PackProcessor { get; set; } = null;

        protected override void Initialize()
        {
            base.Initialize();

            UnpackPatchFunc ??= DefaultUnpacker;
            PackPatchToBytesFunc ??= DefaultPacker;

            Current = DefaultPatch;
        }

        public void ApplyPatchData(byte[] patchData) => Current = PackProcessor != null ? PackProcessor.UnpackToObject(patchData) : UnpackPatchFunc(patchData);
        public void ApplyPatchData(TPatch patchData) => Current = patchData;
        public void ApplyDefaultPatchData() => Current = DefaultPatch;
        protected void SetDefaultPatch(TPatch patchData) => DefaultPatch = patchData;

        public byte[] CreatePatchData(TPatch patchData, int version)
        {
            if (PackProcessor != null)
            {
                return PackProcessor.PackToBytes(patchData);
            }
            
            var packedPatchData = PackPatchToBytesFunc(patchData, version);
            return packedPatchData;
        }

        public static TPatch DefaultUnpacker(byte[] patchData)
        {
            var packedPatchData = Convert.ToBase64String(patchData).DeserializeJsonBase64<PackedGamePatchData>();
            return packedPatchData.IsPatchDataValid() ? packedPatchData.UnpackPatchData(Instance.PackProcessor) : Instance.DefaultPatch;
        }

        public static byte[] DefaultPacker(TPatch patch, int version)
        {
            PackedGamePatchData packedPatch = new PackedGamePatchData();

            var bytes = Instance != null && Instance.PackProcessor != null
                ? Instance.PackProcessor.PackToBytes(patch)
                : Convert.FromBase64String(patch.SerializeToJsonBase64());

            packedPatch.PatchData = bytes;
            packedPatch.Hash = packedPatch.GetPatchDataHash();
            packedPatch.PatchVersion = version;
            return Convert.FromBase64String(packedPatch.SerializeToJsonBase64());
        }
    }
}