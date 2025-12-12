using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadManager : MonoBehaviour
{
    void Start()
    {
        LevelData.instance.loadLevelData();
        SceneManager.LoadScene("Main Menu");
    }
}
