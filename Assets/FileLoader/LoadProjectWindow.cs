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

    private void Awake() {
        loadProjectButton.onClick.AddListener(OnOpenProjectButton);
        initialLoadProjectButton.onClick.AddListener(OnOpenProjectButton);
        confirmLoadProjectButton.onClick.AddListener(OnLoadProjectButton);
        cancelLoadProjectButton.onClick.AddListener(OnCancelLoadProject);
        confirmDeleteButton.onClick.AddListener(OnDeleteConfirmed);
        disabledProjectSelectors = new List<GameObject>();
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

        if (!Directory.Exists(Application.dataPath + "/SavedProjects/")) {
            Directory.CreateDirectory(Application.dataPath + "/SavedProjects/");
            return;
        }

        DirectoryInfo projectsFolder = new DirectoryInfo(Application.dataPath + "/SavedProjects/");
        FileInfo[] files = projectsFolder.GetFiles();

        GameObject selection;

        Debug.Log("Finding existing projects");

        foreach (FileInfo file in files) {
            if (!file.Name.EndsWith(".png")) {
                continue;
            }
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
        string fullPath = Application.dataPath + "/SavedProjects/" + selectedProject + ".png";
        if (!File.Exists(fullPath)) {
            Debug.Log("File not found: " + fullPath);
            return;
        }

        ResetLoadProjectList();

        GetComponent<MenuController>().ProjectLoadConfirmed(selectedProject);

    }

    void OnDeleteConfirmed() {
        string fullPath = Application.dataPath + "/SavedProjects/" + selectedProject + ".png";
        if (!File.Exists(fullPath)) {
            Debug.Log("File not found: " + fullPath);
            return;
        }

        confirmDeleteWindow.SetActive(false);

        File.Delete(fullPath);

        selectedButton.gameObject.SetActive(false);
    }
}
