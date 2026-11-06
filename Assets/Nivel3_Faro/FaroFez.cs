using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================================
// FARO FEZ (nivel 3)
// El faro tiene 4 paredes y cada una tiene dos "paneles": su cara de adentro y
// su cara de afuera. En pantalla se ve siempre UN panel y Hope camina en 2D
// sobre él, con su PlayerController2D de siempre.
//
//   - Al empujar contra el borde, el faro gira 90° y Hope sigue en el panel de
//     al lado (las repisas con flecha continúan a la vuelta de la esquina).
//   - En una puerta o ventana, con la tecla Interactuar (E, se cambia en el menú) Hope pasa al
//     otro lado de la misma pared.
//   - Las repisas de las paredes de costado no se ven ni se pisan.
//
// Cómo se ubica todo: el faro está armado adentro de "Pivote", que gira. La
// cámara nunca gira. "Vista" k = el pivote girado k*90 grados. En la vista k,
// la pared k (0 Norte, 1 Oeste, 2 Sur, 3 Este) queda al fondo si Hope está
// adentro, y la pared opuesta queda de frente si está afuera.
//
// El faro puede ser más angosto arriba (como tu dibujo): "Medio Ancho" es la
// mitad del ancho abajo y "Medio Ancho Arriba" la mitad del ancho en el techo.
// Los bordes donde se dobla siguen esa forma.
// ============================================================================
public class FaroFez : MonoBehaviour
{
    [Header("Medidas del faro (tienen que coincidir con lo construido)")]
    [Tooltip("Mitad del ancho del faro abajo, en el suelo.")]
    public float medioAncho = 4f;
    [Tooltip("Mitad del ancho del faro arriba, en el techo. Igual a Medio Ancho = faro recto.")]
    public float medioAnchoArriba = 4f;
    public float grosorMuro = 0.4f;
    [Tooltip("Cuánto salen las repisas de la pared. Afuera, Hope puede caminar hasta acá antes de doblar.")]
    public float salidaRepisa = 1f;
    [Tooltip("Altura del techo o del piso de la linterna (donde está el farol).")]
    public float alturaLinterna = 30f;
    [Tooltip("Cuánto sobresale el piso de arriba (afuera) más allá de la pared. 0 = solo el techo.")]
    public float salidaTecho = 1f;
    [Tooltip("Afuera se puede pisar el techo de la torre. El botón violeta lo destilda si hay linterna dibujada (n3_exterior_luz_faro): ahí se pisa su balcón.")]
    public bool pisarTechoAfuera = true;

    [Header("Pisos dibujados por vos (Edge Collider 2D)")]
    [Tooltip("Tildado: Hope pisa SOLO los Edge Collider 2D de 'Pisos (Edge Collider)' (uno por cara, afuera y adentro).\n" +
             "Las repisas automáticas y los pisos automáticos se apagan. Para crearlos: botón 'Crear pisos con Edge Collider'.")]
    public bool pisosConEdge = false;
    [Tooltip("Grosor que se le da a cada Edge Collider al dar Play (su Edge Radius). Un poco de grosor evita que Hope se caiga por las puntas de los escalones.")]
    public float grosorEdge = 0.04f;
    [Tooltip("Si Hope se ve flotando sobre el dibujo, subí este número: baja todos tus pisos dibujados esa cantidad (por ejemplo 0.05).")]
    public float ajustePies = 0f;
    [Tooltip("Al caminar, Hope se sube sola a escalones de hasta esta altura (así no se mete adentro de los escalones de las escaleras). 0 = apagado.")]
    public float alturaEscalon = 0.45f;

    [Header("Tamaño (para achicar a Hope respecto del faro)")]
    [Tooltip("1 = la puerta mide 1,5 Hopes. 1.5 = todo el faro 1,5 veces más grande (Hope se ve más chica).\n" +
             "Cambialo y tocá el botón violeta. (Si escalaste Faro_Nivel3, el botón violeta pasa esa escala acá solo.)")]
    public float tamano = 1f;
    [Tooltip("Qué tan alto salta Hope en este nivel. 0 = el salto de Hope multiplicado por Tamaño " +
             "(así los saltos del dibujo le siguen dando). Con Tamaño 1.5 necesita al menos 2.1.")]
    public float alturaSaltoEnFaro = 0f;

    [Header("Dibujo (dónde cae el faro en tus PNG)")]
    [Tooltip("Cuántos píxeles de tu dibujo son 1 unidad de Unity. Lo pone el botón violeta: 97 / Tamaño.")]
    public float pixelesPorUnidad = 97f;
    [Tooltip("Columna del centro del faro en tu PNG (en píxeles, desde la izquierda).")]
    public float centroDibujoX = 767.5f;
    [Tooltip("Fila donde apoya el faro en tu PNG (en píxeles, desde arriba).")]
    public float sueloDibujoY = 2309f;
    [Tooltip("Qué tan lejos, detrás del faro, va el fondo.")]
    public float distanciaFondo = 25f;
    [Tooltip("Tildado: el cielo (n3_fondo) también se ve cuando Hope está adentro.")]
    public bool cieloAdentro = false;

    [Header("Dónde empieza Hope")]
    public bool empiezaAfuera = true;
    [Range(0, 3)] public int vistaInicial = 0;

    [Header("Giros")]
    public float duracionGiro = 0.55f;
    public float duracionPuerta = 0.95f;
    [Tooltip("Si Hope empuja contra el borde, el faro gira solo.")]
    public bool girarAlLlegarAlBorde = true;
    [Tooltip("Segundos empujando contra el borde para que gire. Más = menos giros sin querer.")]
    public float segundosEmpujando = 0.15f;
    [Tooltip("Si está destildado, el faro solo gira con Hope parada (no en medio de un salto).")]
    public bool girarEnElAire = false;
    [Tooltip("Qué tan cerca de la puerta tiene que estar Hope para poder pasar.")]
    public float distanciaPuerta = 0.75f;
    [Tooltip("Tildado: al pasar por una puerta o ventana el faro NO da media vuelta; se funde a negro y Hope " +
             "queda en el mismo lugar de la pantalla (como en tus dibujos, donde la ventana de adentro y la de afuera están en el mismo lugar).")]
    public bool puertasDeFrente = false;
    [Tooltip("Segundos del fundido a negro al pasar por una puerta (con Puertas De Frente).")]
    public float duracionFundido = 0.6f;
    [Tooltip("Escribe en la Consola por qué el faro no gira cuando Hope empuja contra el borde (para encontrar problemas).")]
    public bool diagnostico = true;
    [HideInInspector] public int versionArmado;   // la pone el botón violeta; 4 = dibujos por cara + montones + linterna + Tamaño

    [Header("Cámara")]
    public float zoomAdentro = 4.5f;
    public float zoomAfuera = 5f;

    [Header("Profundidad en los giros")]
    [Tooltip("Al doblar una esquina ADENTRO, el rincón (que está lejos) se aleja: se achica hacia los pies de Hope, las repisas " +
             "forman una V y se oscurece. Las puntas de las paredes no se mueven (la torre no se abre). Sin esto, adentro y afuera giran igual.")]
    public bool profundidadDeCostado = true;
    [Tooltip("Lo mismo al doblar afuera (la esquina se acerca).")]
    public bool profundidadAfuera = false;
    [Tooltip("Destildado: el rincón se achica de costado y de alto, hacia los pies de Hope (las repisas forman una V; el piso donde está Hope queda derecho).\n" +
             "Tildado: solo se achica de costado.")]
    public bool profundidadSoloDeCostado = false;
    [Tooltip("Qué tan lejos se va el rincón en el giro de adentro. Menos = más lejos (más V). 1 = como la vista previa; 2 = suave.")]
    public float perspectivaRincon = 1f;
    [Tooltip("Destildado: en el giro de adentro las paredes NO se deforman; el rincón solo se oscurece (sombra).\n" +
             "Tildado: además el rincón se aleja (la V).")]
    public bool moverRincon = false;
    [Tooltip("Cuánto se oscurece el rincón durante el giro (0 = nada, 1 = negro).")]
    [Range(0f, 1f)] public float sombraRincon = 0.45f;

