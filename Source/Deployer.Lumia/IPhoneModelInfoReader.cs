namespace Deployer.Lumia
{
    public interface IPhoneModelInfoReader
    {
        PhoneModelInfo GetPhoneModel(uint diskNumber);
    }
}