using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PaintInterface : MonoBehaviour, IPointerClickHandler, IDragHandler{

    public static Action<PointerEventData> paintedPoint;

    RectTransform rT;

    public bool fillMode;

    void Awake() {
        rT = GetComponent<RectTransform>();
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Left) {
            UpdatePixel(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData) {
        if(fillMode) {
            return;
        }
        if (eventData.button == PointerEventData.InputButton.Left) {
            UpdatePixel(eventData);
        }
    }

    void UpdatePixel(PointerEventData eventData) {
        //Vector3 pos = rT.InverseTransformPoint(eventData.position);

        //pos.x = Mathf.Clamp(pos.x, 0, rT.sizeDelta.x - 0.1f);
        //pos.y = Mathf.Clamp(pos.y, 0, rT.sizeDelta.y - 0.1f);

        paintedPoint?.Invoke(eventData);

        //int pixelX = (int)(pos.x / rT.sizeDelta.x * resolution.x);
        //int pixelY = (int)(pos.y / rT.sizeDelta.y * resolution.y);

        //texture.SetPixel(pixelX, pixelY, currentColor);
        //texture.Apply();
    }
}
