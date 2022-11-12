using Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace Utility.Logging
{
	public class DbLog
	{
		List<string> _buffer = new List<string>();
		DbLogProvider _provider;

		public const string DEFAULT_EMAIL_HOST = "mailoutbound.tutor.com";
		public const string DEFAULT_EMAIL_SENDER = "tutor@tutor.com";

		public int ApplicationId { get; set; }
		public string ApplicationName { get; set; }
		public string EmailRecipient { get; set; }

		public DbLog(int applicationId)
			: this(applicationId, null)
		{ }

		public DbLog(int applicationId, string emailRecipient)
		{
			this.ApplicationId = applicationId;
			this.EmailRecipient = emailRecipient;
			_provider = new DbLogProvider(this.ApplicationId, this.EmailRecipient);

			this.ApplicationName = _provider.GetApplicationName(this.ApplicationId);
		}

		/// <summary>
		/// To be used with a lazy-loading DbLog. This will cause the constructor to fire and test database connectivity without writing anything to the database.
		/// </summary>
		public void Test()
		{
			// force constructor
		}

		public void Write(string source, Exception ex)
		{
			Write(source, String.Format("[{0}] {1}", ex.GetType().Name, ex.ToString()));
		}

		public void Write(string source, string message)
		{
			// add to (email) buffer
			_buffer.Add(message);
			// log to db
			_provider.LogException(source, message);
		}

		public void EmailBuffer()
		{
			if (String.IsNullOrEmpty(this.EmailRecipient))
				throw new ArgumentNullException("EmailRecipient is required");

			if (EnvironmentMode.Current() == EnvironmentMode.Production && _buffer.Count > 0)
			{
				var tries = 0;
				var retryLimit = 3;
				var retryDelay = new TimeSpan(0, 5, 0); // five minutes

				while (tries < retryLimit)
				{
					try
					{
						// build body
						var body = new StringBuilder();
						foreach (var b in _buffer)
							body.AppendFormat("{0}\r\n", b);

						//email contents of log
						var c = new System.Net.Mail.SmtpClient();
						c.Host = DEFAULT_EMAIL_HOST;
						c.Send(DEFAULT_EMAIL_SENDER, this.EmailRecipient, String.Format("{0} ERRORS in {1}", _buffer.Count, this.ApplicationName), body.ToString());

						// clear the buffer
						_buffer.Clear();

						break; // abort the loop
					}
					catch (Exception ex)
					{
						// add mail error to log
						this.Write("EmailBuffer", ex);
					}
					tries++;
					Thread.Sleep(retryDelay);
				}
			}
		}
	}

	internal class DbLogProvider : Data.DataProvider
	{
		// DbLogProvider.Log doesn't make sense
		public new DbLog Log { get { throw new NotImplementedException(); } set { } }

		internal DbLogProvider(int applicationId, string loggingEmail = null)
			: base(applicationId, loggingEmail)
		{ }

		internal string GetApplicationName(int applicationId)
		{
			var result = ExecuteProc<SystemApplication>("SystemApplicationGet",
				new
				{
					@applicationId = applicationId,
				});

			if (result.Count == 0)
				throw new ArgumentException("ApplicationId " + applicationId + " not found");

			var x = new SystemApplication[1];
			result.CopyTo(x, 0);
			return x[0].ApplicationDesc;
		}

		internal void LogException(string source, string message)
		{
			try
			{
				ExecuteProc("dbo.SystemApplicationsErrorLogCreate", new
				{
					@ErrorDate = System.DateTime.Now,
					@ApplicationID = this.ApplicationId,
					@Message = message,
					@ServerName = System.Environment.MachineName,
					@Source = source,
				});
			}
			catch { /* suppress */ }
		}

		internal void LogException(string source, Exception ex)
		{
			this.LogException(source, ex.ToString());
		}
	}

	public class SystemApplication
	{
		public int ApplicationId { get; private set; }
		public string ApplicationDesc { get; private set; }
		public byte Deleted { get; private set; }
	}
}
