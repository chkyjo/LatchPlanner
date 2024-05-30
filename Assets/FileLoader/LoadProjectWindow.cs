using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class LoadProjectWindow : MonoBehaviour{

    [SerializeField] Button loadProjectButton;
    [SerializeField] Button initialLoadProjectButton;
    [SerializeField] GameObject loadProjectWindow;
    [SerializeField] GameObject projectSelectPrefab;
    [SerializeField] Transform savedProjectsList;
    [SerializeField] Button confirmLoadProjectButton;
    [SerializeField] Button cancelLoadProjectButton;

    [SerializeField] Button confirmDeleteButton;
    [SerializeField] GameObject confirmDeleteWindow;

    List<GameObject> disabledProjectSelectors;
    GameObject selectedButton;
    string selectedProject = "";

    int topPadding = 5;
    Vector2 cellSize = new Vector2(100, 130);
    Vector2 spacing = new Vector2(5, 5);
    int numColumns = 5;
    float minContentHeight = 260;

    private void Awake() {
        loadProjectButton.onClick.AddListener(OnOpenProjectButton);
        initialLoadProjectButton.onClick.AddListener(OnOpenProjectButton);
        confirmLoadProjectButton.onClick.AddListener(OnLoadProjectButton);
        cancelLoadProjectButton.onClick.AddListener(OnCancelLoadProject);
        confirmDeleteButton.onClick.AddListener(OnDeleteConfirmed);
        disabledProjectSelectors = new List<GameObject>();

        savedProjectsList.GetComponent<GridLayoutGroup>().cellSize = cellSize;
        savedProjectsList.GetComponent<GridLayoutGroup>().spacing = spacing;
    }

    private void OnEnable() {
        LoadProjectItem.savedProjectSelected += OnProjectSelected;
    }

    private void OnDisable() {
        LoadProjectItem.savedProjectSelected -= OnProjectSelected;
    }

    void OnCancelLoadProject() {
        ResetLoadProjectList();
    }

    void ResetLoadProjectList() {
        disabledProjectSelectors.Clear();
        for (int i = 0; i < savedProjectsList.childCount; i++) {
            disabledProjectSelectors.Add(savedProjectsList.GetChild(i).gameObject);
            savedProjectsList.GetChild(i).gameObject.SetActive(false);
        }

        loadProjectWindow.gameObject.SetActive(false);
    }

    //folder icon button
    void OnOpenProjectButton() {
        loadProjectWindow.gameObject.SetActive(true);

        if (!Directory.Exists(Application.persistentDataPath + "/SavedProjects/")) {
            Directory.CreateDirectory(Application.persistentDataPath + "/SavedProjects/");
            return;
        }

        DirectoryInfo projectsFolder = new DirectoryInfo(Application.persistentDataPath + "/SavedProjects/");
        FileInfo[] files = projectsFolder.GetFiles();

        GameObject selection;
        int numFiles = 0;

        foreach (FileInfo file in files) {
            if (!file.Name.EndsWith(".png")) {
                continue;
            }
            numFiles++;
            if (disabledProjectSelectors.Count > 0) {
                selection = disabledProjectSelectors[0];
                selection.SetActive(true);
                disabledProjectSelectors.RemoveAt(0);
            }
            else {
                selection = Instantiate(projectSelectPrefab, savedProjectsList);
            }
            selection.GetComponent<LoadProjectItem>().SetProjectName(file.Name.Split('.')[0]);

            Texture2D texture = new Texture2D(2, 2);
            byte[] fileData = File.ReadAllBytes(file.FullName);
            texture.LoadImage(fileData);
            selection.GetComponent<LoadProjectItem>().SetProjectPreview(texture);
        }

        int numRows = numFiles / numColumns;
        if(numFiles % numColumns != 0) {
            numRows++;
        }
        float contentHeight = numRows * (cellSize.y + spacing.y) + topPadding;
        if(contentHeight < minContentHeight) {
            contentHeight = minContentHeight;
        }
        savedProjectsList.GetComponent<RectTransform>().sizeDelta = new Vector2(0, contentHeight);
    }

    //called by buttons
    void OnProjectSelected(GameObject button) {
        if(selectedButton != null) {
            selectedButton.GetComponent<LoadProjectItem>().ShowUnselected();
        }
        selectedButton = button;
        selectedButton.GetComponent<LoadProjectItem>().ShowSelected();
        selectedProject = selectedButton.GetComponent<LoadProjectItem>().GetProjectName();
    }

    void OnLoadProjectButton() {
        if (selectedProject == "") {
            Debug.Log("Selected project name empty");
            return;
        }
        string fullPath = Application.persistentDataPath + "/SavedProjects/" + selectedProject + ".png";
        if (!File.Exists(fullPath)) {
            Debug.Log("File not found: " + fullPath);
            return;
        }

        ResetLoadProjectList();

        GetComponent<MenuController>().ProjectLoadConfirmed(selectedProject);

    }

    void OnDeleteConfirmed() {
        string fullPath = Application.persistentDataPath + "/SavedProjects/" + selectedProject + ".png";
        if (!File.Exists(fullPath)) {
            Debug.Log("File not found: " + fullPath);
            return;
        }

        confirmDeleteWindow.SetActive(false);

        File.Delete(fullPath);

        selectedButton.gameObject.SetActive(false);
    }
}
