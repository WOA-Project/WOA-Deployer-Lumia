namespace Deployer.Lumia
{
    public class PhoneModelInfo
    {
        public PhoneModel Model { get; }
        public Variant Variant { get; }

        public PhoneModelInfo(PhoneModel model, Variant variant)
        {
            Model = model;
            Variant = variant;
        }

        protected bool Equals(PhoneModelInfo other)
        {
            return Model == other.Model && Variant == other.Variant;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((PhoneModelInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Model * 397) ^ (int) Variant;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Model)}: {Model}, {nameof(Variant)}: {Variant}";
        }
    }
}