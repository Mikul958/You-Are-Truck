using UnityEngine;
using UnityEngine.Events;

public class GameUI : MonoBehaviour
{
    // Main Menu
    [HideInInspector]
    public UnityEvent onLevelSelectClicked;
    [HideInInspector]
    public UnityEvent onSettingsClicked;
    [HideInInspector]
    public UnityEvent onExitClicked;

    public void clickPlay()
    {
        onLevelSelectClicked.Invoke();
    }
    public void clickSettings()
    {
        onSettingsClicked.Invoke();
    }
    public void clickExit()
    {
        onExitClicked.Invoke();
    }

    // Level Select & Settings
    [HideInInspector]
    public UnityEvent onLevelSelectExitClicked;
    [HideInInspector]
    public UnityEvent<int> onLevelClicked;
    [HideInInspector]
    public UnityEvent onSettingsExitClicked;

    public void clickLevelSelectExit()
    {
        onLevelSelectExitClicked.Invoke();
    }
    public void clickLevel(int levelNumber)
    {
        onLevelClicked.Invoke(levelNumber);
    }
    public void clickSettingsExit()
    {
        onSettingsExitClicked.Invoke();
    }
    public void clickTestScene()
    {
        onLevelClicked.Invoke(-1);
    }

    // Settings
    // TODO

    // Gameplay
    [HideInInspector]
    public UnityEvent onLevelPauseClicked;
    [HideInInspector]
    public UnityEvent onLevelResumeClicked;
    [HideInInspector]
    public UnityEvent onLevelRestartClicked;
    [HideInInspector]
    public UnityEvent onLevelExitClicked;
    [HideInInspector]
    public UnityEvent onLevelNextClicked;
    
    public void clickLevelPause()
    {
        onLevelPauseClicked.Invoke();
    }
    public void clickLevelResume()
    {
        onLevelResumeClicked.Invoke();
    }
    public void clickLevelRestart()
    {
        onLevelRestartClicked.Invoke();
    }
    public void clickLevelExit()
    {
        onLevelExitClicked.Invoke();
    }
    public void clickLevelNext()
    {
        onLevelNextClicked.Invoke();
    }
}
