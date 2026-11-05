using System.Collections.Generic;
using UnityEngine;

/// <summary>Prison progress lasts for the current run, including checkpoint reloads.</summary>
public static class PrisonProgress
{
    public static bool BossDefeated { get; set; }
    public static bool HasKey { get; set; }
    static readonly HashSet<int> openedCells = new();

    public static bool IsCellOpen(int id) => openedCells.Contains(id);
    public static void OpenCell(int id) => openedCells.Add(id);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        BossDefeated = false;
        HasKey = false;
        openedCells.Clear();
    }
}
