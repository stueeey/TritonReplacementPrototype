using System;
using log4net;
using log4net.Core;
using Xunit.Abstractions;

namespace Apollo.Mocks
{
	public class MockLogger : ILog
	{
		private readonly ITestOutputHelper _logger;
		private readonly string _loggerName;
		public MockLogger(ITestOutputHelper logger, string loggerName)
		{
			_logger = logger;
			_loggerName = loggerName;
		}

		#region Implementation of ILoggerWrapper

		public ILogger Logger => throw new NotImplementedException();

		#endregion

		#region Implementation of ILog

		public void Debug(object message)
		{
			_logger.WriteLine($"DEBUG {_loggerName}: {message}");
		}

		public void Debug(object message, Exception exception)
		{
			_logger.WriteLine($"DEBUG {_loggerName}: {message} ({exception.Message})");
		}

		public void DebugFormat(string format, params object[] args)
		{
			_logger.WriteLine(format, args);
		}

		public void DebugFormat(string format, object arg0)
		{
			throw new NotImplementedException();
		}

		public void DebugFormat(string format, object arg0, object arg1)
		{
			throw new NotImplementedException();
		}

		public void DebugFormat(string format, object arg0, object arg1, object arg2)
		{
			throw new NotImplementedException();
		}

		public void DebugFormat(IFormatProvider provider, string format, params object[] args)
		{
			throw new NotImplementedException();
		}

		public void Info(object message)
		{
			_logger.WriteLine($"INFO {_loggerName}: {message}");
		}

		public void Info(object message, Exception exception)
		{
			_logger.WriteLine($"INFO {_loggerName}: {message} ({exception.Message})");
		}

		public void InfoFormat(string format, params object[] args)
		{
			_logger.WriteLine(format, args);
		}

		public void InfoFormat(string format, object arg0)
		{
			throw new NotImplementedException();
		}

		public void InfoFormat(string format, object arg0, object arg1)
		{
			throw new NotImplementedException();
		}

		public void InfoFormat(string format, object arg0, object arg1, object arg2)
		{
			throw new NotImplementedException();
		}

		public void InfoFormat(IFormatProvider provider, string format, params object[] args)
		{
			throw new NotImplementedException();
		}

		public void Warn(object message)
		{
			_logger.WriteLine($"WARN {_loggerName}: {message}");
		}

		public void Warn(object message, Exception exception)
		{
			_logger.WriteLine($"WARN {_loggerName}: {message} ({exception.Message})");
		}

		public void WarnFormat(string format, params object[] args)
		{
			_logger.WriteLine(format, args);
		}

		public void WarnFormat(string format, object arg0)
		{
			throw new NotImplementedException();
		}

		public void WarnFormat(string format, object arg0, object arg1)
		{
			throw new NotImplementedException();
		}

		public void WarnFormat(string format, object arg0, object arg1, object arg2)
		{
			throw new NotImplementedException();
		}

		public void WarnFormat(IFormatProvider provider, string format, params object[] args)
		{
			throw new NotImplementedException();
		}

		public void Error(object message)
		{
			_logger.WriteLine($"ERROR {_loggerName}: {message}");
		}

		public void Error(object message, Exception exception)
		{
			_logger.WriteLine($"ERROR {_loggerName}: {message} ({exception.Message})");
		}

		public void ErrorFormat(string format, params object[] args)
		{
			_logger.WriteLine(format, args);
		}

		public void ErrorFormat(string format, object arg0)
		{
			throw new NotImplementedException();
		}

		public void ErrorFormat(string format, object arg0, object arg1)
		{
			throw new NotImplementedException();
		}

		public void ErrorFormat(string format, object arg0, object arg1, object arg2)
		{
			throw new NotImplementedException();
		}

		public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
		{
			throw new NotImplementedException();
		}

		public void Fatal(object message)
		{
			_logger.WriteLine($"FATAL {_loggerName}: {message}");
		}

		public void Fatal(object message, Exception exception)
		{
			_logger.WriteLine($"FATAL {_loggerName}: {message} ({exception.Message})");
		}

		public void FatalFormat(string format, params object[] args)
		{
			_logger.WriteLine(format, args);
		}

		public void FatalFormat(string format, object arg0)
		{
			throw new NotImplementedException();
		}

		public void FatalFormat(string format, object arg0, object arg1)
		{
			throw new NotImplementedException();
		}

		public void FatalFormat(string format, object arg0, object arg1, object arg2)
		{
			throw new NotImplementedException();
		}

		public void FatalFormat(IFormatProvider provider, string format, params object[] args)
		{
			throw new NotImplementedException();
		}

		public bool IsDebugEnabled { get; set; } = true;
		public bool IsInfoEnabled { get; set; } = true;
		public bool IsWarnEnabled { get; set; } = true;
		public bool IsErrorEnabled { get; set; } = true;
		public bool IsFatalEnabled { get; set; } = true;

		#endregion
	}
}
