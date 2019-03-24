using System;
using System.Collections.Generic;
using System.Text;

namespace Fileicsh.Abstraction.Helpers
{
    internal static class FileHelpers
    {
        private sealed class FileEqualityComparer : IEqualityComparer<IFileInfo>
        {
            public bool Equals(IFileInfo x, IFileInfo y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                return string.Equals(x.FileName, y.FileName);
            }

            public int GetHashCode(IFileInfo obj)
            {
                unchecked
                {
                    return (obj.FileName.GetHashCode() * 397);
                }
            }
        }

        /// <summary>
        /// An <see cref="IEqualityComparer{T}"/> for <see cref="IFileInfo"/> which will
        /// compare two files' file names.
        /// </summary>
        public static readonly IEqualityComparer<IFileInfo> FileComparer = new FileEqualityComparer();
    }
}
