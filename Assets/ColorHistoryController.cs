using System;
using UnityEngine;
using UnityEngine.UI;

public class ColorHistoryController : MonoBehaviour{

    public static Action<Color> colorSelected;

    [SerializeField] ColorPicker colorPicker;

    public void OnCurrentColorClicked() {
        colorPicker.Open(ColorSelectionMode.currentColor);
    }

    public void OnColorButtonClicked(GameObject button) {
        colorSelected?.Invoke(button.GetComponent<Image>().color);
    }
}
