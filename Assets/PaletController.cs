using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaletController : MonoBehaviour{

    public static Action<Color> colorSelected;
    public static Action<List<Color>> paletteColorsUpdated;

    [SerializeField] Transform colorsPanel;
    [SerializeField] ColorPicker colorPicker;

    [SerializeField] Button addPaletteColorButton;
    [SerializeField] Button removePaletteColorButton;

    List<Color> colors;

    GameObject selectedColor;

    int maxColors = 15;

    bool deleting = false; //true after clicking removePaletteColorButton

    private void Awake() {
        colors = new List<Color>();
        selectedColor = colorsPanel.GetChild(0).gameObject;

        addPaletteColorButton.onClick.AddListener(OnPaletteColorAddButton);
        removePaletteColorButton.onClick.AddListener(OnDeleteButton);
    }

    private void OnEnable() {
        //MenuController.newBlankProjectCreated += OnNewBlankProject;
        //MenuController.photoUploadConfirmed += OnImageLoaded;
        //MenuController.projectLoaded += OnImageLoaded;
        ColorPicker.paletteColorSelected += AddColor;
    }

    private void OnDisable() {
        //MenuController.newBlankProjectCreated -= OnNewBlankProject;
        //MenuController.photoUploadConfirmed -= OnImageLoaded;
        //MenuController.projectLoaded -= OnImageLoaded;

        ColorPicker.paletteColorSelected -= AddColor;
    }

    void OnNewBlankProject(int width, int height, Color color) {
        colors.Clear();
        colors.Add(color);

        for (int i = 0; i < colorsPanel.childCount; i++) {
            colorsPanel.GetChild(i).gameObject.SetActive(false);
        }

        selectedColor.transform.GetChild(0).gameObject.SetActive(false); //disable selected icon

        selectedColor = colorsPanel.transform.GetChild(0).gameObject;
        selectedColor.SetActive(true);
        selectedColor.GetComponent<Image>().color = color;
        selectedColor.transform.GetChild(0).gameObject.SetActive(true);
    }

    void OnImageLoaded(Texture2D texture) {
        colors.Clear();
        Color color;
        for (int x = 0; x < texture.width; x++) {
            for (int y = 0; y < texture.height; y++) {
                color = texture.GetPixel(x, y);
                if (!colors.Contains(color)) {
                    colors.Add(color);
                }
            }
        }

        SetColors(colors);
    }

    public void SetColors(List<Color> colors) {
        this.colors = colors;

        int numColors = colors.Count;
        if (numColors > colorsPanel.childCount) {
            numColors = colorsPanel.childCount;
        }

        for (int i = 0; i < numColors; i++) {
            colorsPanel.GetChild(i).GetComponent<Image>().color = colors[i];
            colorsPanel.GetChild(i).gameObject.SetActive(true);
        }

        if (numColors < colorsPanel.childCount) {
            for (int i = numColors; i < colorsPanel.childCount; i++) {
                colorsPanel.GetChild(i).gameObject.SetActive(false);
            }
        }

        selectedColor.transform.GetChild(0).gameObject.SetActive(false); //disable selected icon

        selectedColor = colorsPanel.transform.GetChild(0).gameObject;
        selectedColor.transform.GetChild(0).gameObject.SetActive(true);
    }

    void OnPaletteColorAddButton() {
        colorPicker.Open(ColorSelectionMode.paletteColor);
    }

    public void AddColor(Color color) {
        if(colors.Count >= maxColors) {
            return;
        }

        int index = colors.Count;

        colorsPanel.GetChild(index).gameObject.SetActive(true);
        colorsPanel.GetChild(index).GetComponent<Image>().color = color;

        colors.Add(color);
        paletteColorsUpdated?.Invoke(colors);
    }

    //when a color from the color palette is selected
    public void ColorPaletSelected(GameObject button) {
        if (deleting) {
            RemoveColor(button.transform);
            return;
        }

        selectedColor.transform.GetChild(0).gameObject.SetActive(false); //disable selected icon

        selectedColor = button;
        selectedColor.transform.GetChild(0).gameObject.SetActive(true); //enable selected icon
        colorSelected?.Invoke(selectedColor.GetComponent<Image>().color);
    }

    public void OnDeleteButton() {
        deleting = !deleting;
        ToggleRemoveButtons(deleting);

        if(deleting) {
            removePaletteColorButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "ok";
        }
        else {
            removePaletteColorButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "-";
        }
    }

    void ToggleRemoveButtons(bool buttons) {
        for (int i = 0; i < colorsPanel.childCount; i++) {
            if (colorsPanel.GetChild(i).gameObject.activeSelf) {
                colorsPanel.GetChild(i).GetChild(1).gameObject.SetActive(buttons);
            }
        }
    }

    public void OnRemoveColorButton(GameObject button) {
        RemoveColor(button.transform);
    }

    public void RemoveColor(Transform button) {
        int index = button.GetSiblingIndex();
        if (colors.Count <= index) {
            Debug.LogError("Index is greater than or equal to number of colors saved");
            return;
        }

        Debug.Log("Removing palette color");

        colors.RemoveAt(index);

        button.GetChild(0).gameObject.SetActive(false); //disable selected icon
        button.GetChild(1).gameObject.SetActive(false); //disable remove button
        button.gameObject.SetActive(false);
        button.SetAsLastSibling();
        paletteColorsUpdated?.Invoke(colors);
    }
}
