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
    public UnityEvent onHow2Play;
    [HideInInspector]
    public UnityEvent onAboutMenu;
    [HideInInspector]

    public UnityEvent onExitClicked;

    public void clickPlay()
    {
        audioManager.playSoundEffect(buttonSound);
        onLevelSelectClicked.Invoke();
    }
    public void clickSettings()
    {
        audioManager.playSoundEffect(buttonSound);
        onSettingsClicked.Invoke();
    }
    public void clickHow2Play()
    {
        audioManager.playSoundEffect(buttonSound);
        onHow2Play.Invoke();
    }
    public void clickAboutMenu()
    {
        audioManager.playSoundEffect(buttonSound);
        onAboutMenu.Invoke();
    }
    public void clickExit()
    {
        audioManager.playSoundEffect(buttonSound);
        onExitClicked.Invoke();
    }

    // How 2 Play
    
    [HideInInspector]
    public UnityEvent onHow2PlayExitClicked;
    public void clickHow2PlayExit()
    {
        audioManager.playSoundEffect(buttonSound);
        onHow2PlayExitClicked.Invoke();
    }

    // About Menu
    
    [HideInInspector]
    public UnityEvent onAboutMenuExitClicked;
    public void clickAboutMenuExit()
    {
        audioManager.playSoundEffect(buttonSound);
        onAboutMenuExitClicked.Invoke();
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
        audioManager.playSoundEffect(buttonSound);
        onLevelSelectExitClicked.Invoke();
    }
    public void clickLevel(int levelNumber)
    {
        audioManager.playSoundEffect(buttonSound);
        onLevelClicked.Invoke(levelNumber);
    }
    public void clickSettingsExit()
    {
        audioManager.playSoundEffect(buttonSound);
        onSettingsExitClicked.Invoke();
    }
    public void clickTestScene()
    {
        audioManager.playSoundEffect(buttonSound);
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
        audioManager.playSoundEffect(buttonSound);
        onLevelPauseClicked.Invoke();
    }
    public void clickLevelResume()
    {
        audioManager.playSoundEffect(buttonSound);
        onLevelResumeClicked.Invoke();
    }
    public void clickLevelRestart()
    {
        audioManager.playSoundEffect(buttonSound);
        onLevelRestartClicked.Invoke();
    }
    public void clickLevelExit()
    {
        audioManager.playSoundEffect(buttonSound);
        onLevelExitClicked.Invoke();
    }
    public void clickLevelNext()
    {
        audioManager.playSoundEffect(buttonSound);
        onLevelNextClicked.Invoke();
    }

    // Audio Manager / Sound references
    public SoundEffect buttonSound;
    private AudioManager audioManager;
    void Start()
    {
        GameObject audioManagerObject = GameObject.FindWithTag("AudioManager");
        if (audioManagerObject != null)
            audioManager = audioManagerObject.GetComponent<AudioManager>();
    }
}
