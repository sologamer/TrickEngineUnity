using System;
using System.Linq;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using UnityEngine;

namespace TrickCore
{
    public static class ECDHUtil
    {
        /// <summary>
        /// Initialization Vector size in bytes.
        /// </summary>
        private const int IvSize =  16;

        /// <summary>
        /// Encrypt a message. The message includes the unique IV and the encrypted message
        /// </summary>
        /// <param name="sharedKey"></param>
        /// <param name="message"></param>
        /// <param name="encryptedMessageWithIv"></param>
        /// <param name="iv"></param>
        public static bool EncryptMessage(byte[] sharedKey, byte[] message, out byte[] encryptedMessageWithIv)
        {
            try
            {
                //AesLightEngine engine = new AesLightEngine();
                AesEngine engine = new AesEngine();
                PaddedBufferedBlockCipher cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(engine), new Pkcs7Padding());
                KeyParameter keyParam = new KeyParameter(sharedKey);
            
                SecureRandom random = new SecureRandom();
                byte[] iv = new byte[IvSize];
                random.NextBytes(iv);

                ParametersWithIV keyParamWithIv = new ParametersWithIV(keyParam, iv, 0, IvSize);

                // Encrypt
                cipher.Init(true, keyParamWithIv);
                byte[] encryptedMessage = new byte[cipher.GetOutputSize(message.Length)];
                int outputMessageSize = cipher.ProcessBytes(message, encryptedMessage, 0);
                outputMessageSize += cipher.DoFinal(encryptedMessage, outputMessageSize); //Do the final block
            
                encryptedMessageWithIv = new byte[IvSize + outputMessageSize];
                // Put IV in the byte array
                Buffer.BlockCopy(iv, 0, encryptedMessageWithIv, 0, IvSize);
                // Put the encrypted data in byte array
                Buffer.BlockCopy(encryptedMessage, 0, encryptedMessageWithIv, IvSize, outputMessageSize);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                encryptedMessageWithIv = null;
                return false;
            }
        }

        public static byte[] DecryptMessage(byte[] sharedKey, byte[] encryptedMessageWithIv)
        {
            if (encryptedMessageWithIv.Length - IvSize <= 0)
            {
                return null;
            }

            //AesLightEngine engine = new AesLightEngine();
            AesEngine engine = new AesEngine();
            CbcBlockCipher blockCipher = new CbcBlockCipher(engine); //CBC
            PaddedBufferedBlockCipher cipher = new PaddedBufferedBlockCipher(blockCipher, new Pkcs7Padding()); //Default scheme is PKCS5/PKCS7
            KeyParameter keyParam = new KeyParameter(sharedKey);

            var iv = new byte[IvSize];
            Buffer.BlockCopy(encryptedMessageWithIv, 0, iv, 0, IvSize);

            ParametersWithIV keyParamWithIv = new ParametersWithIV(keyParam, iv, 0, IvSize);

            //Decrypt            
            cipher.Init(false, keyParamWithIv);
            int outputSize = encryptedMessageWithIv.Length - iv.Length;
            byte[] encryptedMessage = new byte[outputSize];
            Buffer.BlockCopy(encryptedMessageWithIv, iv.Length, encryptedMessage, 0, outputSize);
            byte[] decryptMessage = new byte[cipher.GetOutputSize(outputSize)];

            int blockSize = cipher.GetBlockSize();
            int steps = decryptMessage.Length / blockSize;
            int bytesProcessed = 0;
            for (int i = 0; i < steps; i++)
            {
                bytesProcessed += cipher.ProcessBytes(encryptedMessage, i * blockSize, blockSize, decryptMessage, bytesProcessed);
            }
            bytesProcessed += cipher.DoFinal(decryptMessage, bytesProcessed); //Do the final block

            // Same length
            if (decryptMessage.Length == bytesProcessed)
            {
                return decryptMessage;
            }

            // Length mismatch, we create a new array of the processed bytes (remove padding)
            byte[] outputBytes = new byte[bytesProcessed];
            Buffer.BlockCopy(decryptMessage, 0, outputBytes, 0, bytesProcessed);

            return outputBytes;
        }
    }
}