using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChromaticTrials
{
    public class Speen : MonoBehaviour
    {
        public RectTransform rt;
        private bool smalling;
        private float spinspeed = 0.005f;
        public void FixedUpdate()
        {
            if (rt != null)
            {
                rt.Rotate(0f, 0f, 0.4f);

                // scale size
                if (smalling)
                {
                    rt.anchorMin = new Vector2(rt.anchorMin.x + spinspeed, rt.anchorMin.y + spinspeed);
                    rt.anchorMax = new Vector2(rt.anchorMax.x - spinspeed, rt.anchorMax.y - spinspeed);
                    if (rt.anchorMin.x >= -.65f) smalling = false;
                }
                else
                {
                    rt.anchorMin = new Vector2(rt.anchorMin.x - spinspeed, rt.anchorMin.y - spinspeed);
                    rt.anchorMax = new Vector2(rt.anchorMax.x + spinspeed, rt.anchorMax.y + spinspeed);
                    if (rt.anchorMin.x <= -1f) smalling = true;
                }
            }
        }
    }
}
