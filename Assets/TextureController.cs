using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TextureController : MonoBehaviour{

    public static Action textureUpdated;
    public static Action latchFinished;

    Texture2D texture;
    Color currentColor = Color.white;

    float maxWidth;
    float maxHeight;

    int maxRows = 100;
    int maxColumns = 100;

    Vector2Int resolution = new Vector2Int(25, 25);

    [SerializeField] GameObject pixelCanvas;
    [SerializeField] Transform scrollableArea;
    [SerializeField] PaintInterface paintInterface;

    [SerializeField] Transform verticalLines;
    [SerializeField] Transform horizontalLines;

    [SerializeField] Transform rowNumbers;
    [SerializeField] Transform columnNumbers;

    bool editMode = false; //false = remove  true = add
    [SerializeField] Button expandButton;
    [SerializeField] Button removeButton;
    [SerializeField] Button editColumnLeft;
    [SerializeField] Button editColumnRight;
    [SerializeField] Button editTopRowButton;
    [SerializeField] Button editBottomRowButton;
    [SerializeField] Button paintButton;
    [SerializeField] Button dragButton;
    [SerializeField] Button zoomButton;
    [SerializeField] Button zoomOutButton;
    [SerializeField] Button fillToolButton;
    [SerializeField] Texture2D fillCursor;

    [SerializeField] RectTransform rowHighlighter;
    [SerializeField] RectTransform columnHighlighter;
    [SerializeField] RawImage highlighter;
    Vector2Int highlightedPixel = Vector2Int.zero;
    Color highlightedColor = new Color(0, 0, 0, 0);
    Color notHighlightedColor = new Color(0, 0, 0, .7f);
    int currentStep = 0;
    bool latchMode = false;

    int topAndLeftPadding = 30;

    RectTransform rT;

    [SerializeField] Texture2D dragCursor;
    [SerializeField] Texture2D standardCursor;

    float fillSpeed = 0.01f; //time per pixel
    int numFillRoutines = 0; //used to determine when a fill is completed

    CanvasMode canvasMode;

    void Awake() {

        for (int i = 0; i < maxRows; i++) {
            rowNumbers.GetChild(i).GetComponent<TMP_Text>().text = (i + 1).ToString();
            columnNumbers.GetChild(i).GetComponent<TMP_Text>().text = (i + 1).ToString();
        }

        expandButton.onClick.AddListener(OnExpandButton);
        removeButton.onClick.AddListener(OnRemoveButton);
        editColumnLeft.onClick.AddListener(OnEditLeftColumn);
        editBottomRowButton.onClick.AddListener(OnEditBottomRow);
        editColumnRight.onClick.AddListener(OnEditRightColumn);
        editTopRowButton.onClick.AddListener(OnEditTopRow);
        dragButton.onClick.AddListener(OnDragButton);
        zoomButton.onClick.AddListener(OnZoomButton);
        zoomOutButton.onClick.AddListener(OnZoomOutButton);
        fillToolButton.onClick.AddListener(OnFillToolButton);
        paintButton.onClick.AddListener(OnPaintButton);
    }

    void Start() {
        rT = pixelCanvas.GetComponent<RectTransform>();
        maxWidth = Mathf.Abs(GetComponent<RectTransform>().rect.width) - topAndLeftPadding;
        maxHeight = Mathf.Abs(GetComponent<RectTransform>().rect.height) - topAndLeftPadding;
        rT.sizeDelta = new Vector2(maxWidth, maxHeight);
        //pixelCanvas.SetActive(false);
    }

    private void OnEnable() {
        ColorPicker.colorSelected += ColorSelected;
        MenuController.newBlankProjectCreated += NewBlankProject;
        MenuController.projectLoaded += OnProjectLoaded;
        MenuController.photoUploadConfirmed += OnTextureUpload;
        PaletController.colorSelected += ColorSelected;
        ColorHistoryController.colorSelected += ColorSelected;
        PaintInterface.paintedPoint += UpdatePixel;
    }

    private void OnDisable() {
        ColorPicker.colorSelected -= ColorSelected;
        MenuController.newBlankProjectCreated -= NewBlankProject;
        MenuController.projectLoaded -= OnProjectLoaded;
        MenuController.photoUploadConfirmed -= OnTextureUpload;
        PaletController.colorSelected -= ColorSelected;
        ColorHistoryController.colorSelected -= ColorSelected;
        PaintInterface.paintedPoint -= UpdatePixel;
    }

    void Update() {
        if(Input.mouseScrollDelta.y > 0) {
            if (scrollableArea.localScale.x > 3) {
                return;
            }
            scrollableArea.localScale += new Vector3(0.1f, 0.1f, 0.1f);
        }
        else if(Input.mouseScrollDelta.y < 0) {
            if(scrollableArea.localScale.x < 0.2) {
                return;
            }
            scrollableArea.localScale -= new Vector3(0.1f, 0.1f, 0.1f);
        }
    }

    public void NewBlankProject(int width, int height, Color baseColor) {
        EnableCanvas();
        SetResolution(width, height);
        CreateNewGrid(baseColor);
    }

    public void EnableCanvas() {
        pixelCanvas.SetActive(true);
    }

    void OnProjectLoaded(Texture2D uploadedProject) {
        SetResolution(uploadedProject.width, uploadedProject.height);

        ResizeCanvas();

        UpdateGridLines();

        uploadedProject.filterMode = FilterMode.Point;
        texture = uploadedProject;
        pixelCanvas.GetComponent<RawImage>().texture = texture;
        pixelCanvas.SetActive(true);
    }

    void OnTextureUpload(Texture2D uploadedTexture) {

        SetResolution(uploadedTexture.width, uploadedTexture.height);

        ResizeCanvas();

        UpdateGridLines();

        texture = uploadedTexture;
        pixelCanvas.GetComponent<RawImage>().texture = uploadedTexture;
        pixelCanvas.SetActive(true);
    }

    void OnExpandButton() {
        editMode = true;
        editColumnLeft.transform.GetChild(0).GetComponent<TMP_Text>().text = "+";
        editColumnLeft.GetComponent<Image>().color = Color.green;

        editColumnRight.transform.GetChild(0).GetComponent<TMP_Text>().text = "+";
        editColumnRight.GetComponent<Image>().color = Color.green;

        editTopRowButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "+";
        editTopRowButton.GetComponent<Image>().color = Color.green;

        editBottomRowButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "+";
        editBottomRowButton.GetComponent<Image>().color = Color.green;
    }

    void OnRemoveButton() {
        editMode = false;
        editColumnLeft.transform.GetChild(0).GetComponent<TMP_Text>().text = "-";
        editColumnLeft.GetComponent<Image>().color = Color.red;

        editColumnRight.transform.GetChild(0).GetComponent<TMP_Text>().text = "-";
        editColumnRight.GetComponent<Image>().color = Color.red;

        editTopRowButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "-";
        editTopRowButton.GetComponent<Image>().color = Color.red;

        editBottomRowButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "-";
        editBottomRowButton.GetComponent<Image>().color = Color.red;
    }

    void ColorSelected(Color col) {
        currentColor = col;
    }

    public void SetResolution(int width, int height) {
        resolution.x = width;
        resolution.y = height;
    }

    public void SetColor(Color color) {
        currentColor = color;
    }

    public void CreateNewGrid(Color color) {

        ResizeCanvas();

        UpdateGridLines();

        texture = new Texture2D(resolution.x, resolution.y);
        texture.filterMode = FilterMode.Point;
        texture.name = "LatchPattern";

        for (int x = 0; x < resolution.x; x++) {
            for (int y = 0; y < resolution.y; y++) {
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        pixelCanvas.GetComponent<RawImage>().texture = texture;
    }

    void OnEditLeftColumn() {
        if (editMode && resolution.x >= maxColumns) {
            Debug.Log("Reached max columns");
            return;
        }
        else if (!editMode && resolution.x < 2) {
            Debug.Log("Min columns reached");
            return;
        }

        if (editMode) {
            resolution.x += 1;
        }
        else {
            resolution.x -= 1;
        }

        Texture2D newTexture = GetNewTexture();

        if (editMode) {
            for (int x = 0; x < texture.width; x++) {
                for (int y = 0; y < texture.height; y++) {
                    newTexture.SetPixel(x + 1, y, texture.GetPixel(x, y));
                }
            }
        }
        else {
            for (int x = 0; x < texture.width - 1; x++) {
                for (int y = 0; y < texture.height; y++) {
                    newTexture.SetPixel(x, y, texture.GetPixel(x + 1, y));
                }
            }
        }

        ApplyNewTexture(newTexture);
    }

    void OnEditRightColumn() {
        if (editMode && resolution.x >= maxColumns) {
            Debug.Log("Reached max columns");
            return;
        }
        else if (!editMode && resolution.x < 2) {
            Debug.Log("Min columns reached");
            return;
        }

        if (editMode) {
            resolution.x += 1;
        }
        else {
            resolution.x -= 1;
        }

        Texture2D newTexture = GetNewTexture();

        if (editMode) {
            for (int x = 0; x < texture.width; x++) {
                for (int y = 0; y < texture.height; y++) {
                    newTexture.SetPixel(x, y, texture.GetPixel(x, y));
                }
            }
        }
        else {
            for (int x = 0; x < texture.width - 1; x++) {
                for (int y = 0; y < texture.height; y++) {
                    newTexture.SetPixel(x, y, texture.GetPixel(x, y));
                }
            }
        }

        ApplyNewTexture(newTexture);
    }

    void OnEditTopRow() {
        if (editMode && resolution.y >= maxColumns) {
            Debug.Log("Reached max columns");
            return;
        }
        else if (!editMode && resolution.y < 2) {
            Debug.Log("Min columns reached");
            return;
        }

        if (editMode) {
            resolution.y += 1;
        }
        else {
            resolution.y -= 1;
        }

        Texture2D newTexture = GetNewTexture();

        if (editMode) {
            for (int x = 0; x < texture.width; x++) {
                for (int y = 0; y < texture.height; y++) {
                    newTexture.SetPixel(x, y, texture.GetPixel(x, y));
                }
            }
        }
        else {
            for (int x = 0; x < texture.width; x++) {
                for (int y = 0; y < texture.height - 1; y++) {
                    newTexture.SetPixel(x, y, texture.GetPixel(x, y));
                }
            }
        }

        ApplyNewTexture(newTexture);
    }

    void OnEditBottomRow() {
        if (editMode) {
            AddBottomRow();
        }
        else{
            RemoveBottomRow();
        }
    }

    void AddBottomRow() {
        if (resolution.y >= maxColumns) {
            Debug.Log("Reached max columns");
            return;
        }

        resolution.y += 1;

        Texture2D newTexture = GetNewTexture();

        for (int x = 0; x < texture.width; x++) {
            for (int y = 0; y < texture.height; y++) {
                newTexture.SetPixel(x, y + 1, texture.GetPixel(x, y));
            }
        }

        ApplyNewTexture(newTexture);
    }

    void RemoveBottomRow() {
        if (resolution.y < 2) {
            Debug.Log("Min columns reached");
            return;
        }

        resolution.y -= 1;

        Texture2D newTexture = GetNewTexture();

        for (int x = 0; x < texture.width; x++) {
            for (int y = 0; y < texture.height - 1; y++) {
                newTexture.SetPixel(x, y, texture.GetPixel(x, y + 1));
            }
        }

        ApplyNewTexture(newTexture);
    }

    Texture2D GetNewTexture() {
        Texture2D texture = new Texture2D(resolution.x, resolution.y);
        texture.filterMode = FilterMode.Point;
        texture.name = "LatchPattern";
        return texture;
    }

    void ApplyNewTexture(Texture2D newTexture) {
        texture = newTexture;
        texture.Apply();
        pixelCanvas.GetComponent<RawImage>().texture = texture;

        ResizeCanvas();

        UpdateGridLines();

        textureUpdated?.Invoke();
    }

    void UpdateGridLines() {
        for (int i = 0; i < resolution.x - 1; i++) {
            verticalLines.GetChild(i).gameObject.SetActive(true);
            columnNumbers.GetChild(i).gameObject.SetActive(true);
        }
        for (int i = resolution.x - 1; i < maxColumns; i++) {
            verticalLines.GetChild(i).gameObject.SetActive(false);
            columnNumbers.GetChild(i).gameObject.SetActive(false);
        }

        int pixelsPerUnit = (int)(rT.sizeDelta.x / resolution.x);
        verticalLines.GetComponent<HorizontalLayoutGroup>().padding.left = pixelsPerUnit;

        for (int i = 0; i < resolution.y - 1; i++) {
            horizontalLines.GetChild(i).gameObject.SetActive(true);
            rowNumbers.GetChild(i).gameObject.SetActive(true);
        }
        for (int i = resolution.y - 1; i < maxRows; i++) {
            horizontalLines.GetChild(i).gameObject.SetActive(false);
            rowNumbers.GetChild(i).gameObject.SetActive(false);
        }

        horizontalLines.GetComponent<VerticalLayoutGroup>().padding.top = pixelsPerUnit;

        //add one more number for row and column
        rowNumbers.GetChild(resolution.y - 1).gameObject.SetActive(true);
        columnNumbers.GetChild(resolution.x - 1).gameObject.SetActive(true);

        columnNumbers.GetComponent<GridLayoutGroup>().cellSize = new Vector2(rT.sizeDelta.x / resolution.x, 30);
    }

    void ResizeCanvas() {
        if (resolution.x / maxWidth > resolution.y / maxHeight) {
            float multiplier = maxWidth / resolution.x;
            float height = multiplier * resolution.y;
            rT.sizeDelta = new Vector2(maxWidth, height);
            scrollableArea.GetComponent<RectTransform>().sizeDelta = new Vector2(maxWidth + 50, height + 50);
        }
        else {
            float multiplier = maxHeight / resolution.y;
            float width = multiplier * resolution.x;
            rT.sizeDelta = new Vector2(width, maxHeight);
            scrollableArea.GetComponent<RectTransform>().sizeDelta = new Vector2(width + 50, maxHeight + 50);
        }

    }

    public void ToggleGrid(bool grid) {
        verticalLines.gameObject.SetActive(grid);
        horizontalLines.gameObject.SetActive(grid);
    }

    void OnPaintButton() {
        canvasMode = CanvasMode.paintMode;
        GetComponent<CanvasScroller>().canvasMode = canvasMode;
        paintInterface.gameObject.SetActive(true);
        Cursor.SetCursor(standardCursor, new Vector2(0, 0), CursorMode.ForceSoftware);
    }

    void OnDragButton() {
        canvasMode = CanvasMode.dragMode;
        GetComponent<CanvasScroller>().canvasMode = canvasMode;
        paintInterface.gameObject.SetActive(false);
        Cursor.SetCursor(dragCursor, new Vector2(32, 32), CursorMode.ForceSoftware);
    }

    void OnZoomButton() {
        if (scrollableArea.localScale.x > 3) {
            return;
        }
        scrollableArea.localScale += new Vector3(0.1f, 0.1f, 0.1f);
    }

    void OnZoomOutButton() {
        if (scrollableArea.localScale.x < 0.2) {
            return;
        }
        scrollableArea.localScale -= new Vector3(0.1f, 0.1f, 0.1f);
    }

    void OnFillToolButton() {
        canvasMode = CanvasMode.fillMode;
        GetComponent<CanvasScroller>().canvasMode = canvasMode;
        paintInterface.gameObject.SetActive(true);
        Cursor.SetCursor(fillCursor, Vector2.zero, CursorMode.ForceSoftware);
    }

    void UpdatePixel(PointerEventData eventData) {
        Vector3 pos = rT.InverseTransformPoint(eventData.position);

        pos.x = Mathf.Clamp(pos.x, 0, rT.sizeDelta.x - 0.1f);
        pos.y = Mathf.Clamp(pos.y, 0, rT.sizeDelta.y - 0.1f);

        int pixelX = (int)(pos.x / rT.sizeDelta.x * resolution.x);
        int pixelY = (int)(pos.y / rT.sizeDelta.y * resolution.y);

        if (canvasMode == CanvasMode.fillMode) {
            Color oldColor = texture.GetPixel(pixelX, pixelY);
            if (oldColor == currentColor) {
                return;
            }
            StartCoroutine(FillPixels(pixelX, pixelY, oldColor));
        }
        else {
            texture.SetPixel(pixelX, pixelY, currentColor);
            texture.Apply();
            textureUpdated?.Invoke();
        }
    }

    IEnumerator FillPixels(int posX, int posY, Color oldColor) {
        numFillRoutines++;
        texture.SetPixel(posX, posY, currentColor);
        texture.Apply();
        yield return new WaitForSeconds(fillSpeed);

        if (posX < resolution.x - 1) {
            if(texture.GetPixel(posX + 1, posY) == oldColor) {
                StartCoroutine(FillRight(posX + 1, posY, oldColor));
            }
        }

        if(posY > 0) {
            if (texture.GetPixel(posX, posY - 1) == oldColor) {
                StartCoroutine(FillDown(posX, posY - 1, oldColor));
            }
        }

        if(posX > 0) {
            if (texture.GetPixel(posX - 1, posY) == oldColor) {
                StartCoroutine(FillLeft(posX - 1, posY, oldColor));
            }
        }

        if(posY < resolution.y - 1) {
            if (texture.GetPixel(posX, posY + 1) == oldColor) {
                StartCoroutine(FillUp(posX, posY + 1, oldColor));
            }
        }

        numFillRoutines--;
        if (numFillRoutines == 0) {
            textureUpdated?.Invoke();
        }
    }

    IEnumerator FillRight(int posX, int posY, Color oldColor) {
        numFillRoutines++;
        texture.SetPixel(posX, posY, currentColor);
        texture.Apply();
        yield return new WaitForSeconds(fillSpeed);

        if (posX < resolution.x - 1) {
            if (texture.GetPixel(posX + 1, posY) == oldColor) {
                StartCoroutine(FillRight(posX + 1, posY, oldColor));
            }
        }

        if (posY > 0) {
            if (texture.GetPixel(posX, posY - 1) == oldColor) {
                StartCoroutine(FillDown(posX, posY - 1, oldColor));
            }
        }

        if (posY < resolution.y - 1) {
            if (texture.GetPixel(posX, posY + 1) == oldColor) {
                StartCoroutine(FillUp(posX, posY + 1, oldColor));
            }
        }

        numFillRoutines--;
        if (numFillRoutines == 0) {
            textureUpdated?.Invoke();
        }
    }

    IEnumerator FillDown(int posX, int posY, Color oldColor) {
        numFillRoutines++;
        texture.SetPixel(posX, posY, currentColor);
        texture.Apply();
        yield return new WaitForSeconds(fillSpeed);

        if (posX < resolution.x - 1) {
            if (texture.GetPixel(posX + 1, posY) == oldColor) {
                StartCoroutine(FillRight(posX + 1, posY, oldColor));
            }
        }

        if (posY > 0) {
            if (texture.GetPixel(posX, posY - 1) == oldColor) {
                StartCoroutine(FillDown(posX, posY - 1, oldColor));
            }
        }

        if (posX > 0) {
            if (texture.GetPixel(posX - 1, posY) == oldColor) {
                StartCoroutine(FillLeft(posX - 1, posY, oldColor));
            }
        }

        numFillRoutines--;
        if (numFillRoutines == 0) {
            textureUpdated?.Invoke();
        }
    }

    IEnumerator FillLeft(int posX, int posY, Color oldColor) {
        numFillRoutines++;
        texture.SetPixel(posX, posY, currentColor);
        texture.Apply();
        yield return new WaitForSeconds(fillSpeed);

        if (posY > 0) {
            if (texture.GetPixel(posX, posY - 1) == oldColor) {
                StartCoroutine(FillDown(posX, posY - 1, oldColor));
            }
        }

        if (posX > 0) {
            if (texture.GetPixel(posX - 1, posY) == oldColor) {
                StartCoroutine(FillLeft(posX - 1, posY, oldColor));
            }
        }

        if (posY < resolution.y - 1) {
            if (texture.GetPixel(posX, posY + 1) == oldColor) {
                StartCoroutine(FillUp(posX, posY + 1, oldColor));
            }
        }

        numFillRoutines--;
        if (numFillRoutines == 0) {
            textureUpdated?.Invoke();
        }
    }

    IEnumerator FillUp(int posX, int posY, Color oldColor) {
        numFillRoutines++;
        texture.SetPixel(posX, posY, currentColor);
        texture.Apply();
        yield return new WaitForSeconds(fillSpeed);

        if (posX < resolution.x - 1) {
            if (texture.GetPixel(posX + 1, posY) == oldColor) {
                StartCoroutine(FillRight(posX + 1, posY, oldColor));
            }
        }

        if (posX > 0) {
            if (texture.GetPixel(posX - 1, posY) == oldColor) {
                StartCoroutine(FillLeft(posX - 1, posY, oldColor));
            }
        }

        if (posY < resolution.y - 1) {
            if (texture.GetPixel(posX, posY + 1) == oldColor) {
                StartCoroutine(FillUp(posX, posY + 1, oldColor));
            }
        }

        numFillRoutines--;
        if (numFillRoutines == 0) {
            textureUpdated?.Invoke();
        }
    }

    public Texture2D GetImage() {
        return texture;
    }

    public void ContinueLatch(int step, bool mode) {
        currentStep = step;
        int pixelSize = (int)rT.rect.height / resolution.y;
        int rowIndex = step;
        Texture2D highlighterTexture = InitHighlighter(mode);
        latchMode = true;
        paintInterface.gameObject.SetActive(false);

        if (mode) {
            rowIndex = currentStep / resolution.x;
            columnHighlighter.gameObject.SetActive(true);
            columnHighlighter.sizeDelta = new Vector2(pixelSize, rT.rect.height);
            int columnIndex = currentStep % resolution.x;
            highlighterTexture.SetPixel(columnIndex, rowIndex, highlightedColor);
            highlightedPixel.x = columnIndex;
            highlightedPixel.y = rowIndex;
            HighlightTile(columnIndex);
        }
        else {
            highlighterTexture.SetPixel(0, rowIndex, highlightedColor);
        }

        highlighterTexture.Apply();

        rowHighlighter.gameObject.SetActive(true);
        rowHighlighter.sizeDelta = new Vector2(rT.rect.width, pixelSize);
        HighlightRow(rowIndex);

    }

    public void StartLatch(bool mode) {

        int pixelSize = (int)rT.rect.height / resolution.y;
        Texture2D highlighterTexture = InitHighlighter(mode);
        latchMode = true;
        paintInterface.gameObject.SetActive(false);

        rowHighlighter.gameObject.SetActive(true);
        rowHighlighter.localPosition = new Vector3(0, 0, 0);
        rowHighlighter.sizeDelta = new Vector2(rT.rect.width, pixelSize);

        if(mode) {
            columnHighlighter.gameObject.SetActive(true);
            columnHighlighter.localPosition = new Vector3(0, 0, 0);
            columnHighlighter.sizeDelta = new Vector2(pixelSize, rT.rect.height);

            highlighterTexture.SetPixel(0, 0, highlightedColor);
            highlightedPixel = Vector2Int.zero;
        }
        else {
            highlighterTexture.SetPixel(0, 0, highlightedColor);
        }

        highlighterTexture.Apply();
    }

    Texture2D InitHighlighter(bool mode) {
        highlighter.gameObject.SetActive(true);
        Texture2D highlighterTexture;
        if(mode) {
            highlighterTexture = new Texture2D(resolution.x, resolution.y);
        }
        else {
            highlighterTexture = new Texture2D(1, resolution.y);
        }
        highlighterTexture.filterMode = FilterMode.Point;
        highlighter.GetComponent<RawImage>().texture = highlighterTexture;

        //for (int rowIndex = 0; rowIndex < resolution.y; rowIndex++) {
        //    for (int columnIndex = 0; columnIndex < resolution.x; columnIndex++) {
        //        highlighterTexture.SetPixel(columnIndex, rowIndex, notHighlightedColor);
        //    }
        //}

        return highlighterTexture;
    }

    public int ChangeProgressMode(bool mode) {

        Texture2D highlighterTexture = InitHighlighter(mode);

        if (mode) {
            highlighterTexture.SetPixel(0, currentStep, highlightedColor);
            highlightedPixel.x = 0;
            highlightedPixel.y = currentStep;

            currentStep *= resolution.x;
            columnHighlighter.gameObject.SetActive(true);
            int pixelSize = (int)rT.rect.height / resolution.y;
            columnHighlighter.sizeDelta = new Vector2(pixelSize, rT.rect.height);
            columnHighlighter.localPosition = new Vector3(0, 0, 0);
        }
        else {
            highlighterTexture.SetPixel(highlightedPixel.x, highlightedPixel.y, notHighlightedColor);
            currentStep /= resolution.x;
            highlighterTexture.SetPixel(0, currentStep, highlightedColor);
            columnHighlighter.gameObject.SetActive(false);
        }

        highlighterTexture.Apply();

        return currentStep;
    }

    public int NextLatchStep() {
        currentStep++;
        int lastStep = resolution.y - 1;
        if (columnHighlighter.gameObject.activeSelf) { //if tile by tile
            lastStep = resolution.x * resolution.y - 1;
        }
        
        if(currentStep > lastStep) {
            LatchFinished();
            return 0;
        }

        Texture2D highlighterTexture = (Texture2D)highlighter.GetComponent<RawImage>().texture;

        if (columnHighlighter.gameObject.activeSelf) { //if tile by tile
            highlighterTexture.SetPixel(highlightedPixel.x, highlightedPixel.y, notHighlightedColor);
            int columnIndex = currentStep % resolution.x;
            int rowIndex = currentStep / resolution.x;
            highlighterTexture.SetPixel(columnIndex, rowIndex, highlightedColor);
            highlightedPixel.x = columnIndex;
            highlightedPixel.y = rowIndex;

            int oldRowIndex = (currentStep - 1) / resolution.x;
            if (oldRowIndex < rowIndex) {
                ShiftCanvas(-1);
            }
        }
        else {
            highlighterTexture.SetPixel(0, currentStep - 1, notHighlightedColor);
            highlighterTexture.SetPixel(0, currentStep, highlightedColor);
            ShiftCanvas(-1);
        }

        highlighterTexture.Apply();

        UpdateProgressUI();
        return currentStep;
    }

    public int PrevLatchStep() {
        currentStep--;
        if(currentStep < 0) {
            currentStep = 0;
            return currentStep;
        }

        Texture2D highlighterTexture = (Texture2D)highlighter.GetComponent<RawImage>().texture;

        if (columnHighlighter.gameObject.activeSelf) { //if tile by tile
            highlighterTexture.SetPixel(highlightedPixel.x, highlightedPixel.y, notHighlightedColor);
            int columnIndex = currentStep % resolution.x;
            int rowIndex = currentStep / resolution.x;
            highlighterTexture.SetPixel(columnIndex, rowIndex, highlightedColor);
            highlightedPixel.x = columnIndex;
            highlightedPixel.y = rowIndex;

            int oldRowIndex = (currentStep + 1) / resolution.x;
            if(oldRowIndex > rowIndex) {
                ShiftCanvas(1);
            }
        }
        else {
            highlighterTexture.SetPixel(0, currentStep + 1, notHighlightedColor);
            highlighterTexture.SetPixel(0, currentStep, highlightedColor);
            ShiftCanvas(1);
        }

        highlighterTexture.Apply();

        UpdateProgressUI();
        return currentStep;
    }

    void UpdateProgressUI() {
        if (columnHighlighter.gameObject.activeSelf) { //if tile by tile
            int columnIndex = currentStep % resolution.x;
            HighlightTile(columnIndex);
            if (columnIndex == 0 || columnIndex == resolution.x - 1) {
                int rowIndex = currentStep / resolution.x;
                HighlightRow(rowIndex);
            }
        }
        else {
            HighlightRow(currentStep);
        }
    }

    public void HighlightRow(int rowIndex) {
        float pixelSize = rT.rect.height / resolution.y;
        rowHighlighter.localPosition = new Vector3(0, pixelSize * rowIndex, 0);
    }

    public void HighlightTile(int columnIndex) {
        float pixelSize = rT.rect.width / resolution.x;
        columnHighlighter.transform.localPosition = new Vector3(pixelSize * columnIndex, 0, 0);
    }

    void LatchFinished() {
        currentStep = 0;
        StopLatch();
        latchFinished?.Invoke();
    }

    public void StopLatch() {
        latchMode = false;
        paintInterface.gameObject.SetActive(true);
        rowHighlighter.gameObject.SetActive(false);
        columnHighlighter.gameObject.SetActive(false);
        highlighter.gameObject.SetActive(false);
    }

    void ShiftCanvas(int direction) {
        float distance = scrollableArea.localScale.x * (rT.rect.width / resolution.x);
        scrollableArea.Translate(new Vector3(0, direction * distance, 0));
    }
}

public enum CanvasMode {
    paintMode,
    dragMode,
    fillMode
}
