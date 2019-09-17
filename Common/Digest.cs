using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OrdinaryDumpDeduplicator.Common
{
    public class Digest : IEquatable<Digest>
    {
        public const int LENGTH = 20;

        private readonly byte[] _sha1;

        public Digest(byte[] sha1Digest)
        {
            if (sha1Digest == null)
            {

            }

            if (sha1Digest.Length != 20)
            {

            }

            _sha1 = sha1Digest;
        }

        public Digest(IEnumerable<byte> sha1Digest) : this(sha1Digest.ToArray())
        {
        }

        public bool Equals(Digest other)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
