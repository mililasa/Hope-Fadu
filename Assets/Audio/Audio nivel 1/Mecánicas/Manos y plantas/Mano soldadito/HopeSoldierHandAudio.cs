using UnityEngine;

public sealed class HopeSoldierHandAudio : MonoBehaviour
{
    public HopeSoldierHandSettings ajustes;
    Animator animacion;
    string estado;
    AudioSource[] fuentes;
    bool[] emitida = new bool[4];
    bool pausado;
    public static void Iniciar(Component secuencia, Animator mano, string nombreEstado)
    {
        if (secuencia.gameObject.scene.name != "SampleScene" || mano == null) return;
        var go = new GameObject("Audio - Mano se lleva al soldadito");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, secuencia.gameObject.scene);
        var audio = go.AddComponent<HopeSoldierHandAudio>();
        audio.animacion = mano; audio.estado = nombreEstado;
        audio.Configurar();
    }
    void Configurar()
    {
        ajustes = Resources.Load<HopeSoldierHandSettings>("HopeSoldierHandSettings");
        if (ajustes == null) { Destroy(gameObject); return; }
        fuentes = new AudioSource[4];
        string[] nombres = { "Aparicion electrica", "Anticipo del motivo", "Agarre del soldado", "Retirada de la mano" };
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject(nombres[i]); go.transform.SetParent(transform, false);
            var f = go.AddComponent<AudioSource>(); f.playOnAwake = false; f.loop = false; f.spatialBlend = 0;
            HopeAudioRouting.Asignar(f, i == 2 ? HopeAudioBus.Foley : HopeAudioBus.Mecanicas);
            fuentes[i] = f;
        }
    }
    void Update()
    {
        if (fuentes == null) return;
        if (Time.timeScale <= 0)
        {
            if (!pausado) { foreach (var f in fuentes) f.Pause(); pausado = true; }
            return;
        }
        if (pausado) { foreach (var f in fuentes) f.UnPause(); pausado = false; }
        float[] volumenes = { ajustes.volumenAparicion, ajustes.volumenMotivo, ajustes.volumenAgarre, ajustes.volumenRetirada };
        for (int i = 0; i < 4; i++) fuentes[i].volume = ajustes.volumenGeneral * volumenes[i];
        if (animacion != null && animacion.isActiveAndEnabled)
        {
            var info = animacion.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(estado))
            {
                AudioClip[] clips = { ajustes.aparicion, ajustes.motivo, ajustes.agarre, ajustes.retirada };
                float[] tiempos = { ajustes.faseAparicion, ajustes.faseMotivo, ajustes.faseAgarre, ajustes.faseRetirada };
                for (int i = 0; i < 4; i++)
                    if (!emitida[i] && info.normalizedTime >= tiempos[i])
                    {
                        emitida[i] = true;
                        if (clips[i] != null) { fuentes[i].clip = clips[i]; fuentes[i].Play(); }
                    }
            }
        }
        bool todas = true, sonando = false;
        for (int i = 0; i < 4; i++) { todas &= emitida[i]; sonando |= fuentes[i].isPlaying; }
        if ((todas || animacion == null || !animacion.gameObject.activeInHierarchy) && !sonando) Destroy(gameObject);
    }
    void OnDisable() { if (fuentes != null) foreach (var f in fuentes) if (f != null) f.Stop(); }
}
