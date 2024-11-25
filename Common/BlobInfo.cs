using System;
using System.Collections.Generic;

namespace OrdinaryDumpDeduplicator.Common
{
    public class BlobInfo
    {
        public const Int32 LENGTH = 20;

        private static BlobInfo _emptyBlobInfo;
        private static BlobInfo _brokenBlobInfo;

        private readonly Int64 _size;
        private readonly Byte[] _sha1Hash;

        private String _hexString;

        #region Constructors and creators

        public BlobInfo(Int64 size, Byte[] sha1Digest)
        {
            if (size < 1)
            {
                throw new ArgumentException("size");
            }

            if (sha1Digest == null)
            {
                throw new ArgumentNullException(nameof(sha1Digest));
            }

            if (sha1Digest.Length != 20)
            {
                throw new ArgumentException("sha1Digest");
            }

            _size = size;
            _sha1Hash = sha1Digest;
            _hexString = null;
        }

        public BlobInfo(Int64 size, IEnumerable<Byte> sha1Digest) : this(size, MakeArray(sha1Digest)) { }

        private BlobInfo(Int64 size)
        {
            this._size = size;
            if (_size == 0)
            {
                this._sha1Hash = new Byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            }
            else if (_size == -1)
            {
                // da39a3ee5e6b4b0d3255bfef95601890afd80709
                this._sha1Hash = new Byte[] { 218, 57, 163, 238, 94, 107, 75, 13, 50, 85, 191, 239, 149, 96, 24, 144, 175, 216, 7, 9 }; // Empty hash.
            }
            else
            {
                throw new ArgumentException("size");
            }
        }

        public static BlobInfo Create(Int64 size, Byte[] sha1Digest)
        {
            BlobInfo blobInfo;

            if (size > 0)
            {
                blobInfo = new BlobInfo(size, sha1Digest);
            }
            else if (size == 0)
            {
                blobInfo = EmptyBlobInfo;
            }
            else
            {
                blobInfo = BrokenBlobInfo;
            }

            return blobInfo;
        }

        #endregion

        #region Public properties

        public static BlobInfo EmptyBlobInfo
        {
            get
            {
                if (_emptyBlobInfo == null)
                {
                    _emptyBlobInfo = new BlobInfo(size: 0);
                }

                return _emptyBlobInfo;
            }
        }

        public static BlobInfo BrokenBlobInfo
        {
            get
            {
                if (_brokenBlobInfo == null)
                {
                    _brokenBlobInfo = new BlobInfo(size: -1);
                }

                return _brokenBlobInfo;
            }
        }

        public Int64 Size => _size;

        public String HexString
        {
            get
            {
                if (_hexString == null)
                {
                    var hexString = BitConverter.ToString(_sha1Hash);
                    _hexString = hexString.Replace("-", String.Empty);
                }

                return _hexString;
            }
        }

        #endregion

        #region Overrides of object

        public override Boolean Equals(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            BlobInfo other = obj as BlobInfo;
            return Equals(other);
        }

        public Boolean Equals(BlobInfo other)
        {
            if (other == null || this._size != other._size)
            {
                return false;
            }

            if (this._sha1Hash.Equals(other._sha1Hash))
            {
                return true;
            }

            for (int index = 0; index < LENGTH; index++)
            {
                if (!this._sha1Hash[index].Equals(other._sha1Hash[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public override Int32 GetHashCode()
        {
            if (_sha1Hash == null)
            {
                return 0;
            }

            Int32 hash = _size.GetHashCode();
            for (int i = 0; i < 5; i++)
            {
                Int32 hashPart = 0;
                for (int j = 0; j < 4; j++)
                {
                    Int32 index = i * 4 + j;
                    hashPart = hashPart * 256 + _sha1Hash[index];
                }

                hash ^= hashPart;
            }

            return hash;
        }

        public override String ToString()
        {
            var dataSizeString = Helper.GetDataSizeString(_size);

            return $"{dataSizeString} | {HexString}";
        }

        #endregion

        private static Byte[] MakeArray(IEnumerable<Byte> sha1Digest)
        {
            var sha1Hash = new List<Byte>(sha1Digest);
            return sha1Hash.ToArray();
        }
    }
}
