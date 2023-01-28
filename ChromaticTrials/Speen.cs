using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChromaticTrials
{
    public class EpicScrollRect : ScrollRect
    {
        private const int scrollSpeed = 20;
        public override void Awake()
        {
            content = gameObject.transform.Find("Content").gameObject.GetComponent<RectTransform>(); // get the rect transform automatically
            scrollSensitivity = scrollSpeed;
            viewport = gameObject.GetComponent<RectTransform>();
            horizontal = false;
            gameObject.AddComponent<RectMask2D>(); // add rect mask 2d.
            inertia = true;
        }

        public override void OnBeginDrag(PointerEventData eventData) { }
        public override void OnDrag(PointerEventData eventData) { }
        public override void OnEndDrag(PointerEventData eventData) { }
    }
}
