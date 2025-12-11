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
        uiScript.onExitClicked.AddListener(this.exitGame);
        uiScript.onLevelSelectExitClicked.AddListener(this.exitLevelSelect);
        uiScript.onLevelClicked.AddListener(this.enterLevel);
        uiScript.onSettingsExitClicked.AddListener(this.exitSettings);
;    }

    private void toLevelSelect()
    {
        SceneManager.LoadScene("Level Select");
    }

    private void toSettings()
    {
        SceneManager.LoadScene("Settings");
    }

    private void exitGame()
    {
        Application.Quit();
    }

    private void exitLevelSelect()
    {
        SceneManager.LoadScene("Main Menu");
    }

    private void enterLevel(int levelNumber)
    {
        if (levelNumber <= 0)
            SceneManager.LoadScene("TestScene");  // TODO remove this part
        else
        {
            LevelData.instance.setCurrentLevel(levelNumber);
            SceneManager.LoadScene("Level " + levelNumber);
        }
    }

    private void exitSettings()
    {
        // TODO save settings if implemented
        SceneManager.LoadScene("Main Menu");
    }
}
