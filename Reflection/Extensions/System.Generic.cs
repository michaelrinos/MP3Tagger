using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Reflection {
	public static partial class SystemExtensions
	{
		public static bool IsEmpty<T>(this List<T> list)
		{
			return (list == null || list.Count == 0);
		}

		public static bool IsNotEmpty<T>(this List<T> list)
		{
			return (list.IsEmpty() == false);
		}

		public static bool IsEmpty<T, V>(this Dictionary<T, V> dict)
		{
			return (dict == null || dict.Count == 0);
		}

		public static bool IsNotEmpty<T, V>(this Dictionary<T, V> dict)
		{
			return (dict.IsEmpty() == false);
		}

		/// <summary>
		///     Evaluates if a values is between an lower and upper bounds
		/// </summary>
		/// <typeparam name="T">Comparable object</typeparam>
		/// <param name="value">Value to evaluate</param>
		/// <param name="lowerBound">Lower bound</param>
		/// <param name="upperBound">Upper bound</param>
		/// <param name="inclusive">Include lower and upper bounds values</param>
		/// <returns>Next enumeration item of <typeparamref name="T" /></returns>
		public static bool IsBetween<T>(this T value, T lowerBound, T upperBound, bool inclusive = false)
			where T : IComparable
		{
			if(value == null)
				return false;

			return inclusive ?
				value.CompareTo(lowerBound) >= 0 && value.CompareTo(upperBound) <= 0 :
				value.CompareTo(lowerBound) > 0 && value.CompareTo(upperBound) < 0;
		}

		/// <summary>
		///     Evaluates if a values is between an lower and upper bounds
		/// </summary>
		/// <typeparam name="T">Comparable object</typeparam>
		/// <param name="value">Null-able value to evaluate</param>
		/// <param name="lowerBound">Lower bound</param>
		/// <param name="upperBound">Upper bound</param>
		/// <param name="inclusive">Include lower and upper bounds values</param>
		/// <returns>Next enumeration item of <typeparamref name="T" /></returns>
		public static bool IsBetween<T>(this T? value, T lowerBound, T upperBound, bool inclusive = false)
			where T : struct
		{
			if (!value.HasValue)
				return false;

			return inclusive ?
				Nullable.Compare(value, lowerBound) >= 0 && Nullable.Compare(value, upperBound) <= 0 :
				Nullable.Compare(value, lowerBound) > 0 && Nullable.Compare(value, upperBound) < 0;
		}

		public static IEnumerable<IEnumerable<T>> GroupWhile<T>(this IEnumerable<T> seq, Func<T, T, bool> condition)
		{
			T prev = seq.First();
			List<T> list = new List<T>() { prev };

			foreach (T item in seq.Skip(1))
			{
				if (condition(prev, item) == false)
				{
					yield return list;
					list = new List<T>();
				}
				list.Add(item);
				prev = item;
			}

			yield return list;
		}

		public static void Shuffle<T>(this IList<T> list)
		{
			RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
			int n = list.Count;
			while (n > 1)
			{
				byte[] box = new byte[1];
				do provider.GetBytes(box);
				while (!(box[0] < n * (Byte.MaxValue / n)));
				int k = (box[0] % n);
				n--;
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

	}
}
