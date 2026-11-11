using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class PrisonPlayValidation
{
    private const string ActiveKey = "UniDoom.PrisonPlayValidation.Active";
    private const string ReportKey = "UniDoom.PrisonPlayValidation.Report";

    static PrisonPlayValidation() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

    public static void Run()
    {
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetString(ReportKey, string.Empty);
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Basement/Basement.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) EditorApplication.delayCall += Validate;
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/prison-play-validation.txt"));
            File.WriteAllText(output, SessionState.GetString(ReportKey, "No report."));
            SessionState.EraseBool(ActiveKey);
            SessionState.EraseString(ReportKey);
            Debug.Log($"[PrisonPlayValidation] {output}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }

    private static void Validate()
    {
        var report = new StringBuilder();
        try
        {
            var controller = Object.FindAnyObjectByType<PrisonController>();
            if (controller == null) throw new System.InvalidOperationException("PrisonController not found.");
            if (UniDoom.UI.GameMenus.Instance != null) Object.DestroyImmediate(UniDoom.UI.GameMenus.Instance.gameObject);
            var player = new GameObject("[Prison validation player]");
            var keys = player.AddComponent<PlayerPickupKey>();
            var inventory = player.AddComponent<PlayerCellKeys>();
            PrisonProgress.Reset();
            var gates = Object.FindObjectsByType<PrisonCellGate>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(g => g.CellId).ToArray();
            foreach (var gate in gates)
                inventory.TryAddKey(PrisonProgress.CellKeyId(gate.CellId));
            report.AppendLine($"gates={gates.Length}");
            foreach (var gate in gates)
            {
                var voice = gate.Voice;
                bool hasClip = voice != null && voice.clip != null;
                bool opened = controller.TryOpenCell(gate, keys);
                report.AppendLine($"cell={gate.CellId:00}; opened={opened}; isOpen={gate.IsOpen}; teacher={(voice != null ? voice.name : "NO_AUDIO_SOURCE")}; clip={(hasClip ? voice.clip.name : "NO_CLIP")}; playRequested={(hasClip && voice.isPlaying)}");
                if (voice != null) voice.Stop();
            }
            Object.Destroy(player);
        }
        catch (System.Exception e) { report.AppendLine("ERROR=" + e); }
        string result = report.ToString();
        SessionState.SetString(ReportKey, result);
        var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/prison-play-validation.txt"));
        File.WriteAllText(output, result);
        Debug.Log("[PrisonPlayValidationResult]\n" + result);
        EditorApplication.ExitPlaymode();
    }
}
