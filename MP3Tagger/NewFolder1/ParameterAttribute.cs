using System;
using System.Collections.Generic;
using System.Text;

namespace nGEN.Data
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class ParameterAttribute : Attribute
	{
		public string ParameterName { get; set; }
		public bool Ignore { get; set; }

		public ParameterAttribute()
			: base()
		{ }

		public ParameterAttribute(string parameterName)
			: this()
		{
			ParameterName = parameterName;
		}
	}
}