    [Header("Flechas de esquina")]
    public Material materialFlecha;
    public float tamanoFlecha = 0.9f;
    [Tooltip("Qué tan arriba de la repisa flota la flecha (Hope mide un poco más de 1).")]
    public float alturaFlecha = 1.9f;

    [Header("Referencias (las completa el constructor)")]
    public Transform pivote;
    public PlayerController2D hope;
    [Tooltip("El farol de arriba (tu Lantern). Cuando Hope lo prende, termina el nivel.")]
    public Lantern farolFinal;
    [Tooltip("Los haces de luz que giran al ganar.")]
    public GameObject hazDeLuz;

    [Header("Final del nivel")]
    public string escenaSiguiente = "Menu";
    public float segundosAntesDeSalir = 4f;
    public Color fondoAlGanar = new Color(0.37f, 0.23f, 0.53f);
    [Tooltip("Objetos que se prenden cuando Hope enciende el farol.")]
    public GameObject[] activarAlGanar;

    // ---------------- estado ----------------
    bool adentro;          // ¿Hope está adentro del faro?
    int vista;             // 0..3
    float angulo;          // giro actual del pivote, en grados
    bool animando, ganado;
    float tiempoDesdeGiro = 99f;
    Vector2 ultimaPos;

    class Repisa { public PiezaFaro pieza; public bool adentro; public int pared, vista; public float a, b, top, grosor; public BoxCollider2D col; }
    class Flecha { public Transform t; public Repisa r; public Vector3 baseLocal; }
    class Puerta { public PuertaFaro p; public int pared; public float u, y, yAfuera; }
    readonly List<Repisa> repisas = new List<Repisa>();
    readonly List<Flecha> flechas = new List<Flecha>();
    readonly List<Puerta> puertas = new List<Puerta>();
    PiezaFaro[] piezas;
    LaminaFaro[] laminas;
    PisoCara[] pisosCara = new PisoCara[0];
    bool usarEdge;          // pisosConEdge y hay grupos de pisos
    [Header("Láminas (los dibujos; las arma el botón violeta)")]
    [Tooltip("Cada dibujo del faro: qué es, de qué cara y qué objeto lo dibuja.")]
    public List<LaminaFaro> laminasDibujo = new List<LaminaFaro>();
    Renderer fundido;      // cuadro negro delante de la cámara (para pasar por puertas de frente)

    Rigidbody2D rb;
    Collider2D colHope;
    CameraFollow camara;
    int capaPiso;
    BoxCollider2D pisoAdentro, sueloAfuera, linternaAdentro, balconAfuera, paredIzq, paredDer;
    readonly Collider2D[] golpes = new Collider2D[8];
    PhysicsMaterial2D sinFriccion;          // para que Hope no se quede pegada a las paredes
    readonly bool[] bordeArmado = { true, true };      // [0] izquierda, [1] derecha
    readonly float[] bordeEmpujando = { 0f, 0f };
    readonly bool[] bloqueoAterrizaje = { false, false };   // aterrizó empujando: hay que soltar y volver a empujar
    bool estabaEnPiso = true;

    // ---------------- cuentas de paneles ----------------
    public static int ParedDeVista(bool adentro, int k) { return adentro ? k : (k + 2) % 4; }
    public static int VistaDePared(bool adentro, int pared) { return adentro ? pared : (pared + 2) % 4; }
    // Con los dibujos por cara: el interior de la cara c se ve en la vista de adentro (6 - c) % 4,
    // pegado a esa pared. Así, adentro como afuera, a la derecha del frente está la cara derecha.
    // (frente → vista 0, derecha → 3, atrás → 2, izquierda → 1)
    public static int ParedAdentroDeCara(int cara) { return (6 - cara) % 4; }
    public static int VistaDeCara(bool adentro, int cara) { return adentro ? ParedAdentroDeCara(cara) : VistaDePared(false, cara); }
    // Para qué lado gira (en vistas) al doblar la esquina hacia "lado" (+1 derecha, -1 izquierda)
    public static int GiroPorBorde(bool adentro, int lado) { return lado * (adentro ? -1 : 1); }

    // Mitad del ancho del faro a la altura y (medida desde el suelo).
    public float MedioAnchoEn(float y)
    {
        float t = alturaLinterna > 0.01f ? Mathf.Clamp01(y / alturaLinterna) : 0f;
        return Mathf.Lerp(medioAncho, medioAnchoArriba, t);
    }

    // Hasta dónde puede caminar Hope antes de doblar, a la altura y.
    public float Borde(bool enAdentro, float y)
    {
        float m = MedioAnchoEn(y);
        return enAdentro ? m - grosorMuro * 0.5f : m + grosorMuro * 0.5f + salidaRepisa;
    }

    // Punto del faro (en coordenadas del Pivote) que en la vista k queda en la posición u de la pantalla,
    // a la altura y, separado "d" de la pared (d positivo = hacia la cámara).
    public Vector3 PuntoDePanel(bool enAdentro, int k, float u, float y, float d)
    {
        float m = MedioAnchoEn(y);
        float z = enAdentro ? (m - grosorMuro * 0.5f - d) : -(m + grosorMuro * 0.5f + d);
        return Quaternion.Euler(0f, -90f * k, 0f) * new Vector3(u, y, z);
    }

    // Altura de los pies de Hope, medida desde el suelo del faro
    float PiesHope() { return colHope.bounds.min.y - pivote.position.y; }

