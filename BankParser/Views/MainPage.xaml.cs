using System.Collections.Specialized;
using System.ComponentModel;

using BankParser.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Newtonsoft.Json.Linq;

using Syncfusion.UI.Xaml.DataGrid;
using Syncfusion.UI.Xaml.TreeGrid;

namespace BankParser.Views;

// TODO: Change the grid as appropriate for your app. Adjust the column definitions on DataGridPage.xaml.
// For more details, see the documentation at https://docs.microsoft.com/windows/communitytoolkit/controls/datagrid.
public sealed partial class MainPage
{
    public MainViewModel? ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>()!;

        Loaded += MainPage_Loaded;

        InitializeComponent();
    }

    private void MainPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

        ViewModel!.GroupedByDate.CollectionChanged -= Source_CollectionChanged;
        ViewModel.GroupedByDate.CollectionChanged += Source_CollectionChanged; ;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.PropertyChanging -= ViewModel_PropertyChanging;
        ViewModel.PropertyChanging += ViewModel_PropertyChanging;
    }

    private void ViewModel_PropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewModel.Selected):
                break;
        }
    }

    private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                Tree.ExpandAllNodes();
                ResizeColumns();
                //Tree.RepopulateTree();
                break;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName == "Reset")
        {
            Tree.ClearFilters();
            Tree.ClearSelections(false);
        }
    }

    private void SfDataGrid_Loaded(object sender, RoutedEventArgs e)
    {
        if(sender is SfTreeGrid grid)
        {

        }
    }

    private void Tree_ItemsSourceChanged(object sender, TreeGridItemsSourceChangedEventArgs e)
    {

    }

    private void Tree_NodeExpanded(object sender, NodeExpandedEventArgs e)
    {
    }

    private void Main_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ResizeColumns();
    }

    private void ResizeColumns()
    {
        foreach (var column in Tree.Columns)
        {
            Tree.ColumnSizer.ResetAutoCalculation(column);
        }
        Tree.ColumnSizer.Refresh();
    }
}
