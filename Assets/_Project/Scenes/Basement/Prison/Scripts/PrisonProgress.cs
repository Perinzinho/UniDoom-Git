using System.Collections.Generic;
using UnityEngine;
using UniDoom.CampusLibrary;

/// <summary>Prison progress lasts for the current run, including checkpoint reloads.</summary>
public static class PrisonProgress
{
    public const int CellCount = 14;
    public static bool BossDefeated { get; set; }
    public static bool AllCellsOpened => LibraryMissionProgress.IsLibraryGeneratorPowered && openedCells.Count >= CellCount;
    public static bool FinaleStarted { get; private set; }
    public static string CellKeyId(int id) => id >= 1 && id <= CellCount ? "professor-cell-" + id.ToString("00") : string.Empty;
    public static bool HasCellKey(int id, PlayerCellKeys inventory) =>
        inventory != null && inventory.HasKey(CellKeyId(id));
    public static bool HasKey
    {
        get
        {
            var player = GameManager.SessionPlayer;
            var inventory = player ? player.GetComponent<PlayerCellKeys>() : null;
            for (int id = 1; id <= CellCount; id++) if (HasCellKey(id, inventory)) return true;
            return false;
        }
    }
    static readonly HashSet<int> openedCells = new();

    public static bool CanOpenCell(int id, PlayerCellKeys inventory) =>
        id >= 1 && id <= CellCount && LibraryMissionProgress.IsLibraryGeneratorPowered;
    public static bool IsCellOpen(int id) => CanOpenCell(id, null) && openedCells.Contains(id);
    public static bool OpenCell(int id)
    {
        return CanOpenCell(id, null) && openedCells.Add(id);
    }

    public static bool BeginFinale()
    {
        if (!AllCellsOpened) return false;
        FinaleStarted = true;
        return true;
    }

    public static void BeginFinaleTest()
    {
        // The dedicated ending shortcut prepares the real generator state in its clean session.
        LibraryMissionProgress.ResetSession();
        LibraryMissionProgress.Discover();
        for (int id = 0; id < 3; id++) LibraryMissionProgress.Collect(id);
        while (LibraryMissionProgress.Install()) { }
        int[] sequence = { 1, 2, 3, 4, 5, 6, 7 };
        foreach (int button in sequence) LibraryMissionProgress.Press(button, sequence, out _);
        LibraryMissionProgress.PowerOn();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        BossDefeated = false;
        FinaleStarted = false;
        openedCells.Clear();
    }
}
