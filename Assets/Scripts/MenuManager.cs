using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // UI Components
    private GameObject canvas;
    private GameUI uiScript;
    
    void Start()
    {
        // Get canvas and script
        canvas = GameObject.FindGameObjectWithTag("UI");
        if (canvas == null)
            return;
        uiScript = canvas.GetComponent<GameUI>();
        if (uiScript == null)
            return;
        
        // Subscribe to necessary UI events
        uiScript.onLevelSelectClicked.AddListener(this.toLevelSelect);
        uiScript.onSettingsClicked.AddListener(this.toSettings);
        uiScript.onHowToPlayClicked.AddListener(this.toHowToPlay);
        uiScript.onAboutMenuClicked.AddListener(this.toAboutMenu);
        uiScript.onExitClicked.AddListener(this.exitGame);
        uiScript.onLevelSelectExitClicked.AddListener(this.exitLevelSelect);
        uiScript.onLevelClicked.AddListener(this.enterLevel);
        uiScript.onSettingsExitClicked.AddListener(this.exitSettings);
        uiScript.onHowToPlayExitClicked.AddListener(this.exitHowToPlay);
        uiScript.onAboutMenuExitClicked.AddListener(this.exitAboutMenu);
;    }

    private void toLevelSelect()
    {
        SceneManager.LoadScene("Level Select");
    }

    private void toSettings()
    {
        SceneManager.LoadScene("Settings");
    }

    private void toHowToPlay()
    {
        SceneManager.LoadScene("How To Play");
    }

    private void toAboutMenu()
    {
        SceneManager.LoadScene("About Menu");
    }

    private void exitGame()
    {
        Application.Quit();
    }

    private void exitLevelSelect()
    {
        SceneManager.LoadScene("Main Menu");
    }

    private void exitHowToPlay()
    {
        SceneManager.LoadScene("Main Menu");
    }

    private void exitAboutMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }

    private void enterLevel(int levelNumber)
    {
        LevelData.instance.setCurrentLevel(levelNumber);
        SceneManager.LoadScene("Level " + levelNumber);
    }

    private void exitSettings()
    {
        // TODO save settings if implemented
        SceneManager.LoadScene("Main Menu");
    }
}
