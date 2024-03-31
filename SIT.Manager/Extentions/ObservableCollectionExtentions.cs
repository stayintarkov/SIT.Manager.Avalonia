using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIT.Manager.Extentions;

public static class ObservableCollectionExtentions
{
    public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> values)
    {
        foreach (var item in values)
        {
            collection.Add(item);
        }
    }
}
