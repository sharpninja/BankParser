using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

using BankParser.Contracts.ViewModels;
using BankParser.Core.Models;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;

using Newtonsoft.Json;

using WinRT.Interop;

using Syncfusion.UI.Xaml.Editors;

// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace BankParser.ViewModels;

public partial class MainViewModel : ObservableRecipient, INavigationAware
{
    // ReSharper disable once UnusedMember.Global
    public const string DEFAULT_FILENAME = @"C:\Users\kingd\OneDrive\Desktop\transactions.json";

    private NotifyingList<BankTransaction> _source = null!;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(FilerSelectedVisibility)), ]
    private string? _filterText;

    [ObservableProperty]
    private SfComboBoxItem? _selectedProperty;

    public PropertyInfo? SelectedPropertyInfo => (_selectedProperty?
            .Content as string ?? "Other") switch
    {
        "Desc" => typeof(BankTransaction).GetProperty(nameof(BankTransaction.Description)),
        "Memo" => typeof(BankTransaction).GetProperty(nameof(BankTransaction.Memo)),
        "Date" => typeof(BankTransaction).GetProperty(nameof(BankTransaction.Date)),
        "Deposit" => typeof(BankTransaction).GetProperty(nameof(BankTransaction.AmountCredit)),
        "Withdrawal" => typeof(BankTransaction).GetProperty(nameof(BankTransaction.AmountDebit)),
        _ => typeof(BankTransaction).GetProperty(nameof(BankTransaction.OtherParty)),
    };


    [JsonIgnore]
    private List<BankTransaction> Unmodified
    {
        get;
        set;
    } = null!;

    [JsonIgnore]
    public List<string> PotentialFilters =>
        SelectedTransaction is
        {
        } bt
            ? bt.PotentialFilters.ToList()
            : new();

    [JsonIgnore]
    public Visibility FilerSelectedVisibility
        => _filterText is not (null or "") && SelectedTransaction is not null
            ? Visibility.Visible
            : Visibility.Collapsed;

    public MainViewModel()
    {
        //LoadData(DEFAULT_FILENAME);

        OpenFile(CancellationToken.None).GetAwaiter().OnCompleted(() =>
        {
            PropertyChanged += MainViewModel_PropertyChanged;

            Source.CollectionChanged += CollectionChanged;
        });

    }

    private void CollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {

        if (e.Action is NotifyCollectionChangedAction.Add)
        {
            if (e.NewItems?.Count != Unmodified.Count)
            {
                SetGroupByDate(e.NewItems?.OfType<object>() ?? Array.Empty<object>());
            }
            else
            {
                SetGroupByDate(Unmodified);
            }
        }

        OnPropertyChanged(nameof(TotalCredits));
        OnPropertyChanged(nameof(TotalDebits));

        OnPropertyChanged(nameof(MainViewModel));
    }

    private void SetGroupByDate(IEnumerable<object> objects)
    {
        GroupedByDate.Clear();

        List<object> items = objects.ToList();

        if (!items.Any())
        {
            return;
        }

        int count = items.Count;
        if (count != Unmodified.Count)
        {
            GroupedByDate.AddRange(items);
        }
        else
        {
            IEnumerable<IGrouping<DateOnly, BankTransaction>> grouped =
                items.OfType<BankTransaction>()
                    .Where(static bt => bt.Date is not null)
                    .GroupBy(static i => (DateOnly)i.Date!);

            GroupedByDate.AddRange(
                grouped.Select(
                    static g => new DateGroup(
                    g.Key,
                    g.Cast<object>().ToList()
                )).Cast<object>());
        }

        OnPropertyChanged(nameof(MainViewModel.TotalCredits));
        OnPropertyChanged(nameof(MainViewModel.TotalDebits));

        OnPropertyChanged(nameof(MainViewModel));
    }

    private void LoadData(string fileName)
    {
        if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            Unmodified = BankTransaction.FromJson(File.ReadAllText(fileName))?.ToList() ?? new();
        }
        else if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            Unmodified = BankTransaction.FromCsv(fileName).ToList();
        }

        Source = new(Unmodified);
        SetGroupByDate(Source);

        // ReSharper disable once ExplicitCallerInfoArgument
        OnPropertyChanged("Reset");
    }

    private void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Selected):
                if (Selected is null)
                {
                    FilterExpression = null;
                }
                break;
        }
    }

    internal void ApplyFilter()
    {
        object? currentlySelected;

        if (FilterExpression is not null)
        {
            currentlySelected = Selected;

            if (currentlySelected is null)
            {
                throw new ArgumentNullException(nameof(currentlySelected));
            }

            string? filterText = FilterText;
            ConcurrentBag<BankTransaction> visibleItems = new();

            Parallel.ForEach(Unmodified, item =>
            {
                if (item.ApplyFilter(FilterExpression))
                {
                    visibleItems.Add(item);
                }
            });

            if (visibleItems.Count <= 0)
            {
                return;
            }

            void Callback()
            {
                var filtered =
                    visibleItems.OrderByDescending(
                        static i => i.Date)
                        .ToList();

                Source.Clear();
                Source.AddRange(filtered);

                GroupedByDate.Clear();
                GroupedByDate.AddRange(Source);

                Selected = currentlySelected;
                FilterText = filterText;
            }

            App.TryDispatch(Callback);
        }
        else
        {
            GroupedByDate.Clear();
            SetGroupByDate(Unmodified);
        }
    }

    public void OnNavigatedTo(object parameter)
    {
    }

    public void OnNavigatedFrom()
    {
    }

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

        CurrentFilter = FilterText;

        if (CurrentFilter is null)
        {
            return;
        }

        FilterExpression = new Regex(Regex.Escape(CurrentFilter),
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        ApplyFilter();
    }

    internal void ClearFilter()
    {
        CurrentFilter = null;
        List<BankTransaction> list =
            Unmodified.OrderByDescending(static i => i.Date).ToList();

        void Callback()
        {
            try
            {
                Source.Clear();
                Source.AddRange(list);
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        App.TryDispatch(Callback);
    }

    internal async Task OpenFile(CancellationToken token)
    {
        try
        {
            FileOpenPicker fileOpenPicker = new();

            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(fileOpenPicker, hwnd);

            fileOpenPicker.ViewMode = PickerViewMode.List;
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.Downloads;
            fileOpenPicker.FileTypeFilter.Add(".csv");
            var result = await fileOpenPicker.PickSingleFileAsync();

            LoadData(
                result is not null
                    ? result.Path
                    : @"C:\Users\kingd\Downloads\payton-009-export.csv"
            );
        }
        catch
        {
            // Ignore
        }
    }

    internal Task CloseFile()
    {
        Unmodified.Clear();
        GroupedByDate.Clear();

        App.TryDispatch(
            () =>
            {
                Source.Clear();
                Source.AddRange(
                Unmodified.OrderByDescending(static i => i.Date).ToList());
            });

        return Task.CompletedTask;
    }

    [ObservableProperty]
    private string? _currentFilter;

    [JsonIgnore]
    public NotifyingList<object> GroupedByDate
    {
        get; set;
    } = new();

    [JsonIgnore]
    private Regex? FilterExpression
    {
        get; set;
    }

    public string Filename
    {
        get;
        private set;
    } = null!;

    [JsonIgnore]
    public string TotalCredits => (GroupedByDate.FirstOrDefault() is BankTransaction
        ? GroupedByDate.OfType<BankTransaction>().Sum(static i => i.AmountCredit) ?? decimal.Zero
        : decimal.Zero).ToString("C2");

    [JsonIgnore]
    public string TotalDebits => (GroupedByDate.FirstOrDefault() is BankTransaction
        ? GroupedByDate.OfType<BankTransaction>().Sum(static i => i.AmountDebit) ?? decimal.Zero
        : decimal.Zero).ToString("C2");

    [JsonIgnore]
    public ConcurrentStack<string> UndoStack
    {
        get;
    } = new();

    [JsonIgnore]
    public ConcurrentStack<string> RedoStack
    {
        get;
    } = new();

    public void AddUndo()
    {
        string json = JsonConvert.SerializeObject(this);

        if(UndoStack.TryPeek(out string? old) && (old == json))
        {
            return;
        }

        UndoStack.Push(json);
        RedoStack.Clear();
    }
    public void AddRedo() => RedoStack.Push(JsonConvert.SerializeObject(this));

    [JsonIgnore]
    public bool CanUndo => UndoStack.Count > 0;
    [JsonIgnore]
    public bool CanRedo => RedoStack.Count > 0;

    [JsonIgnore]
    public NotifyingList<BankTransaction> Source
    {
        get => _source;
        set
        {
            if (SetProperty(ref _source, value))
            {
                ApplyFilter();
            }
        }
    }

    public object? Selected
    {
        get => SelectedTransaction;
        set
        {
            if (value == SelectedTransaction)
            {
                return;
            }

            if (value is BankTransaction transaction)
            {
                SelectedTransaction = transaction;
                AddFilterCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(SelectedTransaction));
                OnPropertyChanged(nameof(Selected));
                OnPropertyChanged(nameof(PotentialFilters));
            }
            else if (SelectedTransaction is not null)
            {
                SelectedTransaction = null;
                AddFilterCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(SelectedTransaction));
                OnPropertyChanged(nameof(Selected));
                OnPropertyChanged(nameof(PotentialFilters));
            }
        }
    }
    public BankTransaction? SelectedTransaction
    {
        get;
        private set;
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        if (!UndoStack.TryPop(out string? json))
        {
            return;
        }

        AddRedo();

        MainViewModel? anon =
            JsonConvert
                .DeserializeAnonymousType(json, this);

        if (anon is not null)
        {
            Merge(anon);
        }
    }
    public void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        if (!RedoStack.TryPop(out string? json))
        {
            return;
        }

        MainViewModel? anon =
            JsonConvert
                .DeserializeAnonymousType(json, this);

        if (anon is null)
        {
            return;
        }

        AddUndo();
        Merge(anon);
    }

    private void Merge(MainViewModel anon)
    {
        FilterText = anon.FilterText;
        FilterExpression = FilterText is not (null or "")
            ? new Regex(FilterText)
            : null;
        CurrentFilter = anon.CurrentFilter;
        ApplyFilter();

        Selected = anon.Selected;
        Filename = anon.Filename;
        IsActive = anon.IsActive;
        PotentialFilters.Clear();
        PotentialFilters.AddRange(anon.PotentialFilters);
    }

    [RelayCommand]
    public void Copy(object? text)
    {
        var request = new DataPackage
        {
            RequestedOperation = DataPackageOperation.Copy,
        };

        request.SetText(text?.ToString());

        Clipboard.SetContent(request);
    }

    [ RelayCommand ]
    public void Search(SfAutoComplete autoComplete)
    {
        string pattern = autoComplete.Text;
        Func<BankTransaction, bool> filter =
            trx => false;

        switch (SelectedPropertyInfo?.PropertyType.Name)
        {
            case "String":
                Regex regex = new(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                filter = trx
                    => SelectedPropertyInfo.GetValue(trx) is string value
                                && regex.IsMatch(value);
                break;

            case "Nullable`1":
                Type? type = SelectedPropertyInfo?.PropertyType.GetGenericArguments()
                    .FirstOrDefault();

                object? parsed = null;

                switch (type?.Name)
                {
                    case nameof(Decimal):
                        if (decimal.TryParse(pattern, out decimal d))
                        {
                            parsed = Math.Abs(d);


                            filter = trx => SelectedPropertyInfo?.GetValue(trx) is decimal value
                                            && value.Equals(parsed);
                        }

                        break;

                    case nameof(DateOnly):
                        if (DateOnly.TryParse(pattern, out DateOnly da))
                        {
                            parsed = da;


                            filter = trx => SelectedPropertyInfo?.GetValue(trx) is DateOnly value
                                            && value.Equals(parsed);

                            break;
                        }

                        if (int.TryParse(pattern, out int i))
                        {
                            parsed = i;


                            filter = trx => SelectedPropertyInfo?.GetValue(trx) is DateOnly value &&
                                            value.Year.Equals(parsed);

                            break;
                        }

                        break;

                }
                break;

            case "DateOnly":
                if (!DateOnly.TryParse(pattern, out var date))
                {
                    return;
                }

                filter = trx=>
                {
                    if (SelectedPropertyInfo.GetValue(trx) is not DateOnly value)
                    {
                        return false;

                    }

                    return value == date;
                };

                break;
        }

        var filtered = Unmodified.Where(filter);

        Source.Clear();
        Source.AddRange(filtered);
    }
}
