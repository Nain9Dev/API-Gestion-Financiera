namespace PolicyOperations.Application.Abstractions;

public interface ICurrencyCatalog
{
    bool IsSupported(string currency);
}
