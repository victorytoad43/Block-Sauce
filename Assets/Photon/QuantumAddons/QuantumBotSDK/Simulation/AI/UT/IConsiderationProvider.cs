using Quantum.Collections;

namespace Quantum
{
    public interface IConsiderationProvider
    {
        public AssetRef<Consideration> GetConsideration(QList<AssetRef<Consideration>> sourceList, int id);
        int Count(QList<AssetRef<Consideration>> sourceList);
    }
}
