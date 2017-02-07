using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;

namespace CoAP.OSCOAP
{
#if INCLUDE_OSCOAP
    /**
    * HMAC-based Extract-and-Expand Key Derivation Function (HKDF) implemented
    * according to IETF RFC 5869, May 2010 as specified by H. Krawczyk, IBM
    * Research &amp; P. Eronen, Nokia. It uses a HMac internally to compute de OKM
    * (output keying material) and is likely to have better security properties
    * than KDF's based on just a hash function.
    */
    public class HkdfBytesGenerator
        : IDerivationFunction
    {
        private HMac hMacHash;
        private int hashLen;

        private byte[] info;
        private byte[] currentT;

        private int generatedBytes;

        /**
         * Creates a HKDFBytesGenerator based on the given hash function.
         *
         * @param hash the digest to be used as the source of generatedBytes bytes
         */
        public HkdfBytesGenerator(IDigest hash)
        {
            this.hMacHash = new HMac(hash);
            this.hashLen = hash.GetDigestSize();
        }

        public virtual void Init(IDerivationParameters parameters)
        {
            if (!(parameters is HkdfParameters))
                throw new ArgumentException("HKDF parameters required for HkdfBytesGenerator", "parameters");

            HkdfParameters hkdfParameters = (HkdfParameters)parameters;
            if (hkdfParameters.SkipExtract) {
                // use IKM directly as PRK
                hMacHash.Init(new KeyParameter(hkdfParameters.GetIkm()));
            }
            else {
                hMacHash.Init(Extract(hkdfParameters.GetSalt(), hkdfParameters.GetIkm()));
            }

            info = hkdfParameters.GetInfo();

            generatedBytes = 0;
            currentT = new byte[hashLen];
        }

        /**
         * Performs the extract part of the key derivation function.
         *
         * @param salt the salt to use
         * @param ikm  the input keying material
         * @return the PRK as KeyParameter
         */
        private KeyParameter Extract(byte[] salt, byte[] ikm)
        {
            hMacHash.Init(new KeyParameter(ikm));
            if (salt == null) {
                // TODO check if hashLen is indeed same as HMAC size
                hMacHash.Init(new KeyParameter(new byte[hashLen]));
            }
            else {
                hMacHash.Init(new KeyParameter(salt));
            }

            hMacHash.BlockUpdate(ikm, 0, ikm.Length);

            byte[] prk = new byte[hashLen];
            hMacHash.DoFinal(prk, 0);
            return new KeyParameter(prk);
        }

        /**
         * Performs the expand part of the key derivation function, using currentT
         * as input and output buffer.
         *
         * @throws DataLengthException if the total number of bytes generated is larger than the one
         * specified by RFC 5869 (255 * HashLen)
         */
        private void ExpandNext()
        {
            int n = generatedBytes / hashLen + 1;
            if (n >= 256) {
                throw new DataLengthException(
                    "HKDF cannot generate more than 255 blocks of HashLen size");
            }
            // special case for T(0): T(0) is empty, so no update
            if (generatedBytes != 0) {
                hMacHash.BlockUpdate(currentT, 0, hashLen);
            }
            hMacHash.BlockUpdate(info, 0, info.Length);
            hMacHash.Update((byte)n);
            hMacHash.DoFinal(currentT, 0);
        }

        public virtual IDigest Digest {
            get { return hMacHash.GetUnderlyingDigest(); }
        }

        public virtual int GenerateBytes(byte[] output, int outOff, int len)
        {
            if (generatedBytes + len > 255 * hashLen) {
                throw new DataLengthException(
                    "HKDF may only be used for 255 * HashLen bytes of output");
            }

            if (generatedBytes % hashLen == 0) {
                ExpandNext();
            }

            // copy what is left in the currentT (1..hash
            int toGenerate = len;
            int posInT = generatedBytes % hashLen;
            int leftInT = hashLen - generatedBytes % hashLen;
            int toCopy = System.Math.Min(leftInT, toGenerate);
            Array.Copy(currentT, posInT, output, outOff, toCopy);
            generatedBytes += toCopy;
            toGenerate -= toCopy;
            outOff += toCopy;

            while (toGenerate > 0) {
                ExpandNext();
                toCopy = System.Math.Min(hashLen, toGenerate);
                Array.Copy(currentT, 0, output, outOff, toCopy);
                generatedBytes += toCopy;
                toGenerate -= toCopy;
                outOff += toCopy;
            }

            return len;
        }
    }

    /**
      * Parameter class for the HkdfBytesGenerator class.
      */
    public class HkdfParameters
        : IDerivationParameters
    {
        private readonly byte[] ikm;
        private readonly bool skipExpand;
        private readonly byte[] salt;
        private readonly byte[] info;

        private HkdfParameters(byte[] ikm, bool skip, byte[] salt, byte[] info)
        {
            if (ikm == null)
                throw new ArgumentNullException("ikm");

            this.ikm = Arrays.Clone(ikm);
            this.skipExpand = skip;

            if (salt == null || salt.Length == 0) {
                this.salt = null;
            }
            else {
                this.salt = Arrays.Clone(salt);
            }

            if (info == null) {
                this.info = new byte[0];
            }
            else {
                this.info = Arrays.Clone(info);
            }
        }

        /**
         * Generates parameters for HKDF, specifying both the optional salt and
         * optional info. Step 1: Extract won't be skipped.
         *
         * @param ikm  the input keying material or seed
         * @param salt the salt to use, may be null for a salt for hashLen zeros
         * @param info the info to use, may be null for an info field of zero bytes
         */
        public HkdfParameters(byte[] ikm, byte[] salt, byte[] info)
            : this(ikm, false, salt, info)
        {
        }

        /**
         * Factory method that makes the HKDF skip the extract part of the key
         * derivation function.
         *
         * @param ikm  the input keying material or seed, directly used for step 2:
         *             Expand
         * @param info the info to use, may be null for an info field of zero bytes
         * @return HKDFParameters that makes the implementation skip step 1
         */
        public static HkdfParameters SkipExtractParameters(byte[] ikm, byte[] info)
        {
            return new HkdfParameters(ikm, true, null, info);
        }

        public static HkdfParameters DefaultParameters(byte[] ikm)
        {
            return new HkdfParameters(ikm, false, null, null);
        }

        /**
         * Returns the input keying material or seed.
         *
         * @return the keying material
         */
        public virtual byte[] GetIkm()
        {
            return Arrays.Clone(ikm);
        }

        /**
         * Returns if step 1: extract has to be skipped or not
         *
         * @return true for skipping, false for no skipping of step 1
         */
        public virtual bool SkipExtract {
            get { return skipExpand; }
        }

        /**
         * Returns the salt, or null if the salt should be generated as a byte array
         * of HashLen zeros.
         *
         * @return the salt, or null
         */
        public virtual byte[] GetSalt()
        {
            return Arrays.Clone(salt);
        }

        /**
         * Returns the info field, which may be empty (null is converted to empty).
         *
         * @return the info field, never null
         */
        public virtual byte[] GetInfo()
        {
            return Arrays.Clone(info);
        }
    }
#endif
}
