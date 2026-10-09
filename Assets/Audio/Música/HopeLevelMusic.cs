using UnityEngine;

// Se agrega al jugador al iniciar: no necesita modificar ni guardar la escena.
[DefaultExecutionOrder(1100)]
[DisallowMultipleComponent]
public sealed class HopeLevelMusic : MonoBehaviour
{
    public HopeMusicSettings ajustes;
    public Transform salida;
    AudioSource principal, paso;
    PlayerController2D jugador;
    float mezcla;
    float mezclaMano;
    HopeHandAudio[] manos;
    bool cerca, finalizado;

    void Start()
    {
        jugador = GetComponent<PlayerController2D>();
        manos = FindObjectsOfType<HopeHandAudio>(true);
        if (ajustes == null) ajustes = Resources.Load<HopeMusicSettings>("HopeMusicSettings");
        if (ajustes == null || ajustes.musicaMain == null || ajustes.pasoDeNivel == null)
        {
            Debug.LogWarning("Hope: falta la configuracion o un clip de musica.", this);
            enabled = false;
            return;
        }
        if (salida == null)
        {
            GameObject destino = GameObject.Find(ajustes.nombreSalida);
            if (destino != null) salida = destino.transform;
        }
        if (salida == null) Debug.LogWarning("Hope: no se encontro el escenario de referencia de la puerta.", this);
        principal = CrearFuente("Musica Main", ajustes.musicaMain);
        paso = CrearFuente("Musica Paso de Nivel", ajustes.pasoDeNivel);
        principal.volume = ajustes.volumenMain;
        principal.Play();
    }

    AudioSource CrearFuente(string nombre, AudioClip clip)
    {
        var objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);
        var fuente = objeto.AddComponent<AudioSource>();
        HopeAudioRouting.Asignar(fuente, HopeAudioBus.Musica);
        fuente.playOnAwake = false;
        fuente.loop = true;
        fuente.spatialBlend = 0f;
        fuente.dopplerLevel = 0f;
        fuente.volume = 0f;
        fuente.clip = clip;
        return fuente;
    }

    void Update()
    {
        if (principal == null || paso == null) return;
        if (!finalizado && salida != null)
        {
            float distancia = DistanciaPuerta();
            float anticipacion = Mathf.Abs(jugador.moveSpeed) * ajustes.segundosAnticipacion;
            // El margen evita reiniciar la transicion al oscilar en el borde.
            if (!cerca && distancia <= anticipacion) cerca = true;
            else if (cerca && distancia > anticipacion + ajustes.margenRetirada) cerca = false;
        }
        bool entrar = finalizado || cerca;
        if (entrar && !paso.isPlaying)
        {
            paso.time = Mathf.Clamp(ajustes.inicioPaso, 0f, Mathf.Max(0f, paso.clip.length - 0.05f));
            paso.Play();
        }
        mezcla = Mathf.MoveTowards(mezcla, entrar ? 1f : 0f,
            Time.deltaTime / Mathf.Max(0.05f, ajustes.segundosFundido));
        // El volumen de Main se aplica en LateUpdate, despues del motivo de la mano.
        paso.volume = ajustes.volumenPaso * mezcla;
        if (!entrar && mezcla <= 0f && paso.isPlaying) paso.Stop();
    }

    void LateUpdate()
    {
        if (principal == null || ajustes == null) return;
        // La mano de la ChaseZone se activa tarde y recien ahi recibe su HopeHandAudio.
        if (Time.frameCount % 30 == 0) manos = FindObjectsOfType<HopeHandAudio>(true);
        float presencia = 0f;
        float entrada = 0.28f, salidaMano = 0.85f;
        if (manos != null)
        {
            foreach (HopeHandAudio mano in manos)
            {
                if (mano == null) continue;
                if (mano.NivelMusical >= presencia)
                {
                    presencia = mano.NivelMusical;
                    if (mano.ajustes != null)
                    {
                        entrada = mano.ajustes.entrada;
                        salidaMano = mano.ajustes.salida;
                    }
                }
            }
        }
        float tiempo = presencia > mezclaMano ? entrada : salidaMano;
        mezclaMano = Mathf.MoveTowards(mezclaMano, presencia, Time.deltaTime / Mathf.Max(0.01f, tiempo));
        // La mano desplaza Main; la mezcla de la cueva sigue teniendo su propio control.
        principal.volume = ajustes.volumenMain * (1f - mezcla) * (1f - mezclaMano);
    }

    float DistanciaPuerta()
    {
        return Vector2.Distance(transform.position, salida.TransformPoint(ajustes.puntoPuertaLocal));
    }

    public void CompletarNivel()
    {
        // El marcador antiguo NIVEL2 esta a mitad del escenario: no debe activar musica aqui.
        if (ajustes != null && jugador != null && salida != null &&
            DistanciaPuerta() <= Mathf.Abs(jugador.moveSpeed) * ajustes.segundosAnticipacion)
            finalizado = true;
    }

    void OnDrawGizmosSelected()
    {
        if (salida == null || ajustes == null || jugador == null) return;
        Vector3 puerta = salida.TransformPoint(ajustes.puntoPuertaLocal);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(puerta, Mathf.Abs(jugador.moveSpeed) * ajustes.segundosAnticipacion);
        Gizmos.DrawWireSphere(puerta, 0.25f);
    }

    public void ReiniciarNivel()
    {
        finalizado = false;
        cerca = false;
        // La principal sigue sincronizada y recupera su volumen con el mismo fundido.
    }
}
