using System;

namespace Reflection {
	public enum SerializationType {
		None = 0,
		//Xml,
		Json,
	}

	/// <summary>
	/// Used for specifying the parameter behavior
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Enum, AllowMultiple = false)]
	public class SqlQueryParameterAttribute : Attribute {
		/// <summary>
		/// The query parameter name when it differs from the object property name
		/// </summary>
		public string ParameterName { get; set; }

		/// <summary>
		/// If the property should be ignored when creating a parameter list from the object, and when creating the object from a datatable
		/// </summary>
		public bool Ignore { get; set; }

		/// <summary>
		/// If the property should be ignored when creating a parameter list from the object; will NOT be ignored when creating the object from a datatable
		/// </summary>
		public bool IgnoreInOnly { get; set; }

		/// <summary>
		/// Max chars to pass
		/// </summary>
		public int MaxLength { get; set; }

		/// <summary>
		/// When other than 'None', object is serialized to specified type
		/// </summary>
		public SerializationType Serialize { get; set; }

		public SqlQueryParameterAttribute()
			: base() { }

		public SqlQueryParameterAttribute(string parameterName)
			: this() {
			ParameterName = parameterName;
		}
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Enum, AllowMultiple = false)]
	public class DerivedAttribute : SqlQueryParameterAttribute {
		public DerivedAttribute()
			: base() {
			base.Ignore = true;
		}
	}
}