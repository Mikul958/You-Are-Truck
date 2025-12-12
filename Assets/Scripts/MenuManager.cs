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
        uiScript.onHow2Play.AddListener(this.toHow2Play);
        uiScript.onAboutMenu.AddListener(this.toAboutMenu);
        uiScript.onExitClicked.AddListener(this.exitGame);
        uiScript.onLevelSelectExitClicked.AddListener(this.exitLevelSelect);
        uiScript.onLevelClicked.AddListener(this.enterLevel);
        uiScript.onSettingsExitClicked.AddListener(this.exitSettings);
        uiScript.onHow2PlayExitClicked.AddListener(this.exitHow2Play);
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

    private void toHow2Play()
    {
        SceneManager.LoadScene("How 2 Play");
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

    private void exitHow2Play()
    {
        SceneManager.LoadScene("Main Menu");
    }

    private void exitAboutMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }

    private void enterLevel(int levelNumber)
    {
        // TODO prepare level data / saving when created, remove test scene when done
        if (levelNumber < 0)
            SceneManager.LoadScene("TestScene");
        else
            SceneManager.LoadScene("Level " + levelNumber);
    }

    private void exitSettings()
    {
        // TODO save settings if implemented
        SceneManager.LoadScene("Main Menu");
    }
}
