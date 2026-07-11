using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Services;

/// <summary>
/// Per-circuit (Scoped) holder for the demo-critical wizard + result state. Blazor destroys the
/// Home component when the user navigates to Settings/History (or clicks the active nav / brand
/// link), which would otherwise wipe a generated roadmap and every answer. Keeping this state in a
/// circuit-scoped service means navigating away and back rehydrates the same wizard/roadmap.
/// </summary>
public class WizardState
{
    public AssessmentAnswers Answers { get; private set; } = new();
    public int CurrentStep { get; set; } = 1;
    public RoadmapResult? Result { get; set; }

    /// <summary>Which roadmap steps the user has ticked (view-only), kept here so they survive navigation.</summary>
    public HashSet<string> CheckedSteps { get; } = new();

    /// <summary>Full reset for "Start over": new answers, back to step 1, clear result and ticks.</summary>
    public void ResetAll()
    {
        Answers = new();
        CurrentStep = 1;
        Result = null;
        CheckedSteps.Clear();
    }
}
