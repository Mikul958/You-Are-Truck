using UnityEngine;
using UnityEngine.Events;

public class GameUI : MonoBehaviour
{
    // Main Menu
    public UnityEvent onPlayClicked;
    public UnityEvent onSettingsClicked;
    public UnityEvent onExitClicked;

    public void clickPlay()
    {
        onPlayClicked.Invoke();
    }
    public void clickSettings()
    {
        onSettingsClicked.Invoke();
    }
    public void clickExit()
    {
        onExitClicked.Invoke();
    }

    // Level Select
    public UnityEvent onMainMenuClicked;
    public UnityEvent<int> onLevelClicked;

    public void clickMainMenu()
    {
        onMainMenuClicked.Invoke();
    }
    public void clickLevel(int levelNumber)
    {
        onLevelClicked.Invoke(levelNumber);
    }

    // Settings
    // TODO

    // Gameplay
    public UnityEvent onLevelPauseClicked;
    public UnityEvent onLevelResumeClicked;
    public UnityEvent onLevelRestartClicked;
    public UnityEvent onLevelExitClicked;
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
