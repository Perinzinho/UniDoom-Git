using System;
using System.IO;
using UnityEngine;
using UnityEditor;

public static class EntranceFinalValidation
{
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/EntranceRebuild/"))throw new Exception("Use isolated project");
        AssetDatabase.Refresh();
        EntranceRebuilder.VerifyAndRender();
        Debug.Log("ENTRANCE_FINAL_PASS");
        EntrancePlayCheck.Run();
    }
}
