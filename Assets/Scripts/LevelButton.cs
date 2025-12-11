using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    public int levelNumber;

    // References to button / subsequent path
    private GameObject button = null;
    private List<Image> buttonPaths = new List<Image>();

    void Start()
    {
        Transform buttonTransform = gameObject.transform.Find("Button");
        Transform pathTransform = gameObject.transform.Find("Path");

        assignButtonRefs(buttonTransform);
        assignPathRefs(pathTransform);

        updateLevelButton();
        updateLevelPaths();
    }

    private void assignButtonRefs(Transform buttonTransform)
    {
        if (buttonTransform != null)
            button = buttonTransform.gameObject;
    }

    private void assignPathRefs(Transform pathTransform)
    {
        if (pathTransform == null)
            return;
        
        GameObject pathObject = pathTransform.gameObject;
        Image[] pathImages = pathObject.GetComponentsInChildren<Image>();
        foreach (Image pathImage in pathImages)
            buttonPaths.Add(pathImage);
    }

    private void updateLevelButton()
    {
        if (levelNumber <= 1 || button == null)
            return;
        LevelData.Level previousLevelInfo = LevelData.instance.getLevelDataAt(levelNumber - 1);
        if (previousLevelInfo.isComplete)
            return;
        
        Button buttonInteract = button.GetComponent<Button>();
        if (buttonInteract != null)
            buttonInteract.interactable = false;
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
            setHalfOpacity(buttonImage);
    }

    private void updateLevelPaths()
    {
        LevelData.Level currentLevelInfo = LevelData.instance.getLevelDataAt(levelNumber);
        if (currentLevelInfo.isComplete)
            return;
        
        foreach (Image pathImage in buttonPaths)
            setHalfOpacity(pathImage);
    }

    private void setHalfOpacity(Image image)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, 0.5f);
    }
}
