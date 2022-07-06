using System.ComponentModel;

namespace BankParser.ViewModels;

public static class ObservableCollectionExtension
{
    public static void AddRange<TValue>(this BindingList<TValue> col, IEnumerable<TValue> values)
        => values.ToList().ForEach(col.Add);
}
