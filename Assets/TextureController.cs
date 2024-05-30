using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ActionHistory;

public class TextureController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler{

    public static Action textureUpdated;
    public static Action latchFinished;

    Texture2D texture;
    Color currentColor = Color.white;

    float tileSize = 20;
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
    [SerializeField] Button undoButton;
    [SerializeField] Button redoButton;

    [SerializeField] RectTransform rowHighlighter;
    [SerializeField] RectTransform columnHighlighter;
    [SerializeField] RawImage highlighter;
    Vector2Int highlightedPixel = Vector2Int.zero;
    Color highlightedColor = new Color(0, 0, 0, 0);
    Color notHighlightedColor = new Color(0, 0, 0, .7f);
    int currentStep = 0;

    int topAndLeftPadding = 30;

    RectTransform rT;

    [SerializeField] Texture2D defaultCursor;
    [SerializeField] Texture2D dragCursor;
    [SerializeField] Texture2D paintCursor;
    [SerializeField] Texture2D fillCursor;

    CanvasMode canvasMode;

    Image selectedTool;
    Texture2D currentCursor;

    ActionHistory history;

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

        undoButton.onClick.AddListener(Undo);
        redoButton.onClick.AddListener(Redo);

        selectedTool = paintButton.GetComponent<Image>();
        selectedTool.color -= new Color(0.2f, 0.2f, 0.2f, 0);

        currentCursor = paintCursor;

