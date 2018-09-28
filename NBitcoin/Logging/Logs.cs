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
			Configuration = factory.CreateLogger("Configuration");
			Explorer = factory.CreateLogger("Explorer");
			Events = factory.CreateLogger("Events");
		}
		public static ILogger Configuration
		{
			get; set;
		}
		public static ILogger Events
		{
			get; set;
		}
		public static ILogger Explorer
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
}