using System;

namespace Exceptions.Data
{
	public class DataManagerNotAvailable : System.ApplicationException
	{ public DataManagerNotAvailable(string sMsg, System.Exception pInnerException) : base(sMsg, pInnerException) { } }
}
