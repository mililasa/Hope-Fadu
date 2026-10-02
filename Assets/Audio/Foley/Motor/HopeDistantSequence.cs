using UnityEngine;

[DisallowMultipleComponent]
public sealed class HopeDistantSequence : MonoBehaviour
{
    public HopeDistantSettings ajustes;
    AudioSource motor, eco;
    float reloj, transcurrido, proximaPermitida;
    int siguiente;
    bool sonando, ecoIniciado;

    void Start()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeDistantSettings>("HopeDistantSettings");
        if (ajustes == null || ajustes.motor == null || ajustes.eco == null)
        {
            Debug.LogWarning("Hope: faltan clips o ajustes para motor y eco.", this);
            enabled = false;
            return;
        }
        motor = Fuente("F - Motor (canal 17)");
        eco = Fuente("F - Eco lejano de posguerra (canal 18)");
        motor.clip = ajustes.motor;
        eco.clip = ajustes.eco;
        Reiniciar();
    }

    AudioSource Fuente(string nombre)
    {
        var objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);
        var fuente = objeto.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;
        fuente.dopplerLevel = 0f;
        fuente.volume = 0f;
        return fuente;
    }

    void Update()
    {
        if (motor == null || Time.deltaTime <= 0f) return;
        reloj += Time.deltaTime;
        if (sonando)
        {
            transcurrido += Time.deltaTime;
            if (!ecoIniciado && transcurrido >= ajustes.retrasoEco)
            {
                ecoIniciado = true;
                eco.Play();
            }
            motor.volume = ajustes.volumenMotor * Envolvente(transcurrido, ajustes.motor.length, 1.2f, 2f);
            eco.volume = ajustes.volumenEco * Envolvente(transcurrido - ajustes.retrasoEco, ajustes.eco.length, 0.06f, 1.1f);
            if (transcurrido >= Mathf.Max(ajustes.motor.length, ajustes.retrasoEco + ajustes.eco.length))
            {
                motor.Stop(); eco.Stop(); sonando = false;
            }
        }
        if (sonando || reloj < ajustes.esperaInicial || reloj < proximaPermitida) return;
        float paneo = siguiente % 2 == 0 ? -0.5f : 0.5f;
        motor.panStereo = eco.panStereo = paneo;
        motor.volume = eco.volume = 0f;
        motor.Play();
        transcurrido = 0f;
        sonando = true;
        ecoIniciado = false;
        siguiente = (siguiente + 1) % 2;
        float minimo = Mathf.Max(0f, ajustes.intervaloMinimo);
        proximaPermitida = reloj + Random.Range(minimo, Mathf.Max(minimo, ajustes.intervaloMaximo));
    }

    static float Envolvente(float tiempo, float largo, float entrada, float salida)
    {
        if (tiempo < 0f || tiempo >= largo) return 0f;
        return Mathf.Min(Mathf.Clamp01(tiempo / entrada), Mathf.Clamp01((largo - tiempo) / salida));
    }

    public void Reiniciar()
    {
        if (motor != null) motor.Stop();
        if (eco != null) eco.Stop();
        reloj = transcurrido = proximaPermitida = 0f;
        siguiente = 0;
        sonando = ecoIniciado = false;
        if (ajustes == null) return;
        proximaPermitida = Mathf.Max(0f, ajustes.esperaInicial);
    }
}
