using UnityEngine;
using UnityEngine.SceneManagement;

// Pausa con la tecla del menu. Muestra el mismo menu de inicio.
public class PausaJuego : MonoBehaviour
{
    public static bool EstaPausado { get; private set; }

    bool pausado;
    float timeScaleAntes = 1f;
    int framePausa;
    MenuInicio menu;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCrear()
    {
        if (FindObjectOfType<PausaJuego>() != null) return;
        GameObject go = new GameObject("PausaJuego");
        go.AddComponent<PausaJuego>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            if (pausado) Reanudar();
            return;
        }

        if (!pausado && HopeControles.Presionada(HopeControles.Accion.Pausa))
            Pausar();
    }

    void LateUpdate()
    {
        if (!pausado || Time.frameCount == framePausa) return;
        if (menu != null && menu.ConsumioPausaEsteFrame) return;
        if (HopeControles.Presionada(HopeControles.Accion.Pausa))
            Reanudar();
    }

    void Pausar()
    {
        if (pausado) return;
        pausado = true;
        EstaPausado = true;
        framePausa = Time.frameCount;
        timeScaleAntes = Time.timeScale > 0.01f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        ActivarBlur(true);
        AsegurarMenu();
        if (menu != null) menu.Abrir();
    }

    void Reanudar()
    {
        if (!pausado) return;
        pausado = false;
        EstaPausado = false;
        Time.timeScale = timeScaleAntes > 0.01f ? timeScaleAntes : 1f;
        ActivarBlur(false);
        if (menu != null) menu.Ocultar();
    }

    void SalirAlMenu()
    {
        pausado = false;
        EstaPausado = false;
        Time.timeScale = 1f;
        ActivarBlur(false);
        if (menu != null)
        {
            Destroy(menu.gameObject);
            menu = null;
        }
        SceneManager.LoadScene("Menu");
    }

    void AsegurarMenu()
    {
        if (menu != null) return;
        GameObject go = new GameObject("MenuPausa");
        go.transform.SetParent(transform, false);
        MenuInicio.MarcarCreacionPausa();
        menu = go.AddComponent<MenuInicio>();
        menu.DefinirPausa(Reanudar, SalirAlMenu);
    }

    void ActivarBlur(bool activo)
    {
        if (!activo)
        {
            if (blur != null) blur.enabled = false;
            return;
        }
        Camera cam = Camera.main;
        if (cam == null) return;
        if (blur == null || blur.gameObject != cam.gameObject)
            blur = cam.GetComponent<HopeBlurPausa>();
        if (blur == null)
            blur = cam.gameObject.AddComponent<HopeBlurPausa>();
        blur.enabled = true;
    }

    void OnDestroy()
    {
        if (pausado) Time.timeScale = 1f;
        if (EstaPausado) EstaPausado = false;
        ActivarBlur(false);
    }

    HopeBlurPausa blur;
}

public class HopeBlurPausa : MonoBehaviour
{
    Material material;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!PausaJuego.EstaPausado)
        {
            Graphics.Blit(src, dest);
            return;
        }
        if (material == null)
        {
            Shader shader = Resources.Load<Shader>("HopeBlur");
            if (shader != null) material = new Material(shader);
        }
        if (material == null)
        {
            Graphics.Blit(src, dest);
            return;
        }
        int ancho = Mathf.Max(16, src.width / 2);
        int alto = Mathf.Max(16, src.height / 2);
        RenderTexture tmp = RenderTexture.GetTemporary(ancho, alto, 0);
        material.SetFloat("_Fuerza", 3f);
        Graphics.Blit(src, tmp, material);
        material.SetFloat("_Fuerza", 5f);
        Graphics.Blit(tmp, dest, material);
        RenderTexture.ReleaseTemporary(tmp);
    }

    void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
