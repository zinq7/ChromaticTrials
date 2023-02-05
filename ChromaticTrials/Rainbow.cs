using UnityEngine;

namespace ChromaticTrials
{
    public class Rainbow : MonoBehaviour
    {
        public Material[] mat;

        public void FixedUpdate()
        {
            if (mat != null)
            {
                foreach (Material m in mat)
                {
                    m.color = Color.HSVToRGB(Mathf.PingPong(Time.time * 0.3f, 1), 1, 1);
                }
            }
        }
    }
}
