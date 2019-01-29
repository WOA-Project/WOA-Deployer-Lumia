namespace Deployer.Execution
{
    public abstract class Argument
    {
        public Argument(object value)
        {
            Value = value;
        }

        public object Value { get; }
    }
}