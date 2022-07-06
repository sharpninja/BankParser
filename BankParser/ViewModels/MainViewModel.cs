using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;

using ABI.Windows.Media.Devices;

using BankParser.Contracts.ViewModels;
using BankParser.Core.Contracts.Services;
using BankParser.Core.Models;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace BankParser.ViewModels;

public partial class MainViewModel : ObservableRecipient, INavigationAware
{
    private const string DEFAULT_FILENAME = @"C:\Users\kingd\OneDrive\Desktop\transactions.json";

    private NotifyingList<BankTransaction> _source = null!;
    private ConcurrentStack<string> _redoStack;
    private ConcurrentStack<string> _undoStack;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(FilerSelectedVisibility))]
    private string? _filterText;

    private BankTransaction? _selected = null;

    [JsonIgnore]
    private List<BankTransaction> Unmodified
    {
        get;
        set;
    } = null!;

    [JsonIgnore]
    public List<string> PotentialFilters =>
        (_selected is BankTransaction bt)
            ? bt.PotentialFilters.ToList()
            : new();

    [JsonIgnore]
    public Visibility FilerSelectedVisibility
        => _filterText is not (null or "") && _selected is BankTransaction
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
                SetGroupByDate(Unmodified.OfType<object>());
            }
        }

        OnPropertyChanged(nameof(TotalCredits));
        OnPropertyChanged(nameof(TotalDebits));

        OnPropertyChanged(nameof(MainViewModel));
    }

    private void SetGroupByDate(IEnumerable<object> objects)
    {
        GroupedByDate.Clear();
        if (objects.Any())
        {
            int count = objects.Count();
            if (count != Unmodified.Count)
            {
                GroupedByDate.AddRange(objects);
            }
            else
            {
                GroupedByDate.AddRange(objects
                    .OfType<BankTransaction>()
                    .GroupBy(i => i.Date)
                    .Select(g => new DateGroup(g.Key, g.OfType<BankTransaction>().Cast<object>().ToList()))
                    .Cast<object>());
            }
        }
    }

    private void LoadData(string fileName)
    {
        if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            Unmodified = BankTransaction.FromJson(File.ReadAllText(fileName)).ToList();
        }
        else if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            Unmodified = BankTransaction.FromCsv(fileName).ToList();
        }

        Source = new(Unmodified);
        SetGroupByDate(Unmodified);

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
        if (FilterExpression is not null)
        {
            object? curentlySelected = Selected;
            string? filterText = FilterText;
            ConcurrentBag<BankTransaction> visibleItems = new();

            Parallel.ForEach(Unmodified, item =>
            {
                if (item.ApplyFilter(FilterExpression))
                {
                    visibleItems.Add(item);
                }
            });

            if (visibleItems.Count > 0)
            {
                void Callback()
                {
                    Source.Clear();

                    var filtered =
                        visibleItems
                            .OrderByDescending(i => i.Date)
                            .ToList();

                    Source.AddRange(filtered);

                    Selected = curentlySelected;
                    FilterText = filterText;
                }

                App.TryDispatch(Callback);
            }
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
        if (FilterText is not (null or ""))
        {
            string filterText = FilterText;
            FilterText = null;
            AddUndo();
            FilterText = filterText;

            CurrentFilter = FilterText;
            if (CurrentFilter is not null)
            {
                FilterExpression = new Regex(Regex.Escape(CurrentFilter),
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                ApplyFilter();
            }
        }
    }

    internal void ClearFilter()
    {
        CurrentFilter = null;
        List<BankTransaction> list =
            Unmodified
                .OrderByDescending(i => i.Date)
                .ToList();

        void callback()
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

        App.TryDispatch(callback);
    }

    internal async Task OpenFile(CancellationToken token)
    {
        try
        {
            FileOpenPicker fileOpenPicker = new();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, hwnd);

            fileOpenPicker.ViewMode = PickerViewMode.List;
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.Downloads;
            fileOpenPicker.FileTypeFilter.Add(".csv");
            var result = await fileOpenPicker.PickSingleFileAsync();

            if (result is not null)
            {
                LoadData(result.Path);
            }
        }
        catch
        {

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
                Unmodified.OrderByDescending(i => i.Date).ToList());
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
    public string TotalCredits => ((GroupedByDate.FirstOrDefault() is BankTransaction)
        ? GroupedByDate.OfType<BankTransaction>().Sum(i => i.AmountCredit) ?? decimal.Zero
        : decimal.Zero).ToString("C2");

    [JsonIgnore]
    public string TotalDebits => ((GroupedByDate.FirstOrDefault() is BankTransaction)
        ? GroupedByDate.OfType<BankTransaction>().Sum(i => i.AmountDebit) ?? decimal.Zero
        : decimal.Zero).ToString("C2");

    [JsonIgnore]
    public ConcurrentStack<string> UndoStack => _undoStack ??= new();

    [JsonIgnore]
    public ConcurrentStack<string> RedoStack => _redoStack ??= new();

    public void AddUndo()
    {
        string json = JsonConvert.SerializeObject(this);

        if(UndoStack.TryPeek(out string old) ? old == json : false)
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
        get => _source ??= new ();
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
        get => _selected;
        set
        {
            if (value == _selected)
            {
                return;
            }

            if (value is BankTransaction transaction)
            {
                _selected = transaction;
                AddFilterCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(SelectedTransaction));
                OnPropertyChanged(nameof(Selected));
                OnPropertyChanged(nameof(PotentialFilters));
            }
            else if (_selected is not null)
            {
                _selected = null;
                AddFilterCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(SelectedTransaction));
                OnPropertyChanged(nameof(Selected));
                OnPropertyChanged(nameof(PotentialFilters));
            }
        }
    }
    public BankTransaction? SelectedTransaction => _selected;

    public void Undo()
    {
        if (CanUndo)
        {
            if (UndoStack.TryPop(out var json))
            {
                AddRedo();

                MainViewModel? anon = JsonConvert.DeserializeAnonymousType(json, this);

                if (anon is not null)
                {
                    Merge(anon);
                }
            }
        }
    }
    public void Redo()
    {
        if (CanRedo)
        {
            if (RedoStack.TryPop(out var json))
            {
                MainViewModel? anon = JsonConvert.DeserializeAnonymousType(json, this);

                if (anon is not null)
                {
                    AddUndo();
                    Merge(anon);
                }
            }
        }
    }

    private void Merge(MainViewModel anon)
    {
        FilterText = anon.FilterText;
        if (FilterText is not (null or ""))
        {
            FilterExpression = new Regex(FilterText);
        }
        else
        {
            FilterExpression = null;
        }
        CurrentFilter = anon.CurrentFilter;
        ApplyFilter();

        Selected = anon.Selected;
        Filename = anon.Filename;
        IsActive = anon.IsActive;
        PotentialFilters.Clear();
        PotentialFilters.AddRange(anon.PotentialFilters);
    }

    [RelayCommand]
    public void Copy(object text)
    {
        var request = new DataPackage()
        {
            RequestedOperation = DataPackageOperation.Copy,
        };

        request.SetText(text?.ToString());

        Clipboard.SetContent(request);
    }
}
