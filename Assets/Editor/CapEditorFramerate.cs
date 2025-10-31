using UnityEngine;

/**
 * Script to cap framerate so that the editor doesn't run at 2000+ FPS and blow up my GPU. Runs ONLY in the editor.
 */
[UnityEditor.InitializeOnLoad]
public static class CapEditorFramerate
{
    static CapEditorFramerate()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
    }
}
