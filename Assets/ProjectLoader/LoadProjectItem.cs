using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class LoadProjectItem : MonoBehaviour{

    public static Action<GameObject> savedProjectSelected;

    string projectName;

    Color selectedColor = Color.green;
    Color unselectedColor = Color.white;

    private void Awake() {
        GetComponent<Button>().onClick.AddListener(Selected);
    }

    public void SetProjectName(string name) {
        projectName = name;
        transform.GetChild(0).GetComponent<TMP_Text>().text = name;
    }

    public void SetProjectPreview(Texture2D texture) {
        texture.filterMode = FilterMode.Point;
        GetComponent<RawImage>().texture = texture;
    }

    public string GetProjectName() {
        return projectName;
    }

    void Selected() {
        savedProjectSelected?.Invoke(gameObject);
    }

    public void ShowSelected() {
        GetComponent<RawImage>().color = selectedColor;
    }

    public void ShowUnselected() {
        GetComponent<RawImage>().color = unselectedColor;
    }

}
