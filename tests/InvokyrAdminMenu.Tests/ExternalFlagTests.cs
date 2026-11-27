using Xunit;

namespace InvokyrAdminMenu.Tests
{
    public static class FakeModSettings
    {
        public static bool IsOpen { get; set; }
    }

    public class ExternalFlagTests
    {
        [Fact]
        public void ReadsOtherModsFlag()
        {
            var flag = new ExternalFlag("InvokyrAdminMenu.Tests.FakeModSettings", "IsOpen");
            FakeModSettings.IsOpen = true;
            Assert.True(flag.Get());
            FakeModSettings.IsOpen = false;
            Assert.False(flag.Get());
        }

        [Fact]
        public void MissingModReadsClosed()
        {
            Assert.False(new ExternalFlag("InvokyrConfig.OverlayState", "IsOpen").Get());
        }
    }
}
