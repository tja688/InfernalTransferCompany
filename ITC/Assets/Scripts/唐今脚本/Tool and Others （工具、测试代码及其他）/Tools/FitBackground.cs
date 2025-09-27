using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FitBackground : MonoBehaviour
{
    public SpriteRenderer background;

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        float screenRatio = (float)Screen.width / Screen.height;
        float targetRatio = background.bounds.size.x / background.bounds.size.y;

        if (screenRatio >= targetRatio)
        {
            cam.orthographicSize = background.bounds.size.y / 2;
        }
        else
        {
            float differenceInSize = targetRatio / screenRatio;
            cam.orthographicSize = background.bounds.size.y / 2 * differenceInSize;
        }
    }
}