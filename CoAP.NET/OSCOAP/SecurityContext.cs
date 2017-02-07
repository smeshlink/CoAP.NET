using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PeterO.Cbor;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Com.AugustCellars.COSE;

namespace CoAP.OSCOAP
{
#if INCLUDE_OSCOAP
    public class SecurityContext
    {
        public class replayWindow
        {
            BitArray _hits;
            Int64 _baseValue;

            public replayWindow(int baseValue, int arraySize)
            {
                _baseValue = baseValue;
                _hits = new BitArray(arraySize);
            }

            public bool HitTest(Int64 index)
            {
                index -= _baseValue;
                if (index < 0) return true;
                if (index > _hits.Length) return false;
                return _hits.Get((int)index);
            }

            public void SetHit(Int64 index)
            {
                index -= _baseValue;
                if (index < 0) return;
                if (index > _hits.Length) {
                    if (index > _hits.Length * 3 / 2) {
                        int v = _hits.Length / 2;
                        _baseValue += v;
                        BitArray t = new BitArray(_hits.Length);
                        for (int i = 0; i < v; i++) t[i] = _hits[i + v];
                        _hits = t;
                        index -= v;
                    }
                    else {
                        _baseValue = index;
                        _hits.SetAll(false);
                        index = 0;
                    }
                }
                _hits.Set((int)index, true);
            }
        }

        public class EntityContext
        {
            CBORObject _algorithm;
            byte[] _baseIV;
            byte[] _key;
            byte[] _id;
            int _sequenceNumber;
            replayWindow _replay;

            public CBORObject Algorithm
            {
                get { return _algorithm; }
                set { _algorithm = value; }
            }
            public byte[] BaseIV { get { return _baseIV; } set { _baseIV = value; } }
            public byte[] Id { get { return _id; } set { _id = value; } }
            public byte[] Key { get { return _key; } set { _key = value; } }
            public int SequenceNumber { get { return _sequenceNumber; } set { _sequenceNumber = value; } }
            public byte[] PartialIV
            {
                get
                {
                    byte[] part = BitConverter.GetBytes(_sequenceNumber);
                    if (BitConverter.IsLittleEndian) Array.Reverse(part);
                    int i;
                    for (i = 0; i < part.Length - 1; i++) if (part[i] != 0) break;
                    Array.Copy(part, i, part, 0, part.Length - i);
                    Array.Resize(ref part, part.Length - i);

                    return part;
                }
            }

            public CBORObject GetIV(CBORObject partialIV)
            {
                return GetIV(partialIV.GetByteString());
            }
            public CBORObject GetIV(byte[] partialIV)
            {
                byte[] IV = (byte[])_baseIV.Clone();
                int offset = IV.Length - partialIV.Length;

                for (int i = 0; i < partialIV.Length; i++) IV[i + offset] ^= partialIV[i];

                return CBORObject.FromObject(IV);
            }
            public replayWindow ReplayWindow { get { return _replay; } set { _replay = value; } }
            public void IncrementSequenceNumber() { _sequenceNumber += 1; }
        }

        static int ContextNumber = 0;
        int _contextNo;
        public int ContextNo { get { return _contextNo; } }

        EntityContext _sender = new EntityContext();
        public EntityContext Sender { get { return _sender; } }

        EntityContext _recipient = new EntityContext();
        public EntityContext Recipient { get { return _recipient; } }

        byte[] _Cid;
        public byte[] Cid { get { return _Cid; } set { _Cid = value; } }

        public static SecurityContext DeriveContext(byte[] Cid, byte[] MasterSecret, byte[] SenderId, byte[] RecipientId, CBORObject AEADAlg = null)
        {
            SecurityContext ctx = new SecurityContext();
            ctx.Cid = Cid;
            if (AEADAlg == null) ctx.Sender.Algorithm = AlgorithmValues.AES_CCM_64_64_128;
            else ctx.Sender.Algorithm = AEADAlg;
            if (SenderId == null) throw new ArgumentNullException("SenderId");
            ctx.Sender.Id = SenderId;
            ctx.Recipient.Algorithm = ctx.Sender.Algorithm;
            if (RecipientId == null) throw new ArgumentNullException("RecipientId");
            ctx.Recipient.Id = RecipientId;
            ctx.Recipient.ReplayWindow = new replayWindow(0, 64);

            CBORObject info = CBORObject.NewArray();
            info.Add(Cid);
            info.Add(SenderId);
            info.Add(ctx.Sender.Algorithm);
            info.Add("Key");
            info.Add(128);

            IDigest sha256 = new Sha256Digest();
            IDerivationFunction hkdf = new HkdfBytesGenerator(sha256);
            hkdf.Init(new HkdfParameters(MasterSecret, null, info.EncodeToBytes()));

            ctx.Sender.Key = new byte[128/8];
            hkdf.GenerateBytes(ctx.Sender.Key, 0, ctx.Sender.Key.Length);

            info[1] = CBORObject.FromObject(RecipientId);
            hkdf.Init(new HkdfParameters(MasterSecret, null, info.EncodeToBytes()));
            ctx.Recipient.Key = new byte[128/8];
            hkdf.GenerateBytes(ctx.Recipient.Key, 0, ctx.Recipient.Key.Length);
 
            info[3] = CBORObject.FromObject("IV");
            info[4] = CBORObject.FromObject(56);
            hkdf.Init(new HkdfParameters(MasterSecret, null, info.EncodeToBytes()));
            ctx.Recipient.BaseIV = new byte[56/8];
            hkdf.GenerateBytes(ctx.Recipient.BaseIV, 0, ctx.Recipient.BaseIV.Length);

            info[1] = CBORObject.FromObject(SenderId);
            hkdf.Init(new HkdfParameters(MasterSecret, null, info.EncodeToBytes()));
            ctx.Sender.BaseIV = new byte[56/8];
            hkdf.GenerateBytes(ctx.Sender.BaseIV, 0, ctx.Sender.BaseIV.Length);

            ctx._contextNo = SecurityContext.ContextNumber;
            SecurityContext.ContextNumber += 1;

            return ctx;
        }
    }
#endif
}
