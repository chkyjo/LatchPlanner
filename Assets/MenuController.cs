using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;
using System.Collections.Generic;
using System.Collections;

public class MenuController : MonoBehaviour{

    public static Action<int, int, Color> newBlankProjectCreated;
    public static Action<Texture2D> projectLoaded;
    public static Action<Texture2D> photoUploadConfirmed;

    public Color defaultBackgroundColor;
    [SerializeField] GameObject backgroundPanel;
    [SerializeField] GameObject settingsPanel;

    [SerializeField] GameObject initialScreen;

    [SerializeField] Button newProjectButton;
    [SerializeField] GameObject newProjectWindow;
    [SerializeField] TMP_InputField newProjectNameField;
    [SerializeField] Button newCanvasButton;
    [SerializeField] GameObject colorButton; //color button when creating new blank canvas
    [SerializeField] ErrorPanel errorPanel;

    [SerializeField] Button saveAsButton;
    [SerializeField] GameObject saveAsPanel;
    [SerializeField] TMP_InputField saveAsNameInput;

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
        saveAsButton.onClick.AddListener(OnSaveAsButton);

        startLatchButton.onClick.AddListener(OnStartLatchButton);
        stopLatchButton.onClick.AddListener(OnStopLatchButton);
        nextStepButton.onClick.AddListener(OnNextButton);
        latchModeButton.onClick.AddListener(OnLatchModeButton);
        prevStepButton.onClick.AddListener(OnPrevButton);

