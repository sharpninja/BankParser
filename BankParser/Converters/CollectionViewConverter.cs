using System;

using BankParser.Core.Models;
using BankParser.ViewModels;

using Microsoft.UI.Xaml.Data;

namespace BankParser.Converters;

public class CollectionViewConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if(value is ICollectionView collectionView)
        {
            return new NotifyingList<BankTransaction>(collectionView.OfType<BankTransaction>());
        }

        return null;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
