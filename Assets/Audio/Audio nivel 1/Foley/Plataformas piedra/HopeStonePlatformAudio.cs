using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class HopeStonePlatformAudio : MonoBehaviour
{
    public HopeStonePlatformSettings ajustes;
    PlataformaMovil plataforma;
    Rigidbody2D cuerpo;
    Transform jugador;
    AudioSource roce, contacto;
    Vector2 anterior;
    float velocidad, cercania, ultimoSentido, ultimoGiro = -10;
    int ultimoClip = -1;
    bool pausado;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Instalar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }
    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "SampleScene") return;
        foreach (var p in FindObjectsOfType<PlataformaMovil>())
            if (p.gameObject.scene == escena && p.GetComponent<HopeStonePlatformAudio>() == null)
                p.gameObject.AddComponent<HopeStonePlatformAudio>();
    }
    void Start()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeStonePlatformSettings>("HopeStonePlatformSettings");
        plataforma = GetComponent<PlataformaMovil>(); cuerpo = GetComponent<Rigidbody2D>();
        foreach (var j in FindObjectsOfType<PlayerController2D>())
            if (j.gameObject.scene == gameObject.scene) { jugador = j.transform; break; }
        if (ajustes == null || plataforma == null || cuerpo == null || jugador == null) { enabled = false; return; }
        anterior = cuerpo.position;
        roce = Crear("F - Plataforma piedra - roce mineral");
        contacto = Crear("F - Plataforma piedra - cambio de direccion");
        roce.clip = ajustes.roce; roce.loop = true;
    }
    AudioSource Crear(string nombre)
    {
        var go = new GameObject(nombre); go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; s.loop = false; s.spatialBlend = 0; s.volume = 0;
        HopeAudioRouting.Asignar(s, HopeAudioBus.Foley); return s;
    }
    void FixedUpdate()
    {
        if (cuerpo == null) return;
        Vector2 delta = cuerpo.position - anterior; anterior = cuerpo.position;
        velocidad = delta.magnitude / Mathf.Max(.001f, Time.fixedDeltaTime);
        if (Mathf.Abs(delta.y) < .0001f || !plataforma.isActiveAndEnabled) return;
        float sentido = Mathf.Sign(delta.y);
        if (ultimoSentido != 0 && sentido != ultimoSentido && Time.time - ultimoGiro > .3f)
        {
            ultimoGiro = Time.time;
            if (cercania > .01f && ajustes.contactos != null && ajustes.contactos.Length > 0)
            {
                int i = Random.Range(0, ajustes.contactos.Length);
                if (ajustes.contactos.Length > 1 && i == ultimoClip) i = (i + 1) % ajustes.contactos.Length;
                ultimoClip = i; contacto.volume = ajustes.volumenContacto * cercania;
                contacto.PlayOneShot(ajustes.contactos[i]);
            }
        }
        ultimoSentido = sentido;
    }
    void Update()
    {
        if (roce == null) return;
        if (Time.timeScale <= 0)
        {
            if (!pausado) { roce.Pause(); contacto.Pause(); pausado = true; }
            return;
        }
        if (pausado) { roce.UnPause(); contacto.UnPause(); pausado = false; }
        float distancia = Vector2.Distance(jugador.position, transform.position);
        cercania = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(ajustes.distanciaPlena, ajustes.distanciaSilencio, distancia));
        float movimiento = plataforma.isActiveAndEnabled ? Mathf.Clamp01(velocidad / Mathf.Max(.01f, plataforma.velocidad)) : 0;
        float objetivo = ajustes.volumenRoce * cercania * Mathf.Sqrt(movimiento);
        roce.volume = Mathf.MoveTowards(roce.volume, objetivo, Time.deltaTime * 2);
        if (objetivo > .001f && !roce.isPlaying && roce.clip != null) roce.Play();
        if (cercania == 0 && roce.volume <= .0001f) roce.Stop();
    }
    void OnDisable()
    {
        if (roce != null) roce.Stop();
        if (contacto != null) contacto.Stop();
    }
}