        history = new ActionHistory();
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
        ColorHistoryController.currentColorUpdated += ColorSelected;
        PaintInterface.paintedPoint += UpdatePixel;
    }

    private void OnDisable() {
        ColorPicker.colorSelected -= ColorSelected;
        MenuController.newBlankProjectCreated -= NewBlankProject;
        MenuController.projectLoaded -= OnProjectLoaded;
        MenuController.photoUploadConfirmed -= OnTextureUpload;
        PaletController.colorSelected -= ColorSelected;
        ColorHistoryController.currentColorUpdated -= ColorSelected;
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

        if (Input.GetKey(KeyCode.LeftShift)) {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z)) {
                Redo();
            }
        }
        else {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z)) {
                Undo();
            }
        }

    }

    void Undo() {
        if(history.AtBottom()) {
            return;
        }
        HistoryAction action = history.GetCurrentAction();
        foreach(Vector2Int pixel in action.pixels) {
            texture.SetPixel(pixel.x, pixel.y, action.fromColor);
        }
        texture.Apply();

        history.Undo();
    }

    void Redo() {
        if (history.AtTop()) {
            return;
        }
        HistoryAction action = history.GetNextAction();
        foreach (Vector2Int pixel in action.pixels) {
            texture.SetPixel(pixel.x, pixel.y, action.toColor);
        }
        texture.Apply();

        history.Redo();
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
        //paintButton.GetComponent<Image>().color = currentColor;
    }

    public void SetResolution(int width, int height) {
        resolution.x = width;
        resolution.y = height;
    }

    public void SetColor(Color color) {
        currentColor = color;
        //paintButton.GetComponent<Image>().color = currentColor;
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
            //columnNumbers.GetChild(i).GetComponent<TMP_Text>().fontSize = fontSize;
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
            //rowNumbers.GetChild(i).GetComponent<TMP_Text>().fontSize = fontSize;
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
        rT.sizeDelta = new Vector2(resolution.x * tileSize, resolution.y * tileSize);
        scrollableArea.GetComponent<RectTransform>().sizeDelta = new Vector2(resolution.x * tileSize + 50, resolution.y * tileSize + 50);

    }

    public void ToggleGrid(bool grid) {
        verticalLines.gameObject.SetActive(grid);
        horizontalLines.gameObject.SetActive(grid);
    }

    public void OnClearButton() {
        for (int x = 0; x < resolution.x; x++) {
            for (int y = 0; y < resolution.y; y++) {
                texture.SetPixel(x, y, currentColor);
            }
        }

        texture.Apply();
    }

    void OnPaintButton() {
        if(canvasMode == CanvasMode.paintMode) {
            return;
        }
        canvasMode = CanvasMode.paintMode;
        UpdateSelectedButtonColors(selectedTool, paintButton.GetComponent<Image>());

        GetComponent<CanvasScroller>().canvasMode = canvasMode;
        paintInterface.gameObject.SetActive(true);
        paintInterface.fillMode = false;
        Cursor.SetCursor(paintCursor, new Vector2(0, 0), CursorMode.ForceSoftware);
        currentCursor = paintCursor;
    }

    void OnDragButton() {
        if (canvasMode == CanvasMode.dragMode) {
            return;
        }
        canvasMode = CanvasMode.dragMode;
        UpdateSelectedButtonColors(selectedTool, dragButton.GetComponent<Image>());

        GetComponent<CanvasScroller>().canvasMode = canvasMode;
        paintInterface.gameObject.SetActive(false);
        paintInterface.fillMode = false;
        currentCursor = dragCursor;
        Cursor.SetCursor(currentCursor, new Vector2(32, 32), CursorMode.ForceSoftware);
    }

    void OnFillToolButton() {
        if (canvasMode == CanvasMode.fillMode) {
            return;
        }
        canvasMode = CanvasMode.fillMode;
        UpdateSelectedButtonColors(selectedTool, fillToolButton.GetComponent<Image>());

        GetComponent<CanvasScroller>().canvasMode = canvasMode;
        paintInterface.gameObject.SetActive(true);
        paintInterface.fillMode = true;
        Cursor.SetCursor(fillCursor, Vector2.zero, CursorMode.ForceSoftware);
        currentCursor = fillCursor;
    }

    void UpdateSelectedButtonColors(Image oldSelection, Image newSelection) {
        oldSelection.color += new Color(0.2f, 0.2f, 0.2f, 0);
        selectedTool = newSelection;
        newSelection.color -= new Color(0.2f, 0.2f, 0.2f, 0);
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

    void UpdatePixel(PointerEventData eventData) {
        Vector3 pos = rT.InverseTransformPoint(eventData.position);

        pos.x = Mathf.Clamp(pos.x, 0, rT.sizeDelta.x - 0.1f);
        pos.y = Mathf.Clamp(pos.y, 0, rT.sizeDelta.y - 0.1f);

        int pixelX = (int)(pos.x / rT.sizeDelta.x * resolution.x);
        int pixelY = (int)(pos.y / rT.sizeDelta.y * resolution.y);

        Color oldColor = texture.GetPixel(pixelX, pixelY);
        if (Math.Round(oldColor.r, 2) == Math.Round(currentColor.r, 2)
            && Math.Round(oldColor.g, 2) == Math.Round(currentColor.g, 2)
            && Math.Round(oldColor.b, 2) == Math.Round(currentColor.b, 2)) {
            return;
        }

        if (canvasMode == CanvasMode.fillMode) {
            List<Vector2Int> pixels = FindPixels(pixelX, pixelY, oldColor);
            foreach (Vector2Int pixel in pixels) {
                texture.SetPixel(pixel.x, pixel.y, currentColor);
            }

            history.AddAction(pixels.ToArray(), oldColor, currentColor);
        }
        else {
            texture.SetPixel(pixelX, pixelY, currentColor);
            history.AddAction(new Vector2Int[] {new Vector2Int(pixelX, pixelY) }, oldColor, currentColor);
        }

        texture.Apply();
        textureUpdated?.Invoke();
    }

    List<Vector2Int> FindPixels(int posX, int posY, Color color) {

        int[][] checkedPixels = new int[resolution.x][];
        for (int x = 0; x < resolution.x; x++) {
            checkedPixels[x] = new int[resolution.y];
        }

        List<Vector2Int> pixelsToCheck = new List<Vector2Int> { new Vector2Int(posX, posY) };
        List<Vector2Int> pixelsToReturn = new List<Vector2Int>();

        do {
            Vector2Int pixel = pixelsToCheck[0];
            pixelsToCheck.RemoveAt(0);
            pixelsToReturn.Add(pixel);

            if (pixel.x < resolution.x - 1) {
                if (checkedPixels[pixel.x + 1][pixel.y] == 0) { // if unchecked pixel
                    checkedPixels[pixel.x + 1][pixel.y] = 1;
                    if (texture.GetPixel(pixel.x + 1, pixel.y) == color) {
                        pixelsToCheck.Add(new Vector2Int(pixel.x + 1, pixel.y));
                    }
                }
            }

            if (pixel.y > 0) {
                if (checkedPixels[pixel.x][pixel.y - 1] == 0) { // if unchecked pixel
                    checkedPixels[pixel.x][pixel.y - 1] = 1;
                    if (texture.GetPixel(pixel.x, pixel.y - 1) == color) {
                        pixelsToCheck.Add(new Vector2Int(pixel.x, pixel.y - 1));
                    }
                }
            }

            if (pixel.x > 0) {
                if (checkedPixels[pixel.x - 1][pixel.y] == 0) { // if unchecked pixel
                    checkedPixels[pixel.x - 1][pixel.y] = 1;
                    if (texture.GetPixel(pixel.x - 1, pixel.y) == color) {
                        pixelsToCheck.Add(new Vector2Int(pixel.x - 1, pixel.y));
                    }
                }
            }

            if (pixel.y < resolution.y - 1) {
                if (checkedPixels[pixel.x][pixel.y + 1] == 0) { // if unchecked pixel
                    checkedPixels[pixel.x][pixel.y + 1] = 1;
                    if (texture.GetPixel(pixel.x, pixel.y + 1) == color) {
                        pixelsToCheck.Add(new Vector2Int(pixel.x, pixel.y + 1));
                    }
                }
            }
        } while (pixelsToCheck.Count > 0);

        return pixelsToReturn;

    }

    public Texture2D GetImage() {
        return texture;
    }

    public void ContinueLatch(int step, bool mode) {
        currentStep = step;
        int pixelSize = (int)rT.rect.height / resolution.y;
        int rowIndex = step;
        Texture2D highlighterTexture = InitHighlighter(mode);
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
        if(canvasMode != CanvasMode.dragMode) {
            paintInterface.gameObject.SetActive(true);
        }

        rowHighlighter.gameObject.SetActive(false);
        columnHighlighter.gameObject.SetActive(false);
        highlighter.gameObject.SetActive(false);
    }

    void ShiftCanvas(int direction) {
        float distance = scrollableArea.localScale.x * (rT.rect.width / resolution.x);
        scrollableArea.Translate(new Vector3(0, direction * distance, 0));
    }

    public void OnPointerEnter(PointerEventData eventData) {
        Cursor.SetCursor(currentCursor, Vector2.zero, CursorMode.ForceSoftware);
    }

    public void OnPointerExit(PointerEventData eventData) {
        Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.ForceSoftware);
    }
}

