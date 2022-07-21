// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public interface IParameters
{
    public void MapParameters(Type type, params object[] parameters)
    {
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        if (properties.Length != parameters.Length)
        {
            throw new ArgumentNullException(
                $"Expected {properties.Length} parameters, received {parameters.Length}."
            );
        }

        List<Exception> errors = new();

        for (int index = 0; index < properties.Length; index++)
        {
            PropertyInfo pi = properties[index];
            Type pType = parameters[index]
                .GetType();

            if (!pType.IsAssignableTo(pi.PropertyType))
            {
                errors.Add(new ApplicationException($"Type Mismatch for {pi.Name}.  Expected {pi.PropertyType.FullName}, received {pType.FullName}"));
            }
            else
            {
                pi.SetValue(this, parameters[index]);
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(errors);
        }
    }
}
