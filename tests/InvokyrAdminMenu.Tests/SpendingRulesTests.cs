using InvokyrAdminMenu;
using Xunit;

public class SpendingRulesTests
{
    [Fact]
    public void ParsesAndRoundTrips()
    {
        var rules = SpendingRules.Parse("76561198000000002:block, 76561198000000001:allow,junk,0:block,5:maybe", false);
        Assert.False(rules.IsAllowed(76561198000000002));
        Assert.True(rules.IsAllowed(76561198000000001));
        Assert.False(rules.Knows(5));
        Assert.Equal("76561198000000001:allow,76561198000000002:block", rules.Serialize());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void UnknownPlayersFollowTheDefault(bool blockNew, bool expected) =>
        Assert.Equal(expected, SpendingRules.Parse("", blockNew).IsAllowed(42));

    [Fact]
    public void RememberKeepsTheDefaultOfThatMoment()
    {
        var rules = SpendingRules.Parse("", true);
        Assert.True(rules.Remember(42, isHost: false));
        Assert.False(rules.Remember(42, isHost: false));
        rules.BlockNewPlayers = false;
        Assert.False(rules.IsAllowed(42));
        Assert.True(rules.IsAllowed(43));
    }

    [Fact]
    public void HostIsAlwaysRememberedAllowed()
    {
        var rules = SpendingRules.Parse("", true);
        rules.Remember(1, isHost: true);
        Assert.True(rules.IsAllowed(1));
    }

    [Fact]
    public void SetOverrides()
    {
        var rules = SpendingRules.Parse("7:allow", false);
        rules.Set(7, false);
        Assert.False(rules.IsAllowed(7));
        rules.Set(0, false);
        Assert.False(rules.Knows(0));
    }
}
