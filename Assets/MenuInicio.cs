using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuInicio : MonoBehaviour
{
    void Start()
    {
        var lienzo = new GameObject("MenuCanvas");
        var canvas = lienzo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var escala = lienzo.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        lienzo.AddComponent<GraphicRaycaster>();

        var objeto = new GameObject("Texto");
        objeto.transform.SetParent(lienzo.transform, false);
        var texto = objeto.AddComponent<Text>();
        Font fuente = Resources.Load<Font>("Tiny5-Regular");
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.font = fuente;
        texto.text = "toca cualquier boton para comenzar";
        texto.fontSize = 52;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        texto.verticalOverflow = VerticalWrapMode.Overflow;

        ColocarDebajoDelLogo(texto);
    }

    void ColocarDebajoDelLogo(Text texto)
    {
        var rect = texto.rectTransform;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        texto.alignment = TextAnchor.UpperCenter;

        SpriteRenderer fondo = null;
        float area = 0f;
        foreach (SpriteRenderer sr in FindObjectsOfType<SpriteRenderer>())
        {
            float a = sr.bounds.size.x * sr.bounds.size.y;
            if (a > area)
            {
                area = a;
                fondo = sr;
            }
        }

        if (fondo == null || Camera.main == null)
        {
            rect.anchorMin = new Vector2(0.08f, 0.12f);
            rect.anchorMax = new Vector2(0.92f, 0.30f);
            return;
        }

        Bounds limites = fondo.bounds;
        // El logo ocupa la parte alta del cuadro; el texto arranca justo debajo.
        float yMundo = Mathf.Lerp(limites.max.y, limites.min.y, 0.66f);
        Vector3 pantalla = Camera.main.WorldToViewportPoint(new Vector3(limites.center.x, yMundo, limites.center.z));
        float y = Mathf.Clamp(pantalla.y, 0.16f, 0.7f);
        rect.anchorMin = new Vector2(0.08f, y - 0.12f);
        rect.anchorMax = new Vector2(0.92f, y);
    }

    void Update()
    {
        if (Input.anyKeyDown)
            SceneManager.LoadScene("SampleScene");
    }
}
