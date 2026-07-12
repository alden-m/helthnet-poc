namespace FindMyPath.Poc.Models;

/// <summary>All option lists for the questionnaire (Appendix C of the spec). Use exactly these.</summary>
public static class OptionCatalog
{
    public static readonly string[] Professions =
    {
        "Physician", "Nurse", "Pharmacist", "Dentist", "Physiotherapist",
        "Occupational Therapist", "Medical Laboratory Technologist", "Other"
    };

    public static readonly string[] YearsExperience =
    {
        "Less than 1 year", "1–3 years", "4–7 years", "8+ years"
    };

    public static readonly string[] Provinces =
    {
        "Alberta", "British Columbia", "Manitoba", "New Brunswick",
        "Newfoundland and Labrador", "Nova Scotia", "Ontario",
        "Prince Edward Island", "Quebec", "Saskatchewan",
        "Northwest Territories", "Nunavut", "Yukon"
    };

    /// <summary>Provinces plus a "Not sure yet" escape hatch, for the folded-in Target Province question.</summary>
    public static readonly string[] TargetProvinces =
        Provinces.OrderBy(p => p).Append("Not sure yet").ToArray();

    public static readonly string[] ImmigrationStatuses =
    {
        "Citizen", "PR", "Work Permit", "Study Permit",
        "Visitor", "Refugee", "Other"
    };

    public static readonly string[] Exams =
    {
        "MCCQE Part I", "NAC OSCE", "NCLEX", "PEBC", "OSCE",
        "IELTS", "CELBAN", "OET", "None"
    };

    public static readonly string[] LanguageTests =
    {
        "IELTS", "CELBAN", "OET", "TOEFL", "Other"
    };

    public static readonly string[] Goals =
    {
        "Obtain professional licence", "Practise in Canada", "Prepare for examinations",
        "Find employment", "Explore alternative careers", "Improve communication skills",
        "Build professional network"
    };

    public static readonly string[] LearningNeeds =
    {
        "Canadian Healthcare System", "Clinical Skills", "Communication Skills",
        "Clinical Ethics", "Documentation", "Interview Preparation",
        "Resume Development", "Career Planning", "Cultural Competence",
        "Emotional Intelligence", "Leadership", "Research Skills", "Exam Preparation"
    };

    /// <summary>Full country list (Appendix C). Alphabetical, with "Other" kept last.</summary>
    public static readonly string[] Countries =
    {
        "Afghanistan", "Albania", "Algeria", "Andorra", "Angola", "Antigua and Barbuda",
        "Argentina", "Armenia", "Australia", "Austria", "Azerbaijan", "Bahamas", "Bahrain",
        "Bangladesh", "Barbados", "Belarus", "Belgium", "Belize", "Benin", "Bhutan", "Bolivia",
        "Bosnia and Herzegovina", "Botswana", "Brazil", "Brunei", "Bulgaria", "Burkina Faso",
        "Burundi", "Cabo Verde", "Cambodia", "Cameroon", "Canada", "Central African Republic",
        "Chad", "Chile", "China", "Colombia", "Comoros", "Congo (Republic of the)",
        "Congo (Democratic Republic of the)", "Costa Rica", "Croatia", "Cuba", "Cyprus",
        "Czechia", "Denmark", "Djibouti", "Dominica", "Dominican Republic", "Ecuador", "Egypt",
        "El Salvador", "Equatorial Guinea", "Eritrea", "Estonia", "Eswatini", "Ethiopia", "Fiji",
        "Finland", "France", "Gabon", "Gambia", "Georgia", "Germany", "Ghana", "Greece",
        "Grenada", "Guatemala", "Guinea", "Guinea-Bissau", "Guyana", "Haiti", "Honduras",
        "Hungary", "Iceland", "India", "Indonesia", "Iran", "Iraq", "Ireland", "Israel", "Italy",
        "Ivory Coast", "Jamaica", "Japan", "Jordan", "Kazakhstan", "Kenya", "Kiribati", "Kosovo",
        "Kuwait", "Kyrgyzstan", "Laos", "Latvia", "Lebanon", "Lesotho", "Liberia", "Libya",
        "Liechtenstein", "Lithuania", "Luxembourg", "Madagascar", "Malawi", "Malaysia",
        "Maldives", "Mali", "Malta", "Marshall Islands", "Mauritania", "Mauritius", "Mexico",
        "Micronesia", "Moldova", "Monaco", "Mongolia", "Montenegro", "Morocco", "Mozambique",
        "Myanmar", "Namibia", "Nauru", "Nepal", "Netherlands", "New Zealand", "Nicaragua",
        "Niger", "Nigeria", "North Korea", "North Macedonia", "Norway", "Oman", "Pakistan",
        "Palau", "Palestine", "Panama", "Papua New Guinea", "Paraguay", "Peru", "Philippines",
        "Poland", "Portugal", "Qatar", "Romania", "Russia", "Rwanda", "Saint Kitts and Nevis",
        "Saint Lucia", "Saint Vincent and the Grenadines", "Samoa", "San Marino",
        "Sao Tome and Principe", "Saudi Arabia", "Senegal", "Serbia", "Seychelles",
        "Sierra Leone", "Singapore", "Slovakia", "Slovenia", "Solomon Islands", "Somalia",
        "South Africa", "South Korea", "South Sudan", "Spain", "Sri Lanka", "Sudan", "Suriname",
        "Sweden", "Switzerland", "Syria", "Taiwan", "Tajikistan", "Tanzania", "Thailand",
        "Timor-Leste", "Togo", "Tonga", "Trinidad and Tobago", "Tunisia", "Turkey",
        "Turkmenistan", "Tuvalu", "Uganda", "Ukraine", "United Arab Emirates", "United Kingdom",
        "United States", "Uruguay", "Uzbekistan", "Vanuatu", "Vatican City", "Venezuela",
        "Vietnam", "Yemen", "Zambia", "Zimbabwe", "Other"
    };
}
