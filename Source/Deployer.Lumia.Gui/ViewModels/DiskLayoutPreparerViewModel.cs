using Deployer.Tasks;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class DiskLayoutPreparerViewModel
    {
        public string Name { get; }
        public IDiskLayoutPreparer Preparer { get; }

        public DiskLayoutPreparerViewModel(string name, IDiskLayoutPreparer preparer)
        {
            Name = name;
            Preparer = preparer;
        }

        protected bool Equals(DiskLayoutPreparerViewModel other)
        {
            return string.Equals(Name, other.Name);
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

            return Equals((DiskLayoutPreparerViewModel) obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }
    }
}