using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ABI.Windows.Media.Devices;
using BankParser.Contracts.ViewModels;
using BankParser.Core.Contracts.Services;
using BankParser.Core.Models;

using CommunityToolkit.Mvvm.ComponentModel;

namespace BankParser.ViewModels;

public partial class MainViewModel : ObservableRecipient, INavigationAware
{
    private const string DEFAULT_FILENAME=@"C:\Users\kingd\OneDrive\Desktop\transactions.json";

    private readonly ISampleDataService _sampleDataService;

    public ObservableCollection<BankTransaction> Source
    {
        get;
    } = null!;

    public IEnumerable<BankTransaction> GroupedTransactions
        => Source.OrderBy(b => b.OtherParty)
                .ThenByDescending(b => b.Date);

    [ObservableProperty]
    private BankTransaction? _selected = null;

    [ObservableProperty]
    private string _filterText;

    public MainViewModel(ISampleDataService sampleDataService)
    {
        _sampleDataService = sampleDataService;

        Source = new(BankTransaction.FromJson(File.ReadAllText(DEFAULT_FILENAME)));
    }

    public void OnNavigatedTo(object parameter)
    {
    }

    public void OnNavigatedFrom()
    {
    }
}
