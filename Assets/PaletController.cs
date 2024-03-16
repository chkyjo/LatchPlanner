using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaletController : MonoBehaviour{

    public static Action<Color> colorSelected;
    public static Action<List<Color>> paletteColorsUpdated;

    [SerializeField] ColorPicker colorPicker;

    [SerializeField] Button addPaletteColorButton;
    [SerializeField] Button removePaletteColorButton;

    List<Color> colors;

    GameObject selectedColor;

    int maxColors = 15;

    bool deleting = false; //true after clicking removePaletteColorButton

    private void Awake() {
        colors = new List<Color>();
        selectedColor = transform.GetChild(0).gameObject;

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

        Transform colorButtons = transform.GetChild(0);

        for (int i = 0; i < colorButtons.childCount; i++) {
            colorButtons.GetChild(i).gameObject.SetActive(false);
        }

        selectedColor.transform.GetChild(0).gameObject.SetActive(false); //disable selected icon

        selectedColor = colorButtons.transform.GetChild(0).gameObject;
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
        Transform colorButtons = transform.GetChild(0);
        int numColors = colors.Count;
        if (numColors > colorButtons.childCount) {
            numColors = colorButtons.childCount;
        }

        for (int i = 0; i < numColors; i++) {
            colorButtons.GetChild(i).GetComponent<Image>().color = colors[i];
            colorButtons.GetChild(i).gameObject.SetActive(true);
        }

        if (numColors < colorButtons.childCount) {
            for (int i = numColors; i < colorButtons.childCount; i++) {
                colorButtons.GetChild(i).gameObject.SetActive(false);
            }
        }

        selectedColor.transform.GetChild(0).gameObject.SetActive(false); //disable selected icon

        selectedColor = colorButtons.transform.GetChild(0).gameObject;
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

        Transform colorButtons = transform.GetChild(0);
        colorButtons.GetChild(index).gameObject.SetActive(true);
        colorButtons.GetChild(index).GetComponent<Image>().color = color;

        colors.Add(color);
        paletteColorsUpdated?.Invoke(colors);
    }

    //when a color from the color palette is selected
    public void ColorPaletSelected(GameObject button) {
        if (deleting) {
            colors.RemoveAt(button.transform.GetSiblingIndex());
            button.transform.GetChild(0).gameObject.SetActive(false); //disable selected icon
            button.SetActive(false);
            button.transform.SetAsLastSibling();
            paletteColorsUpdated?.Invoke(colors);
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
            removePaletteColorButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Confirm";
        }
        else {
            removePaletteColorButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Remove";
        }
    }

    void ToggleRemoveButtons(bool buttons) {
        Transform colorButtons = transform.GetChild(0);
        for (int i = 0; i < colorButtons.childCount; i++) {
            if (colorButtons.GetChild(i).gameObject.activeSelf) {
                colorButtons.GetChild(i).GetChild(1).gameObject.SetActive(buttons);
            }
        }
    }

    public void OnRemoveColorButton(GameObject button) {
        RemoveColor(button.transform.GetSiblingIndex());
    }

    public void RemoveColor(int index) {
        if (colors.Count <= index) {
            return;
        }

        colors.RemoveAt(index);

        Transform colorButtons = transform.GetChild(0);
        colorButtons.GetChild(index).gameObject.SetActive(false);
    }
}
