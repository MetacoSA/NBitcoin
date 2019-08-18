using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NBitcoin.Logging
{
	public class Logs
	{
		static Logs()
		{
			Configure(new FuncLoggerFactory(n => NullLogger.Instance));
		}
		public static void Configure(ILoggerFactory factory)
		{
			NodeServer = factory.CreateLogger("NodeServer");
			Utils = factory.CreateLogger("Utils");
		}
		public static ILogger NodeServer
		{
			get; set;
		}

		public static ILogger Utils
		{
			get; set;
		}

		public const int ColumnLength = 16;
	}

	public class FuncLoggerFactory : ILoggerFactory
	{
		private Func<string, ILogger> createLogger;
		public FuncLoggerFactory(Func<string, ILogger> createLogger)
		{
			this.createLogger = createLogger;
		}
		public void AddProvider(ILoggerProvider provider)
		{

		}

		public ILogger CreateLogger(string categoryName)
		{
			return createLogger(categoryName);
		}

		public void Dispose()
		{

		}
	}


	/// <summary>
	/// Minimalistic logger that does nothing.
	/// </summary>
	public class NullLogger : ILogger
	{
		public static NullLogger Instance { get; } = new NullLogger();

		private NullLogger()
		{
		}

		/// <inheritdoc />
		public IDisposable BeginScope<TState>(TState state)
		{
			return NullScope.Instance;
		}

		/// <inheritdoc />
		public bool IsEnabled(LogLevel logLevel)
		{
			return false;
		}

		/// <inheritdoc />
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
		}
	}

	/// <summary>
	/// An empty scope without any logic
	/// </summary>
	public class NullScope : IDisposable
	{
		public static NullScope Instance { get; } = new NullScope();

		private NullScope()
		{
		}

		/// <inheritdoc />
		public void Dispose()
		{
		}
	}

}