public enum CanvasMode {
    paintMode,
    dragMode,
    fillMode
}

public class ActionHistory {
    public struct HistoryAction {
        public Vector2Int[] pixels;
        public Color fromColor, toColor;

        public HistoryAction(Vector2Int[] pixels, Color from, Color to) {
            this.pixels = pixels;
            fromColor = from;
            toColor = to;
        }
    }

    List<HistoryAction> history = new List<HistoryAction>();
    int currentAction = -1;

    public void AddAction(Vector2Int[] pixels, Color fromColor, Color toColor) {
        //if current action is not the last action in history
        for(int i = history.Count - 1; i > currentAction; i--) {
            history.RemoveAt(i);
        }

        if(history.Count > 50) {
            history.RemoveAt(0);
        }
        else {
            currentAction++;
        }

        history.Add(new HistoryAction(pixels, fromColor, toColor));
    }

    public HistoryAction GetCurrentAction() {
        return history[currentAction];
    }

    public HistoryAction GetNextAction() {
        return history[currentAction + 1];
    }

    public bool AtBottom() {
        return currentAction == -1;
    }

    public bool AtTop() {
        return currentAction == history.Count - 1;
    }

    public void Undo() {
        currentAction--;
    }

    public void Redo() {
        if (currentAction < history.Count - 1) {
            currentAction++;
        }
    }
}
