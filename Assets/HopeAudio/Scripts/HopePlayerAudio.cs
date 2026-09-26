using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The controller sends confirmed gameplay actions; audio never changes physics.
[DisallowMultipleComponent]
public sealed class HopePlayerAudio : MonoBehaviour
{
    public HopeAudioSettings ajustes;
    AudioSource music, footsteps, actions;
    readonly Dictionary<GameObject, Lamp> lamps = new Dictionary<GameObject, Lamp>();
    Vector3 lastPosition;
    float walked, lastJump = -100, lastLanding = -100, lastBounce = -100;
    float reliefUntil, musicEnvelope;
    int lastStep = -1, lastRebound = -1;
    bool suspended;

    sealed class Lamp
    {
        public GameObject target, root;
        public Transform position;
        public AudioSource fx, flame, melody;
    }

    public static HopePlayerAudio Attach(GameObject player)
    {
        var existing = player.GetComponent<HopePlayerAudio>();
        return existing != null ? existing : player.AddComponent<HopePlayerAudio>();
    }

    void Awake()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeAudioSettings>("HopeAudioSettings");
        lastPosition = transform.position;
        if (ajustes == null)
        {
            Debug.LogWarning("Hope: falta la configuracion HopeAudioSettings; audio desactivado.", this);
            enabled = false;
            return;
        }
        music = Source(gameObject, "Musica continua", true);
        footsteps = Source(gameObject, "Pasos", false);
        actions = Source(gameObject, "Acciones", false);
        footsteps.volume = actions.volume = ajustes.general * ajustes.foleys;
        music.clip = ajustes.pulsoContinuo;
        music.volume = 0;
        if (music.clip != null) music.Play();
    }

    static AudioSource Source(GameObject parent, string label, bool loop)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent.transform, false);
        var audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.loop = loop;
        audio.spatialBlend = 0;
        audio.dopplerLevel = 0;
        return audio;
    }

    void Update()
    {
        if (ajustes == null) return;
        float target = suspended ? 0 : (Time.time < reliefUntil ? ajustes.musicaConFarol : 1);
        musicEnvelope = Mathf.MoveTowards(musicEnvelope, target, Time.deltaTime);
        music.volume = ajustes.general * ajustes.musica * musicEnvelope;
        footsteps.volume = ajustes.general * ajustes.foleys;
        actions.volume = ajustes.general * ajustes.foleys;
        foreach (var lamp in lamps.Values)
        {
            if (lamp.root == null) continue;
            if (lamp.target == null || !lamp.target.activeInHierarchy || suspended)
            {
                lamp.fx.volume = lamp.flame.volume = lamp.melody.volume = 0;
                continue;
            }
            Vector3 pos = lamp.position != null ? lamp.position.position : lamp.target.transform.position;
            lamp.root.transform.position = pos;
            float distance = Vector2.Distance(transform.position, pos);
            float proximity = Mathf.Clamp01(1 - distance / Mathf.Max(0.1f, ajustes.distanciaFarol));
            float gain = ajustes.general * ajustes.mecanicas * proximity * proximity;
            lamp.fx.volume = gain;
            lamp.flame.volume = gain * ajustes.volumenLlama;
            lamp.melody.volume = gain * ajustes.volumenMotivo;
            float pan = Mathf.Clamp((pos.x - transform.position.x) / Mathf.Max(0.1f, ajustes.distanciaFarol), -0.7f, 0.7f);
            lamp.fx.panStereo = lamp.flame.panStereo = lamp.melody.panStereo = pan;
        }
    }

    public void Movement(bool onGround, bool walking, bool climbing)
    {
        float delta = Mathf.Abs(transform.position.x - lastPosition.x);
        lastPosition = transform.position;
        if (!enabled || suspended || !onGround || !walking || climbing || Time.time - lastJump < 0.15f)
        {
            walked = 0;
            return;
        }
        // Ignore teleports; steps follow actual displacement rather than held input.
        if (delta > 2) { walked = 0; return; }
        walked += delta;
        if (walked < ajustes.distanciaEntrePasos) return;
        walked %= Mathf.Max(0.1f, ajustes.distanciaEntrePasos);
        if (Time.time - lastLanding < 0.15f) return;
        var clip = Variant(ajustes.pasos, ref lastStep);
        if (clip != null) footsteps.PlayOneShot(clip, ajustes.volumenPaso);
    }

    static AudioClip Variant(AudioClip[] clips, ref int previous)
    {
        if (clips == null || clips.Length == 0) return null;
        int next = Random.Range(0, clips.Length);
        if (clips.Length > 1 && next == previous) next = (next + 1) % clips.Length;
        previous = next;
        return clips[next];
    }

    public void Jump(bool rebound)
    {
        if (!enabled || suspended) return;
        if (rebound && Time.time - lastBounce < 0.1f) return;
        if (rebound) lastBounce = Time.time;
        lastJump = Time.time;
        walked = 0;
        footsteps.Stop();
        var clip = rebound ? Variant(ajustes.rebotes, ref lastRebound) : ajustes.salto;
        if (clip != null) actions.PlayOneShot(clip, rebound ? ajustes.volumenRebote : ajustes.volumenSalto);
    }

    public void Land()
    {
        if (!enabled || suspended || Time.time - lastJump < 0.16f || Time.time - lastLanding < 0.15f) return;
        lastLanding = Time.time;
        walked = 0;
        if (ajustes.apoyo != null) actions.PlayOneShot(ajustes.apoyo, ajustes.volumenApoyo);
    }

    public void LightLamp(GameObject target, Transform position)
    {
        if (!enabled || target == null || lamps.ContainsKey(target)) return;
        var lamp = new Lamp { target = target, position = position, root = new GameObject("Audio farol - " + target.name) };
        lamp.root.transform.position = position != null ? position.position : target.transform.position;
        lamp.fx = Source(lamp.root, "Chispa y encendido", false);
        lamp.flame = Source(lamp.root, "Llama", true);
        lamp.melody = Source(lamp.root, "Motivo de luz", false);
        lamp.fx.volume = lamp.flame.volume = lamp.melody.volume = 0;
        lamps.Add(target, lamp);
        reliefUntil = Time.time + ajustes.segundosAlivio;
        Update();
        StartCoroutine(Ignite(lamp));
    }

    IEnumerator Ignite(Lamp lamp)
    {
        if (ajustes.chispa != null) lamp.fx.PlayOneShot(ajustes.chispa, ajustes.volumenChispa);
        yield return new WaitForSeconds(0.1f);
        if (lamp.root == null) yield break;
        if (ajustes.encendido != null) lamp.fx.PlayOneShot(ajustes.encendido, ajustes.volumenEncendido);
        yield return new WaitForSeconds(0.15f);
        if (lamp.root == null) yield break;
        lamp.flame.clip = ajustes.llama;
        if (lamp.flame.clip != null) lamp.flame.Play();
        if (ajustes.motivoLuz != null) lamp.melody.PlayOneShot(ajustes.motivoLuz);
    }

    public void ResetLamps()
    {
        StopAllCoroutines();
        foreach (var lamp in lamps.Values) if (lamp.root != null) Destroy(lamp.root);
        lamps.Clear();
        reliefUntil = 0;
    }

    public void ResetPlayer()
    {
        suspended = false;
        walked = 0;
        lastPosition = transform.position;
        lastLanding = Time.time;
        if (footsteps != null) footsteps.Stop();
        if (actions != null) actions.Stop();
        ResetLamps();
    }

    public void BeginTransition()
    {
        suspended = true;
        if (footsteps != null) footsteps.Stop();
        if (actions != null) actions.Stop();
    }

    void OnDestroy() { ResetLamps(); }
}
