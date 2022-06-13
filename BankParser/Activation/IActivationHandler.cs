using System.Threading.Tasks;

namespace BankParser.Activation;

public interface IActivationHandler
{
    bool CanHandle(object args);

    Task HandleAsync(object args);
}
