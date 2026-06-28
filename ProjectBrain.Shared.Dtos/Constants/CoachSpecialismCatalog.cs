namespace ProjectBrain.Shared.Constants;

/// <summary>
/// Canonical coach specialism options seeded into CoachSpecialismOptions.
/// Merged and deduplicated from web find-coaches and Flutter find-coach lists.
/// </summary>
public static class CoachSpecialismCatalog
{
    public static readonly IReadOnlyList<string> DefaultOptions = new[]
    {
        "Academic Support",
        "ADHD",
        "Anxiety",
        "Autism",
        "Behavioral Issues",
        "Bipolar Disorder",
        "Career Coaching",
        "Depression",
        "Dyscalculia",
        "Dysgraphia",
        "Dyspraxia",
        "Dyslexia",
        "Executive Functioning",
        "Learning Disabilities",
        "Life Coaching",
        "OCD",
        "Other",
        "Social Skills",
        "Tourette Syndrome",
    };
}
