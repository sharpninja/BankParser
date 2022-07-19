using BankParser.Core.Models;

using CommunityToolkit.Mvvm.ComponentModel;

namespace BankParser.ViewModels;

[ObservableObject]
public partial class MainViewModelBase
{
    [ObservableProperty]
    private string? _currentFilter;

    [ ObservableProperty ]
    private List<ImmutableBankTransaction> _immutable = null!;

    [ ObservableProperty ]
    private BankTransactionView? _selectedTransaction;

    public string Filename
    {
        get; set;
    } = null!;
}
