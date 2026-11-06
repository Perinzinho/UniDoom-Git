using System.Collections.Generic;
using UnityEngine;

/// <summary>Cell-specific keys carried by the persistent player during this run.</summary>
[DisallowMultipleComponent]
public sealed class PlayerCellKeys : MonoBehaviour
{
    readonly HashSet<string> keys = new HashSet<string>();
    readonly Dictionary<string,string> locations = new Dictionary<string,string>();
    readonly HashSet<string> completedEvents = new HashSet<string>();

    public bool HasKey(string cellId) => !string.IsNullOrEmpty(cellId) && keys.Contains(cellId);
    public bool TryAddKey(string cellId) => !string.IsNullOrEmpty(cellId) && keys.Add(cellId);
    public bool HasCompletedEvent(string eventId) => completedEvents.Contains(eventId);
    public void CompleteEvent(string eventId) => completedEvents.Add(eventId);

    public string GetOrChooseKeyLocation(string keyId, System.Collections.Generic.IReadOnlyList<string> choices)
    {
        if (choices == null || choices.Count == 0) return null;
        if (locations.TryGetValue(keyId,out var chosen))
            foreach (var choice in choices)
                if (choice == chosen) return chosen;
        string preference = "UniDoom.LastCellKeyRoom." + keyId;
        string previous = PlayerPrefs.GetString(preference, "");
        var candidates = new List<string>();
        foreach (var choice in choices) if (choice != previous || choices.Count == 1) candidates.Add(choice);
        chosen = candidates[Random.Range(0,candidates.Count)];
        locations[keyId] = chosen;
        PlayerPrefs.SetString(preference,chosen);
        PlayerPrefs.Save();
        return chosen;
    }
}
