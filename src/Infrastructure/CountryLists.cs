namespace TransactionCompliance.Infrastructure;

public static class CountryLists
{
    public static readonly string[] SanctionedCountries =
        ["KP", "IR", "SY", "CU", "RU"];

    public static readonly string[] HighRiskCountries =
        [..SanctionedCountries, "AF", "IQ", "LY", "YE", "SO"];
}
