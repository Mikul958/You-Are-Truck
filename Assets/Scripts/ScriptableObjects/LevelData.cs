using System;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "level-data", menuName = "Levels/LevelData")]
public class LevelData : ScriptableObject
{
    [Serializable]
    public class Level
    {
        [SerializeField] public bool isComplete;
        [SerializeField] public int bestTime;

        public Level()
        {
            isComplete = false;
            bestTime = -1;
        }

        public Level(bool isComplete, int bestTime)
        {
            this.isComplete = isComplete;
            this.bestTime = bestTime;
        }
    }

    private string LEVEL_PATH;
    private const int LEVEL_COUNT = 10;

    [SerializeField] private Level[] levels = new Level[LEVEL_COUNT];
    private int currentLevel = -1;

    private static LevelData _instance;
    public static LevelData instance
    {
        get
        {
            if (_instance == null)
                _instance = new LevelData();
            return _instance;
        }
    }

    // Generates default level data before attempting file load so that default data can be saved if levels.json does not exist
    private LevelData()
    {
        foreach (Level level in levels)
        {
            level.isComplete = false;
            level.bestTime = -1;
        }
    }
    
    void OnEnable()
    {
        LEVEL_PATH = Path.Combine(Application.persistentDataPath, "levels.json");
        loadLevelData();
    }

    public void loadLevelData()
    {
        // If no save file, generate one with default save data
        if (!File.Exists(LEVEL_PATH))
        {
            saveLevelData();
            return;
        }

        string levelJSON = File.ReadAllText(LEVEL_PATH);
        JsonUtility.FromJsonOverwrite(levelJSON, this);
    }

    public void saveLevelData()
    {
        string levelJSON = JsonUtility.ToJson(this, true);
        File.WriteAllText(LEVEL_PATH, levelJSON);
    }

    public void resetLevelData()
    {
        foreach (Level level in levels)
        {
            level.isComplete = false;
            level.bestTime = -1;
        }
        saveLevelData();
    }

    public Level getLevelDataAt(int levelNumber)
    {
        if (levelNumber > 0 && levelNumber - 1 < LEVEL_COUNT)
            return levels[levelNumber - 1];
        throw new IndexOutOfRangeException("Level number " + levelNumber + " out of range");
    }

    public int getCurrentLevelNumber()
    {
        return currentLevel;
    }

    public Level getCurrentLevel()
    {
        if (currentLevel > 0 && currentLevel - 1 < LEVEL_COUNT)
            return levels[currentLevel - 1];
        throw new IndexOutOfRangeException("Current level is set to number " + currentLevel + " (index " + (currentLevel - 1) + "); this level number is out of bounds");
    }
    
    public void setCurrentLevel(int levelNumber)
    {
        if (levelNumber > 0 && levelNumber - 1 < LEVEL_COUNT)
            currentLevel = levelNumber;
        else
            throw new IndexOutOfRangeException("Level at number " + levelNumber + " (index " + (levelNumber - 1) + ") does not exist");
    }

    public bool incrementCurrentLevelOrExit()
    {
        if (currentLevel >= LEVEL_COUNT)
            return false;
        currentLevel++;
        return true;
    }

    public void unsetCurrentLevel()
    {
        currentLevel = -1;
    }

    public void updateCurrentLevelData(bool isComplete, int time)
    {
        Level currentLevelData = getCurrentLevel();
        bool dataChanged = false;
        if (!currentLevelData.isComplete && isComplete)
        {
            currentLevelData.isComplete = isComplete;
            dataChanged = true;
        }
        if (currentLevelData.bestTime < 0 || time < currentLevelData.bestTime)
        {
            currentLevelData.bestTime = time;
            dataChanged = true;
        }

        if (dataChanged)
            saveLevelData();
    }
}
