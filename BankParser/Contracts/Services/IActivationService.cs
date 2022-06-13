using System.Threading.Tasks;

namespace BankParser.Contracts.Services;

public interface IActivationService
{
    Task ActivateAsync(object activationArgs);
}