    // =====================================================================
    void Start()
    {
        if (pivote == null) pivote = transform.Find("Pivote");
        if (hope == null) hope = FindObjectOfType<PlayerController2D>();
        if (farolFinal == null) farolFinal = FindObjectOfType<Lantern>();
        if (pivote == null || hope == null)
        {
            Debug.LogWarning("FaroFez: falta el Pivote o Hope. Usá HOPE > Nivel 3 > Crear escena Nivel_3.");
            enabled = false;
            return;
        }
        rb = hope.GetComponent<Rigidbody2D>();
        colHope = hope.GetComponent<Collider2D>();
        // Si el faro está escalado desde el Transform, los dibujos crecen pero las repisas invisibles no.
        float escalaObjeto = pivote.lossyScale.x;
        if (Mathf.Abs(escalaObjeto - 1f) > 0.01f)
            Debug.LogWarning("Faro: Faro_Nivel3 (o su Pivote) tiene escala " + escalaObjeto.ToString("0.##") +
                             ". Así los dibujos y las repisas invisibles no coinciden. Tocá el botón violeta 'Armar faro con mis dibujos': " +
                             "pasa esa escala a 'Tamaño' y deja la escala en 1.", this);
        // Faro más grande = los saltos del dibujo son más altos: Hope salta en proporción
        float salto = alturaSaltoEnFaro > 0f ? alturaSaltoEnFaro : hope.alturaSalto * Mathf.Max(0.1f, tamano);
        if (Mathf.Abs(salto - hope.alturaSalto) > 0.001f)
        {
            if (diagnostico) Debug.Log("Faro: en este nivel Hope salta " + salto.ToString("0.##") + " (en el Inspector de Hope dice " + hope.alturaSalto.ToString("0.##") + ").", this);
            hope.alturaSalto = salto;
        }
        // Cayendo desde muy alto Hope va rápido: con detección continua no atraviesa las repisas
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        camara = FindObjectOfType<CameraFollow>();
        capaPiso = LayerMask.NameToLayer("Piso");
        if (capaPiso < 0) capaPiso = 0;

        sinFriccion = new PhysicsMaterial2D("Faro sin friccion") { friction = 0f, bounciness = 0f };

        // 1) Leer el faro con el pivote derecho. Cada repisa y puerta tiene sus números
        //    (pared, lado, desde, hasta, altura): no se adivina nada mirando los cubos.
        pivote.localRotation = Quaternion.identity;
        Transform raiz = new GameObject("Colisiones2D_Faro").transform;
        piezas = pivote.GetComponentsInChildren<PiezaFaro>(true);
        pisosCara = GetComponentsInChildren<PisoCara>(true);
        usarEdge = pisosConEdge && pisosCara.Length > 0;
        if (pisosConEdge && pisosCara.Length == 0)
            Debug.LogWarning("Faro: 'Pisos Con Edge' está tildado pero no hay pisos dibujados. Tocá 'Crear pisos con Edge Collider' en Faro_Nivel3. Por ahora uso las repisas automáticas.", this);
        foreach (PiezaFaro p in piezas)
        {
            if (p.tipo != PiezaFaro.Tipo.Repisa) continue;
            p.Ubicar(this);            // por las dudas: el dibujo queda exactamente donde dicen los números
            if (!usarEdge) AgregarRepisa(p, raiz);   // con tus pisos dibujados, las automáticas no se usan
        }
        if (usarEdge) PrepararPisos();
        foreach (PuertaFaro p in pivote.GetComponentsInChildren<PuertaFaro>(true))
        {
            p.Ubicar(this);
            puertas.Add(new Puerta { p = p, pared = p.ParedIndice, u = p.posicion, y = p.altura, yAfuera = p.altura + p.desnivelAfuera });
        }
        laminasDibujo.RemoveAll(l => l == null || l.render == null);
        laminas = laminasDibujo.ToArray();   // las de las paredes y el fondo
        foreach (LaminaFaro l in laminas) l.Armar(this);

        // 2) Pisos, paredes invisibles, bordes de giro y flechas
        float yBase = pivote.position.y;
        pisoAdentro = Caja2D(raiz, "Piso adentro", 0f, yBase, 2f * Borde(true, 0f) + 2f, 1f);
        sueloAfuera = Caja2D(raiz, "Suelo afuera", 0f, yBase, 2f * Borde(false, 0f) + 2f, 1f);
        linternaAdentro = Caja2D(raiz, "Piso linterna", 0f, yBase + alturaLinterna, 2f * Borde(true, alturaLinterna), 1f);
        balconAfuera = Caja2D(raiz, "Balcon", 0f, yBase + alturaLinterna,
                              2f * (MedioAnchoEn(alturaLinterna) + grosorMuro * 0.5f + salidaTecho), 1f);
        paredIzq = Caja2D(raiz, "Pared invisible izq", 0f, yBase + alturaLinterna + 20f, 1f, alturaLinterna + 40f);
        paredDer = Caja2D(raiz, "Pared invisible der", 0f, yBase + alturaLinterna + 20f, 1f, alturaLinterna + 40f);
        paredIzq.gameObject.layer = paredDer.gameObject.layer = 0;   // que no cuenten como piso para el salto

        // Los bordes de giro de versiones anteriores ya no hacen falta: el giro se calcula
        // con la posición de Hope contra la pared invisible.
        foreach (BordeGiroFaro b in GetComponentsInChildren<BordeGiroFaro>(true)) b.gameObject.SetActive(false);
        CrearFlechas();
        if (profundidadDeCostado || profundidadAfuera) PrepararProfundidad();
        if (puertasDeFrente) CrearFundido();

        // 3) Arrancar
        if (farolFinal != null)
        {
            farolFinal.segundosPrendidoLibre = 0f;      // que no se apague solo
            farolFinal.mostrarCartelEncender = false;   // sin carteles: solo las flechas
        }
        if (hazDeLuz != null) hazDeLuz.SetActive(false);
        IrAlInicio();
        if (versionArmado < 4)
            Debug.LogWarning("Faro: este faro está armado con una versión vieja (sin los escalones del montón, sin la linterna y sin 'Tamaño'). " +
                             "Seleccioná Faro_Nivel3, tocá el botón violeta 'Armar faro con mis dibujos' y guardá la escena.", this);
        if (diagnostico)
            Debug.Log("Faro listo: " + (usarEdge ? "pisos dibujados con Edge Collider (" + pisosCara.Length + " caras), " : repisas.Count + " repisas, ") + puertas.Count + " puertas/ventanas, " +
                      (laminas != null ? laminas.Length : 0) + " láminas, " + flechas.Count + " flechas. Borde afuera en el suelo: " +
                      Borde(false, 0f).ToString("0.00") + ", adentro: " + Borde(true, 0f).ToString("0.00"), this);
    }

    // ---------------- PARA EL AUDIO ----------------
    // Qué tan "adentro" suena: 0 = afuera, 1 = adentro. Al pasar por una puerta o ventana
    // va de un valor al otro durante el pasaje. Lo usan la música, los pasos, los apoyos
    // y el ambiente del faro (scripts de la carpeta Audio).
    public float MezclaInteriorAudio { get { return mezclaInterior; } }
    float mezclaInterior;

    void Awake() { mezclaInterior = empiezaAfuera ? 0f : 1f; }

    void IrAlInicio()
    {
        adentro = !empiezaAfuera;
        mezclaInterior = adentro ? 1f : 0f;
        vista = vistaInicial;
        angulo = 90f * vista;
        pivote.localRotation = Quaternion.Euler(0f, angulo, 0f);
        AplicarModo();
        VisibilidadQuieto();
        if (camara != null) camara.zoom = adentro ? zoomAdentro : zoomAfuera;
        ApoyarEnElPiso();
        ultimaPos = hope.transform.position;
    }

