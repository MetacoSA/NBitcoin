
using System;
using System.Linq;
using NBitcoin;
using NBitcoin.Tests;
using Xunit;

public enum NetworkTestRule
{
    Skip, Only
}

/// <summary>
/// Attribute that is applied to a method to indicate that it is a test that
/// should be run by the test runner if the current node builder network passes
/// the provided test instructions. A list of networks to test or skip should be
/// provided using the network crypto code (preferred), network name or any of
/// the network's aliases.
/// </summary>
public class ConditionalNetworkTestAttribute : FactAttribute
{
    public ConditionalNetworkTestAttribute(NetworkTestRule rule, params string[] ruleTargets)
    {
        if (ConditionalNetworkTest.CanUseNodeBuilderNetwork(rule, ruleTargets))
            return;

        if (rule == NetworkTestRule.Skip)
            Skip = $"Test skipped for '{String.Join(", ", ruleTargets)}' networks.";
        else
            Skip = $"Test is only for '{String.Join(", ", ruleTargets)}' networks.";
    }
}

/// <summary>
/// Marks a test method as being a data theory. Data theories are tests which
/// are fed various bits of data from a data source, mapping to parameters on
/// the test method. The test will only be run if the current node builder
/// network passes the provided test instructions. A list of networks to test or
/// skip should be provided using the network crypto code (preferred), network
/// name or any of the network's aliases.
/// </summary>
public class ConditionalNetworkTheoryAttribute : TheoryAttribute
{
    public ConditionalNetworkTheoryAttribute(NetworkTestRule rule, params string[] ruleTargets)
    {
        if (ConditionalNetworkTest.CanUseNodeBuilderNetwork(rule, ruleTargets))
            return;

        if (rule == NetworkTestRule.Skip)
            Skip = $"Test skipped for '{String.Join(", ", ruleTargets)}' networks.";
        else
            Skip = $"Test is only for '{String.Join(", ", ruleTargets)}' networks.";
    }
}

public static class ConditionalNetworkTest
{
    // CanUseNodeBuilderNetwork is true if the current node builder network
    // passes the provided instructions.
    public static bool CanUseNodeBuilderNetwork(NetworkTestRule rule, string[] ruleTargets)
    {
        // If a rule is provided but doesn't target any networks, make some sane
        // assumptions below.
        if (ruleTargets.Length == 0)
        {
            if (rule == NetworkTestRule.Only)
                return false; // only allow 0 networks, so always false for every network
            return true; // skip 0 networks, so always true for every network
        }

        // builder will be null if the useNetwork fn returns false, i.e if the
        // node builder network doesn't pass the NetworkTestRule. This prevents
        // NodeBuilderEx.Create from downloading the network binaries as they
        // won't be needed.
        var builder = NodeBuilderEx.Create(useNetwork: (network) => network.passesRule(rule, ruleTargets));
        var canTest = builder != null;
        builder?.Dispose();
        return canTest;
    }

    private static bool passesRule(this Network network, NetworkTestRule rule, string[] ruleTargets)
    {
        switch (rule)
        {
            case NetworkTestRule.Skip:
                return !network.MatchesAny(ruleTargets);
            case NetworkTestRule.Only:
                return network.MatchesAny(ruleTargets);
            default:
                throw new Exception($"unrecognized network rule {rule}");
        }
    }

    public static bool MatchesAny(this Network network, string[] ruleTargets)
    {
        return ruleTargets.Any(network.MatchesDescription);
    }

    public static bool MatchesDescription(this Network network, string description)
    {
        if (string.Equals(network.NetworkSet?.CryptoCode, description, StringComparison.InvariantCultureIgnoreCase))
            return true;
        return network == Network.GetNetwork(description);
    }
}
