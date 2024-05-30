using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class LoadProjectItem : MonoBehaviour{

    public static Action<GameObject> savedProjectSelected;

    [SerializeField] RawImage projectPreview;
    [SerializeField] TMP_Text projectLabel;

    string projectName;

    Color selectedColor = Color.green;
    Color unselectedColor = Color.white;

    private void Awake() {
        GetComponent<Button>().onClick.AddListener(Selected);

        selectedColor = GetComponent<Button>().colors.highlightedColor;
        selectedColor.a += .1f;

        unselectedColor = GetComponent<RawImage>().color;
    }

    public void SetProjectName(string name) {
        projectName = name;
        projectLabel.text = name;
    }

    public void SetProjectPreview(Texture2D texture) {
        texture.filterMode = FilterMode.Point;
        projectPreview.texture = texture;
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
