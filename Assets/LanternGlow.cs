using UnityEngine;

// Va en el objeto "Oscuridad" (el que tiene la mascara negra).
// Hace latir apenas el charco de luz, como una llama real.
public class LanternGlow : MonoBehaviour
{
    [Header("Tamano del halo")]
    public float escalaBase = 1f;

    [Header("Parpadeo")]
    public float amplitud = 0.05f;   // 0.05 = varia un 5%
    public float velocidad = 4f;     // que tan rapido tiembla

    [Header("Apagar / prender")]
    public bool encendida = true;
    public float escalaApagada = 0.15f;
    public float suavidad = 4f;

    private float seed;
    private float escalaActual;

    void Start()
    {
        seed = Random.value * 100f;
        escalaActual = encendida ? escalaBase : escalaApagada;
    }

    void Update()
    {
        float objetivo = encendida ? escalaBase : escalaApagada;
        escalaActual = Mathf.Lerp(escalaActual, objetivo, Time.deltaTime * suavidad);

        float ruido = Mathf.PerlinNoise(seed, Time.time * velocidad); // 0..1
        float factor = 1f + (ruido - 0.5f) * 2f * amplitud;
        if (!encendida) factor = 1f;

        float s = escalaActual * factor;
        transform.localScale = new Vector3(s, s, 1f);
    }

    // Podes llamar a esto desde otro script, o probarlo con la tecla L.
    public void Alternar()
    {
        encendida = !encendida;
    }

    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.L)) Alternar();
    }
}