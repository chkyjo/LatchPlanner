using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorWindow : MonoBehaviour, IDragHandler, IPointerClickHandler{

    public static Action<float, float> satBrightnessSelected;

    RectTransform rT;

    [SerializeField] Image cursorImage;
    [SerializeField] Transform cursor;

    private void Awake() {
        rT = GetComponent<RectTransform>();
    }

    public void OnDrag(PointerEventData eventData) {
        UpdateColor(eventData);
    }

    public void OnPointerClick(PointerEventData eventData) {
        UpdateColor(eventData);
    }

    void UpdateColor(PointerEventData eventData) {

        Vector3 pos = rT.InverseTransformPoint(eventData.position);

        float deltaX = rT.sizeDelta.x * 0.5f;
        float deltaY = rT.sizeDelta.y * 0.5f;

        pos.x = Mathf.Clamp(pos.x, -deltaX, deltaX);
        pos.y = Mathf.Clamp(pos.y, -deltaY, deltaY);

        float x = pos.x + deltaX;
        float y = pos.y + deltaY;

        float xNorm = x / rT.sizeDelta.x;
        float yNorm = y / rT.sizeDelta.y;

        cursor.localPosition = pos;
        cursorImage.color = Color.HSVToRGB(0, 0, 1 - yNorm);

        satBrightnessSelected?.Invoke(xNorm, yNorm);
    }
}
