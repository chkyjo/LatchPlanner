using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;
using System.Collections.Generic;

public class MenuController : MonoBehaviour{

    public static Action<int, int, Color> newBlankProjectCreated;
    public static Action<Texture2D> projectLoaded;
    public static Action<Texture2D> photoUploadConfirmed;

    [SerializeField] GameObject initialScreen;

    [SerializeField] Button newProjectButton;
    [SerializeField] GameObject newProjectWindow;
    [SerializeField] TMP_InputField newProjectNameField;
    [SerializeField] Button newCanvasButton;
    [SerializeField] GameObject colorButton;
    [SerializeField] ErrorPanel errorPanel;

    [SerializeField] PhotoUploadPreview uploadPreview;
    [SerializeField] Button newProjectLoadImageButton;
    [SerializeField] Button confirmPhotoUploadButton;

    [SerializeField] TMP_InputField widthInputField;
    [SerializeField] TMP_InputField heightInputField;
    [SerializeField] Toggle gridToggle;

    [SerializeField] Button loadImageButton;
    
    [SerializeField] TextureController textureController;
    [SerializeField] PaletController paletteController;

    [SerializeField] TMP_Text loadedProjectLabel;

    [SerializeField] Image currentColorImage;
    [SerializeField] Transform prevColors;
    List<Color> colorHistory; //max 8

    List<Color> paletteColors; //max 8

    [SerializeField] GameObject runLatchPanel;
    [SerializeField] Button startLatchButton;
    [SerializeField] Button stopLatchButton;
    [SerializeField] Button latchModeButton; //by row/tile
    [SerializeField] Button nextStepButton;
    [SerializeField] Button prevStepButton;
    [SerializeField] TMP_Text currentStepDisplay;
    bool latchMode = false;

    string loadedProject = "";

    UserSettings userSettings;
    ProjectSettings projectSettings;

    private void Awake() {
        newProjectButton.onClick.AddListener(OnPlusButton);
        newCanvasButton.onClick.AddListener(CreateNewCanvas);
        confirmPhotoUploadButton.onClick.AddListener(OnConfirmPhotoUpload);

        gridToggle.onValueChanged.AddListener(OnGridToggle);
        //exportImageButton.onClick.AddListener(OnSaveImageButton);

        newProjectLoadImageButton.onClick.AddListener(OnLoadImageButton);

        startLatchButton.onClick.AddListener(OnStartLatchButton);
        stopLatchButton.onClick.AddListener(OnStopLatchButton);
        nextStepButton.onClick.AddListener(OnNextButton);
        latchModeButton.onClick.AddListener(OnLatchModeButton);
        prevStepButton.onClick.AddListener(OnPrevButton);

        colorHistory = new List<Color>();
        paletteColors = new List<Color>();

    }

    private void OnEnable() {
        ColorPicker.colorSelected += OnColorSelected;
        TextureController.textureUpdated += SaveProject;
        TextureController.latchFinished += OnLatchFinished;
        PaletController.colorSelected += OnHistoryColorSelected;
        PaletController.paletteColorsUpdated += PaletteColorsUpdated;
        ColorHistoryController.colorSelected += OnHistoryColorSelected;
    }

    private void OnDisable() {
        ColorPicker.colorSelected -= OnColorSelected;
        TextureController.textureUpdated -= SaveProject;
        TextureController.latchFinished -= OnLatchFinished;
        PaletController.colorSelected -= OnHistoryColorSelected;
        PaletController.paletteColorsUpdated -= PaletteColorsUpdated;
        ColorHistoryController.colorSelected -= OnHistoryColorSelected;
    }

    private void Start() {
        //System.Diagnostics.Process.Start("explorer.exe", "/select, " + Application.persistentDataPath);
        //System.Diagnostics.Process.Start("open", "-R your-file-path" + Application.persistentDataPath); //for macOS
        string settingsPath = Application.dataPath + "/UserSettings.txt";
        if (!File.Exists(settingsPath)) {
            userSettings = new UserSettings();
            SaveUserSettings();
        }
        else {
            string settingsText;
            using (StreamReader settingsFile = File.OpenText(settingsPath)) {
                settingsText = settingsFile.ReadToEnd();
            }

            userSettings = JsonUtility.FromJson<UserSettings>(settingsText);
        }

        if (userSettings.loadedProject != string.Empty) {
            ProjectLoadConfirmed(userSettings.loadedProject);
        }
        Debug.Log(userSettings.loadedProject);
    }

