using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// El menu se ve tambien sin darle a Play. Botones y Popup se arrastran en la
// escena, o con Posicion Botones / Posicion Popup en este componente.
[ExecuteAlways]
public class MenuInicio : MonoBehaviour
{
    enum Vista { Nada, Config, Video, Sonido, Controles }

    struct TeclaUi
    {
        public HopeControles.Accion accion;
        public bool alternativa;
        public Text texto;
    }

    [SerializeField] UnityEngine.Audio.AudioMixerGroup salidaAudioMenu;
    [SerializeField] AudioClip audioIntro;

    [Header("Posicion")]
    [Tooltip("Centro de Jugar / Configuracion / Salir, en pixeles desde el centro de la pantalla. Y negativo los baja.")]
    [SerializeField] Vector2 posicionBotones = new Vector2(0f, -300f);
    [Tooltip("Centro del popup. X positivo lo corre a la derecha, Y negativo hacia abajo.")]
    [SerializeField] Vector2 posicionPopup = new Vector2(460f, -140f);
    [Tooltip("En el editor, muestra el popup para poder acomodarlo. En Play arranca cerrado.")]
    [SerializeField] bool verPopupEnEditor;

    const float AnchoPopup = 600f;
    const float AltoPopup = 680f;
    const float MargenPantalla = 32f;

    static readonly Color Tinte = new Color(0.52f, 0.30f, 0.72f, 1f);
    static readonly Color TinteClaro = new Color(0.78f, 0.52f, 0.96f, 1f);
    static readonly Color TinteOscuro = new Color(0.30f, 0.16f, 0.44f, 1f);
    static readonly Color TextoColor = new Color(0.90f, 0.78f, 0.98f, 1f);
    static readonly Color TextoSuave = new Color(0.72f, 0.60f, 0.82f, 1f);

    static Sprite marco;
    static Sprite blanco;
    static Font fuente;
    static KeyCode[] teclasPosibles;

    GameObject cartel;
    RectTransform columna;
    GameObject popup;
    ScrollRect scrollPopup;
    Vector2 popupAplicado;
    Text tituloPopup;
    GameObject vistaConfig;
    GameObject vistaVideo;
    GameObject vistaSonido;
    GameObject vistaControles;
    Text valorResolucion;
    Text valorFps;
    Text valorPantalla;
    Text valorMaster;
    Text valorMusica;
    Text valorEfectos;
    Slider sliderMaster;
    Slider sliderMusica;
    Slider sliderEfectos;
    Text avisoControles;
    readonly List<TeclaUi> teclasUi = new List<TeclaUi>();

    bool saliendo;
    bool escuchando;
    int frameEscucha;
    HopeControles.Accion accionEscucha;
    bool alternativaEscucha;
    bool aplicando;
    bool armado;
    bool juegoListo;
    bool abiertoPorJugador;
    bool popupAbierto;
    bool esPausa;
    bool consumioPausa;
    System.Action alVolver;
    System.Action alSalir;
    Vector2 origenBotones;
    Vector2 destinoBotones;
    float avanceBotones = 1f;
    static bool saliendoDelEditor;
    static bool crearComoPausa;

    public static void MarcarCreacionPausa()
    {
        crearComoPausa = true;
    }

    public bool ConsumioPausaEsteFrame { get { return consumioPausa; } }

    public void DefinirPausa(System.Action volver, System.Action salir)
    {
        alVolver = volver;
        alSalir = salir;
    }

    public void Abrir()
    {
        if (!esPausa) return;
        if (cartel == null) Construir();
        if (cartel != null) cartel.SetActive(true);
        Mostrar(Vista.Nada);
    }

    public void Ocultar()
    {
        escuchando = false;
        consumioPausa = false;
        abiertoPorJugador = false;
        popupAbierto = false;
        if (popup != null) popup.SetActive(false);
        if (cartel != null) cartel.SetActive(false);
    }

#if UNITY_EDITOR
    static MenuInicio()
    {
        UnityEditor.EditorApplication.playModeStateChanged -= AlCambiarPlay;
        UnityEditor.EditorApplication.playModeStateChanged += AlCambiarPlay;
    }

