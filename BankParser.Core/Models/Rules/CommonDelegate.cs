namespace BankParser.Core.Models.Rules;

public record CommonDelegate(Func<BankTransactionView, IParameters, bool> Delegate, Type DelegateType)
{
    public string Name => DelegateType.Name;

    protected virtual object? DynamicInvoke(params object?[] args)
        => Delegate.DynamicInvoke(args);

    public bool Invoke(BankTransactionView trx, IParameters parameters)
        => Delegate.Invoke(trx, parameters);

}
