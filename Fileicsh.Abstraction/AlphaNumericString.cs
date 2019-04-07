using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fileicsh.Abstraction
{
    /// <summary>
    /// Represents a non-null string is either empty or only contains letters or digits.
    /// </summary>
    public struct AlphaNumericString : IEquatable<AlphaNumericString>, IEnumerable<char>
    {
        /// <summary>
        /// Returns an <see cref="AlphaNumericString"/> representing an empty string.
        /// </summary>
        public static readonly AlphaNumericString Empty = new AlphaNumericString(string.Empty);

        private readonly string _s;

        /// <summary>
        /// Returns a non-null string that is either empty or a string
        /// that only contains letters or digits.
        /// </summary>
        /// <returns>
        /// A string that is either empty or that only contains letters or digits.
        /// </returns>
        public string GetValue()
        {
            if (string.IsNullOrWhiteSpace(_s))
            {
                return string.Empty;
            }

            return string.Join(string.Empty, _s.Where(char.IsLetterOrDigit));
        }

        /// <summary>
        /// Returns a flag indicating whether the given object is equal to this
        /// alpha numerical string.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is AlphaNumericString && Equals((AlphaNumericString)obj);
        }

        /// <summary>
        /// Returns a flag indicating whether the given alpha numerical string
        /// is equal to this alpha numerical string.
        /// </summary>
        public bool Equals(AlphaNumericString other)
        {
            return GetValue() == other.GetValue();
        }

        /// <summary>
        /// Returns the hash code of the alpha numerical string.
        /// </summary>
        public override int GetHashCode()
        {
            return -424979009 + EqualityComparer<string>.Default.GetHashCode(GetValue());
        }

        public IEnumerator<char> GetEnumerator() => GetValue().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Creates an alpha numerical string of the given string <paramref name="s"/>.
        /// </summary>
        /// <param name="s">The string to convert to an alpha numerical string.</param>
        public AlphaNumericString(string s)
        {
            _s = s;
        }

        /// <summary>
        /// Returns the string representation of the given alpha numerical string <paramref name="s"/>.
        /// </summary>
        /// <param name="s">The alpha numerical string to convert to <see cref="string"/>.</param>
        public static implicit operator string(AlphaNumericString s) => s.GetValue();

        /// <summary>
        /// Concats two alpha numerical strings together and returns a new alpha numerical string.
        /// </summary>
        public static AlphaNumericString operator +(AlphaNumericString s1, AlphaNumericString s2)
        {
            return new AlphaNumericString(s1.GetValue() + s2.GetValue());
        }

        /// <summary>
        /// Returns the length of the string.
        /// </summary>
        public int Length => GetValue().Length;

        public static bool operator ==(AlphaNumericString string1, AlphaNumericString string2)
        {
            return string1.Equals(string2);
        }

        public static bool operator !=(AlphaNumericString string1, AlphaNumericString string2)
        {
            return !(string1 == string2);
        }
        public AlphaNumericString Substring(int startIndex)
        {
            return new AlphaNumericString(GetValue().Substring(startIndex));
        }

        public AlphaNumericString Substring(int startIndex, int length)
        {
            return new AlphaNumericString(GetValue().Substring(startIndex, length));
        }

        public bool StartsWith(AlphaNumericString str) => str
            .GetValue()
            .StartsWith(str.GetValue());

        public AlphaNumericString ToLower() => new AlphaNumericString(GetValue().ToLower());

        public AlphaNumericString PadRight(int totalWidth, char paddingChar)
        {
            if (!char.IsLetterOrDigit(paddingChar))
            {
                throw new ArgumentException("The padding char must be a letter or a digit.");
            }

            return new AlphaNumericString(GetValue().PadRight(totalWidth, paddingChar));
        }

        public static AlphaNumericString Join(AlphaNumericString separator, IEnumerable<AlphaNumericString> values)
        {
            return new AlphaNumericString(string.Join(separator.GetValue(), values.Select(v => v.GetValue())));
        }

        public override string ToString() => GetValue();
    }
}
