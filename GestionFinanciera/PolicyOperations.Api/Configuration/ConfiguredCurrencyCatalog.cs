using PolicyOperations.Application.Abstractions;

namespace PolicyOperations.Api.Configuration;

internal sealed class ConfiguredCurrencyCatalog : ICurrencyCatalog
{
    private readonly HashSet<string> _supportedCurrencies;

    public ConfiguredCurrencyCatalog(IEnumerable<string> supportedCurrencies)
    {
        _supportedCurrencies = new HashSet<string>(StringComparer.Ordinal);

        foreach (var currency in supportedCurrencies)
        {
            var normalizedCurrency = currency.Trim().ToUpperInvariant();

            if (normalizedCurrency.Length != 3 ||
                normalizedCurrency.Any(character => character is < 'A' or > 'Z'))
            {
                throw new InvalidOperationException(
                    "Configured currencies must use three-letter ISO 4217 alphabetic codes.");
            }

            _supportedCurrencies.Add(normalizedCurrency);
        }

        if (_supportedCurrencies.Count == 0)
        {
            throw new InvalidOperationException(
                "PolicyOperations:SupportedCurrencies must contain at least one currency.");
        }
    }

    public bool IsSupported(string currency)
    {
        return _supportedCurrencies.Contains(currency);
    }
}
