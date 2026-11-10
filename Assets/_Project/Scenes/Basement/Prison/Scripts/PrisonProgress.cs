using System.Collections.Generic;
using UnityEngine;

/// <summary>Prison progress lasts for the current run, including checkpoint reloads.</summary>
public static class PrisonProgress
{
    public const int CellCount = 14;
    // Temporary layout-review state. Set to false when the cell/key design is approved.
    // Checklist: Docs/Maps/Basement-Celas-Pendencias.md.
    public const bool AllowKeylessCellOpeningForLayoutReview = true;
    public static bool BossDefeated { get; set; }
    public static bool AllCellsOpened => openedCells.Count >= CellCount;
    public static bool FinaleStarted { get; private set; }
    public static bool FinaleFlowTestAccess { get; private set; }
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
        AllowKeylessCellOpeningForLayoutReview || FinaleFlowTestAccess || HasCellKey(id, inventory);
    public static bool IsCellOpen(int id) => openedCells.Contains(id);
    public static void OpenCell(int id)
    {
        if (id >= 1 && id <= CellCount) openedCells.Add(id);
    }

    public static bool BeginFinale()
    {
        if (!AllCellsOpened) return false;
        FinaleStarted = true;
        return true;
    }

    public static void BeginFinaleTest()
    {
        // The menu shortcut must exercise the whole ending: cells remain closed and
        // only their key requirement is bypassed for this clean test session.
        FinaleFlowTestAccess = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        BossDefeated = false;
        FinaleStarted = false;
        FinaleFlowTestAccess = false;
        openedCells.Clear();
    }
}
