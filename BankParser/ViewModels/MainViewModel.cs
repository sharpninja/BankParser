// ReSharper disable JoinDeclarationAndInitializer

// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace BankParser.ViewModels;

public partial class MainViewModel : ObservableObject, INavigationAware
{
    private MainViewModelBase _base = new();

    [ ObservableProperty, NotifyPropertyChangedFor(nameof(MainViewModel.FilerSelectedVisibility)), ]
    private string? _filterText;

    public ITransactionService TransactionService
    {
        get;
    }

    [ JsonIgnore ]
    public List<BankTransactionView> Unmodified => TransactionService.Unmodified;

    // ReSharper disable once UnusedMember.Global
    private NotifyingList<BankTransactionView> _source = null!;

    [ObservableProperty]
    private SfComboBoxItem? _selectedProperty;

    public PropertyInfo[] SelectedPropertyInfo
        => (_selectedProperty?.Content as string ?? "All") switch
        {
            "Desc" => new[]
                { typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.Description))!,},
            "Memo" => new[]
                { typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.Memo))!,},
            "Date" => new[]
                { typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.Date))!,},
            "Deposit" => new[]
                { typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.AmountCredit))!,},
            "Withdrawal" => new[]
                { typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.AmountDebit))!,},
            _ => new[]
            {
            typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.OtherParty))!,
            typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.Description))!,
            typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.Memo))!,
            typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.Date))!,
            typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.AmountDebitString))!,
            typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.AmountCreditString))!,
        },
        };

    [JsonIgnore]
    public List<string> PotentialFilters =>
        SelectedTransaction is
        {
        } bt
            ? bt.PotentialFilters.ToList()
            : new();

    [JsonIgnore]
    public Visibility FilerSelectedVisibility
        => FilterText is not (null or "") && SelectedTransaction is not null
            ? Visibility.Visible
            : Visibility.Collapsed;

    public MainViewModel(ITransactionService transactionService)
    {
        TransactionService = transactionService;
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
            IEnumerable<IGrouping<DateOnly, BankTransactionView>> grouped =
                items.OfType<BankTransactionView>()
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

    private void LoadData(string filename)
    {
        TransactionService.LoadData(filename);

        Source = new(Unmodified);
        SetGroupByDate(Source);

        // ReSharper disable once ExplicitCallerInfoArgument
        OnPropertyChanged("Reset");
    }

    [ObservableProperty]
    private RichEditTextDocument _document;

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
            ConcurrentBag<BankTransactionView> visibleItems = new();

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
                List<BankTransactionView> filtered =
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

    internal void ClearFilter()
    {
        _base.CurrentFilter = null;
        List<BankTransactionView> list =
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

            IntPtr hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(fileOpenPicker, hwnd);

            fileOpenPicker.ViewMode = PickerViewMode.List;
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.Downloads;
            fileOpenPicker.FileTypeFilter.Add(".csv");
            StorageFile? result = await fileOpenPicker.PickSingleFileAsync();

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

    private TriggerCollection? _notesTriggers;

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

    [JsonIgnore]
    public string TotalCredits => (GroupedByDate.FirstOrDefault() is BankTransactionView
        ? GroupedByDate.OfType<BankTransactionView>().Sum(static i => i.AmountCredit) ?? decimal.Zero
        : decimal.Zero).ToString("C2");

    [JsonIgnore]
    public string TotalDebits => (GroupedByDate.FirstOrDefault() is BankTransactionView
        ? GroupedByDate.OfType<BankTransactionView>().Sum(static i => i.AmountDebit) ?? decimal.Zero
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
        string json = JsonConvert.SerializeObject(_base);

        if (UndoStack.TryPeek(out string? old) && (old == json))
        {
            return;
        }

        UndoStack.Push(json);
        RedoStack.Clear();
    }
    public void AddRedo() => RedoStack.Push(JsonConvert.SerializeObject(_base));

    [JsonIgnore]
    public bool CanUndo => UndoStack.Count > 0;
    [JsonIgnore]
    public bool CanRedo => RedoStack.Count > 0;

    [JsonIgnore]
    public NotifyingList<BankTransactionView> Source
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

            if (value is BankTransactionView transaction)
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

    [ObservableProperty]
    private ObservableCollection<object> _selectedItems;

    public BankTransactionView? SelectedTransaction
    {
        get => _base.SelectedTransaction;
        private set
        {
            if (value is null)
            {
                _base.SelectedTransaction = null;
                Document.ClearUndoRedoHistory();
                Document.SetText(TextSetOptions.None, "");

                OnPropertyChanged(nameof(SelectedTransaction));
                OnPropertyChanged(nameof(SelectedTransactionIsNotNull));
            }

            if (_base.SelectedTransaction is not null)
            {
                Document.GetText(TextGetOptions.UseCrlf, out string? notes);
                _base.SelectedTransaction.Notes = notes;
            }

            if (_base.SelectedTransaction == value)
            {
                return;
            }

            _base.SelectedTransaction = value;

            OnPropertyChanged(nameof(SelectedTransaction));
            OnPropertyChanged(nameof(SelectedTransactionIsNotNull));

            Document.ClearUndoRedoHistory();
            Document.SetText(
                TextSetOptions.None,
                _base.SelectedTransaction.Notes ?? "");
        }
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

        _base =
            JsonConvert
                .DeserializeAnonymousType(json, _base);
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

        _base =
            JsonConvert
                .DeserializeAnonymousType(json, _base);

        AddUndo();
    }

    private ConcurrentBag<BankTransactionView> FilteredBag
    {
        get;
    } = new();

    private ValueTask ProcessFilter(
        BankTransactionFilters g,
        CancellationToken token
    )
    {
        if (token.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(token);
        }

        (BankTransactionView trx, Dictionary<PropertyInfo, Predicate<BankTransactionView>> predicates) = g;

        foreach (Predicate<BankTransactionView> predicate in predicates.Values)
        {
            if (token.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(token);
            }

            if (!predicate(trx))
            {
                continue;
            }

            FilteredBag.Add(trx);

            return ValueTask.CompletedTask;
        }

        return ValueTask.CompletedTask;
    }

    public bool SelectedTransactionIsNotNull => SelectedTransaction is not null;
}

