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
			NodeServer = factory.CreateLogger("Utils");
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
}