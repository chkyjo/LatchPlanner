using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour{

    public static Action<Texture2D> backgroundImageSelected;

    [SerializeField] ColorPicker colorPicker;

    [SerializeField] Image backgroundColorButtonImage;

    private void OnEnable() {
        ColorPicker.backgroundColorSelected += BackgroundColorSelected;
    }
    private void OnDisable() {
        ColorPicker.backgroundColorSelected -= BackgroundColorSelected;
    }

    public void ToggleMenu() {
        if(gameObject.activeInHierarchy) {
            gameObject.SetActive(false);
        }
        else {
            gameObject.SetActive(true);
        }
    }

    public void OnBackgroundSolidColorButton() {
        colorPicker.Open(ColorSelectionMode.backgroundColor, backgroundColorButtonImage.color);
    }

    void BackgroundColorSelected(Color color) {
        GetComponent<RawImage>().color = color;
        GetComponent<RawImage>().texture = null;
        backgroundColorButtonImage.color = color;
    }

    public void OnBackgroundImageButton(RawImage buttonImage) {
        GetComponent<RawImage>().texture = (Texture2D)buttonImage.mainTexture;
        GetComponent<RawImage>().color = Color.white;
        backgroundImageSelected?.Invoke((Texture2D)buttonImage.mainTexture);
    }
}
