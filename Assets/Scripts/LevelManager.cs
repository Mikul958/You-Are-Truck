using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    // Player Reference
    private GameObject playerTruck;
    private TruckCollide truckCollide;
    
    // UI Components
    private GameObject canvas;
    private GameUI uiScript;
    private GameObject backgroundDim;
    private GameObject pauseMenu;
    private GameObject winMenu;
    private GameObject loseMenu;

    // Instance Variables
    private float time = 0f;          // Time player has taken in the current level
    private bool levelEnded = false;  // Whether the level has already been completed or failed
    
    void Start()
    {
        // Find truck and subscribe to truck win / death events
        playerTruck = GameObject.FindGameObjectWithTag("Player");
        if (playerTruck != null)
            truckCollide = playerTruck.GetComponent<TruckCollide>();
        if (truckCollide != null)
        {
            truckCollide.onGoalEntered.AddListener(this.attemptLevelWin);
            truckCollide.onTruckDeath.AddListener(this.attemptLevelLose);
        }
        
        // Find canvas and subscribe to UI events
        canvas = GameObject.FindGameObjectWithTag("UI");
        if (canvas == null)
            return;
        uiScript = canvas.GetComponent<GameUI>();
        if (uiScript == null)
            return;
        uiScript.onLevelPauseClicked.AddListener(this.pauseLevel);
        uiScript.onLevelResumeClicked.AddListener(this.resumeLevel);
        uiScript.onLevelRestartClicked.AddListener(this.restartLevel);
        uiScript.onLevelExitClicked.AddListener(this.exitLevel);
        uiScript.onLevelNextClicked.AddListener(this.nextLevel);

        // Store references to other important UI components
        backgroundDim = GameObject.FindGameObjectWithTag("BackgroundDim");
        pauseMenu = GameObject.FindGameObjectWithTag("PauseMenu");
        winMenu = GameObject.FindGameObjectWithTag("WinMenu");
        loseMenu = GameObject.FindGameObjectWithTag("LoseMenu");

        // Disable above components (they have to be active to find them in code)
        backgroundDim.SetActive(false);
        pauseMenu.SetActive(false);
        winMenu.SetActive(false);
        loseMenu.SetActive(false);
    }

    void Update()
    {
        time += Time.deltaTime;
    }

    void OnDestroy()
    {
        // Remove all listeners (if they were successfully set)
        if (truckCollide != null)
        {
            truckCollide.onGoalEntered.RemoveListener(this.attemptLevelWin);
            truckCollide.onTruckDeath.RemoveListener(this.attemptLevelLose);
        }
        if (uiScript != null)
        {
            uiScript.onLevelPauseClicked.AddListener(this.pauseLevel);
            uiScript.onLevelResumeClicked.AddListener(this.resumeLevel);
            uiScript.onLevelRestartClicked.AddListener(this.restartLevel);
            uiScript.onLevelExitClicked.AddListener(this.exitLevel);
            uiScript.onLevelNextClicked.AddListener(this.nextLevel);
        }
    }


    private void attemptLevelWin()
    {
        if (levelEnded)
            return;
        
        backgroundDim.SetActive(true);
        winMenu.SetActive(true);
        levelEnded = true;

        LevelData.instance.updateCurrentLevelData(true, (int)time);
    }

    private void attemptLevelLose()
    {
        if (levelEnded)
            return;
        
        backgroundDim.SetActive(true);
        loseMenu.SetActive(true);
        levelEnded = true;
    }

    private void pauseLevel()
    {
        Time.timeScale = 0f;
        backgroundDim.SetActive(true);
        pauseMenu.SetActive(true);
    }

    private void resumeLevel()
    {
        Time.timeScale = 1f;
        backgroundDim.SetActive(false);
        pauseMenu.SetActive(false);
    }

    private void exitLevel()
    {
        Time.timeScale = 1f;
        LevelData.instance.unsetCurrentLevel();
        SceneManager.LoadScene("Level Select");
    }

    private void restartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void nextLevel()
    {
        if (LevelData.instance.incrementCurrentLevelOrExit())
        {
            SceneManager.LoadScene("Level " + LevelData.instance.getCurrentLevelNumber());
        }
        else
        {
            LevelData.instance.unsetCurrentLevel();
            SceneManager.LoadScene("Level " + LevelData.instance.getCurrentLevelNumber());
        }
    }
}