        paletteColors = new List<Color>();
    }

    private void OnEnable() {
        //TextureController.textureUpdated += SaveProject;
        TextureController.latchFinished += OnLatchFinished;
        PaletController.paletteColorsUpdated += PaletteColorsUpdated;
        ColorHistoryController.currentColorUpdated += OnColorUpdated;
        ColorHistoryController.colorHistoryUpdated += OnHistoryUpdated;
        ColorPicker.backgroundColorSelected += OnBackgroundColorSelected;
        SettingsMenu.backgroundImageSelected += OnBackgroundImageSelected;
    }

    private void OnDisable() {
        //TextureController.textureUpdated -= SaveProject;
        TextureController.latchFinished -= OnLatchFinished;
        PaletController.paletteColorsUpdated -= PaletteColorsUpdated;
        ColorHistoryController.currentColorUpdated -= OnColorUpdated;
        ColorHistoryController.colorHistoryUpdated -= OnHistoryUpdated;
        ColorPicker.backgroundColorSelected -= OnBackgroundColorSelected;
        SettingsMenu.backgroundImageSelected -= OnBackgroundImageSelected;
    }

    private void Start() {
        
        string settingsPath = Application.persistentDataPath + "/UserSettings.txt";
        if (!File.Exists(settingsPath)) {
            userSettings = new UserSettings();
            userSettings.backgroundColor = defaultBackgroundColor;
            SaveUserSettings();
        }
        else {
            string settingsText;
            using (StreamReader settingsFile = File.OpenText(settingsPath)) {
                settingsText = settingsFile.ReadToEnd();
            }

            userSettings = JsonUtility.FromJson<UserSettings>(settingsText);
        }

        if(!Directory.Exists(Application.persistentDataPath + "/SavedProjects")) {
            Directory.CreateDirectory(Application.persistentDataPath + "/SavedProjects");
        }

        if (userSettings.loadedProject != string.Empty) {
            Debug.Log(userSettings.loadedProject);
            ProjectLoadConfirmed(userSettings.loadedProject);
        }
        else {
            initialScreen.SetActive(true);
        }

        if (userSettings.solidBackground) {
            backgroundPanel.GetComponent<RawImage>().color = userSettings.backgroundColor;
            settingsPanel.GetComponent<RawImage>().color = userSettings.backgroundColor;
        }
        else {
            if (File.Exists(Application.persistentDataPath + "/background.png")) {
                byte[] fileData = File.ReadAllBytes(Application.persistentDataPath + "/background.png");
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);

                backgroundPanel.GetComponent<RawImage>().texture = texture;
                settingsPanel.GetComponent<RawImage>().texture = texture;
            }

            backgroundPanel.GetComponent<RawImage>().color = Color.white;
            settingsPanel.GetComponent<RawImage>().color = Color.white;
        }

        StartCoroutine(SaveRoutine());
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
        userSettings.loadedProject = loadedProject;

        paletteColors.Clear();
        paletteController.SetColors(paletteColors);

        Color currentColor = colorButton.GetComponent<Image>().color;
        newBlankProjectCreated?.Invoke(width, height, currentColor);
        projectSettings = new ProjectSettings();
        projectSettings.currentColor = currentColor;
        ColorHistoryController.SetCurrentColor(currentColor);
        ColorHistoryController.ClearHistory();

        newProjectWindow.gameObject.SetActive(false);

        SaveProject();
        SaveSettings();
        SaveUserSettings();
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

        ColorHistoryController.ClearHistory();
        ColorHistoryController.SetCurrentColor(Color.white);

        projectSettings = new ProjectSettings();
        projectSettings.currentColor = Color.white;

        paletteColors.Clear();
        paletteController.SetColors(paletteColors);

        SaveProject();
        SaveSettings();
    }

    void OnSaveAsButton() {
        saveAsPanel.gameObject.SetActive(true);
    }
    public void OnConfirmSaveAs() {
        if(saveAsNameInput.text == "") {
            saveAsPanel.GetComponent<ErrorPanel>().DisplayError("Enter a name");
            return;
        }
        saveAsPanel.SetActive(false);
        loadedProject = saveAsNameInput.text;
        loadedProjectLabel.text = loadedProject;
        saveAsNameInput.text = "";
        userSettings.loadedProject = loadedProject;

        SaveProject();
        SaveSettings();
    }

    void OnHistoryUpdated(Color[] colors) {
        projectSettings.colorHistory = colors;
        SaveSettings();
    }

    void OnColorUpdated(Color color) {
        projectSettings.currentColor = color;
        SaveSettings();
    }

    void OnBackgroundColorSelected(Color color) {
        userSettings.backgroundColor = color;
        userSettings.solidBackground = true;
        backgroundPanel.GetComponent<RawImage>().color = color;
        backgroundPanel.GetComponent<RawImage>().texture = null;
        SaveUserSettings();
    }

    void OnBackgroundImageSelected(Texture2D texture) {
        userSettings.backgroundTexture = texture;
        userSettings.solidBackground = false;
        SaveBackgroundImage(texture);
        backgroundPanel.GetComponent<RawImage>().texture = texture;
        backgroundPanel.GetComponent<RawImage>().color = Color.white;
        SaveUserSettings();
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
        string fullPath = Application.persistentDataPath + "/SavedProjects/" + projectName + ".png";

        byte[] fileData = File.ReadAllBytes(fullPath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);

        projectLoaded?.Invoke(texture);
        loadedProject = projectName;
        loadedProjectLabel.text = loadedProject;

        string settingsPath = Application.persistentDataPath + "/SavedProjects/" + projectName + ".txt";
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
            //startLatchButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Continue";
            currentStepDisplay.text = (projectSettings.currentStep + 1).ToString();
        }

        textureController.SetColor(projectSettings.currentColor);

        ColorHistoryController.SetCurrentColor(projectSettings.currentColor);
        ColorHistoryController.SetColorHistory(projectSettings.colorHistory);

        paletteColors.Clear();

        if(projectSettings.paletteColors != null) {
            paletteColors.AddRange(projectSettings.paletteColors);
        }

        paletteController.SetColors(paletteColors);
    }

    void PaletteColorsUpdated(List<Color> paletteColors) {
        projectSettings.paletteColors = paletteColors.ToArray();
        SaveSettings();
    }

    void OnLoadImageButton() {
        //GetComponent<FileBrowserUpdate>().LoadImage();
    }

    IEnumerator SaveRoutine() {
        while (loadedProject != "") {
            yield return new WaitForSeconds(5f);
            SaveProject();
        }
    }

    void SaveProject() {
        Debug.Log("Saving project image file");
        Texture2D image = textureController.GetImage();
        File.WriteAllBytes(Application.persistentDataPath + "/SavedProjects/" + loadedProject + ".png", image.EncodeToPNG());
    }

    void SaveBackgroundImage(Texture2D texture) {
        File.WriteAllBytes(Application.persistentDataPath + "/background.png", texture.EncodeToPNG());
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
        //if(projectSettings.currentStep > 0) {
        //    startLatchButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Continue";
        //}
        //else {
        //    startLatchButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Start";
        //}

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
        //startLatchButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Start";
        currentStepDisplay.text = (projectSettings.currentStep + 1).ToString();
        runLatchPanel.gameObject.SetActive(false);
        latchMode = false;
    }

    void SaveUserSettings() {
        string settingsJson = JsonUtility.ToJson(userSettings);
        StreamWriter saveFile = File.CreateText(Application.persistentDataPath + "/UserSettings.txt");
        saveFile.Write(settingsJson);
        saveFile.Close();
    }

    void SaveSettings() {
        string settingsJson = JsonUtility.ToJson(projectSettings);
        StreamWriter saveFile = File.CreateText(Application.persistentDataPath + "/SavedProjects/" + loadedProject + ".txt");
        saveFile.Write(settingsJson);
        saveFile.Close();
    }

}

[System.Serializable]
public class UserSettings {
    public string loadedProject = string.Empty;
    public Texture2D backgroundImage = null;
    public bool solidBackground = true;
    public Color backgroundColor = Color.white;
    public Texture2D backgroundTexture = null;
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
    paletteColor,
    backgroundColor
}
