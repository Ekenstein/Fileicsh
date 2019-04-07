using System;
using Fileicsh.Abstraction;
using HeyRed.Mime;

namespace Fileicsh.Tests
{
    public static class Generators
    {
        private static readonly string[] ContentTypes = new[]
        {
            "application/octet-stream",
            "text/plain",
            "image/jpeg",
            "application/javascript",
            "application/json",
            "application/xml"
        };

        /// <summary>
        /// Generates a new <see cref="IFile"/> containing random data, random file name
        /// and a random content type.
        /// </summary>
        /// <param name="random">The randomizer to use to generate the file.</param>
        /// <returns>
        /// A generate <see cref="IFile"/>.
        /// </returns>
        public static IFile NextFile(this Random random)
        {
            var data = random.NextBytes();
            var contentType = random.Next(ContentTypes);
            var fileName = $"{random.NextString(1, 20)}.{MimeTypesMap.GetExtension(contentType)}";
            return new MemoryFile(new FileInfo(fileName, contentType), data);
        }

        public static AlphaNumericString NextTag(this Random random, int minLength = 0, int maxLength = 100)
        {
            return new AlphaNumericString(random.NextString(minLength, maxLength, alphabet: DefaultAlphabet));
        }

        private static byte[] NextBytes(this Random random)
        {
            var length = random.Next(1, 100);
            var buffer = new byte[length];
            random.NextBytes(buffer);
            return buffer;
        }

        public const string DefaultAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        /// <summary>
        /// Returns a random string that will have a length between
        /// <paramref name="min"/> and <paramref name="max"/> containing
        /// chars from the given <paramref name="alphabet"/>.
        /// </summary>
        /// <param name="random">The randomizer to use to generate the string. Default is <see cref="DefaultAlphabet"/>.</param>
        /// <param name="min">The min length of the string. Default is zero.</param>
        /// <param name="max">The max length of the string. Default is 100.</param>
        /// <param name="alphabet">The alphabet to use when generating random characters.</param>
        /// <returns>
        /// A randomized string containing only chars from the given <paramref name="alphabet"/>
        /// and that has a length that is between <paramref name="min"/> and <paramref name="max"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="random"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="alphabet"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="min"/> or <paramref name="max"/> is negative.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="min"/> is larger than <paramref name="max"/>.</exception>
        public static string NextString(this Random random, int min = 0, int max = 100,
            string alphabet = DefaultAlphabet)
        {
            return string.Join(string.Empty, random.NextCollection(alphabet.ToCharArray(), min, max));
        }

        /// <summary>
        /// Generates a new collection of the given collection <paramref name="ts"/>
        /// which will have a length between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        /// <typeparam name="T">The type of element in the collection.</typeparam>
        /// <param name="random">The randomizer to use to generate the new collection.</param>
        /// <param name="ts">The collection to generate a new collection of.</param>
        /// <param name="min">The inclusive lowerbound of the length of the collection. The min value must be smaller or equal to the max value.</param>
        /// <param name="max">The exclusive upperbound of the length of the collection. The max value must be larger or equal to the min value.</param>
        /// <returns>
        /// A generated collection containing elements from the given collection <paramref name="ts"/>,
        /// which will have a length between <paramref name="min"/> and <paramref name="max"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="random"/> or <paramref name="ts"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="min"/> is negative.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="max"/> is negative.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="max"/> is smaller than <paramref name="min"/>.</exception>
        public static T[] NextCollection<T>(this Random random, T[] ts, int min = 0, int max = 100) => random.NextCollection(ts, t => t, min, max);

        /// <summary>
        /// Generates a new collection of the given collection <paramref name="ts"/>, where each element
        /// has been mapped to <typeparamref name="T2"/> and
        /// which will have a length between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        /// <typeparam name="T">The type of element in the given collection.</typeparam>
        /// <typeparam name="T2">The type of element in the generated collection.</typeparam>
        /// <param name="random">The randomizer to use to generate the new collection.</param>
        /// <param name="ts">The collection to generate a new collection of.</param>
        /// <param name="min">The inclusive lowerbound of the length of the collection. The min value must be smaller or equal to the max value.</param>
        /// <param name="max">The exclusive upperbound of the length of the collection. The max value must be larger or equal to the min value.</param>
        /// <param name="map">The function transforming elements from the collection <paramref name="ts"/> to <typeparamref name="T2"/>.</param>
        /// <returns>
        /// A generated collection containing elements from the given collection <paramref name="ts"/> mapped to <typeparamref name="T2"/>,
        /// which will have a length between <paramref name="min"/> and <paramref name="max"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="random"/> or <paramref name="ts"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="min"/> is negative.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="max"/> is negative.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="max"/> is smaller than <paramref name="min"/>.</exception>
        public static T2[] NextCollection<T, T2>(this Random random, T[] ts, Func<T, T2> map, int min = 0,
            int max = 100)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (ts == null)
            {
                throw new ArgumentNullException(nameof(ts));
            }

            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (min < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(min), min, "Min must be larger or equal to zero.");
            }

            if (max < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "Max must be larger or equal to zero.");
            }

            var length = random.Next(min, max);
            var collection = new T2[length];
            for (var i = 0; i < collection.Length; i++)
            {
                var index = random.Next(ts.Length);
                collection[i] = map(ts[index]);
            }

            return collection;
        }

        /// <summary>
        /// Returns a random element out of the collection <paramref name="ts"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="random">The randomizer to use to select a random element out of the collection.</param>
        /// <param name="ts">The collection to pick a random element from.</param>
        /// <returns>
        /// A random element from the given collection <paramref name="ts"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="random"/> or <paramref name="ts"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="ts"/> is empty.</exception>
        public static T Next<T>(this Random random, T[] ts) => random
            .Next(ts, _ => throw new ArgumentOutOfRangeException(nameof(ts), ts.Length, "The length of ts must be larger than zero."));

        /// <summary>
        /// Returns a random element out of the collection <paramref name="ts"/> or the default value
        /// produced if the collection is empty
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="random">The randomizer to use to select a random element out of the collection.</param>
        /// <param name="ts">The collection to pick a random element from.</param>
        /// <param name="defaultValue">The default value to use if the collection is empty.</param>
        /// <returns>
        /// A random element from the given collection <paramref name="ts"/> or the default value
        /// produced if the collection is empty.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="random"/> or <paramref name="ts"/> is null.</exception>
        public static T Next<T>(this Random random, T[] ts, Func<Random, T> defaultValue)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (ts == null)
            {
                throw new ArgumentNullException(nameof(ts));
            }

            if (defaultValue == null)
            {
                throw new ArgumentNullException(nameof(defaultValue));
            }

            if (ts.Length <= 0)
            {
                return defaultValue(random);
            }

            return ts[random.Next(ts.Length)];
        }

        /// <summary>
        /// Returns a random <see cref="DateTime"/>.
        /// </summary>
        /// <param name="random">The randomizer to use to randomize the date and time.</param>
        /// <returns>
        /// A random <see cref="DateTime"/>.
        /// </returns>
        public static DateTime NextDateTime(this Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var year = random.Next(1, 9999 + 1);
            var month = random.Next(1, 12 + 1);
            var day = random.Next(1, DateTime.DaysInMonth(year, month) + 1);
            var hour = random.Next(0, 23 + 1);
            var minute = random.Next(0, 59 + 1);
            var second = random.Next(0, 59 + 1);
            var ms = random.Next(0, 999 + 1);

            return new DateTime(year, month, day, hour, minute, second, ms);
        }

        public static bool NextBool(this Random random) => random.Next(0, 2) == 1;
    }
}
