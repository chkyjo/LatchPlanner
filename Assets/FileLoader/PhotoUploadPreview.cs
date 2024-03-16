using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhotoUploadPreview : MonoBehaviour{

    public static Action<Texture2D> onUploadConfirmed;

    [SerializeField] RectTransform preview;
    [SerializeField] TMP_Text sliderLabel;
    [SerializeField] Slider sizeSlider;
    [SerializeField] TMP_Text sizeDisplay;

    int numTiles = 20;

    float maxWidth;
    float maxHeight;

    RectTransform previewRT;

    Texture2D uploadedTexture;

    private void Awake() {
        sizeSlider.onValueChanged.AddListener(SquareSizeUpdated);

        previewRT = preview.GetComponent<RectTransform>();
        maxWidth = GetComponent<RectTransform>().rect.width;
        maxHeight = GetComponent<RectTransform>().rect.height;
        previewRT.sizeDelta = new Vector2(maxHeight, maxHeight);
    }

    private void OnEnable() {
        FileBrowserUpdate.textureUploaded += OnTextureUpload;
    }

    private void OnDisable() {
        FileBrowserUpdate.textureUploaded -= OnTextureUpload;
    }

    void OnTextureUpload(Texture2D uploadedTexture) {
        this.uploadedTexture = uploadedTexture;
        if (uploadedTexture.width > uploadedTexture.height) {
            sliderLabel.text = "Tiles per row:";
            float multiplier = maxWidth / uploadedTexture.width;
            float height = multiplier * uploadedTexture.height;
            previewRT.sizeDelta = new Vector2(maxWidth, height);
        }
        else {
            sliderLabel.text = "Tiles per column:";
            float multiplier = maxHeight / uploadedTexture.height;
            float width = multiplier * uploadedTexture.width;
            previewRT.sizeDelta = new Vector2(width, maxHeight);
        }

        UpdateTexture();
    }

    void SquareSizeUpdated(float value) {
        numTiles = (int)value;
        sizeDisplay.text = numTiles.ToString();
        UpdateTexture();
    }

    void UpdateTexture() {

        int pixelsPerTile = uploadedTexture.width / numTiles;
        int widthInTiles = numTiles;
        int heightInTiles = uploadedTexture.height / pixelsPerTile;

        if (uploadedTexture.height >= uploadedTexture.width) {
            pixelsPerTile = uploadedTexture.height / numTiles;
            heightInTiles = numTiles;
            widthInTiles = uploadedTexture.width / pixelsPerTile;
        }

        Texture2D texture = new Texture2D(widthInTiles, heightInTiles);
        texture.filterMode = FilterMode.Point;
        texture.name = "LatchPattern";

        for (int x = 0; x < widthInTiles; x++) {
            for (int y = 0; y < heightInTiles; y++) {
                Color uploadedPixel = uploadedTexture.GetPixel(x * pixelsPerTile, y * pixelsPerTile);
                texture.SetPixel(x, y, uploadedPixel);
            }
        }

        texture.Apply();

        preview.GetComponent<RawImage>().texture = texture;
    }

    public Texture2D GetTexture() {
        if(preview.GetComponent<RawImage>().texture == null) {
            return null;
        }
        return (Texture2D)preview.GetComponent<RawImage>().texture;
    }
}
