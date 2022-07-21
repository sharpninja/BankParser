//using System.Text.RegularExpressions;

namespace BankParser.Views;

public class CustomAsyncFilter : IAutoCompleteFilterBehavior
{
    public CustomAsyncFilter()
    {
        MainViewModel = App.GetService<MainViewModel>()!;
    }

    private MainViewModel MainViewModel
    {
        get;
    }

    public CustomAsyncFilter(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public Task<object?> GetMatchingItemsAsync(
        SfAutoComplete source,
        AutoCompleteFilterInfo filterInfo
    )
        => Task.FromResult<object?>(null);
    //string pattern = filterInfo.Text;
    //return Task.FromResult(
    //    source.TextSearchMode switch
    //    {
    //        AutoCompleteTextSearchMode.StartsWith =>
    //            MainViewModel.Source.Where(
    //                item =>
    //                    item.ApplyFilter(
    //                    new Regex(
    //                        "^" + pattern,
    //                        RegexOptions.IgnoreCase)))
    //                .Select(static item => item.OtherParty)
    //                .OrderBy(static item => item)
    //                .Distinct(),
    //        AutoCompleteTextSearchMode.Contains =>
    //            MainViewModel.Source.Where(
    //            item =>
    //                item.ApplyFilter(
    //                    new Regex(
    //                        pattern,
    //                        RegexOptions.IgnoreCase)))
    //                .Select(static item => item.OtherParty)
    //                .OrderBy(static item => item)
    //                .Distinct(),
    //        _ => (object)Array.Empty<string>(),
    //    }
    //);
}
