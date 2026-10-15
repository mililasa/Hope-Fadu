using UnityEngine;

// Va en el objeto "Oscuridad" (el que tiene la mascara negra).
// Hace latir apenas el charco de luz, como una llama real.
public class LanternGlow : MonoBehaviour
{
    [Header("Tamano del halo")]
    [Tooltip("Activado: usa la escala que pusiste en el Transform (la Oscuridad de Hope). Desactivado: usa Escala Base (los faroles).")]
    public bool respetarEscalaDelTransform = true;
    public float escalaBase = 1f;
    [Tooltip("Radio del circulo de luz del centro, como parte del sprite. Es lo que se junta con los otros halos y alumbra el escenario. En Oscuridad_Linterna el dibujo termina en ~0.22.")]
    [Range(0.05f, 1.2f)]
    public float radioDelHalo = 0.22f;
    [Tooltip("Solo faroles: cuanto oscurece su propia mascara. 0 = el farol solo da luz y no tapa el halo de Hope.")]
    [Range(0f, 1f)]
    public float opacidadOscuridad = 0f;
    [Tooltip("Solo faroles: hasta donde llega su oscuridad, en veces el radio del halo. Mas alla se desvanece y deja ver la oscuridad normal de Hope.")]
    [Range(1.05f, 4f)]
    public float alcanceOscuridad = 1.8f;

    [Header("Parpadeo")]
    public float amplitud = 0.05f;   // 0.05 = varia un 5%
    public float velocidad = 4f;     // que tan rapido tiembla

    [Header("Apagar / prender")]
    public bool encendida = true;
    public float escalaApagada = 0.15f;
    public float suavidad = 4f;

    private float seed;
    private float escalaActual;
    private Vector3 escalaDelTransform;

    void Awake()
    {
        escalaDelTransform = transform.localScale;
    }

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

        if (respetarEscalaDelTransform)
        {
            float relativo = escalaActual / Mathf.Max(0.0001f, escalaBase);
            Vector3 e = escalaDelTransform * (relativo * factor);
            transform.localScale = new Vector3(e.x, e.y, escalaDelTransform.z);
            return;
        }

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
