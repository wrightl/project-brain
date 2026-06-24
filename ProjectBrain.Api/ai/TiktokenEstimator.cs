namespace ProjectBrain.AI;

using Microsoft.ML.Tokenizers;

/// <summary>Model-aware token counting using cl100k_base (GPT-4 family).</summary>
public sealed class TiktokenEstimator : ITokenEstimator
{
    private static readonly TiktokenTokenizer Tokenizer =
        TiktokenTokenizer.CreateForModel("gpt-4");

    public int EstimateTokens(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return Tokenizer.CountTokens(text);
    }
}
