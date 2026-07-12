using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Tests.Models;

public class OptionCatalogTests
{
    [Fact]
    public void QuestionnaireOptionCatalogsMatchThePrdExactly()
    {
        AssertExact(
            [
                "Physician",
                "Nurse",
                "Pharmacist",
                "Dentist",
                "Physiotherapist",
                "Occupational Therapist",
                "Medical Laboratory Technologist",
                "Other",
            ],
            OptionCatalog.Professions);

        AssertExact(
            ["Less than 1 year", "1–3 years", "4–7 years", "8+ years"],
            OptionCatalog.YearsExperience);

        AssertExact(
            [
                "Alberta",
                "British Columbia",
                "Manitoba",
                "New Brunswick",
                "Newfoundland and Labrador",
                "Nova Scotia",
                "Ontario",
                "Prince Edward Island",
                "Quebec",
                "Saskatchewan",
                "Northwest Territories",
                "Nunavut",
                "Yukon",
            ],
            OptionCatalog.Provinces);

        AssertExact(
            [
                "Citizen",
                "PR",
                "Work Permit",
                "Study Permit",
                "Visitor",
                "Refugee",
                "Other",
            ],
            OptionCatalog.ImmigrationStatuses);

        AssertExact(
            ["MCCQE Part I", "NAC OSCE", "NCLEX", "PEBC", "OSCE", "IELTS", "CELBAN", "OET", "None"],
            OptionCatalog.Exams);

        AssertExact(
            ["IELTS", "CELBAN", "OET", "TOEFL", "Other"],
            OptionCatalog.LanguageTests);

        AssertExact(
            [
                "Obtain professional licence",
                "Practise in Canada",
                "Prepare for examinations",
                "Find employment",
                "Explore alternative careers",
                "Improve communication skills",
                "Build professional network",
            ],
            OptionCatalog.Goals);

        AssertExact(
            [
                "Canadian Healthcare System",
                "Clinical Skills",
                "Communication Skills",
                "Clinical Ethics",
                "Documentation",
                "Interview Preparation",
                "Resume Development",
                "Career Planning",
                "Cultural Competence",
                "Emotional Intelligence",
                "Leadership",
                "Research Skills",
                "Exam Preparation",
            ],
            OptionCatalog.LearningNeeds);
    }

    [Fact]
    public void TargetProvinceEnhancementHasEveryProvinceAlphabeticallyAndNotSureLast()
    {
        AssertExact(
            [
                "Alberta",
                "British Columbia",
                "Manitoba",
                "New Brunswick",
                "Newfoundland and Labrador",
                "Northwest Territories",
                "Nova Scotia",
                "Nunavut",
                "Ontario",
                "Prince Edward Island",
                "Quebec",
                "Saskatchewan",
                "Yukon",
                "Not sure yet",
            ],
            OptionCatalog.TargetProvinces);
    }

    [Fact]
    public void CountryCatalogMatchesThePocAppendixExactly()
    {
        var expected = ExpectedCountries.Split(
            '\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.Equal(198, expected.Length);
        AssertExact(expected, OptionCatalog.Countries);
        Assert.Equal("Other", OptionCatalog.Countries[^1]);
    }

    private static void AssertExact(string[] expected, string[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        Assert.Equal(expected, actual);
    }

    private const string ExpectedCountries = """
        Afghanistan
        Albania
        Algeria
        Andorra
        Angola
        Antigua and Barbuda
        Argentina
        Armenia
        Australia
        Austria
        Azerbaijan
        Bahamas
        Bahrain
        Bangladesh
        Barbados
        Belarus
        Belgium
        Belize
        Benin
        Bhutan
        Bolivia
        Bosnia and Herzegovina
        Botswana
        Brazil
        Brunei
        Bulgaria
        Burkina Faso
        Burundi
        Cabo Verde
        Cambodia
        Cameroon
        Canada
        Central African Republic
        Chad
        Chile
        China
        Colombia
        Comoros
        Congo (Republic of the)
        Congo (Democratic Republic of the)
        Costa Rica
        Croatia
        Cuba
        Cyprus
        Czechia
        Denmark
        Djibouti
        Dominica
        Dominican Republic
        Ecuador
        Egypt
        El Salvador
        Equatorial Guinea
        Eritrea
        Estonia
        Eswatini
        Ethiopia
        Fiji
        Finland
        France
        Gabon
        Gambia
        Georgia
        Germany
        Ghana
        Greece
        Grenada
        Guatemala
        Guinea
        Guinea-Bissau
        Guyana
        Haiti
        Honduras
        Hungary
        Iceland
        India
        Indonesia
        Iran
        Iraq
        Ireland
        Israel
        Italy
        Ivory Coast
        Jamaica
        Japan
        Jordan
        Kazakhstan
        Kenya
        Kiribati
        Kosovo
        Kuwait
        Kyrgyzstan
        Laos
        Latvia
        Lebanon
        Lesotho
        Liberia
        Libya
        Liechtenstein
        Lithuania
        Luxembourg
        Madagascar
        Malawi
        Malaysia
        Maldives
        Mali
        Malta
        Marshall Islands
        Mauritania
        Mauritius
        Mexico
        Micronesia
        Moldova
        Monaco
        Mongolia
        Montenegro
        Morocco
        Mozambique
        Myanmar
        Namibia
        Nauru
        Nepal
        Netherlands
        New Zealand
        Nicaragua
        Niger
        Nigeria
        North Korea
        North Macedonia
        Norway
        Oman
        Pakistan
        Palau
        Palestine
        Panama
        Papua New Guinea
        Paraguay
        Peru
        Philippines
        Poland
        Portugal
        Qatar
        Romania
        Russia
        Rwanda
        Saint Kitts and Nevis
        Saint Lucia
        Saint Vincent and the Grenadines
        Samoa
        San Marino
        Sao Tome and Principe
        Saudi Arabia
        Senegal
        Serbia
        Seychelles
        Sierra Leone
        Singapore
        Slovakia
        Slovenia
        Solomon Islands
        Somalia
        South Africa
        South Korea
        South Sudan
        Spain
        Sri Lanka
        Sudan
        Suriname
        Sweden
        Switzerland
        Syria
        Taiwan
        Tajikistan
        Tanzania
        Thailand
        Timor-Leste
        Togo
        Tonga
        Trinidad and Tobago
        Tunisia
        Turkey
        Turkmenistan
        Tuvalu
        Uganda
        Ukraine
        United Arab Emirates
        United Kingdom
        United States
        Uruguay
        Uzbekistan
        Vanuatu
        Vatican City
        Venezuela
        Vietnam
        Yemen
        Zambia
        Zimbabwe
        Other
        """;
}
