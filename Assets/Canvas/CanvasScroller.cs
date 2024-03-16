using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CanvasScroller : ScrollRect{

    public CanvasMode canvasMode;

    public override void OnDrag(PointerEventData eventData) {
        if (canvasMode != CanvasMode.dragMode) {
            return;
        }
        base.OnDrag(eventData);
    }

}
