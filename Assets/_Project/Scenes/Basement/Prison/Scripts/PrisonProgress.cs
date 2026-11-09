using System.Collections.Generic;
using UnityEngine;

/// <summary>Prison progress lasts for the current run, including checkpoint reloads.</summary>
public static class PrisonProgress
{
    public const int CellCount = 13;
    public static bool BossDefeated { get; set; }
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

    public static bool IsCellOpen(int id) => openedCells.Contains(id);
    public static void OpenCell(int id) => openedCells.Add(id);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        BossDefeated = false;
        openedCells.Clear();
    }
}
