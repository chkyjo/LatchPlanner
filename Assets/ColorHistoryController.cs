using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorHistoryController : MonoBehaviour{

    public static Color currentColor;
    static List<Color> colorHistory; //max 8

    public static Action<Color> currentColorUpdated;

    public static Action<Color[]> colorHistoryUpdated;

    [SerializeField] ColorPicker colorPicker;
    [SerializeField] GameObject currentColorButton;
    [SerializeField] GameObject colorsPanel;

    static Image currentColorImage;
    static Transform colorsPanelTransform;

    private void Awake() {
        colorHistory = new List<Color>();
        currentColorImage = currentColorButton.GetComponent<Image>();
        colorsPanelTransform = colorsPanel.transform;
    }

    private void OnEnable() {
        ColorPicker.colorSelected += ColorSelectedForCurrentColor;
        PaletController.colorSelected += ColorSelectedForCurrentColor;
    }

    private void OnDisable() {
        ColorPicker.colorSelected -= ColorSelectedForCurrentColor;
        PaletController.colorSelected -= ColorSelectedForCurrentColor;
    }

    public void OnCurrentColorClicked() {
        colorPicker.Open(ColorSelectionMode.currentColor);
    }

    public static void SetCurrentColor(Color color) {
        currentColor = color;
        currentColorImage.color = currentColor;
    }

    void ColorSelectedForCurrentColor(Color color) {
        AddColorToHistory(color);
        SetCurrentColor(color);
        currentColorUpdated?.Invoke(color);
    }

    public void OnColorButtonClicked(GameObject button) {
        currentColor = button.GetComponent<Image>().color;
        currentColorImage.color = currentColor;
        currentColorUpdated?.Invoke(button.GetComponent<Image>().color);
    }

    public void AddColorToHistory(Color color) {
        colorHistory.Insert(0, color);
        Transform disabledColor = FindDisabledPrevColor();
        if (disabledColor == null) {
            int maxHistory = colorsPanelTransform.childCount - 1;
            colorsPanelTransform.GetChild(maxHistory).GetComponent<Image>().color = color;
            colorsPanelTransform.GetChild(maxHistory).SetAsFirstSibling();
            colorHistory.RemoveAt(maxHistory);
        }
        else {
            disabledColor.gameObject.SetActive(true);
            disabledColor.GetComponent<Image>().color = color;
            disabledColor.SetAsFirstSibling();
        }
        colorHistoryUpdated?.Invoke(colorHistory.ToArray());
    }

    Transform FindDisabledPrevColor() {
        int maxHistory = colorsPanelTransform.childCount;
        for (int i = 0; i < maxHistory; i++) {
            if (!colorsPanelTransform.GetChild(i).gameObject.activeSelf) {
                return colorsPanelTransform.GetChild(i);
            }
        }

        return null;
    }

    public static void ClearHistory() {
        for (int colorIndex = 0; colorIndex < colorsPanelTransform.childCount; colorIndex++) {
            colorsPanelTransform.GetChild(colorIndex).gameObject.SetActive(false);
        }
        colorHistory.Clear();
    }

    public static void SetColorHistory(Color[] colors) {
        ClearHistory();
        if (colors != null) {
            int numColors = colors.Length;
            if (numColors > colorsPanelTransform.childCount) {
                numColors = colorsPanelTransform.childCount;
            }
            for (int colorIndex = 0; colorIndex < numColors; colorIndex++) {
                colorHistory.Add(colors[colorIndex]);
                colorsPanelTransform.GetChild(colorIndex).gameObject.SetActive(true);
                colorsPanelTransform.GetChild(colorIndex).GetComponent<Image>().color = colors[colorIndex];
            }
        }
    }
}
