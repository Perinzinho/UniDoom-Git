using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Temporary capture-only tooling. No gameplay, scene or asset changes are saved.
public static class MenuReferenceCapture
{
    static readonly string Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../.codex/menus"));
    static double readyAt;
    // Kept inactive after capture. Deletion requires explicit authorization in AGENTS.md.
    // There is no load hook and no EditorApplication.update subscription.

    static void Tick()
    {
        string request = Path.Combine(Folder, "capture-request.txt");
        if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlaying)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool("MenuCaptureStartedPlay", true);
            EditorApplication.isPlaying = true;
            return;
        }
        if (readyAt == 0) { readyAt = EditorApplication.timeSinceStartup + 12; return; }
        if (EditorApplication.timeSinceStartup < readyAt) return;
        File.Move(request, Path.Combine(Folder, "capture-request-" + DateTime.UtcNow.Ticks + ".consumed"));
        try
        {
            Camera camera = Camera.main;
            if (camera == null) throw new Exception("No active main camera");
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var previousRotation = camera.transform.rotation;
            var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                foreach (float yaw in new [] { 0f, 90f, 180f, 270f })
                {
                    camera.transform.rotation = Quaternion.Euler(0, yaw, 0) * previousRotation;
                    camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(Path.Combine(Folder, "captura-real-" + yaw + ".png"), image.EncodeToPNG());
                }
                File.WriteAllText(Path.Combine(Folder, "capture-result.txt"), "Real Unity camera render\nScene: " + camera.gameObject.scene.path + "\nCamera: " + camera.name + "\nPosition: " + camera.transform.position + "\nResolution: 1920x1080\n");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.transform.rotation = previousRotation;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
        catch (Exception e) { File.WriteAllText(Path.Combine(Folder, "capture-error.txt"), e.ToString()); }
        finally
        {
            if (SessionState.GetBool("MenuCaptureStartedPlay", false))
            {
                SessionState.SetBool("MenuCaptureStartedPlay", false);
                EditorApplication.isPlaying = false;
            }
        }
    }
}
