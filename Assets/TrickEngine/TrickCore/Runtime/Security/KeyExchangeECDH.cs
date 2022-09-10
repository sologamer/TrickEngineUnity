using System;
using System.Linq;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using ECPoint = Org.BouncyCastle.Math.EC.ECPoint;

namespace TrickCore
{
    /// <summary>
    /// Key exchange class using an Elliptic Curve Diffie Hellman (ECDH) CNG.
    /// </summary>
    public class KeyExchangeECDH : IKeyExchange
    {
        const string KeyPairAlgorithm = "ECDH"; 
        const string Algorithm = "ECDH";
        const int KeyBitSize = 256;
        const int DefaultPrimeProbability = 30;

        /// <summary>
        /// The public key will be sent to the target which can then generate their '<see cref="_targetSharedKey"/>'
        /// </summary>
        private byte[] _myPublicKey;

        /// <summary>
        /// This is my shared key
        /// </summary>
        private byte[] _mySharedKey;

        /// <summary>
        /// This is the shared key of the target/receiver.
        /// </summary>
        private byte[] _targetSharedKey;

        /// <summary>
        /// The ECDH algoithm
        /// </summary>
        private IBasicAgreement _myECDHCng;

        /// <summary>
        /// Cached value
        /// </summary>
        private bool _cachedIsExchanged;

        /// <summary>
        /// Returns true if my '<see cref="_mySharedKey"/>' is the same as the '<see cref="_targetSharedKey"/>'
        /// </summary>
        public bool IsExchanged
        {
            get
            {
                if (_cachedIsExchanged) return true;
                if (_targetSharedKey == null) return false;
                bool isSame = _mySharedKey.SequenceEqual(_targetSharedKey);
                if (isSame) _cachedIsExchanged = true;
                return _cachedIsExchanged;
            }
        }

        /// <summary>
        /// Gets if key share is finished.
        /// </summary>
        public bool KeyShareFinished { get; set; }

        /// <summary>
        /// Initialize the ECDH object.
        /// </summary>
        public void Initialize()
        {
            ECKeyPairGenerator gen = new ECKeyPairGenerator("ECDH");
            SecureRandom secureRandom = new SecureRandom();
            X9ECParameters ecp = NistNamedCurves.GetByName("P-256");
            ECDomainParameters ecSpec = new ECDomainParameters(ecp.Curve, ecp.G, ecp.N, ecp.H, ecp.GetSeed());
            ECKeyGenerationParameters ecgp = new ECKeyGenerationParameters(ecSpec, secureRandom);
            gen.Init(ecgp);
            AsymmetricCipherKeyPair eckp = gen.GenerateKeyPair();
            ECPublicKeyParameters ecPub = (ECPublicKeyParameters)eckp.Public;
            ECPrivateKeyParameters ecPri = (ECPrivateKeyParameters)eckp.Private;
            _myPublicKey = ecPub.Q.GetEncoded(false);
            IBasicAgreement keyAgreement = AgreementUtilities.GetBasicAgreement(Algorithm);
            keyAgreement.Init(ecPri);
            _myECDHCng = keyAgreement;
        }

        /// <summary>
        /// Gets the shared secret.
        /// </summary>
        /// <param name="publicKeyIn">The public key in.</param>
        /// <param name="privateKey"></param>
        /// <returns>Byte[].</returns>
        private byte[] GetSharedSecret(byte[] publicKeyIn)
        {
            X9ECParameters              curve                 = null;
            ECDomainParameters          ecParam               = null;
            ECPublicKeyParameters       pubKey                = null;
            ECPoint                     point                 = null;

            curve     = NistNamedCurves.GetByName( "P-256" );
            ecParam   = new ECDomainParameters( curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed() );
            point     = ecParam.Curve.DecodePoint( publicKeyIn );
            pubKey    = new ECPublicKeyParameters( point, ecParam );

            BigInteger secret = _myECDHCng.CalculateAgreement( pubKey );

            return secret.ToByteArrayUnsigned();
        }
        
        /// <summary>
        /// Finish generating <see cref="_mySharedKey"/> using the targets public key.
        /// </summary>
        /// <param name="targetPublicKey">The target public key</param>
        public void Finish(byte[] targetPublicKey)
        {
            if (targetPublicKey == null) throw new ArgumentNullException(nameof(targetPublicKey));
            _mySharedKey = GetSharedSecret(targetPublicKey);
        }

        /// <summary>
        /// Set key share finished
        /// </summary>
        public void SetKeyShareFinished()
        {
            KeyShareFinished = true;
        }

        /// <summary>
        /// Reset the key exchange object
        /// </summary>
        public void Reset()
        {
            _myPublicKey = null;
            _mySharedKey = null;
            _targetSharedKey = null;
            KeyShareFinished = false;
            _myECDHCng = null;
            _cachedIsExchanged = false;
        }

        /// <inheritdoc />
        public byte[] GetMyPublicKey()
        {
            return _myPublicKey;
        }

        /// <inheritdoc />
        public byte[] GetMySharedKey()
        {
            return _mySharedKey;
        }

        /// <inheritdoc />
        public void SetTargetSharedKey(byte[] sharedKey)
        {
            _targetSharedKey = sharedKey;
        }

        public byte[] DecryptMessage(byte[] sharedKey, byte[] encryptedMessage)
        {
            return ECDHUtil.DecryptMessage(sharedKey, encryptedMessage);
        }

        public bool EncryptMessage(byte[] sharedKey, byte[] message, out byte[] encryptedMessage)
        {
            return ECDHUtil.EncryptMessage(sharedKey, message, out encryptedMessage);
        }
    }
}