using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace NBitcoin
{
	public class PSBTException : InvalidOperationException
	{
		public PSBTException(IEnumerable<PSBTError> errors) : base(GetMessage(errors))
		{
			Errors = errors.ToList();
		}

		public IReadOnlyList<PSBTError> Errors { get; }

		private static string GetMessage(IEnumerable<PSBTError> errors)
		{
			if (errors == null)
				throw new ArgumentNullException(nameof(errors));
			return String.Join(Environment.NewLine, errors.Select(e => e.ToString()).ToArray());
		}
	}

	public class PSBTError
	{
		public PSBTError(uint inputIndex, string errorMessage)
		{
			if (errorMessage == null)
				throw new ArgumentNullException(nameof(errorMessage));
			InputIndex = inputIndex;
			Message = errorMessage;
		}
		public uint InputIndex { get; }
		public string Message { get; }
		public override string ToString()
		{
			return $"Input {InputIndex}: {Message}";
		}
	}
}
