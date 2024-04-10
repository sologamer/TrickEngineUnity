using System.Collections.Generic;

namespace TrickCore
{
    public interface IDropTable
    {
        void Clear();
        void AddObject(object item, float weight);
        bool RemoveObject(object item);
        List<object> GetItems();
        List<(object, float)> GetItemsWithWeights();
    }
}