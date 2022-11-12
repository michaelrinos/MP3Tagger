using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace Utility
{
	public struct EnvironmentMode
	{
		private string _value { get; set; }

		private EnvironmentMode(string value)
			: this()
		{
			_value = value.ToLower();
		}

		public static EnvironmentMode Current(string appSettingsKeyName = "EnvironmentMode")
		{
			return EnvironmentMode.Parse(ConfigurationManager.AppSettings[appSettingsKeyName]);
		}

		public static EnvironmentMode Parse(string value)
		{
			switch (value.ToLower())
			{
				case "debug":
					return EnvironmentMode.Debug;
				case "production":
					return EnvironmentMode.Production;
				case "unittest":
					return EnvironmentMode.UnitTest;
				default:
					throw new InvalidOperationException("Unrecognized environment: '" + value + "'");
			}
		}

		public static EnvironmentMode Debug { get { return new EnvironmentMode("debug"); } }
		public static EnvironmentMode Production { get { return new EnvironmentMode("production"); } }
		public static EnvironmentMode UnitTest { get { return new EnvironmentMode("unittest"); } }

		public override bool Equals(object obj)
		{
			if (obj is EnvironmentMode)
				return String.Compare(((EnvironmentMode)obj)._value, this._value, true) == 0;
			else
				return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public static bool operator ==(EnvironmentMode em1, EnvironmentMode em2)
		{
			return em1.Equals(em2);
		}

		public static bool operator !=(EnvironmentMode em1, EnvironmentMode em2)
		{
			return !em1.Equals(em2);
		}
	}
}
