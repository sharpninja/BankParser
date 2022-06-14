using BankParser.ViewModels;

using Microsoft.UI.Xaml.Controls;

using Syncfusion.UI.Xaml.DataGrid;

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
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }

    private void SfDataGrid_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if(sender is SfDataGrid grid)
        {

        }
    }
}