    private void Update() {
        if (latchMode) {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                OnPrevButton();
            }
            else if(Input.GetKeyDown(KeyCode.RightArrow)) { 
                OnNextButton();
            }
            else if(Input.GetKeyDown(KeyCode.Space)) {
                OnNextButton();
            }
        }
    }

    void OnPlusButton() {
        newProjectWindow.gameObject.SetActive(true);
    }

    void CreateNewCanvas() {
        if(newProjectNameField.text == "") {
            Debug.Log("Empty project name");
            errorPanel.DisplayError("Empty project name");
            return;
        }
        else if (newProjectNameField.text.Contains('.')) {
            errorPanel.DisplayError("Name cannot contain a period");
            Debug.Log("Name cannot contain a period");
            return;
        }

        if (!int.TryParse(widthInputField.text, out var width)) {
            errorPanel.DisplayError("Width is invalid");
            Debug.Log("Width is invalid");
            return;
        }
        if (!int.TryParse(heightInputField.text, out var height)) {
            errorPanel.DisplayError("Height is invalid");
            Debug.Log("Height is invalid");
            return;
        }

        if(width < 1 || height < 1) {
            errorPanel.DisplayError("Dimensions too small");
            Debug.Log("Dimensions too small");
            return;
        }
        if(width > 100 || height > 100) {
            errorPanel.DisplayError("Dimensions too large");
            Debug.Log("Dimensions too large");
            return;
        }

        if (initialScreen.activeInHierarchy) {
            initialScreen.SetActive(false);
        }

        loadedProject = newProjectNameField.text;
        loadedProjectLabel.text = loadedProject;
        newProjectNameField.text = "";

        ClearColorHistory();

        newBlankProjectCreated?.Invoke(width, height, colorButton.GetComponent<Image>().color);

        newProjectWindow.gameObject.SetActive(false);

        projectSettings = new ProjectSettings();

        AddColorToHistory(projectSettings.currentColor);

        SaveProject();
        SaveSettings();

    }

    void OnConfirmPhotoUpload() {
        if (newProjectNameField.text == "") {
            Debug.Log("Empty project name");
            errorPanel.DisplayError("Empty project name");
            return;
        }
        else if (newProjectNameField.text.Contains('.')) {
            errorPanel.DisplayError("Name cannot contain a period");
            Debug.Log("Name cannot contain a period");
            return;
        }

        if(uploadPreview.GetTexture() == null) {
            errorPanel.DisplayError("No photo selected");
            Debug.Log("No photo selected");
            return;
        }

        if(initialScreen.activeInHierarchy) {
            initialScreen.SetActive(false);
        }

        loadedProject = newProjectNameField.text;
        loadedProjectLabel.text = loadedProject;
        newProjectNameField.text = "";

        photoUploadConfirmed?.Invoke(uploadPreview.GetTexture());
        newProjectWindow.gameObject.SetActive(false);

        ClearColorHistory();

        projectSettings = new ProjectSettings();

        projectSettings.currentColor = currentColorImage.color;
        AddColorToHistory(projectSettings.currentColor);

        paletteColors.Clear();
        paletteController.SetColors(paletteColors);

        SaveProject();
        SaveSettings();
    }

    void OnColorSelected(Color color) {
        AddColorToHistory(color);

        currentColorImage.color = color;
        projectSettings.currentColor = color;

        SaveSettings();
    }

    void OnHistoryColorSelected(Color color) {
        currentColorImage.color = color;
        projectSettings.currentColor = color;

        SaveSettings();
    }

    void AddColorToHistory(Color color) {
        Transform disabledColor = FindDisabledPrevColor();
        if (disabledColor == null) {
            prevColors.GetChild(7).GetComponent<Image>().color = color;
            prevColors.GetChild(7).SetAsFirstSibling();
            colorHistory.RemoveAt(7);
        }
        else {
            disabledColor.gameObject.SetActive(true);
            disabledColor.GetComponent<Image>().color = color;
            disabledColor.SetAsFirstSibling();
        }
        colorHistory.Insert(0, color);
        projectSettings.colorHistory = colorHistory.ToArray();
    }

    Transform FindDisabledPrevColor() {
        for(int i = 0; i < 8; i++) {
            if(!prevColors.GetChild(i).gameObject.activeSelf) {
                return prevColors.GetChild(i);
            }
        }

        return null;
    }

    void OnGridToggle(bool value) {
        textureController.ToggleGrid(value);
    }

    public void ProjectLoadConfirmed(string projectName) {
        if(userSettings.loadedProject != projectName) {
            userSettings.loadedProject = projectName;
            SaveUserSettings();
        }

        if (initialScreen.activeInHierarchy) {
            initialScreen.SetActive(false);
        }
        textureController.EnableCanvas();
        string fullPath = Application.dataPath + "/SavedProjects/" + projectName + ".png";

        byte[] fileData = File.ReadAllBytes(fullPath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        Debug.Log(texture.width+ "x" + texture.height);

        projectLoaded?.Invoke(texture);
        loadedProject = projectName;
        loadedProjectLabel.text = loadedProject;

        string settingsPath = Application.dataPath + "/SavedProjects/" + projectName + ".txt";
        if(!File.Exists(settingsPath)) {
            projectSettings = new ProjectSettings();
            SaveSettings();
        }
        else {
            string settingsText;
            using (StreamReader settingsFile = File.OpenText(settingsPath)) {
                settingsText = settingsFile.ReadToEnd();
            }

            projectSettings = JsonUtility.FromJson<ProjectSettings>(settingsText);
        }

        if(projectSettings.currentStep > 0) {
            startLatchButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Continue";
            currentStepDisplay.text = (projectSettings.currentStep + 1).ToString();
        }

        currentColorImage.color = projectSettings.currentColor;
        textureController.SetColor(currentColorImage.color);

        SetColorHistory(projectSettings.colorHistory);

        paletteColors.Clear();

        if(projectSettings.paletteColors != null) {
            paletteColors.AddRange(projectSettings.paletteColors);
        }

        paletteController.SetColors(paletteColors);
    }

    void SetColorHistory(Color[] colors) {
        ClearColorHistory();
        if (colors != null) {
            for (int colorIndex = 0; colorIndex < projectSettings.colorHistory.Length; colorIndex++) {
                colorHistory.Add(projectSettings.colorHistory[colorIndex]);
                prevColors.GetChild(colorIndex).gameObject.SetActive(true);
                prevColors.GetChild(colorIndex).GetComponent<Image>().color = projectSettings.colorHistory[colorIndex];
            }
        }
    }

    void ClearColorHistory() {
        for (int colorIndex = 0; colorIndex < prevColors.childCount; colorIndex++) {
            prevColors.GetChild(colorIndex).gameObject.SetActive(false);
        }
        colorHistory.Clear();
    }

    void PaletteColorsUpdated(List<Color> paletteColors) {
        projectSettings.paletteColors = paletteColors.ToArray();
        SaveSettings();
    }

    void OnLoadImageButton() {
        //GetComponent<FileBrowserUpdate>().LoadImage();
    }

    void SaveProject() {
        Texture2D image = textureController.GetImage();
        File.WriteAllBytes(Application.dataPath + "/SavedProjects/" + loadedProject + ".png", image.EncodeToPNG());
    }

    void OnLatchModeButton() {
        projectSettings.stepMode = !projectSettings.stepMode;
        if (projectSettings.stepMode) {
            latchModeButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "By tile";
        }
        else {
            latchModeButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "By row";
        }

        projectSettings.currentStep = textureController.ChangeProgressMode(projectSettings.stepMode);
        currentStepDisplay.text = (projectSettings.currentStep + 1).ToString();
        SaveSettings();
    }

    void OnStartLatchButton() {
        runLatchPanel.gameObject.SetActive(true);
        latchMode = true;
        if (projectSettings.currentStep > 0) {
            textureController.ContinueLatch(projectSettings.currentStep, projectSettings.stepMode);
        }
        else {
            textureController.StartLatch(projectSettings.stepMode);
        }
    }

    void OnStopLatchButton() {
        if(projectSettings.currentStep > 0) {
            startLatchButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Continue";
        }
        else {
            startLatchButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Start";
        }

        runLatchPanel.gameObject.SetActive(false);
        latchMode = false;
        textureController.StopLatch();
    }

    void OnNextButton() {
        projectSettings.currentStep = textureController.NextLatchStep();
        currentStepDisplay.text = (projectSettings.currentStep + 1).ToString();
        SaveSettings();
    }

    void OnPrevButton() {
        projectSettings.currentStep = textureController.PrevLatchStep();
        currentStepDisplay.text = (projectSettings.currentStep + 1).ToString();
        SaveSettings();
    }

    void OnLatchFinished() {
        startLatchButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Start";
        currentStepDisplay.text = (projectSettings.currentStep + 1).ToString();
        runLatchPanel.gameObject.SetActive(false);
        latchMode = false;
    }

    void SaveUserSettings() {
        string settingsJson = JsonUtility.ToJson(userSettings);
        StreamWriter saveFile = File.CreateText(Application.dataPath + "/UserSettings.txt");
        saveFile.Write(settingsJson);
        saveFile.Close();
    }

    void SaveSettings() {
        string settingsJson = JsonUtility.ToJson(projectSettings);
        StreamWriter saveFile = File.CreateText(Application.dataPath + "/SavedProjects/" + loadedProject + ".txt");
        saveFile.Write(settingsJson);
        saveFile.Close();
    }

}

[System.Serializable]
public class UserSettings {
    public string loadedProject = string.Empty;
    public Texture2D backgroundImage = null;

}

[System.Serializable]
public class ProjectSettings {
    public bool stepMode = false; //false = by row, true = by tile
    public int currentStep = 0;
    public Color currentColor = Color.white;
    public Color[] colorHistory = null;
    public Color[] paletteColors = null;
}

public enum ColorSelectionMode {
    currentColor,
    paletteColor
}
