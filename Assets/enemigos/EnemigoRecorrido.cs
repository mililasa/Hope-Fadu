using UnityEngine;

// Ida y vuelta entre A y B. Si ve a Hope dentro de ese tramo, corre
// hacia ella sin salir de A y B. Solo la mata si la agarra.
// En el barril (parte 1) o fuera del tramo, no puede tocarla.
[RequireComponent(typeof(SpriteRenderer))]
public class EnemigoRecorrido : MonoBehaviour
{
    public Transform[] puntos;
    public float velocidad = 1.5f;
    public float velocidadCorrer = 3.2f;
    [Tooltip("Qué tan cerca tiene que estar de Hope para verla.")]
    public float rangoVista = 3.5f;
    [Tooltip("Distancia a la que la agarra y la manda al checkpoint.")]
    public float radioAgarre = 0.55f;
    [Tooltip("Baja el centro del agarre. Un valor negativo lo acerca al piso.")]
    public float desfaseAgarreY = 0f;
    public float alturaMirada = 1.25f;
    [Tooltip("El dibujo de Hope_Enemigo mira a la derecha si está destildado.")]
    public bool spriteMiraALaIzquierda = false;
    public LayerMask piso;
    public float graciaTrasCaptura = 1.4f;

    int indice;
    float gracia;
    bool persiguiendo;
    public bool PersiguiendoParaAudio { get { return persiguiendo; } }
    public Vector2 PosicionSonora { get { return CentroAgarre(); } }
    SpriteRenderer sr;
    PlayerController2D hope;
    BarrilesInteraccion barriles;
    Transform cajaPiso;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Piso";
            sr.sortingOrder = 3;
        }

        GameObject jugador = GameObject.Find("Pp");
        if (jugador != null)
            hope = jugador.GetComponent<PlayerController2D>();
        barriles = FindObjectOfType<BarrilesInteraccion>();

        if (piso.value == 0)
            piso = LayerMask.GetMask("Piso");

        GameObject caja = GameObject.Find("PisoBox");
        if (caja != null)
            cajaPiso = caja.transform;

        if (puntos == null || puntos.Length == 0)
            JuntarPuntosHijos();
        FijarPuntosEnElMundo();
        indice = 0;
        ApoyarEnElPiso();
        HopeEnemyAudio.Conectar(this);
    }

    void FijarPuntosEnElMundo()
    {
        if (puntos == null) return;
        for (int i = 0; i < puntos.Length; i++)
        {
            if (puntos[i] == null) continue;
            if (puntos[i].IsChildOf(transform))
                puntos[i].SetParent(null, true);
        }
    }

    void Update()
    {
        if (gracia > 0f)
            gracia -= Time.deltaTime;

        persiguiendo = VeAHope();
        if (persiguiendo)
            Perseguir();
        else
            Recorrer();

        if (gracia <= 0f && PuedeAgarrar())
            Capturar();
    }

    void Recorrer()
    {
        if (!HayPunto()) return;
        MoverEnX(puntos[indice].position.x, velocidad);
        MirarHacia(puntos[indice].position.x - transform.position.x);
        if (Mathf.Abs(transform.position.x - puntos[indice].position.x) > 0.02f) return;
        indice++;
        if (indice >= puntos.Length) indice = 0;
    }

    void Perseguir()
    {
        float minX, maxX;
        Limites(out minX, out maxX);
        float objetivo = Mathf.Clamp(hope.transform.position.x, minX, maxX);
        MoverEnX(objetivo, velocidadCorrer);
        MirarHacia(hope.transform.position.x - transform.position.x);
    }

    void MoverEnX(float xDestino, float rapidez)
    {
        Vector3 actual = transform.position;
        float paso = rapidez * Time.deltaTime;
        float dx = xDestino - actual.x;
        if (Mathf.Abs(dx) <= paso)
            actual.x = xDestino;
        else
            actual.x += Mathf.Sign(dx) * paso;

        float minX, maxX;
        Limites(out minX, out maxX);
        actual.x = Mathf.Clamp(actual.x, minX, maxX);
        transform.position = actual;
        ApoyarEnElPiso();
    }

    void ApoyarEnElPiso()
    {
        if (sr == null || sr.sprite == null) return;
        float pies = transform.position.y - sr.bounds.min.y;
        if (pies < 0.05f) pies = 0.05f;
        Vector2 origen = new Vector2(transform.position.x, sr.bounds.max.y + 0.5f);
        float largo = sr.bounds.size.y + 8f;
        RaycastHit2D[] golpes = Physics2D.RaycastAll(origen, Vector2.down, largo, piso);
        float mejor = float.MaxValue;
        bool hay = false;
        Vector2 punto = origen;
        for (int i = 0; i < golpes.Length; i++)
        {
            if (!EsSuelo(golpes[i])) continue;
            if (golpes[i].distance >= mejor) continue;
            mejor = golpes[i].distance;
            punto = golpes[i].point;
            hay = true;
        }
        if (!hay) return;

        Vector3 p = transform.position;
        p.y = punto.y + pies;
        transform.position = p;
    }

    bool EsSuelo(RaycastHit2D golpe)
    {
        if (golpe.collider == null || golpe.collider.isTrigger) return false;
        if (golpe.normal.y < 0.2f) return false;
        if (golpe.collider.transform == transform || golpe.collider.transform.IsChildOf(transform))
            return false;
        if (EsPlataforma(golpe.collider.transform))
            return false;
        if (cajaPiso != null)
            return golpe.collider.transform == cajaPiso || golpe.collider.transform.IsChildOf(cajaPiso);
        return golpe.collider.name.StartsWith("Piso", System.StringComparison.OrdinalIgnoreCase);
    }

    static bool EsPlataforma(Transform t)
    {
        while (t != null)
        {
            if (t.name.IndexOf("Plataforma", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            t = t.parent;
        }
        return false;
    }

    bool VeAHope()
    {
        if (hope == null || Time.timeScale < 0.01f) return false;
        if (barriles == null) barriles = FindObjectOfType<BarrilesInteraccion>();
        if (barriles != null && barriles.HopeEscondidaEnBarril) return false;
        if (!DentroDelTramo(hope.transform.position.x)) return false;
        return DistanciaAHope(false) <= Mathf.Max(0.1f, rangoVista);
    }

    bool PuedeAgarrar()
    {
        if (hope == null) return false;
        if (barriles != null && barriles.HopeEscondidaEnBarril) return false;
        if (!DentroDelTramo(hope.transform.position.x)) return false;
        return DistanciaAHope(true) <= Mathf.Max(0.1f, radioAgarre);
    }

    // El PNG nuevo es 128x128 con mucho vacío: el recuadro del sprite no es el cuerpo.
    float DistanciaAHope(bool alCuerpo)
    {
        Vector2 centro = CentroAgarre();
        Collider2D cuerpo = hope.GetComponent<Collider2D>();
        Vector2 punto = cuerpo != null ? (Vector2)cuerpo.ClosestPoint(centro) : (Vector2)hope.transform.position;
        if (!alCuerpo && cuerpo != null)
            punto = cuerpo.bounds.center;
        return Vector2.Distance(centro, punto);
    }

    Vector2 CentroAgarre()
    {
        if (sr != null && sr.sprite != null)
        {
            Bounds b = sr.bounds;
            float alto = Mathf.Clamp(b.size.y * 0.22f, 0.45f, 1.0f);
            return new Vector2(b.center.x, b.min.y + alto);
        }
        return (Vector2)transform.position + Vector2.up * desfaseAgarreY;
    }

    bool DentroDelTramo(float x)
    {
        float minX, maxX;
        if (!Limites(out minX, out maxX)) return true;
        return x >= minX && x <= maxX;
    }

    bool Limites(out float minX, out float maxX)
    {
        minX = maxX = transform.position.x;
        if (puntos == null || puntos.Length == 0) return false;
        bool hay = false;
        for (int i = 0; i < puntos.Length; i++)
        {
            if (puntos[i] == null) continue;
            float x = puntos[i].position.x;
            if (!hay)
            {
                minX = maxX = x;
                hay = true;
            }
            else
            {
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }
        }
        return hay;
    }

    void MirarHacia(float dx)
    {
        if (sr == null || Mathf.Abs(dx) < 0.02f) return;
        bool haciaDerecha = dx > 0f;
        sr.flipX = spriteMiraALaIzquierda ? haciaDerecha : !haciaDerecha;
    }

    bool HayPunto()
    {
        return puntos != null && puntos.Length > 0 && indice >= 0 && indice < puntos.Length && puntos[indice] != null;
    }

    void Capturar()
    {
        gracia = graciaTrasCaptura;
        persiguiendo = false;
        bool nivelDos = gameObject.scene.name == "Nivel_2";
        hope.SonarMuerte(nivelDos);
        if (nivelDos)
        {
            hope.LockMovement(.12f);
            StartCoroutine(VolverTrasCapturaSonora());
        }
        else hope.VolverAlCheckPoint();
    }

    System.Collections.IEnumerator VolverTrasCapturaSonora()
    {
        yield return new WaitForSeconds(.12f);
        if (hope != null) hope.VolverAlCheckPoint();
    }

    void JuntarPuntosHijos()
    {
        int n = transform.childCount;
        if (n == 0) return;
        puntos = new Transform[n];
        for (int i = 0; i < n; i++)
            puntos[i] = transform.GetChild(i);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, rangoVista);
        Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(CentroAgarre(), radioAgarre);

        float minX, maxX;
        if (!Limites(out minX, out maxX)) return;
        Vector3 a = new Vector3(minX, transform.position.y, transform.position.z);
        Vector3 b = new Vector3(maxX, transform.position.y, transform.position.z);
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.9f);
        Gizmos.DrawLine(a, b);
    }
}
