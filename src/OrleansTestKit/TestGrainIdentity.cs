using System;
using System.Diagnostics;
using Orleans.Core;

namespace Orleans.TestKit
{
    [DebuggerStepThrough]
    public sealed class TestGrainIdentity : IGrainIdentity
    {
        internal enum KeyTypes
        {
            String,
            Guid,
            Long,
            GuidCompound,
            LongCompound
        }

        internal readonly KeyTypes KeyType;

        public Guid PrimaryKey { get; }

        public long PrimaryKeyLong { get; }

        public string PrimaryKeyString { get; }

        public string KeyExtension { get; }

        public string IdentityString
        {
            get
            {
                switch (KeyType)
                {
                    case KeyTypes.String:
                        return PrimaryKeyString;
                    case KeyTypes.Guid:
                        return PrimaryKey.ToString();
                    case KeyTypes.Long:
                        return PrimaryKeyLong.ToString();
                    case KeyTypes.GuidCompound:
                        return $"{PrimaryKey}|{KeyExtension}";
                    case KeyTypes.LongCompound:
                        return $"{PrimaryKeyLong}|{KeyExtension}";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool IsClient { get { throw new NotImplementedException(); } }

        public int TypeCode { get { throw new NotImplementedException(); } }

        public TestGrainIdentity(Guid id, string keyExtension = null)
        {
            PrimaryKey = id;
            KeyType = keyExtension != null ? KeyTypes.GuidCompound : KeyTypes.Guid;
            KeyExtension = keyExtension;
        }

        public TestGrainIdentity(long id, string keyExtension = null)
        {
            PrimaryKeyLong = id;
            KeyType = keyExtension != null ? KeyTypes.LongCompound : KeyTypes.Long;
            KeyExtension = keyExtension;
        }

        public TestGrainIdentity(string id)
        {
            PrimaryKeyString = id;
            KeyType = KeyTypes.String;
        }

        public long GetPrimaryKeyLong(out string keyExt)
        {
            keyExt = KeyExtension;
            return PrimaryKeyLong;
        }

        public Guid GetPrimaryKey(out string keyExt)
        {
            keyExt = KeyExtension;
            return PrimaryKey;
        }

        public uint GetUniformHashCode() 
        {
            throw new NotImplementedException();
        }
    }
}
