using System;
using System.Collections.Generic;

namespace PoliteEorzea;

/// <summary>Chooses the next greeting or farewell, randomly or in sequence.</summary>
public sealed class MessagePicker
{
    private readonly Random random = new();
    private int greetIndex;
    private int byeIndex;
    private string? lastGreet;
    private string? lastBye;

    public string? NextGreeting(Configuration config) =>
        Next(config.Greetings, config.RandomOrder, ref greetIndex, ref lastGreet);

    public string? NextFarewell(Configuration config) =>
        Next(config.Farewells, config.RandomOrder, ref byeIndex, ref lastBye);

    private string? Next(List<string> list, bool randomOrder, ref int index, ref string? last)
    {
        var candidates = new List<string>(list.Count);
        foreach (var s in list)
        {
            if (!string.IsNullOrWhiteSpace(s))
                candidates.Add(s.Trim());
        }

        if (candidates.Count == 0)
            return null;

        string chosen;
        if (randomOrder)
        {
            chosen = candidates[random.Next(candidates.Count)];

            // Avoid repeating the same line twice in a row when we have a choice.
            if (candidates.Count > 1 && chosen == last)
                chosen = candidates[(candidates.IndexOf(chosen) + 1 + random.Next(candidates.Count - 1)) % candidates.Count];
        }
        else
        {
            if (index >= candidates.Count)
                index = 0;
            chosen = candidates[index];
            index = (index + 1) % candidates.Count;
        }

        last = chosen;
        return chosen;
    }
}
