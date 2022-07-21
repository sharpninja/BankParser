namespace BankParser.ViewModels;

public partial class MainViewModel
{
    [RelayCommand]
    private void AddFilter()
    {
        if (FilterText is null or "")
        {
            return;
        }

        string filterText = FilterText.Trim();
        FilterText = null;
        AddUndo();
        FilterText = filterText;

        _base.CurrentFilter = FilterText;

        if (_base.CurrentFilter is null)
        {
            return;
        }

        FilterExpression = new Regex(
            Regex.Escape(_base.CurrentFilter),
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        ApplyFilter();
    }

    [RelayCommand]
    public void Copy(object? text)
    {
        DataPackage request = new DataPackage
        {
            RequestedOperation = DataPackageOperation.Copy,
        };

        request.SetText(text?.ToString());

        Clipboard.SetContent(request);
    }

    [RelayCommand]
    public async Task Search(SfAutoComplete autoComplete)
    {
        if (autoComplete.Text is "")
        {
            ClearFilter();
        }
        else
        {
            FilterText = null;
            FilterExpression = null;
        }

        string pattern = autoComplete.Text;
        Regex regex = new(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        Dictionary<PropertyInfo, Predicate<BankTransactionView>> map = new();

        foreach (PropertyInfo? pi in SelectedPropertyInfo)
        {
            switch (pi.Name)
            {
                case nameof(BankTransactionView.AmountCredit):
                case nameof(BankTransactionView.AmountDebit):
                    if (decimal.TryParse(pattern, out decimal d))
                    {
                        decimal parsed = Math.Abs(d);

                        bool FilterMoney(BankTransactionView trx)
                        {
                            bool isMatch
                                = pi.GetValue(trx) is decimal value && value.Equals(parsed);

                            return isMatch;
                        }

                        map.Add(pi,
                            FilterMoney
                        );
                    }

                    break;


                case nameof(BankTransactionView.Date):
                    if (DateOnly.TryParse(pattern, out DateOnly da))
                    {
                        DateOnly parsed = da;

                        bool FilterDateByDate(BankTransactionView trx)
                        {
                            bool isMatch = pi.GetValue(trx) is DateOnly value &&
                                           value.Equals(parsed);

                            return isMatch;
                        }

                        map.Add(
                            pi,
                            FilterDateByDate
                        );

                        break;
                    }

                    if (int.TryParse(pattern, out int i))
                    {
                        int parsed = i;

                        bool FilterDateByYear(BankTransactionView trx)
                        {

                            bool isMatch = pi.GetValue(trx) is DateOnly value &&
                                           value.Year.Equals(parsed);

                            return isMatch;
                        }

                        map.Add(pi, FilterDateByYear);
                    }

                    break;

                default:

                    bool FilterString(BankTransactionView trx)
                    {
                        if (pi.GetValue(trx) is not string toMatch)
                        {
                            return false;
                        }

                        bool isMatch = regex.IsMatch(toMatch);

                        return isMatch;
                    }

                    map.Add(pi,
                        FilterString
                    );

                    break;
            }
        }

        FilteredBag.Clear();

        List<BankTransactionFilters> toFilter = Unmodified
            .Select(trx => new BankTransactionFilters(trx, map))
            .ToList();

        await Parallel.ForEachAsync(
            toFilter,
            ProcessFilter);

        Source.Clear();
        Source.AddRange(
            FilteredBag
                .OrderByDescending(static trx => trx.Date)
                .ThenBy(static trx => trx.TransactionNumber)
                .Distinct());
    }

    private static bool CanCreateRule(object pattern)
        => pattern is not (null or "");

    [RelayCommand(CanExecute = nameof(CanCreateRule))]
    public void CreateRule(object pattern)
    {

    }
}