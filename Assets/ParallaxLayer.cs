using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)]
    public float parallaxFactorX = 0.5f;
    [Range(0f, 1f)]
    public float parallaxFactorY = 0f; // normalmente 0 en un side-scroller

    private Transform cam;
    private Vector3 lastCamPos;

    void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;
    }

    void LateUpdate()
    {
        Vector3 delta = cam.position - lastCamPos;
        transform.position += new Vector3(delta.x * parallaxFactorX, delta.y * parallaxFactorY, 0f);
        lastCamPos = cam.position;
    }
}