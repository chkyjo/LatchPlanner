using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ColorPicker : MonoBehaviour{

    public static Action<Color> colorSelected;
    public static Action<Color> paletteColorSelected;

    [SerializeField] RawImage saturationImage;
    [SerializeField] RawImage hueImage;
    [SerializeField] RawImage outputImage;

    [SerializeField] Slider hueSlider;

    [SerializeField] TMP_InputField hexInputField;

    Texture2D satTexture, hueTexture, outputTexture;

    [SerializeField] Button confirmButton;

    ColorSelectionMode selectionMode; //what color is being selected for

    float currentSat = 0.5f;
    float currentBrightness = 0.5f;
    float currentHue = 0;

    private void Awake() {
        confirmButton.onClick.AddListener(OnConfirmClicked);
        hueSlider.onValueChanged.AddListener(OnHueChanged);
        hexInputField.onEndEdit.AddListener(OnHexInput);
    }

    private void OnEnable() {
        ColorWindow.satBrightnessSelected += SetSaturationBrightness;
    }

    private void OnDisable() {
        ColorWindow.satBrightnessSelected -= SetSaturationBrightness;
    }

    private void Start() {
        CreateHueImage();
        CreatePickerImage();
        CreateOutputImage();
        UpdateOutputImage();
        gameObject.SetActive(false);
    }

    public void Open(ColorSelectionMode mode) {
        selectionMode = mode;
        gameObject.SetActive(true);
    }

    void CreateHueImage() {
        hueTexture = new Texture2D(1, 16);
        hueTexture.wrapMode = TextureWrapMode.Clamp;
        hueTexture.name = "HueTexture";

        hueSlider.value = currentHue;

        for(int i = 0; i < hueTexture.height; i++) {
            //return color from hue, sateraction, and brightness value
            hueTexture.SetPixel(0, i, Color.HSVToRGB((float)i / hueTexture.height, 1, 0.95f));
        }

        hueTexture.Apply();
        hueImage.texture = hueTexture;
    }

    void CreatePickerImage() {
        satTexture = new Texture2D(16, 16);
        satTexture.wrapMode = TextureWrapMode.Clamp;
        satTexture.name = "SatBrightnessImage";

        UpdateSatTexture();

        saturationImage.texture = satTexture;


    }

    void CreateOutputImage() {
        outputTexture = new Texture2D(16, 1);
        outputTexture.wrapMode = TextureWrapMode.Clamp;
        outputTexture.name = "outputImage";

        UpdateOutputImage();

        outputImage.texture = outputTexture;
    }

    void SetSaturationBrightness(float sat, float brightness) {
        currentSat = sat;
        currentBrightness = brightness;

        UpdateOutputImage();
    }

    void OnHueChanged(float value) {
        currentHue = value;
        UpdateSatTexture();
        UpdateOutputImage();
    }

    void UpdateSatTexture() {
        for (int y = 0; y < satTexture.height; y++) {
            for (int x = 0; x < satTexture.width; x++) {
                satTexture.SetPixel(x, y, Color.HSVToRGB(currentHue, 
                    (float)x / satTexture.width, 
                    (float)y / satTexture.height));
            }
        }

        satTexture.Apply();
    }

    void UpdateOutputImage() {
        Color currentColor = Color.HSVToRGB(currentHue, currentSat, currentBrightness);

        for (int i = 0; i < outputTexture.width; i++) {
            outputTexture.SetPixel(i, 0, currentColor);
        }

        outputTexture.Apply();

        hexInputField.text = ColorUtility.ToHtmlStringRGB(currentColor);

    }

    public void OnConfirmClicked() {
        if(selectionMode == ColorSelectionMode.currentColor) {
            colorSelected?.Invoke(Color.HSVToRGB(currentHue, currentSat, currentBrightness));
        }
        else if(selectionMode == ColorSelectionMode.paletteColor) {
            paletteColorSelected?.Invoke(Color.HSVToRGB(currentHue, currentSat, currentBrightness));
        }
        gameObject.SetActive(false);
    }

    public void OnHexInput(string hexVal) {
        if(hexVal.Length < 6) {
            return;
        }

        Color newCol;

        if(!ColorUtility.TryParseHtmlString("#" + hexVal, out newCol)) {
            return;
        }

        Color.RGBToHSV(newCol, out currentHue, out currentSat, out currentBrightness);
        hueSlider.value = currentHue;
        hexInputField.text = "";

        UpdateOutputImage();
    }

}