    // Al empezar (y al volver al checkpoint) Hope puede quedar un poco en el aire:
    // se la baja hasta lo primero que haya debajo de sus pies (piso o plataforma).
    void ApoyarEnElPiso()
    {
        Physics2D.SyncTransforms();
        Bounds b = colHope.bounds;
        // desde un poco más arriba de los pies: si quedó metida en un escalón, la sube a él
        RaycastHit2D toque = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.6f), Vector2.down, 40f, 1 << capaPiso);
        if (toque.collider == null)
        {
            Debug.LogWarning("Faro: debajo de Hope no hay ningún piso (en x = " + b.center.x.ToString("0.00") + "): se va a caer. " +
                             "Dibujá un Edge Collider debajo de donde empieza (en el grupo de la cara donde arranca) o movela encima de uno.", this);
            return;
        }
        float d = toque.point.y - b.min.y;
        if (Mathf.Abs(d) < 0.005f) return;
        hope.transform.position += Vector3.up * (d + 0.01f);
        rb.position = hope.transform.position;
        rb.velocity = Vector2.zero;
    }

    void AgregarRepisa(PiezaFaro p, Transform raiz)
    {
        if (p.ParedIndice < 0 || p.lado == PiezaFaro.Lado.Ambos) return;
        var r = new Repisa { pieza = p, adentro = p.EsAdentro, pared = p.ParedIndice };
        r.vista = VistaDePared(r.adentro, r.pared);
        r.a = Mathf.Min(p.desde, p.hasta);
        r.b = Mathf.Max(p.desde, p.hasta);
        r.top = p.altura;
        r.grosor = Mathf.Max(0.1f, p.grosor);
        // El collider es más grueso que el dibujo (hacia abajo) para que no se atraviese cayendo rápido
        r.col = Caja2D(raiz, "Col_" + p.name, (r.a + r.b) * 0.5f, pivote.position.y + r.top, r.b - r.a, Mathf.Max(1f, r.grosor));
        repisas.Add(r);
    }

    // Collider 2D de un piso o repisa. "arriba" es la altura donde se apoyan los pies.
    // Las de un solo lado (repisas, piso de la linterna) se prenden y apagan en ActualizarRepisas().
    BoxCollider2D Caja2D(Transform raiz, string nombre, float u, float arriba, float ancho, float alto)
    {
        GameObject go = new GameObject(nombre);
        go.layer = capaPiso;
        go.transform.SetParent(raiz, false);
        go.transform.position = new Vector3(pivote.position.x + u, arriba - alto * 0.5f, 0f);
        BoxCollider2D c = go.AddComponent<BoxCollider2D>();
        c.size = new Vector2(Mathf.Max(0.05f, ancho), alto);
        c.sharedMaterial = sinFriccion;
        return c;
    }

    // Una flecha curva en cada punta de repisa que sigue en el panel de al lado
    void CrearFlechas()
    {
        foreach (Repisa r in repisas)
        {
            if (r.pieza.rota || r.pieza.sinFlecha) continue;
            float B = Borde(r.adentro, r.top);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                bool tocaBorde = lado > 0 ? r.b >= B - 0.25f : r.a <= -B + 0.25f;
                if (!tocaBorde) continue;
                int k2 = (r.vista + GiroPorBorde(r.adentro, lado) + 4) % 4;
                bool sigue = false;
                foreach (Repisa o in repisas)
                    if (o != r && o.adentro == r.adentro && o.vista == k2 && Mathf.Abs(o.top - r.top) < 0.05f &&
                        (lado > 0 ? o.a <= -B + 0.25f : o.b >= B - 0.25f)) { sigue = true; break; }
                if (!sigue) continue;

                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(go.GetComponent<Collider>());
                go.name = "Flecha " + r.pieza.name + (lado > 0 ? " der" : " izq");
                go.transform.SetParent(pivote, false);
                Vector3 baseLocal = PuntoDePanel(r.adentro, r.vista, lado * (B - 0.45f), r.top + alturaFlecha, 0.5f);
                go.transform.localPosition = baseLocal;
                go.transform.localRotation = Quaternion.Euler(0f, -90f * r.vista, 0f);   // de frente a la cámara en su vista
                go.transform.localScale = new Vector3(tamanoFlecha * lado, tamanoFlecha, 1f);
                if (materialFlecha != null) go.GetComponent<Renderer>().sharedMaterial = materialFlecha;
                flechas.Add(new Flecha { t = go.transform, r = r, baseLocal = baseLocal });
            }
        }
    }

    // =====================================================================
    void Update()
    {
        if (diagnostico) Diagnosticar();
        // las flechas flotan un poquito
        float bote = Mathf.Sin(Time.time * 4f) * 0.08f;
        foreach (Flecha f in flechas) f.t.localPosition = f.baseLocal + Vector3.up * bote;

        if (ganado)
        {
            if (hazDeLuz != null) hazDeLuz.transform.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
            return;
        }
        if (farolFinal != null && farolFinal.EstadoActual == Lantern.Estado.Lit) { Ganar(); return; }
        if (animando || hope.IsLocked) return;
        tiempoDesdeGiro += Time.deltaTime;

        if (InteraccionPresionada())
        {
            Puerta p = PuertaCerca();
            if (p != null)
            {
                bool m1 = !adentro;
                if (puertasDeFrente)
                    StartCoroutine(Transicion(m1, VistaDeCara(m1, p.pared), p.u, 0, duracionFundido, p.pared, m1 ? p.y : p.yAfuera));
                else
                    StartCoroutine(Transicion(m1, (vista + 2) % 4, m1 ? p.u : -p.u, 2, duracionPuerta, p.pared));
                return;
            }
        }
        RevisarBordes();
    }

    // ---------------- TECLA DE INTERACCIÓN ----------------
    // La de HopeControles: "Interactuar" (por defecto E), la que se cambia en el menú
    // Configuración > Controles. Con el juego en pausa no hace nada.
    bool InteraccionPresionada()
    {
        return HopeControles.Presionada(HopeControles.Accion.Interactuar);
    }

    void FixedUpdate()
    {
        if (animando || ganado) return;
        // Si Hope volvió al checkpoint (por ejemplo, al morir), el faro vuelve a como empezó
        Vector2 pos = hope.transform.position;
        if ((pos - ultimaPos).sqrMagnitude > 9f && hope.checkPoint != null &&
            ((Vector2)hope.checkPoint.position - pos).sqrMagnitude < 1f)
            IrAlInicio();
        ultimaPos = pos;
        ActualizarRepisas();
        fueraDelFaro = !adentro && HopeFueraDelFaro();
        if (paredIzq.enabled == fueraDelFaro) paredIzq.enabled = paredDer.enabled = !fueraDelFaro;
        SubirEscalon();
        // Si el faro es más angosto arriba, las paredes invisibles siguen la forma a la altura de Hope
        if (medioAnchoArriba != medioAncho) MoverParedesInvisibles();
    }

    void MoverParedesInvisibles()
    {
        float B = Borde(adentro, PiesHope()), x = pivote.position.x;
        MoverX(paredIzq.transform, x - B - 0.5f);
        MoverX(paredDer.transform, x + B + 0.5f);
    }

    // ---------------- REPISAS DE UN SOLO LADO ----------------
    // Una repisa solo es sólida si los pies de Hope están por encima de ella.
    // Así se la atraviesa saltando desde abajo y se la pisa al caer desde arriba.
    //   - Para prenderse: los pies tienen que estar casi a la altura de arriba de la repisa.
    //   - Para apagarse: los pies tienen que bajar un poco más (así no se cae estando parada).
    void ActualizarRepisas()
    {
        float pies = colHope.bounds.min.y, y0 = pivote.position.y;
        foreach (Repisa r in repisas)
            r.col.enabled = r.adentro == adentro && r.vista == vista && PiesArriba(r.col.enabled, pies, y0 + r.top);
        linternaAdentro.enabled = !usarEdge && adentro && PiesArriba(linternaAdentro.enabled, pies, y0 + alturaLinterna);
        balconAfuera.enabled = !usarEdge && !adentro && pisarTechoAfuera && PiesArriba(balconAfuera.enabled, pies, y0 + alturaLinterna);
        ActualizarPisos();
    }

    // ---------------- PISOS DIBUJADOS (Edge Collider 2D) ----------------
    // La cara que se está viendo: afuera, la pared de enfrente; adentro, la cara cuyo dibujo está al fondo
    int CaraActual() { return adentro ? ParedAdentroDeCara(vista) : ParedDeVista(false, vista); }

    // Solo queda prendido el grupo de la cara que se ve
    void ActualizarPisos()
    {
        if (!usarEdge) return;
        int c = CaraActual();
        float y = pivote.position.y - grosorEdge - ajustePies;   // se lee siempre: podés cambiar Ajuste Pies con el juego andando
        foreach (PisoCara g in pisosCara)
        {
            if (g == null) continue;
            bool prendido = g.EsAdentro == adentro && g.ParedIndice == c;
            if (g.gameObject.activeSelf != prendido) g.gameObject.SetActive(prendido);
            if (prendido && Mathf.Abs(g.transform.position.y - y) > 0.0001f)
                g.transform.position = new Vector3(pivote.position.x, y, 0f);
        }
    }

    // Al dar Play: cada grupo al pie del faro, todo en la capa Piso y sin fricción;
    // los de "Se atraviesan desde abajo" usan su Platform Effector 2D.
    void PrepararPisos()
    {
        foreach (PisoCara g in pisosCara)
        {
            // un poco más abajo: el grosor de las líneas no levanta a Hope del dibujo
            g.transform.position = new Vector3(pivote.position.x, pivote.position.y - grosorEdge - ajustePies, 0f);
            g.transform.rotation = Quaternion.identity;
            g.transform.localScale = Vector3.one;
            foreach (Collider2D c in g.GetComponentsInChildren<Collider2D>(true))
            {
                c.gameObject.layer = capaPiso;
                if (c.sharedMaterial == null) c.sharedMaterial = sinFriccion;
                if (c.GetComponent<PlatformEffector2D>() != null) c.usedByEffector = true;
                // un poquito de grosor: en las puntas de los escalones Hope no se "resbala" por el borde de la línea
                EdgeCollider2D e = c as EdgeCollider2D;
                if (e != null && e.edgeRadius < grosorEdge) e.edgeRadius = grosorEdge;
            }
            // Los Edge Collider de un mismo objeto funcionan como uno solo: al pasar de un escalón
            // al siguiente (donde se tocan dos líneas) no se atraviesan.
            foreach (PlatformEffector2D ef in g.GetComponentsInChildren<PlatformEffector2D>(true))
            {
                ef.useOneWayGrouping = true;
                ef.surfaceArc = Mathf.Max(ef.surfaceArc, 170f);
            }
        }
    }
    static bool PiesArriba(bool estabaPrendida, float pies, float arriba)
    {
        return estabaPrendida ? pies >= arriba - 0.15f : pies >= arriba - 0.02f;
    }

    float UHope() { return hope.transform.position.x - pivote.position.x; }

    bool TocaPiso()
    {
        Bounds b = colHope.bounds;
        int n = Physics2D.OverlapBoxNonAlloc(new Vector2(b.center.x, b.min.y - 0.05f),
            new Vector2(b.size.x * 0.9f, 0.12f), 0f, golpes, 1 << capaPiso);
        for (int i = 0; i < n; i++) if (golpes[i] != null && golpes[i].enabled) return true;
        return false;
    }

    Puerta PuertaCerca()
    {
        if (!TocaPiso()) return null;
        int w = ParedDeVista(adentro, vista);
        float pies = PiesHope(), u = UHope();
        foreach (Puerta p in puertas)
        {
            if (puertasDeFrente)
            {
                // De frente: la puerta se usa desde la vista donde se ve su cara (afuera o adentro),
                // adentro y afuera en el mismo lugar de la pantalla (cada lado con su altura).
                if (VistaDeCara(adentro, p.pared) != vista) continue;
                float yP = adentro ? p.y : p.yAfuera;
                if (Mathf.Abs(pies - yP) < 0.3f && Mathf.Abs(u - p.u) < distanciaPuerta) return p;
                continue;
            }
            if (p.pared != w) continue;
            float uP = adentro ? p.u : -p.u;   // del otro lado de la pared, la puerta queda espejada
            if (Mathf.Abs(pies - p.y) < 0.3f && Mathf.Abs(u - uP) < distanciaPuerta) return p;
        }
        return null;
    }

    // ---------------- DOBLAR LA ESQUINA ----------------
    // Hope toca el borde cuando su costado llega a la pared invisible.
    // Si sigue empujando un ratito para ese lado, el faro gira.
    // ---------------- AFUERA, ANTES DE LLEGAR AL FARO ----------------
    // Al principio Hope puede estar en el terreno de alrededor (por ejemplo, abajo de la
    // escalera de la entrada): más abajo que el piso del faro o más allá de sus bordes.
    // Ahí no hay paredes invisibles ni giros. Cuando entra del todo a la zona del faro,
    // empiezan a funcionar (y desde ahí, en los bordes se dobla).
    bool fueraDelFaro;
    bool HopeFueraDelFaro()
    {
        float pies = PiesHope();
        if (pies < -0.25f) return true;
        Bounds hb = colHope.bounds;
        float B = Borde(false, Mathf.Max(0f, pies)), x0 = pivote.position.x;
        if (fueraDelFaro) return hb.min.x < x0 - B - 0.02f || hb.max.x > x0 + B + 0.02f;   // todavía no entró del todo
        return hb.center.x < x0 - B - 0.05f || hb.center.x > x0 + B + 0.05f;               // salió (cayendo por el costado, etc.)
    }

    // ---------------- SUBIR ESCALONES CAMINANDO ----------------
    // Si Hope camina contra un escalón bajo (la contrahuella de una escalera), se sube sola.
    // Sin esto, en las plataformas que se atraviesan desde abajo Hope entra de costado en el
    // escalón, queda con los pies adentro y se cae.
    void SubirEscalon()
    {
        if (alturaEscalon <= 0f || animando || hope.IsLocked) return;
        float dir = EntradaHorizontal();
        if (Mathf.Abs(dir) < 0.3f || rb.velocity.y > 0.5f || !TocaPiso()) return;
        Bounds b = colHope.bounds;
        float s = Mathf.Sign(dir);
        float frente = s > 0f ? b.max.x + 0.06f : b.min.x - 0.06f;
        float pies = b.min.y;
        RaycastHit2D toque = Physics2D.Raycast(new Vector2(frente, pies + alturaEscalon + 0.02f), Vector2.down, alturaEscalon + 0.02f, 1 << capaPiso);
        if (toque.collider == null || toque.normal.y < 0.6f) return;
        float sube = toque.point.y - pies;
        if (sube <= 0.03f || sube > alturaEscalon) return;
        Vector3 p = hope.transform.position + new Vector3(s * 0.05f, sube + 0.01f, 0f);
        hope.transform.position = p;
        rb.position = p;
        if (rb.velocity.y < 0f) rb.velocity = new Vector2(rb.velocity.x, 0f);
    }

    void RevisarBordes()
    {
        if (!girarAlLlegarAlBorde || tiempoDesdeGiro < 0.3f || fueraDelFaro) return;
        float entrada = EntradaHorizontal();
        float B = Borde(adentro, PiesHope()), x0 = pivote.position.x;
        Bounds hb = colHope.bounds;

        // Si Hope aterriza mientras empuja hacia un lado (por ejemplo, saltando a la repisa
        // de la esquina), ese empuje no cuenta: hay que soltar la flecha y volver a empujar.
        bool enPiso = TocaPiso();
        if (enPiso && !estabaEnPiso)
        {
            bloqueoAterrizaje[0] = entrada < -0.5f && hb.min.x <= x0 - B + 0.6f;
            bloqueoAterrizaje[1] = entrada > 0.5f && hb.max.x >= x0 + B - 0.6f;
        }
        estabaEnPiso = enPiso;

        for (int lado = -1; lado <= 1; lado += 2)
        {
            int i = lado > 0 ? 1 : 0;
            if (entrada * lado <= 0.5f) bloqueoAterrizaje[i] = false;   // soltó la flecha
            bool toca = lado > 0 ? hb.max.x >= x0 + B - 0.08f : hb.min.x <= x0 - B + 0.08f;
            if (!toca) { bordeArmado[i] = true; bordeEmpujando[i] = 0f; bloqueoAterrizaje[i] = false; continue; }
            if (!bordeArmado[i]) continue;
            bool puedeGirar = (enPiso || girarEnElAire) && !bloqueoAterrizaje[i];
            bool empuja = puedeGirar && entrada * lado > 0.5f;
            bordeEmpujando[i] = empuja ? bordeEmpujando[i] + Time.deltaTime : 0f;
            if (bordeEmpujando[i] < segundosEmpujando) continue;

            int dk = GiroPorBorde(adentro, lado);
            // Hope aparece al principio del panel de al lado, a la misma altura
            StartCoroutine(Transicion(adentro, (vista + dk + 4) % 4, -lado * (B - 0.5f), dk, duracionGiro, -1));
            return;
        }
    }

    // ---------------- PARA QUÉ LADO EMPUJA HOPE ----------------
    // HopeControles.Horizontal(): lo mismo que usa PlayerController2D para moverla.
    float EntradaHorizontal()
    {
        // Las mismas teclas con las que se mueve Hope (las que se cambian en el menú, o el mando)
        return HopeControles.Horizontal();
    }

    // ---------------- DIAGNÓSTICO ----------------
    // Si Hope empuja hacia un costado medio segundo y el faro no gira, dice por qué en la Consola
    // (una vez por empujón). Se apaga destildando "Diagnostico" en Faro_Nivel3.
    float empujandoSinGirar, xAlEmpujar;
    bool diagnosticoDicho;
    float contraBorde; bool contraBordeDicho;
    void Diagnosticar()
    {
        if (hope == null || colHope == null || fueraDelFaro) return;
        // Aviso aparte: Hope pegada a un borde más de 1 segundo (aunque no se detecte el empujón)
        {
            float Bb = Borde(adentro, PiesHope()), x0b = pivote.position.x;
            Bounds hb0 = colHope.bounds;
            bool pegada = hb0.max.x >= x0b + Bb - 0.08f || hb0.min.x <= x0b - Bb + 0.08f;
            if (!pegada || animando) { contraBorde = 0f; contraBordeDicho = false; }
            else
            {
                contraBorde += Time.deltaTime;
                if (contraBorde > 1f && !contraBordeDicho)
                {
                    contraBordeDicho = true;
                    int ld = hb0.max.x >= x0b + Bb - 0.08f ? 1 : 0;
                    Debug.Log("Faro: Hope está contra el borde " + (ld == 1 ? "derecho" : "izquierdo") + " hace 1 s. Empuje leído: " +
                              EntradaHorizontal().ToString("0.00") + " (tiene que ser 1 o -1), en el piso: " + TocaPiso() +
                              ", bloqueada: " + hope.IsLocked + ", armado: " + bordeArmado[ld] + ", aterrizó empujando: " + bloqueoAterrizaje[ld] +
                              ", ganado: " + ganado + ", girar al llegar al borde: " + girarAlLlegarAlBorde, this);
                }
            }
        }
        float entrada = EntradaHorizontal();
        if (Mathf.Abs(entrada) < 0.5f || animando) { empujandoSinGirar = 0f; diagnosticoDicho = false; return; }
        float x = hope.transform.position.x;
        // Se mira cuánto avanzó Hope en ventanas de 0,8 s (contra una pared tiembla un poquito, eso no cuenta)
        if (empujandoSinGirar == 0f) { xAlEmpujar = x; empujandoSinGirar = Time.deltaTime; return; }
        empujandoSinGirar += Time.deltaTime;
        if (empujandoSinGirar < 0.8f) return;
        bool trabada = Mathf.Abs(x - xAlEmpujar) < 0.3f;
        xAlEmpujar = x; empujandoSinGirar = Time.deltaTime;
        if (!trabada || diagnosticoDicho) return;
        diagnosticoDicho = true;

        int lado = entrada > 0 ? 1 : -1, i = lado > 0 ? 1 : 0;
        float B = Borde(adentro, PiesHope()), x0 = pivote.position.x;
        Bounds hb = colHope.bounds;
        float punta = lado > 0 ? hb.max.x - x0 : -(hb.min.x - x0);
        string donde = (adentro ? "adentro" : "afuera") + ", vista " + vista + ", pies a " + PiesHope().ToString("0.00");
        string motivo;
        if (ganado) motivo = "el nivel ya está ganado (el farol de arriba está prendido).";
        else if (hope.IsLocked) motivo = "Hope está bloqueada (IsLocked) por otro script.";
        else if (!girarAlLlegarAlBorde) motivo = "'Girar Al Llegar Al Borde' está destildado.";
        else if (punta < B - 0.08f)
        {
            motivo = "Hope está quieta a " + punta.ToString("0.00") + " del centro y el borde está en " + B.ToString("0.00") + ": algo la frena antes.";
            Collider2D[] choques = Physics2D.OverlapBoxAll(new Vector2(lado > 0 ? hb.max.x + 0.05f : hb.min.x - 0.05f, hb.center.y), new Vector2(0.1f, hb.size.y * 0.8f), 0f);
            foreach (Collider2D c in choques)
                if (c != colHope && c.enabled && !c.isTrigger) motivo += " Choca con: '" + c.name + "' (en " + RutaDe(c.transform) + ").";
        }
        else if (!TocaPiso()) motivo = "Hope no está parada sobre algo de la capa 'Piso' (el faro solo gira con Hope en el piso).";
        else if (bloqueoAterrizaje[i]) motivo = "aterrizó empujando contra el borde: soltá la flecha y volvé a empujar.";
        else if (!bordeArmado[i]) motivo = "recién giró por este borde: alejate un poco y volvé.";
        else motivo = "no encontré el motivo (borde " + B.ToString("0.00") + ", punta " + punta.ToString("0.00") + ").";
        Debug.Log("Faro: no gira hacia la " + (lado > 0 ? "derecha" : "izquierda") + " (" + donde + "): " + motivo, this);
    }
    static string RutaDe(Transform t) { string r = t.name; while (t.parent != null) { t = t.parent; r = t.name + "/" + r; } return r; }

    // ---------------- GIRO (esquina o puerta) ----------------
    // dk = cuántas vistas gira (±1 en una esquina, 2 en una puerta, 0 en una puerta de frente).
    // paredPuerta = -1 si es una esquina. piesDestino = altura de los pies al llegar (solo puertas de frente).
    IEnumerator Transicion(bool m1, int k1, float u1, int dk, float duracion, int paredPuerta, float piesDestino = float.NaN)
    {
        animando = true;
        bool esPuerta = paredPuerta >= 0;
        bool deFrente = esPuerta && dk == 0;
        bool m0 = adentro;
        int k0 = vista;
        float a0 = angulo, a1 = angulo + 90f * dk;
        if (deFrente) a1 = 90f * k1;           // la vista de la misma cara, del otro lado
        float p0x = UHope();
        // Dónde está Hope en el faro antes y después (para moverla junto con el giro)
        float yPies = PiesHope();
        Vector3 p0 = PuntoDePanel(m0, k0, UHope(), yPies, 0.5f);
        Vector3 p1 = PuntoDePanel(m1, k1, u1, yPies, 0.5f);
        float altoPies = hope.transform.position.y - colHope.bounds.min.y;   // de los pies al centro de Hope
        float y0Hope = hope.transform.position.y;
        float y1Hope = float.IsNaN(piesDestino) ? y0Hope : pivote.position.y + piesDestino + altoPies + 0.02f;
        Vector2 velocidad = rb.velocity;
        rb.simulated = false;
        float z0 = camara != null ? camara.zoom : 0f, z1 = m1 ? zoomAdentro : zoomAfuera;

        if (deFrente) { }   // se sigue viendo igual hasta la mitad del fundido
        else if (esPuerta) VisibilidadPuerta(m0, paredPuerta);
        else VisibilidadEsquina(k0, k1);
        bool conProfundidad = !esPuerta && ((m0 && m1 && profundidadDeCostado) || (!m0 && !m1 && profundidadAfuera));
        if (conProfundidad) EmpezarProfundidad(m0);

        float t = 0f;
        bool segundaMitad = false;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.05f, duracion);
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            float a = Mathf.Lerp(a0, a1, e);
            if (deFrente) a = t < 0.5f ? a0 : a1;   // puerta de frente: no gira; cambia de vista en lo negro
            pivote.localRotation = Quaternion.Euler(0f, a, 0f);
            Vector3 w = Quaternion.Euler(0f, a, 0f) * Vector3.Lerp(p0, p1, e);
            if (deFrente) w = new Vector3(Mathf.Lerp(p0x, u1, e), 0f, 0f);
            float fuerza = conProfundidad ? Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)) : 0f;
            if (conProfundidad)
            {
                Shader.SetGlobalFloat(IdFuerza, fuerza);
            }
            // Hope se mueve igual que el rincón donde está parada (con la misma profundidad que las paredes)
            Vector3 hp = hope.transform.position;
            float yHope = deFrente ? Mathf.Lerp(y0Hope, y1Hope, e) : hp.y;
            hope.transform.position = new Vector3(pivote.position.x + XConProfundidad(w.x, w.z, yPies, fuerza), yHope, hp.z);
            if (camara != null && esPuerta) camara.zoom = Mathf.Lerp(z0, z1, e);
            if (esPuerta) mezclaInterior = Mathf.Lerp(m0 ? 1f : 0f, m1 ? 1f : 0f, e);
            if (deFrente) Fundido(Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)));
            if (!segundaMitad && t >= 0.5f)
            {
                segundaMitad = true;
                if (deFrente) VisibilidadDe(m1, k1, false);
                else if (esPuerta) VisibilidadPuerta(m1, paredPuerta);
                else { paredAlrededor = ParedDeVista(m1, k1); MostrarFondo(); }   // esquina: cambia lo que rodea al faro
            }
            yield return null;
        }
        if (conProfundidad) Shader.SetGlobalFloat(IdFuerza, 0f);
        if (deFrente) Fundido(0f);

        adentro = m1;
        mezclaInterior = adentro ? 1f : 0f;
        vista = k1;
        angulo = Mathf.Repeat(a1, 360f);
        pivote.localRotation = Quaternion.Euler(0f, angulo, 0f);
        Vector3 fin = hope.transform.position;
        hope.transform.position = new Vector3(pivote.position.x + u1, deFrente ? y1Hope : fin.y, fin.z);
        // Esquina: si a la vuelta la plataforma quedó un poquito más alta (los dibujos no coinciden
        // al píxel), Hope sube a ella en vez de quedar adentro de la tabla y caerse.
        if (!esPuerta && usarEdge)
        {
            // con tus pisos: se busca un Edge Collider apenas por encima de los pies, en la cara nueva
            ActualizarPisos();
            Physics2D.SyncTransforms();
            float pies0 = pivote.position.y + yPies;
            RaycastHit2D toque = Physics2D.Raycast(new Vector2(hope.transform.position.x, pies0 + 0.5f), Vector2.down, 0.5f, 1 << capaPiso);
            if (toque.collider != null && toque.point.y > pies0 + 0.01f)
                hope.transform.position += Vector3.up * (toque.point.y - pies0 + 0.02f);
        }
        else if (!esPuerta)
        {
            float subir = 0f;
            foreach (Repisa r in repisas)
                if (r.adentro == m1 && r.vista == k1 && u1 >= r.a - 0.3f && u1 <= r.b + 0.3f &&
                    r.top > yPies + 0.01f && r.top - yPies <= 0.5f) subir = Mathf.Max(subir, r.top - yPies);
            if (subir > 0f) hope.transform.position += Vector3.up * (subir + 0.02f);
        }
        if (camara != null) camara.zoom = z1;
        AplicarModo();
        VisibilidadQuieto();

        Physics2D.SyncTransforms();
        rb.simulated = true;
        rb.position = hope.transform.position;
        rb.velocity = esPuerta ? Vector2.zero : velocidad;
        bordeArmado[0] = bordeArmado[1] = false;     // espera a que Hope se aleje del borde
        bordeEmpujando[0] = bordeEmpujando[1] = 0f;
        tiempoDesdeGiro = 0f;
        ultimaPos = hope.transform.position;
        animando = false;
    }

    // ---------------- PROFUNDIDAD DE COSTADO EN LOS GIROS DE ADENTRO ----------------
    // La cámara es ortográfica (sin perspectiva): así, una esquina de adentro y una de afuera
    // se ven iguales. En el piso se entiende por la puerta, pero arriba no hay de dónde agarrarse.
    // Para que siempre se note, durante el giro de adentro:
    //   - lo que está más lejos (el rincón) se acerca al centro: se angosta, como en perspectiva,
    //     pero SOLO de costado (arriba y abajo no cambia, así nunca se ve el piso);
    //   - lo que está más lejos se oscurece un poco (el rincón queda en sombra).
    // Lo hacen los shaders Hope/FaroOpaco y Hope/FaroSprite (carpeta Resources).
    // Acá solo se los ponemos a las piezas y les pasamos los números.
    static readonly int IdCentro = Shader.PropertyToID("_FaroCentro");
    static readonly int IdDist = Shader.PropertyToID("_FaroDist");
    static readonly int IdFuerza = Shader.PropertyToID("_FaroFuerza");
    static readonly int IdForma = Shader.PropertyToID("_FaroForma");
    static readonly int IdSombra = Shader.PropertyToID("_FaroSombra");
    static readonly int IdRadio = Shader.PropertyToID("_FaroRadio");

    // Al dar Play: cambia los materiales de las piezas por copias con el shader del faro.
    // (Son copias: tus materiales de la carpeta no se tocan.)
    void PrepararProfundidad()
    {
        Shader opaco = Shader.Find("Hope/FaroOpaco"), sprite = Shader.Find("Hope/FaroSprite");
        if (opaco == null || sprite == null) { Debug.LogWarning("Faro: no encuentro los shaders Hope/FaroOpaco y Hope/FaroSprite."); return; }

        var copias = new Dictionary<Material, Material>();
        foreach (Renderer r in pivote.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;
                if (!copias.TryGetValue(m, out Material copia))
                {
                    string nombre = m.shader.name;
                    Shader nuevo = nombre == "Unlit/Texture" || nombre == "Unlit/Color" ? opaco
                                 : nombre == "Sprites/Default" ? sprite : null;
                    copia = null;
                    if (nuevo != null)
                    {
                        copia = new Material(m) { name = m.name + " (faro)" };
                        copia.shader = nuevo;
                        if (nombre == "Unlit/Color") copia.mainTexture = Texture2D.whiteTexture;
                    }
                    copias[m] = copia;
                }
                if (copia != null) mats[i] = copia;
            }
            r.sharedMaterials = mats;
        }

        // Los cubos de Unity tienen una sola cara por lado. Para que el rincón se angoste de a poco
        // (y no toda la pared de golpe) las piezas usan un cubo cortado en tiras verticales.
        Mesh cubo = CuboEnTiras(12);
        foreach (PiezaFaro p in piezas)
        {
            MeshFilter mf = p.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name == "Cube") mf.sharedMesh = cubo;
        }
        Shader.SetGlobalFloat(IdFuerza, 0f);
    }

    static readonly int IdMedidas = Shader.PropertyToID("_FaroMedidas");
    static readonly int IdLado = Shader.PropertyToID("_FaroLado");
    static readonly int IdAlto = Shader.PropertyToID("_FaroAlto");
    float ladoGiro = 1f;

    // Lo mismo que el shader, para un punto del faro en coordenadas del Pivote ya girado
    // (x de costado, z de profundidad, y altura desde el suelo): dónde queda x en pantalla.
    float XConProfundidad(float x, float z, float y, float fuerza)
    {
        if (fuerza <= 0f || !moverRincon) return x;
        float d = Mathf.Max(0.05f, perspectivaRincon);
        float zn = z / Mathf.Max(0.01f, MedioAnchoEn(y));
        float escala = Mathf.Min(1f, Mathf.Max(0.3f * d, d + ladoGiro) / Mathf.Max(0.3f * d, d + zn));
        return x * Mathf.Lerp(1f, escala, fuerza);
    }

    void EmpezarProfundidad(bool enAdentro)
    {
        // El centro del faro (hacia donde se angosta lo lejano) y las medidas, para que el shader
        // sepa a qué distancia está la pared de siempre a cada altura
        Vector3 c = pivote.position;
        Shader.SetGlobalVector(IdCentro, new Vector4(c.x, colHope.bounds.min.y, c.z, c.y));
        Shader.SetGlobalVector(IdMedidas, new Vector4(medioAncho, medioAnchoArriba, alturaLinterna, grosorMuro * 0.5f));
        Shader.SetGlobalFloat(IdLado, enAdentro ? 1f : -1f);
        Shader.SetGlobalFloat(IdAlto, profundidadSoloDeCostado ? 0f : 1f);
        ladoGiro = enAdentro ? 1f : -1f;
        Shader.SetGlobalFloat(IdDist, Mathf.Max(0.05f, perspectivaRincon));
        Shader.SetGlobalFloat(IdForma, moverRincon ? 1f : 0f);
        Shader.SetGlobalFloat(IdSombra, sombraRincon);
        Shader.SetGlobalFloat(IdRadio, medioAncho * 0.42f);   // el rincón está 0,42 medio-anchos más lejos que la pared
        Shader.SetGlobalFloat(IdFuerza, 0f);
    }

    void OnDisable() { Shader.SetGlobalFloat(IdFuerza, 0f); }

