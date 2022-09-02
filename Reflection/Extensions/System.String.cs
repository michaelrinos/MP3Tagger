using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Reflection {
	public static partial class SystemExtensions
	{
		private static Regex _htmlPattern = new Regex("<(.|\n)*?>", RegexOptions.Compiled);

		/// <summary>
		/// Prepend the given value with the proper indefinite article
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string AddArticle(this string value)
		{
			return String.Concat(GetArticle(value), " ", value);
		}

		public static bool ContainsHtml(this string value)
		{
			return _htmlPattern.IsMatch(value);
		}

		/// <summary>
		/// Prepend the given value with the proper indefinite article
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string GetArticle(this string value)
		{
			return (new List<char>() { 'a', 'e', 'i', 'o', 'u', }).Contains(value.ToLower()[0]) ? "an" : "a";
		}

		/// <summary>
		/// Returns a string containing the first letter of each significant word
		/// </summary>
		/// <param name="value">The phrase to turn into acronym</param>
		public static string ToAcronym(this string value)
		{
			if (String.IsNullOrWhiteSpace(value))
				return "";

			var result = new StringBuilder();
			var skipWords = new[] { "of", "the", "a", "for" };
			var splitChars = new[] { ' ' };
			foreach (var word in value.Split(splitChars, StringSplitOptions.RemoveEmptyEntries))
			{
				if (!skipWords.Contains(word))
					result.Append(word.Substring(0, 1).ToUpperInvariant());
			}

			return result.ToString();
		}

		/// <summary>
		/// Inserts spaces when case goes from lower to upper or from letter to number
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static string Humanize(this string input)
		{
			if (input == null)
				return "";

			var sb = new StringBuilder();
			var spaceEquivilents = new[] { '_' };
			var uncapitalized = new[] {
				"at","by","down","for","from","in","into","like","near","of","off","on","onto","over","past","to","upon","with", // prepositions
				"and","as","but","for","if","nor","once","or","so","than","that","till","when","yet", // conjuntions
			};

			char last = '\0';
			foreach (char c in input)
			{
				if (last != '\0' && spaceEquivilents.Contains(c))
					sb.Append(' ');
				else
				{
					if (last != '\0' && !char.IsSeparator(last))
					{
						if (spaceEquivilents.Contains(c)
							|| (char.IsLower(last) && char.IsUpper(c))
							|| char.IsNumber(last) != char.IsNumber(c))
							sb.Append(' ');
					}

					if (c != '_')
						sb.Append(c);
				}
				last = c;
			}

			var sbStr = sb.ToString();
			foreach (var u in uncapitalized)
			{
				var uRegex = new Regex(@"\b" + u.ToTitleCase() + "\b");
				sbStr = uRegex.Replace(sbStr, u.ToLowerInvariant());
			}

			return sbStr;
		}



		public static string Truncate(this string value, int maxLength, string truncateAt = " ", bool isEnding = false)
		{
			if (string.IsNullOrEmpty(value))
				return value;

			if (value.Length < maxLength)
				return value;

			//find the space to truncate at
			int index = value.Substring(0, maxLength).LastIndexOf(truncateAt);

			if (index == -1)
			{
				index = maxLength;

				value = value.Substring(0, index);
			}
			else
			{
				value = value.Substring(0, index + truncateAt.Length);
			}

			return value + ((isEnding) ? "" : "...");
		}

		public static string KillHyphens(this string value)
		{
			if (value.IsNullOrEmpty())
				return value;

			return value.Replace("- ", "");
		}

		public static bool IsNullOrEmpty(this string value)
		{
			return String.IsNullOrWhiteSpace(value);
		}

		public static bool IsNotNullOrEmpty(this string value)
		{
			return String.IsNullOrWhiteSpace(value) == false;
		}

		public static string ToTitleCase(this string value)
		{
			var ret = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());

			//easy roman numerals
			return ret
				.Replace("Ii", "II")
				.Replace("Iii", "III")
			;
		}

		/// <summary>
		/// Returns an HTML element Id-safe version of the string
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string ToHtmlIdSafeString(this string value)
		{
			return System.Text.RegularExpressions.Regex.Replace(value, @"[^-A-Za-z0-9_:]", "");
		}

		/// <summary>
		/// Returns a URL-safe version of the string
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string ToUrlSafeString(this string item, bool encode = true)
		{
			// lower/trim
			var s = item.ToLower().Trim();

			s = s
				.Replace(".", "")
				.Replace("+", "")
				.Replace("(", "")
				.Replace(")", "")
				.Replace(":", "")
				.Replace(";", "")
				.Replace("\"", "")
				.Replace("'", "")
				.Replace("’", "")
				.Replace("“", "")
				.Replace("”", "")
				.Replace("the ", "")
				.Replace(" the ", " ")
				.Replace(" and ", " ")
				.Replace(" of ", " ")
				.Replace(" a ", " ")
				.Replace("é", "e")
			;

			// reduce multi-spaces to single 
			s = Regex.Replace(s, @"\s+", " ");

			s = s
				.Replace("&", "-")
				.Replace("/", "-")
				.Replace(",", "-")
				.Replace(" ", "-")
				.Replace("–", "-")
				.Replace("—", "-")
			;

			if (encode)
				return System.Web.HttpUtility.UrlEncode(s);
			else
				return s;
		}

		public static string ToCssSafe(this string value)
		{
			// lower/trim
			var s = value.ToUrlSafeString()
				.Replace(" ", "-");

			return s;
		}

		/// <summary>
		/// Inserts word-break character at particular characters
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public static string ToHtmlWrappable(this string val)
		{
			return val
				.Replace("\\", "\\<wbr />")
				.Replace("/", "/<wbr />");

		}

		public static string StripHtmlTags(this string item)
		{
			var s = item.ToLower()
				.Replace("<p>", "")
				.Replace("</p>", "")
				.Replace("<br/>", "")
				.Replace("<br />", "")
				.Replace("<BR/>", "")
				.Replace("<BR />", "")
			;
			return s;
		}

		public static string StripHtmlTagsPreserveCase(this string item)
		{
			var s = item
				.ReplaceInsensitive("<p>", "")
				.ReplaceInsensitive("</p>", "")
				.ReplaceInsensitive("<br/>", "")
				.ReplaceInsensitive("<br />", "")
			;
			return s;
		}

		//todo: combine two methods StripHtmlTags and RemoveHtmlTags
		public static string RemoveHtmlTags(this string strHtml, string tag = null)
		{
			var strText = Regex.Replace(
				strHtml,
				(tag == null) ? "<(.|\n)*?>" : "(<" + tag + ".?>|</" + tag + ">)",
				string.Empty
			);

			return Regex.Replace(
				HttpUtility.HtmlDecode(strText),
				@"\s+",
				" "
			);
		}

		/// <summary>
		/// Counts the number of times a substring appears in a string
		/// </summary>
		/// <param name="valueToFind">Substring to find</param>
		/// <returns>Int32</returns>
		public static Int32 ContainsHowMany(this string value, String parameter)
		{
			int foundAt = 0;
			int searchFromIndex = 0;
			int howMany = 0;

			while (foundAt >= 0)
			{
				foundAt = value.IndexOf(parameter, searchFromIndex);
				if (foundAt >= searchFromIndex)
				{
					searchFromIndex = ++foundAt;
					howMany++;
				}
			}

			return howMany;
		}

		public static string PrependStr(this string value, string beginStr, string endStr = "")
		{
			if (value.IsNullOrEmpty())
				return value;

			return beginStr + value + endStr;
		}

		public static IEnumerable<string> SplitByLength(this string str, int maxLength)
		{
			if (str != null)
			{
				for (int index = 0; index < str.Length; index += maxLength)
				{
					yield return str.Substring(index, Math.Min(maxLength, str.Length - index));
				}
			}
		}

		public static string TrimEnd(
			this string input,
			string suffixToRemove,
			StringComparison comparisonType)
		{	
			if (input != null && suffixToRemove != null
			  && input.EndsWith(suffixToRemove, comparisonType))
			{
				return input.Substring(0, input.Length - suffixToRemove.Length);
			}
			else return input;
		}

		public static string ReplaceInsensitive(this string str, string from, string to)
		{
			if(str == null)
			{
				return str;
			}
			str = Regex.Replace(str, from, to, RegexOptions.IgnoreCase);
			return str;
		}
	}
}

// */