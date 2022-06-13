using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ABI.Windows.Media.Devices;
using BankParser.Contracts.ViewModels;
using BankParser.Core.Contracts.Services;
using BankParser.Core.Models;

using CommunityToolkit.Mvvm.ComponentModel;

namespace BankParser.ViewModels;

public class MainViewModel : ObservableRecipient, INavigationAware
{
    private const string DEFAULT_FILENAME=@"C:\Users\kingd\Downloads\transactions.json";

    private readonly ISampleDataService _sampleDataService;

    public ObservableCollection<BankTransaction> Source
    {
        get;
    } = null!;

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
