using Deployer.Lumia.NetFx.PhoneMetadata;
using Xunit;

namespace Deployer.Lumia.Tests
{
    public class PhoneReaderTests
    {
        [Fact]
        public void Read()
        {
            var reader = new PhoneModelInfoInfoReader(new PhoneInfoReader());
            var info = reader.GetPhoneModel(4);
        }
    }
}