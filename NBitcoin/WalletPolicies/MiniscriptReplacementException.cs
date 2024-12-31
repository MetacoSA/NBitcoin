#if !NO_RECORDS
#nullable enable

using System;

namespace NBitcoin.WalletPolicies;

public class MiniscriptReplacementException : Exception
{
	public MiniscriptReplacementException(string parameterName, MiniscriptNode.ParameterRequirement requirement)
		: base($"The parameter {parameterName} doesn't fit the requirement ({requirement})")
	{
		ParameterName = parameterName;
		Requirement = requirement;
	}
	public string ParameterName { get; }
	public MiniscriptNode.ParameterRequirement Requirement { get; }
}
#endif