#if UNITY_EDITOR
    // En el editor: si cambiás las medidas del faro, las láminas, repisas y puertas se reacomodan.
    void OnValidate()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || pivote == null) return;
            foreach (LaminaFaro l in laminasDibujo) if (l != null) l.Armar(this);
            foreach (PiezaFaro p in pivote.GetComponentsInChildren<PiezaFaro>(true)) p.Ubicar(this);
            foreach (PuertaFaro p in pivote.GetComponentsInChildren<PuertaFaro>(true)) p.Ubicar(this);
        };
    }
#endif

    // Un cubo de 1x1x1 como el de Unity (mismas caras y mismas coordenadas de textura),
    // pero con cada cara cortada en "tiras" a lo largo de sus lados horizontales.
    static Mesh cuboEnTiras;
    static Mesh CuboEnTiras(int tiras)
    {
        if (cuboEnTiras != null) return cuboEnTiras;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        // normal, eje u, eje v de cada cara
        Vector3[,] caras = {
            { Vector3.back,    Vector3.right, Vector3.up },
            { Vector3.forward, Vector3.left,  Vector3.up },
            { Vector3.right,   Vector3.forward, Vector3.up },
            { Vector3.left,    Vector3.back,  Vector3.up },
            { Vector3.up,      Vector3.right, Vector3.forward },
            { Vector3.down,    Vector3.right, Vector3.back },
        };
        for (int c = 0; c < 6; c++)
        {
            Vector3 normal = caras[c, 0], eu = caras[c, 1], ev = caras[c, 2];
            int su = eu.y == 0f ? tiras : 1, sv = ev.y == 0f ? tiras : 1;   // solo se corta lo horizontal
            int primero = v.Count;
            for (int j = 0; j <= sv; j++)
                for (int i = 0; i <= su; i++)
                {
                    float a = (float)i / su, b = (float)j / sv;
                    v.Add(normal * 0.5f + eu * (a - 0.5f) + ev * (b - 0.5f));
                    n.Add(normal);
                    uv.Add(new Vector2(a, b));
                }
            for (int j = 0; j < sv; j++)
                for (int i = 0; i < su; i++)
                {
                    int i00 = primero + j * (su + 1) + i, i10 = i00 + 1, i01 = i00 + su + 1, i11 = i01 + 1;
                    tri.Add(i00); tri.Add(i01); tri.Add(i11);
                    tri.Add(i00); tri.Add(i11); tri.Add(i10);
                }
        }
        cuboEnTiras = new Mesh { name = "Cubo en tiras (faro)" };
        cuboEnTiras.SetVertices(v); cuboEnTiras.SetNormals(n); cuboEnTiras.SetUVs(0, uv); cuboEnTiras.SetTriangles(tri, 0);
        cuboEnTiras.RecalculateBounds();
        return cuboEnTiras;
    }

    // Prende los colliders del panel actual y acomoda pisos, paredes invisibles y bordes
    void AplicarModo()
    {
        // Al cambiar de panel, las repisas del panel nuevo arrancan apagadas y
        // ActualizarRepisas() prende las que están debajo de los pies de Hope.
        foreach (Repisa r in repisas) r.col.enabled = false;
        linternaAdentro.enabled = balconAfuera.enabled = false;
        pisoAdentro.enabled = !usarEdge && adentro;
        sueloAfuera.enabled = !usarEdge && !adentro;
        ActualizarPisos();
        ActualizarRepisas();
        float B = Borde(adentro, PiesHope()), x = pivote.position.x;
        MoverX(paredIzq.transform, x - B - 0.5f);
        MoverX(paredDer.transform, x + B + 0.5f);
    }
    static void MoverX(Transform t, float x) { Vector3 p = t.position; t.position = new Vector3(x, p.y, p.z); }

    // ---------------- QUÉ SE VE ----------------
    void VisibilidadQuieto() { VisibilidadDe(adentro, vista, true); }
    void VisibilidadDe(bool modo, int k, bool conFlechas)
    {
        paredAlrededor = ParedDeVista(modo, k);
        var paredes = new HashSet<int> { ParedDeVista(modo, k) };
        var ocultos = new HashSet<int>();
        if (modo) ocultos.Add((k + 2) % 4);   // la pared de adelante
        Aplicar(modo, paredes, ocultos, conFlechas);
    }
    void VisibilidadEsquina(int k0, int k1)
    {
        var paredes = new HashSet<int> { ParedDeVista(adentro, k0), ParedDeVista(adentro, k1) };
        var ocultos = new HashSet<int>();
        if (adentro) { ocultos.Add((k0 + 2) % 4); ocultos.Add((k1 + 2) % 4); }
        Aplicar(adentro, paredes, ocultos, false);
    }
    void VisibilidadPuerta(bool modo, int pared)
    {
        var paredes = new HashSet<int> { pared };
        var ocultos = new HashSet<int>();
        if (modo) for (int w = 0; w < 4; w++) if (w != pared) ocultos.Add(w);
        Aplicar(modo, paredes, ocultos, false);
    }
    void Aplicar(bool modo, HashSet<int> paredes, HashSet<int> ocultos, bool conFlechas)
    {
        foreach (PiezaFaro p in piezas)
        {
            switch (p.tipo)
            {
                case PiezaFaro.Tipo.Repisa:
                case PiezaFaro.Tipo.Decoracion:
                    bool deEsteLado = p.lado == PiezaFaro.Lado.Ambos || p.EsAdentro == modo;
                    p.Mostrar(deEsteLado && (p.ParedIndice < 0 || paredes.Contains(p.ParedIndice)));
                    break;
                case PiezaFaro.Tipo.Pared:
                    p.Mostrar(!ocultos.Contains(p.ParedIndice));
                    break;
                default:
                    p.Mostrar(p.lado == PiezaFaro.Lado.Ambos || p.EsAdentro == modo);
                    break;
            }
        }
        foreach (Puerta p in puertas)
            p.p.Mostrar(modo && paredes.Contains(p.pared), !modo && paredes.Contains(p.pared));
        foreach (Flecha f in flechas)
            f.t.gameObject.SetActive(conFlechas && f.r.adentro == modo && paredes.Contains(f.r.pared));
        // Láminas (tus dibujos): se ve la de la pared de frente, del lado donde está Hope.
        modoLaminas = modo;
        if (laminas != null)
            foreach (LaminaFaro l in laminas)
                if (l.capa != LaminaFaro.Capa.Fondo)
                    l.Mostrar(l.EsAdentro == modo && paredes.Contains(l.ParedFisica));
        MostrarFondo();
    }

    // El fondo: el cielo (si es "Ambos", también adentro) y lo que rodea a UNA cara (suelo, avión),
    // la que está de frente. Al doblar una esquina cambia a mitad del giro.
    int paredAlrededor = -1;
    bool modoLaminas;
    void MostrarFondo()
    {
        if (laminas == null) return;
        foreach (LaminaFaro l in laminas)
        {
            if (l.capa != LaminaFaro.Capa.Fondo) continue;
            if (l.ParedIndice < 0) l.Mostrar(l.lado == PiezaFaro.Lado.Ambos ? !modoLaminas || cieloAdentro : l.EsAdentro == modoLaminas);
            else l.Mostrar(l.EsAdentro == modoLaminas && l.ParedFisica == paredAlrededor);
        }
    }

    // ---------------- FUNDIDO A NEGRO (puertas de frente) ----------------
    void CrearFundido()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(go.GetComponent<Collider>());
        go.name = "Fundido puertas";
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, cam.nearClipPlane + 0.5f);
        go.transform.localScale = new Vector3(200f, 200f, 1f);
        fundido = go.GetComponent<Renderer>();
        fundido.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = new Color(0f, 0f, 0f, 0f) };
        fundido.sortingLayerName = "Oscuridad";   // por encima de la oscuridad de Hope
        fundido.sortingOrder = 100;
        fundido.enabled = false;
    }
    void Fundido(float a)
    {
        if (fundido == null) return;
        fundido.enabled = a > 0.001f;
        fundido.sharedMaterial.color = new Color(0f, 0f, 0f, Mathf.Clamp01(a * 1.25f));
    }

    // ---------------- FINAL ----------------
    void Ganar()
    {
        ganado = true;
        if (hazDeLuz != null) hazDeLuz.SetActive(true);
        if (activarAlGanar != null)
            foreach (GameObject go in activarAlGanar) if (go != null) go.SetActive(true);
        Camera cam = Camera.main;
        if (cam != null) StartCoroutine(AclararCielo(cam));
        StartCoroutine(SalirDelNivel());
    }

    IEnumerator AclararCielo(Camera cam)
    {
        Color desde = cam.backgroundColor;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 2.5f)
        {
            cam.backgroundColor = Color.Lerp(desde, fondoAlGanar, t);
            yield return null;
        }
        cam.backgroundColor = fondoAlGanar;
    }

    IEnumerator SalirDelNivel()
    {
        yield return new WaitForSeconds(segundosAntesDeSalir);
        if (!string.IsNullOrEmpty(escenaSiguiente) && Application.CanStreamedLevelBeLoaded(escenaSiguiente))
            SceneManager.LoadScene(escenaSiguiente);
    }
}
