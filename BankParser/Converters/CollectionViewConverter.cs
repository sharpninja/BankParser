namespace BankParser.Converters;

public class CollectionViewConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if(value is ICollectionView collectionView)
        {
            return new NotifyingList<BankTransactionView>(collectionView.OfType<BankTransactionView>());
        }

        return null;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}


public class PaddingRemovingConverter : IValueConverter
{
    public object? Convert(
        object value,
        Type targetType,
        object parameter,
        string language
    )
    {
        if (value is Border control)
        {
            return control.ActualWidth - control.Padding.Left - control.Padding.Right;
        }

        return null;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        string language
    )
        => throw new NotImplementedException();
}