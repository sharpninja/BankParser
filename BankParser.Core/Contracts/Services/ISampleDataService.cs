using System.Collections.Generic;
using System.Threading.Tasks;

using BankParser.Core.Models;

namespace BankParser.Core.Contracts.Services;

// Remove this class once your pages/features are using your data.
public interface ISampleDataService
{
    Task<IEnumerable<SampleOrder>> GetGridDataAsync();
}