    static void AlCambiarPlay(UnityEditor.PlayModeStateChange estado)
    {
        if (estado == UnityEditor.PlayModeStateChange.ExitingEditMode)
            saliendoDelEditor = true;
        else if (estado == UnityEditor.PlayModeStateChange.EnteredPlayMode
            || estado == UnityEditor.PlayModeStateChange.EnteredEditMode)
            saliendoDelEditor = false;
        if (estado == UnityEditor.PlayModeStateChange.ExitingEditMode
            || estado == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            DestruirMenusViejos(null);
    }
#endif

    void OnEnable()
    {
        if (Application.isPlaying && crearComoPausa)
        {
            crearComoPausa = false;
            esPausa = true;
            Construir();
            if (cartel != null) cartel.SetActive(false);
            juegoListo = cartel != null;
            armado = juegoListo;
            return;
        }
        if (Application.isPlaying)
        {
            DestruirMenusViejos(null);
            LimpiarReferencias();
            PrepararJuego();
            return;
        }
        if (saliendoDelEditor) return;
        if (armado && cartel != null)
        {
            Mostrar(verPopupEnEditor ? Vista.Config : Vista.Nada);
            return;
        }
        Construir();
        Mostrar(verPopupEnEditor ? Vista.Config : Vista.Nada);
    }

    void OnDisable()
    {
        if (Application.isPlaying) return;
        DestruirMenusViejos(null);
        LimpiarReferencias();
    }

    void PrepararJuego()
    {
        if (juegoListo && cartel != null) return;
        if (!armado || cartel == null) Construir();
        juegoListo = cartel != null;
        Mostrar(Vista.Nada);
    }

    void OnValidate()
    {
        if (columna == null || popup == null) return;
        AplicarPosiciones();
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            if (saliendoDelEditor) return;
            TomarArrastre();
            if (popup != null && popup.activeSelf != verPopupEnEditor)
                Mostrar(verPopupEnEditor ? Vista.Config : Vista.Nada);
            AjustarPopup();
            return;
        }
        if (esPausa)
        {
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                if (cartel != null) Destroy(cartel);
                LimpiarReferencias();
                return;
            }
            if (cartel == null || !cartel.activeSelf) return;
            consumioPausa = false;
            AvanzarBotones();
            AjustarPopup();
            LeerTeclaNueva();
            return;
        }
        DestruirMenusViejos(cartel);
        if (cartel == null) juegoListo = false;
        PrepararJuego();
        if (!abiertoPorJugador && popup != null && popup.activeSelf)
            Mostrar(Vista.Nada);
        if (popup != null && popup.activeSelf == false)
            escuchando = false;
        AvanzarBotones();
        AjustarPopup();
        LeerTeclaNueva();
    }

    void LeerTeclaNueva()
    {
        if (!escuchando || Time.frameCount == frameEscucha) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            consumioPausa = true;
            escuchando = false;
            RefrescarTeclas();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
        {
            consumioPausa = true;
            HopeControles.Restaurar(accionEscucha, alternativaEscucha);
            escuchando = false;
            RefrescarTeclas();
            return;
        }
        KeyCode[] teclas = TeclasPosibles();
        for (int i = 0; i < teclas.Length; i++)
        {
            if (!Input.GetKeyDown(teclas[i])) continue;
            consumioPausa = true;
            HopeControles.Asignar(accionEscucha, alternativaEscucha, teclas[i]);
            escuchando = false;
            RefrescarTeclas();
            return;
        }
    }

    void Construir()
    {
        DestruirMenusViejos(null);
        LimpiarReferencias();
        cartel = new GameObject("MenuCanvas");
        if (!Application.isPlaying)
            cartel.hideFlags = HideFlags.DontSave;
        Canvas canvas = cartel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = esPausa ? 900 : 100;
        if (esPausa) cartel.transform.SetParent(transform, false);
        CanvasScaler escala = cartel.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;
        cartel.AddComponent<GraphicRaycaster>();

        AsegurarEventos();

        GameObject columnaGo = new GameObject("Botones");
        columnaGo.transform.SetParent(cartel.transform, false);
        columna = columnaGo.AddComponent<RectTransform>();
        columna.sizeDelta = new Vector2(460f, 0f);
        VerticalLayoutGroup layout = columnaGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter ajuste = columnaGo.AddComponent<ContentSizeFitter>();
        ajuste.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (esPausa)
        {
            CrearBoton(columna, "VOLVER AL JUEGO", VolverAlJuego, 460f, 64f);
            CrearBoton(columna, "CONFIGURACION", () => Mostrar(Vista.Config), 460f, 64f);
            CrearBoton(columna, "SALIR A MENU", SalirAlMenu, 460f, 64f);
        }
        else
        {
            CrearBoton(columna, "JUGAR", Jugar, 460f, 64f);
            CrearBoton(columna, "CONFIGURACION", () => Mostrar(Vista.Config), 460f, 64f);
            CrearBoton(columna, "SALIR", Salir, 460f, 64f);
        }

        popup = new GameObject("Popup");
        popup.transform.SetParent(cartel.transform, false);
        RectTransform popupRt = popup.AddComponent<RectTransform>();
        popupRt.anchorMin = popupRt.anchorMax = new Vector2(0.5f, 0.5f);
        popupRt.pivot = new Vector2(0.5f, 0.5f);
        popupRt.sizeDelta = new Vector2(AnchoPopup, AltoPopup);
        Image fondo = popup.AddComponent<Image>();
        fondo.sprite = Marco();
        fondo.type = Image.Type.Sliced;
        fondo.color = Tinte;
        fondo.pixelsPerUnitMultiplier = 1f;

        tituloPopup = CrearTexto(popup.transform, "CONFIGURACION", 42, TextAnchor.MiddleCenter);
        RectTransform tituloRt = tituloPopup.rectTransform;
        tituloRt.anchorMin = new Vector2(0f, 1f);
        tituloRt.anchorMax = new Vector2(1f, 1f);
        tituloRt.pivot = new Vector2(0.5f, 1f);
        tituloRt.sizeDelta = new Vector2(-40f, 64f);
        tituloRt.anchoredPosition = new Vector2(0f, -12f);

        GameObject cuerpo = new GameObject("Cuerpo");
        cuerpo.transform.SetParent(popup.transform, false);
        RectTransform cuerpoRt = cuerpo.AddComponent<RectTransform>();
        cuerpoRt.anchorMin = Vector2.zero;
        cuerpoRt.anchorMax = Vector2.one;
        cuerpoRt.offsetMin = new Vector2(28f, 20f);
        cuerpoRt.offsetMax = new Vector2(-28f, -84f);
        Image zonaScroll = cuerpo.AddComponent<Image>();
        zonaScroll.color = new Color(0f, 0f, 0f, 0f);
        cuerpo.AddComponent<RectMask2D>();

        GameObject contenido = new GameObject("Contenido");
        contenido.transform.SetParent(cuerpo.transform, false);
        RectTransform contRt = contenido.AddComponent<RectTransform>();
        contRt.anchorMin = new Vector2(0f, 1f);
        contRt.anchorMax = new Vector2(1f, 1f);
        contRt.pivot = new Vector2(0.5f, 1f);
        contRt.anchoredPosition = Vector2.zero;
        contRt.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup contLayout = contenido.AddComponent<VerticalLayoutGroup>();
        contLayout.childAlignment = TextAnchor.UpperCenter;
        contLayout.childControlWidth = true;
        contLayout.childControlHeight = true;
        contLayout.childForceExpandWidth = true;
        contLayout.childForceExpandHeight = false;
        ContentSizeFitter contFit = contenido.AddComponent<ContentSizeFitter>();
        contFit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollPopup = cuerpo.AddComponent<ScrollRect>();
        scrollPopup.content = contRt;
        scrollPopup.viewport = cuerpoRt;
        scrollPopup.horizontal = false;
        scrollPopup.vertical = true;
        scrollPopup.movementType = ScrollRect.MovementType.Clamped;
        scrollPopup.scrollSensitivity = 28f;

        vistaConfig = Panel(contenido.transform);
        CrearBoton(vistaConfig.transform, "VIDEO", () => Mostrar(Vista.Video), 560f, 62f);
        CrearBoton(vistaConfig.transform, "SONIDO", () => Mostrar(Vista.Sonido), 560f, 62f);
        CrearBoton(vistaConfig.transform, "CONTROLES", () => Mostrar(Vista.Controles), 560f, 62f);
        CrearBoton(vistaConfig.transform, esPausa ? "VOLVER" : "SALIR A MENU", () => Mostrar(Vista.Nada), 560f, 62f);
        vistaConfig.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

        vistaVideo = Panel(contenido.transform);
        if (HopeAjustes.PuedeElegirResolucion)
            valorResolucion = FilaSelector(vistaVideo.transform, "RESOLUCION",
                () => { HopeAjustes.PasoResolucion(-1); RefrescarVideo(); },
                () => { HopeAjustes.PasoResolucion(1); RefrescarVideo(); });
        valorFps = FilaSelector(vistaVideo.transform, "FPS",
            () => { HopeAjustes.PasoFps(-1); RefrescarVideo(); },
            () => { HopeAjustes.PasoFps(1); RefrescarVideo(); });
        if (HopeAjustes.PuedeElegirResolucion)
        {
            Button pantalla = CrearBoton(vistaVideo.transform, "PANTALLA COMPLETA: NO", AlternarPantalla, 560f, 62f);
            valorPantalla = pantalla.GetComponentInChildren<Text>();
        }
        CrearBoton(vistaVideo.transform, "VOLVER", () => Mostrar(Vista.Config), 560f, 62f);

        vistaSonido = Panel(contenido.transform);
        sliderMaster = FilaVolumen(vistaSonido.transform, "MASTER", HopeAjustes.Master, HopeAjustes.PonerMaster, out valorMaster);
        sliderMusica = FilaVolumen(vistaSonido.transform, "MUSICA", HopeAjustes.Musica, HopeAjustes.PonerMusica, out valorMusica);
        sliderEfectos = FilaVolumen(vistaSonido.transform, "EFECTOS", HopeAjustes.Efectos, HopeAjustes.PonerEfectos, out valorEfectos);
        CrearBoton(vistaSonido.transform, "VOLVER", () => Mostrar(Vista.Config), 560f, 62f);

        vistaControles = Panel(contenido.transform);
        avisoControles = CrearTexto(vistaControles.transform, "Toca una tecla. Esc cancela. Borrar restaura.", 22, TextAnchor.MiddleCenter);
        avisoControles.color = TextoSuave;
        LayoutElement avisoLe = avisoControles.gameObject.AddComponent<LayoutElement>();
        avisoLe.preferredHeight = 36f;
        avisoLe.minHeight = 36f;
        FilaControl(vistaControles.transform, HopeControles.Accion.Izquierda, "IZQUIERDA");
        FilaControl(vistaControles.transform, HopeControles.Accion.Derecha, "DERECHA");
        FilaControl(vistaControles.transform, HopeControles.Accion.Saltar, "SALTAR");
        FilaControl(vistaControles.transform, HopeControles.Accion.Interactuar, "INTERACTUAR");
        FilaControl(vistaControles.transform, HopeControles.Accion.Pausa, "PAUSA");
        CrearBoton(vistaControles.transform, "RESTABLECER", () =>
        {
            HopeControles.RestaurarTodo();
            escuchando = false;
            RefrescarTeclas();
        }, 560f, 56f);
        CrearBoton(vistaControles.transform, "VOLVER", () => Mostrar(Vista.Config), 560f, 62f);
        if (vistaVideo != null) vistaVideo.SetActive(false);
        if (vistaSonido != null) vistaSonido.SetActive(false);
        if (vistaControles != null) vistaControles.SetActive(false);
        if (popup != null) popup.SetActive(false);
        AplicarPosiciones();
        MarcarNoGuardar(cartel);
        armado = true;
    }

    void AplicarPosiciones()
    {
        aplicando = true;
        AjustarPopup();
        aplicando = false;
        Colocar(popupAbierto);
    }

    void AjustarPopup()
    {
        if (popup == null || cartel == null) return;
        RectTransform rt = popup.transform as RectTransform;
        RectTransform lienzo = cartel.transform as RectTransform;
        if (rt == null || lienzo == null) return;
        Rect area = lienzo.rect;
        if (area.width < 64f || area.height < 64f) return;

        float ancho = Mathf.Min(AnchoPopup, Mathf.Max(0f, area.width - MargenPantalla * 2f));
        float chrome = 104f;
        if (scrollPopup != null && scrollPopup.viewport != null)
            chrome = -scrollPopup.viewport.offsetMax.y + scrollPopup.viewport.offsetMin.y;
        float altoContenido = AltoVistaActiva();
        float altoIdeal = altoContenido > 1f ? altoContenido + chrome : AltoPopup;
        float bottom = BottomCompartido();
        float limiteSup = area.height * 0.5f - MargenPantalla;
        float espacio = Mathf.Max(0f, limiteSup - bottom);
        float alto = Mathf.Min(altoIdeal, espacio);
        if (scrollPopup != null)
            scrollPopup.vertical = altoIdeal > alto + 1f;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(ancho, alto);
        AlinearColumnas();
    }

    float BottomCompartido()
    {
        Vector2 ancla = esPausa ? Vector2.zero : posicionBotones;
        return ancla.y - AlturaBotones() * 0.5f;
    }

    float AlturaBotones()
    {
        if (columna == null) return 224f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(columna);
        float alto = LayoutUtility.GetPreferredHeight(columna);
        if (alto < 1f) alto = columna.rect.height;
        if (alto < 1f) alto = 224f;
        return alto;
    }

    void AlinearColumnas()
    {
        if (columna == null || popup == null || cartel == null) return;
        RectTransform rt = popup.transform as RectTransform;
        RectTransform lienzo = cartel.transform as RectTransform;
        if (rt == null || lienzo == null) return;
        Rect area = lienzo.rect;
        if (area.width < 64f || area.height < 64f) return;

        float altoB = AlturaBotones();
        Vector2 ancla = esPausa ? Vector2.zero : posicionBotones;
        float bottom = ancla.y - altoB * 0.5f;
        float limiteInf = -area.height * 0.5f + MargenPantalla;
        if (bottom < limiteInf) bottom = limiteInf;

        bool abierto = popupAbierto && popup.activeSelf;
        float anchoB = columna.sizeDelta.x > 1f ? columna.sizeDelta.x : 460f;
        float anchoP = rt.sizeDelta.x > 1f ? rt.sizeDelta.x : AnchoPopup;
        const float hueco = 48f;
        float xB = ancla.x;
        float xP = posicionPopup.x;
        if (esPausa && abierto)
            bottom = -Mathf.Max(altoB, rt.sizeDelta.y) * 0.5f;
        if (abierto)
        {
            float total = anchoB + hueco + anchoP;
            xB = -total * 0.5f + anchoB * 0.5f;
            xP = total * 0.5f - anchoP * 0.5f;
            float limite = area.width * 0.5f - MargenPantalla;
            float izquierda = xB - anchoB * 0.5f;
            float derecha = xP + anchoP * 0.5f;
            if (izquierda < -limite) { float corrige = -limite - izquierda; xB += corrige; xP += corrige; }
            if (derecha > limite) { float corrige = derecha - limite; xB -= corrige; xP -= corrige; }
        }

        popupAplicado = new Vector2(xP, bottom);
        rt.anchoredPosition = popupAplicado;

        Vector2 destino = abierto
            ? new Vector2(xB, bottom + altoB * 0.5f)
            : ancla;
        if ((destino - destinoBotones).sqrMagnitude > 0.25f)
        {
            bool primera = destinoBotones == Vector2.zero;
            origenBotones = columna.anchoredPosition;
            destinoBotones = destino;
            avanceBotones = Application.isPlaying && !primera ? 0f : 1f;
        }
        if (avanceBotones >= 1f)
        {
            aplicando = true;
            PonerCentrado(columna, destinoBotones);
            aplicando = false;
        }
    }

    float AltoVistaActiva()
    {
        GameObject vista = vistaConfig;
        if (vistaVideo != null && vistaVideo.activeSelf) vista = vistaVideo;
        else if (vistaSonido != null && vistaSonido.activeSelf) vista = vistaSonido;
        else if (vistaControles != null && vistaControles.activeSelf) vista = vistaControles;
        if (vista == null) return 0f;
        RectTransform rt = vista.transform as RectTransform;
        if (rt == null) return 0f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        return LayoutUtility.GetPreferredHeight(rt);
    }

    void Colocar(bool abierto)
    {
        AlinearColumnas();
    }

    void AvanzarBotones()
    {
        if (columna == null || avanceBotones >= 1f) return;
        avanceBotones = Mathf.Clamp01(avanceBotones + Time.unscaledDeltaTime / 0.2f);
        float t = avanceBotones * avanceBotones * (3f - 2f * avanceBotones);
        aplicando = true;
        PonerCentrado(columna, Vector2.Lerp(origenBotones, destinoBotones, t));
        aplicando = false;
    }

    void AsegurarEventos()
    {
        EventSystem sistema = FindObjectOfType<EventSystem>();
        if (sistema == null)
        {
            GameObject eventos = new GameObject("EventSystem");
            eventos.transform.SetParent(cartel.transform, false);
            sistema = eventos.AddComponent<EventSystem>();
        }
        if (sistema.GetComponent<BaseInputModule>() == null)
            sistema.gameObject.AddComponent<StandaloneInputModule>();
    }

    static void PonerCentrado(RectTransform rt, Vector2 posicion)
    {
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
    }

    void TomarArrastre()
    {
        if (aplicando || columna == null || popup == null) return;
        bool cambioBotones = false;
        if (!popup.activeSelf && avanceBotones >= 1f)
            cambioBotones = (columna.anchoredPosition - posicionBotones).sqrMagnitude > 0.25f;
        if (!cambioBotones) return;
        posicionBotones = columna.anchoredPosition;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    static void MarcarNoGuardar(GameObject go)
    {
        if (Application.isPlaying || go == null) return;
        go.hideFlags = HideFlags.DontSave;
        for (int i = 0; i < go.transform.childCount; i++)
            MarcarNoGuardar(go.transform.GetChild(i).gameObject);
    }

    void LimpiarReferencias()
    {
        teclasUi.Clear();
        cartel = null;
        columna = null;
        popup = null;
        scrollPopup = null;
        tituloPopup = null;
        vistaConfig = null;
        vistaVideo = null;
        vistaSonido = null;
        vistaControles = null;
        valorResolucion = null;
        valorFps = null;
        valorPantalla = null;
        valorMaster = null;
        valorMusica = null;
        valorEfectos = null;
        sliderMaster = null;
        sliderMusica = null;
        sliderEfectos = null;
        avisoControles = null;
        armado = false;
        juegoListo = false;
        abiertoPorJugador = false;
        popupAbierto = false;
    }

    static void DestruirMenusViejos(GameObject conservar)
    {
        Canvas[] lienzos = Resources.FindObjectsOfTypeAll<Canvas>();
        for (int i = 0; i < lienzos.Length; i++)
        {
            if (lienzos[i] == null) continue;
            GameObject go = lienzos[i].gameObject;
            if (go == null || go == conservar) continue;
            if (!go.name.StartsWith("MenuCanvas")) continue;
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.IsPersistent(go)) continue;
#endif
            go.SetActive(false);
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }

    static void DestroyInmediato(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }

    GameObject Panel(Transform padre)
    {
        GameObject panel = new GameObject("Vista");
        panel.transform.SetParent(padre, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return panel;
    }

    void Mostrar(Vista vista)
    {
        escuchando = false;
        if (Application.isPlaying)
            abiertoPorJugador = vista != Vista.Nada;
        bool abierto = vista != Vista.Nada;
        popupAbierto = abierto;
        if (popup != null) popup.SetActive(abierto);
        if (vistaConfig != null) vistaConfig.SetActive(vista == Vista.Config);
        if (vistaVideo != null) vistaVideo.SetActive(vista == Vista.Video);
        if (vistaSonido != null) vistaSonido.SetActive(vista == Vista.Sonido);
        if (vistaControles != null) vistaControles.SetActive(vista == Vista.Controles);
        if (tituloPopup != null)
        {
            if (vista == Vista.Video) tituloPopup.text = "VIDEO";
            else if (vista == Vista.Sonido) tituloPopup.text = "SONIDO";
            else if (vista == Vista.Controles) tituloPopup.text = "CONTROLES";
            else tituloPopup.text = "CONFIGURACION";
        }
        RefrescarVideo();
        RefrescarSonido();
        RefrescarTeclas();
        if (scrollPopup != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollPopup.verticalNormalizedPosition = 1f;
        }
        AjustarPopup();
        Colocar(abierto);
    }

    void RefrescarVideo()
    {
        if (valorResolucion != null) valorResolucion.text = HopeAjustes.TextoResolucion();
        if (valorFps != null) valorFps.text = HopeAjustes.TextoFps();
        if (valorPantalla != null) valorPantalla.text = "PANTALLA COMPLETA: " + HopeAjustes.TextoPantalla();
    }

    void RefrescarSonido()
    {
        PonerSlider(sliderMaster, valorMaster, HopeAjustes.Master);
        PonerSlider(sliderMusica, valorMusica, HopeAjustes.Musica);
        PonerSlider(sliderEfectos, valorEfectos, HopeAjustes.Efectos);
    }

    static void PonerSlider(Slider slider, Text valor, float cantidad)
    {
        if (slider != null) slider.SetValueWithoutNotify(cantidad);
        if (valor != null) valor.text = Mathf.RoundToInt(cantidad * 100f) + "%";
    }

    void RefrescarTeclas()
    {
        if (avisoControles != null)
            avisoControles.text = escuchando
                ? "Toca la tecla nueva. Esc cancela."
                : "Toca una tecla. Esc cancela. Borrar restaura.";
        for (int i = 0; i < teclasUi.Count; i++)
        {
            TeclaUi ui = teclasUi[i];
            if (ui.texto == null) continue;
            bool esta = escuchando && ui.accion == accionEscucha && ui.alternativa == alternativaEscucha;
            ui.texto.text = esta ? "..." : HopeControles.Nombre(HopeControles.Tecla(ui.accion, ui.alternativa));
        }
    }

    void AlternarPantalla()
    {
        HopeAjustes.AlternarPantallaCompleta();
        RefrescarVideo();
    }

    void EmpezarMapeo(HopeControles.Accion accion, bool alternativa)
    {
        escuchando = true;
        frameEscucha = Time.frameCount;
        accionEscucha = accion;
        alternativaEscucha = alternativa;
        RefrescarTeclas();
    }

    Text FilaSelector(Transform padre, string etiqueta, UnityEngine.Events.UnityAction menos, UnityEngine.Events.UnityAction mas)
    {
        Text titulo = CrearTexto(padre, etiqueta, 24, TextAnchor.MiddleLeft);
        titulo.color = TextoSuave;
        LayoutElement tituloLe = titulo.gameObject.AddComponent<LayoutElement>();
        tituloLe.preferredHeight = 28f;
        tituloLe.minHeight = 28f;

        GameObject fila = new GameObject(etiqueta);
        fila.transform.SetParent(padre, false);
        LayoutElement filaLe = fila.AddComponent<LayoutElement>();
        filaLe.preferredHeight = 58f;
        filaLe.minHeight = 58f;
        HorizontalLayoutGroup layout = fila.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        CrearBoton(fila.transform, "<", menos, 64f, 52f);
        Text valor = CrearTexto(fila.transform, "-", 28, TextAnchor.MiddleCenter);
        LayoutElement valorLe = valor.gameObject.AddComponent<LayoutElement>();
        valorLe.flexibleWidth = 1f;
        valorLe.minWidth = 180f;
        CrearBoton(fila.transform, ">", mas, 64f, 52f);
        return valor;
    }

    Slider FilaVolumen(Transform padre, string etiqueta, float inicial, UnityEngine.Events.UnityAction<float> cambio, out Text porcentaje)
    {
        GameObject fila = new GameObject(etiqueta);
        fila.transform.SetParent(padre, false);
        LayoutElement filaLe = fila.AddComponent<LayoutElement>();
        filaLe.preferredHeight = 28f;
        filaLe.minHeight = 28f;
        HorizontalLayoutGroup layout = fila.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        Text titulo = CrearTexto(fila.transform, etiqueta, 26, TextAnchor.MiddleLeft);
        LayoutElement tituloLe = titulo.gameObject.AddComponent<LayoutElement>();
        tituloLe.flexibleWidth = 1f;
        porcentaje = CrearTexto(fila.transform, Mathf.RoundToInt(inicial * 100f) + "%", 26, TextAnchor.MiddleRight);
        LayoutElement valorLe = porcentaje.gameObject.AddComponent<LayoutElement>();
        valorLe.preferredWidth = 90f;
        valorLe.minWidth = 90f;

        Text porcentajeLocal = porcentaje;
        Slider slider = CrearSlider(padre, inicial, valor =>
        {
            if (porcentajeLocal != null) porcentajeLocal.text = Mathf.RoundToInt(valor * 100f) + "%";
            cambio(valor);
        });
        return slider;
    }

    void FilaControl(Transform padre, HopeControles.Accion accion, string etiqueta)
    {
        GameObject fila = new GameObject(etiqueta);
        fila.transform.SetParent(padre, false);
        LayoutElement filaLe = fila.AddComponent<LayoutElement>();
        filaLe.preferredHeight = 52f;
        filaLe.minHeight = 52f;
        HorizontalLayoutGroup layout = fila.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        Text nombre = CrearTexto(fila.transform, etiqueta, 24, TextAnchor.MiddleLeft);
        LayoutElement nombreLe = nombre.gameObject.AddComponent<LayoutElement>();
        nombreLe.flexibleWidth = 1f;
        nombreLe.minWidth = 170f;
        TeclaBoton(fila.transform, accion, false);
        TeclaBoton(fila.transform, accion, true);
    }

    void TeclaBoton(Transform padre, HopeControles.Accion accion, bool alternativa)
    {
        HopeControles.Accion accionLocal = accion;
        bool altLocal = alternativa;
        Button boton = CrearBoton(padre, HopeControles.Nombre(HopeControles.Tecla(accion, alternativa)),
            () => EmpezarMapeo(accionLocal, altLocal), 150f, 46f);
        teclasUi.Add(new TeclaUi
        {
            accion = accion,
            alternativa = alternativa,
            texto = boton.GetComponentInChildren<Text>()
        });
    }

    Slider CrearSlider(Transform padre, float valor, UnityEngine.Events.UnityAction<float> cambio)
    {
        GameObject go = new GameObject("Slider");
        go.transform.SetParent(padre, false);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 26f;
        le.minHeight = 26f;
        le.flexibleWidth = 1f;

        GameObject fondo = new GameObject("Fondo");
        fondo.transform.SetParent(go.transform, false);
        Image fondoImg = fondo.AddComponent<Image>();
        fondoImg.sprite = Blanco();
        fondoImg.color = new Color(0.10f, 0.07f, 0.14f, 1f);
        Estirar(fondoImg.rectTransform, 0f);

        GameObject area = new GameObject("Relleno");
        area.transform.SetParent(go.transform, false);
        RectTransform areaRt = area.AddComponent<RectTransform>();
        Estirar(areaRt, 3f);
        GameObject relleno = new GameObject("Barra");
        relleno.transform.SetParent(area.transform, false);
        Image rellenoImg = relleno.AddComponent<Image>();
        rellenoImg.sprite = Blanco();
        rellenoImg.color = TinteClaro;
        rellenoImg.raycastTarget = false;
        RectTransform rellenoRt = rellenoImg.rectTransform;
        rellenoRt.anchorMin = Vector2.zero;
        rellenoRt.anchorMax = new Vector2(0f, 1f);
        rellenoRt.offsetMin = Vector2.zero;
        rellenoRt.offsetMax = Vector2.zero;

        GameObject zona = new GameObject("Zona");
        zona.transform.SetParent(go.transform, false);
        RectTransform zonaRt = zona.AddComponent<RectTransform>();
        Estirar(zonaRt, 0f);
        GameObject mango = new GameObject("Mango");
        mango.transform.SetParent(zona.transform, false);
        Image mangoImg = mango.AddComponent<Image>();
        mangoImg.sprite = Blanco();
        mangoImg.color = TextoColor;
        RectTransform mangoRt = mangoImg.rectTransform;
        mangoRt.sizeDelta = new Vector2(16f, 32f);

        Slider slider = go.AddComponent<Slider>();
        slider.fillRect = rellenoRt;
        slider.handleRect = mangoRt;
        slider.targetGraphic = mangoImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(valor);
        Navigation nav = slider.navigation;
        nav.mode = Navigation.Mode.None;
        slider.navigation = nav;
        slider.onValueChanged.AddListener(cambio);
        return slider;
    }

    Button CrearBoton(Transform padre, string etiqueta, UnityEngine.Events.UnityAction click, float ancho, float alto)
    {
        GameObject go = new GameObject(etiqueta);
        go.transform.SetParent(padre, false);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = ancho;
        le.preferredHeight = alto;
        le.minHeight = alto;
        le.minWidth = ancho > 200f ? 0f : ancho;

        Image imagen = go.AddComponent<Image>();
        imagen.sprite = Marco();
        imagen.type = Image.Type.Sliced;
        imagen.color = Tinte;
        imagen.pixelsPerUnitMultiplier = 1f;
        imagen.raycastTarget = true;

        Button boton = go.AddComponent<Button>();
        boton.targetGraphic = imagen;
        boton.transition = Selectable.Transition.None;
        Navigation nav = boton.navigation;
        nav.mode = Navigation.Mode.None;
        boton.navigation = nav;
        if (click != null)
        {
            boton.onClick.AddListener(() =>
            {
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(null);
                click();
            });
        }

        Text texto = CrearTexto(go.transform, etiqueta, ancho > 200f ? 34 : 24, TextAnchor.MiddleCenter);
        RectTransform tr = texto.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(12f, 4f);
        tr.offsetMax = new Vector2(-12f, -4f);
        texto.raycastTarget = false;
        EncenderConElMouse(go, imagen, texto);
        return boton;
    }

    static void EncenderConElMouse(GameObject go, Image imagen, Text texto)
    {
        bool encima = false;
        bool apretado = false;
        void Pintar()
        {
            imagen.color = apretado ? TinteOscuro : encima ? TinteClaro : Tinte;
            texto.color = encima || apretado ? Color.white : TextoColor;
        }
        EventTrigger trigger = go.AddComponent<EventTrigger>();
        Sumar(EventTriggerType.PointerEnter, () => { encima = true; Pintar(); });
        Sumar(EventTriggerType.PointerExit, () => { encima = false; apretado = false; Pintar(); });
        Sumar(EventTriggerType.PointerDown, () => { apretado = true; Pintar(); });
        Sumar(EventTriggerType.PointerUp, () => { apretado = false; Pintar(); });

        void Sumar(EventTriggerType tipo, UnityEngine.Events.UnityAction accion)
        {
            EventTrigger.Entry entrada = new EventTrigger.Entry { eventID = tipo };
            entrada.callback.AddListener(_ => accion());
            trigger.triggers.Add(entrada);
        }
    }

    Text CrearTexto(Transform padre, string contenido, int tamano, TextAnchor ancla)
    {
        GameObject go = new GameObject("Texto");
        go.transform.SetParent(padre, false);
        Text texto = go.AddComponent<Text>();
        texto.font = Fuente();
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.alignment = ancla;
        texto.color = TextoColor;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.raycastTarget = false;
        Outline sombra = go.AddComponent<Outline>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sombra.effectDistance = new Vector2(1.5f, -1.5f);
        return texto;
    }

    static void Estirar(RectTransform rt, float margen)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }

    void VolverAlJuego()
    {
        if (alVolver != null) alVolver();
    }

    void SalirAlMenu()
    {
        if (alSalir != null) alSalir();
    }

    void Jugar()
    {
        if (saliendo) return;
        saliendo = true;
        if (cartel != null) cartel.SetActive(false);
        CinematicaInicio.Reproducir(salidaAudioMenu, audioIntro);
    }

    void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static Font Fuente()
    {
        if (fuente != null) return fuente;
        fuente = Resources.Load<Font>("Tiny5-Regular");
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return fuente;
    }

    static Sprite Marco()
    {
        if (marco != null) return marco;
        const int n = 32;
        const int b = 3;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.HideAndDontSave;
        Color borde = Color.white;
        Color centro = new Color(0.16f, 0.16f, 0.16f, 1f);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            bool esBorde = x < b || y < b || x >= n - b || y >= n - b;
            tex.SetPixel(x, y, esBorde ? borde : centro);
        }
        tex.Apply();
        marco = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b), false);
        return marco;
    }

    static Sprite Blanco()
    {
        if (blanco != null) return blanco;
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.hideFlags = HideFlags.HideAndDontSave;
        Color[] pix = new Color[16];
        for (int i = 0; i < pix.Length; i++) pix[i] = Color.white;
        tex.SetPixels(pix);
        tex.Apply();
        blanco = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return blanco;
    }

    static KeyCode[] TeclasPosibles()
    {
        if (teclasPosibles != null) return teclasPosibles;
        var lista = new List<KeyCode>();
        foreach (KeyCode tecla in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (tecla == KeyCode.None || tecla == KeyCode.Escape || tecla == KeyCode.Backspace || tecla == KeyCode.Delete)
                continue;
            if (tecla >= KeyCode.Mouse0) continue;
            lista.Add(tecla);
        }
        teclasPosibles = lista.ToArray();
        return teclasPosibles;
    }
}
