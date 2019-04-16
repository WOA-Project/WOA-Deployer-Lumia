namespace Deployer.Lumia.NetFx
{
    public class TestPhoneModelInfoReader : IPhoneModelInfoReader
    {
        public PhoneModelInfo GetPhoneModel(uint diskNumber)
        {
            return new PhoneModelInfo(PhoneModel.Cityman, Variant.SingleSim);
        }
    }
}