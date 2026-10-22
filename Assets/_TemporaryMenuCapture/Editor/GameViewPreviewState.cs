using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Editor-only preview state. Capture tools must restore zoom/pan as well as resolution.
public sealed class GameViewPreviewState
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Type ViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
    readonly EditorWindow window;
    readonly int sizeIndex;
    readonly Vector2 scale, translation;

    GameViewPreviewState(EditorWindow view)
    {
        window = view;
        sizeIndex = (int)ViewType.GetProperty("selectedSizeIndex", Flags).GetValue(view);
        var data = new SerializedObject(view);
        scale = Required(data, "m_ZoomArea.m_Scale").vector2Value;
        translation = Required(data, "m_ZoomArea.m_Translation").vector2Value;
    }

    public static GameViewPreviewState Capture() => new(EditorWindow.GetWindow(ViewType));

    public void Restore()
    {
        if (window == null) return;
        var selected = ViewType.GetProperty("selectedSizeIndex", Flags);
        if ((int)selected.GetValue(window) != sizeIndex) selected.SetValue(window, sizeIndex);
        SetTransform(window, scale, translation);
    }

    static SerializedProperty Required(SerializedObject data, string path)
        => data.FindProperty(path) ?? throw new InvalidOperationException("Game View preview field unavailable: " + path);

    static void SetTransform(EditorWindow view, Vector2 zoom, Vector2 pan)
    {
        var data = new SerializedObject(view);
        Required(data, "m_ZoomArea.m_Scale").vector2Value = zoom;
        Required(data, "m_ZoomArea.m_Translation").vector2Value = pan;
        data.ApplyModifiedPropertiesWithoutUndo();
        view.Repaint();
    }

    [MenuItem("Tools/UniDoom/Inspect Game View")]
    public static void Inspect() => WriteReport("Current", EditorWindow.GetWindow(ViewType));

    [MenuItem("Tools/UniDoom/Fit Game View")]
    public static void Fit()
    {
        var view = EditorWindow.GetWindow(ViewType);
        WriteReport("Before fit", view);
        var data = new SerializedObject(view);
        var area = Required(data, "m_ZoomArea.m_DrawArea").rectValue;
        var target = (Vector2)ViewType.GetProperty("targetSize", Flags).GetValue(view);
        if (target.x <= 0 || target.y <= 0 || area.width <= 0 || area.height <= 0)
            throw new InvalidOperationException("Game View has no valid preview dimensions.");
        float fit = Mathf.Min(area.width / target.x, area.height / target.y) * EditorGUIUtility.pixelsPerPoint;
        SetTransform(view, Vector2.one * fit, area.size * .5f);
        WriteReport("After fit", view);
    }

    [MenuItem("Tools/UniDoom/Verify Game View Restore")]
    public static void VerifyRestore()
    {
        var saved = Capture();
        var selected = ViewType.GetProperty("selectedSizeIndex", Flags);
        selected.SetValue(saved.window, saved.sizeIndex == 0 ? 1 : 0);
        SetTransform(saved.window, Vector2.one * 2, Vector2.zero);
        saved.Restore();
        var restored = new SerializedObject(saved.window);
        bool valid = (int)selected.GetValue(saved.window) == saved.sizeIndex &&
            Required(restored, "m_ZoomArea.m_Scale").vector2Value == saved.scale &&
            Required(restored, "m_ZoomArea.m_Translation").vector2Value == saved.translation;
        WriteReport(valid ? "PASS: resolution/zoom/pan restored" : "FAIL: preview state changed", saved.window);
    }

    static void WriteReport(string label, EditorWindow view)
    {
        if (view == null) return;
        var data = new SerializedObject(view);
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../.codex/menus"));
        File.AppendAllText(Path.Combine(folder, "game-view-layout.txt"), label +
            "\nResolution=" + ViewType.GetProperty("targetSize", Flags).GetValue(view) +
            "\nViewport=" + Required(data, "m_ZoomArea.m_DrawArea").rectValue +
            "\nZoom=" + Required(data, "m_ZoomArea.m_Scale").vector2Value.ToString("F4") +
            "\nPan=" + Required(data, "m_ZoomArea.m_Translation").vector2Value + "\n\n");
    }
}
