using System;

namespace nGEN.Data
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class CacheAttribute : Attribute
	{
		public TimeSpan Lifetime { get; set; }

		/// <summary>
		/// Defines the cache policy for this method
		/// </summary>
		/// <param name="lifetime">time to cache in minutes</param>
		public CacheAttribute(int hours, int minutes, int seconds)
		{
			this.Lifetime = new TimeSpan(hours, minutes, seconds);
		}
		public CacheAttribute(int lifetime = 5)
			: this(0, 5, 0)
		{ }
	}
}
