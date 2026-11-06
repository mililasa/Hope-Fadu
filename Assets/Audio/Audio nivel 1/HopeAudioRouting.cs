using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum HopeAudioBus
{
    Ambientes,
    Foley,
    Mecanicas,
    Musica
}

public static class HopeAudioRouting
{
    static readonly Dictionary<HopeAudioBus, AudioMixerGroup> grupos = new Dictionary<HopeAudioBus, AudioMixerGroup>();
    static readonly HashSet<HopeAudioBus> avisados = new HashSet<HopeAudioBus>();

    public static void Asignar(AudioSource fuente, HopeAudioBus bus)
    {
        if (fuente == null) return;
        int nivel = Nivel(fuente.gameObject.scene.name);
        AudioMixerGroup grupoNivel = nivel >= 2 ? GrupoNivel(nivel, bus, fuente.name) : null;
        if (grupoNivel != null)
        {
            fuente.outputAudioMixerGroup = grupoNivel;
            HopeLighthouseExteriorAudio.Registrar(fuente);
            return;
        }
        if (!grupos.TryGetValue(bus, out AudioMixerGroup grupo) || grupo == null)
        {
            string nombre = "HopeMixer" + bus.ToString();
            AudioMixer mixer = Resources.Load<AudioMixer>(nombre);
            AudioMixerGroup[] master = mixer != null ? mixer.FindMatchingGroups("Master") : null;
            if (master != null)
                foreach (var candidato in master)
                    if (candidato.name == "Master") { grupo = candidato; break; }
            grupos[bus] = grupo;
        }
        if (grupo != null)
        {
            if (bus == HopeAudioBus.Foley && fuente.gameObject.scene.name == "SampleScene")
                grupo = GrupoFoleyNivel1(fuente.name, grupo);
            if (nivel == 1 && bus != HopeAudioBus.Foley)
                grupo = Buscar(grupo.audioMixer, Familia(bus, fuente.name, nivel)) ?? grupo;
            fuente.outputAudioMixerGroup = grupo;
        }
        else if (avisados.Add(bus))
            Debug.LogWarning("Hope: no se pudo cargar el submixer para " + bus + ". RevisÃ¡ HopeMixer" + bus + " en Resources.", fuente);
        HopeLighthouseExteriorAudio.Registrar(fuente);
    }

    static readonly (string prefijo, string grupo)[] familiasFoleyNivel1 =
    {
        ("F - Pasos puentes", "Puentes de madera"),
        ("F - Pasos", "Pasos"),
        ("F - Apoyos", "Apoyos"),
        ("F - Saltos", "Saltos"),
        ("F - Capa", "Capa"),
        ("Caballito -", "Caballito"),
        ("Explosion -", "Explosión pintura"),
        ("F - Faroles", "Faroles"),
        ("F - Gas fuego", "Faroles"),
        ("F - Lampara", "Lámpara"),
        ("F - Mano apagando", "Mano apagando el fuego"),
        ("F - Motor", "Motor"),
        ("F - Eco lejano", "Eco lejano de posguerra"),
        ("F - Plataforma piedra", "Plataformas piedra"),
        ("F - Plataforma Pium", "Plataformas Pium"),
        ("Pila -", "Pila cadáveres"),
    };

    static AudioMixerGroup GrupoFoleyNivel1(string nombre, AudioMixerGroup master)
    {
        foreach (var familia in familiasFoleyNivel1)
        {
            if (!nombre.StartsWith(familia.prefijo, System.StringComparison.Ordinal)) continue;
            foreach (var grupo in master.audioMixer.FindMatchingGroups(familia.grupo))
                if (grupo.name == familia.grupo) return grupo;
            break;
        }
        return master;
    }

    static readonly Dictionary<int, AudioMixer> mixersNivel = new Dictionary<int, AudioMixer>();

    static int Nivel(string escena)
    {
        if (escena == "SampleScene") return 1;
        if (escena == "Nivel_2") return 2;
        if (escena == "Nivel_3") return 3;
        return 0;
    }

    static AudioMixerGroup GrupoNivel(int nivel, HopeAudioBus bus, string fuente)
    {
        if (nivel == 3)
        {
            var premix = Resources.Load<AudioMixer>("HopeMixer" + bus + "Nivel3");
            return Buscar(premix, Familia(bus, fuente, nivel)) ?? Buscar(premix, "Master");
        }
        if (!mixersNivel.TryGetValue(nivel, out var mixer) || mixer == null)
        {
            mixer = Resources.Load<AudioMixer>("HopeMixerNivel" + nivel);
            mixersNivel[nivel] = mixer;
        }
        string categoria = bus == HopeAudioBus.Mecanicas ? "Mecánicas" :
                           bus == HopeAudioBus.Musica ? "Música" : bus.ToString();
        string familia = Familia(bus, fuente, nivel);
        return Buscar(mixer, familia != null ? categoria + "/" + familia : categoria)
            ?? Buscar(mixer, categoria);
    }

    static AudioMixerGroup Buscar(AudioMixer mixer, string ruta)
    {
        if (mixer == null || string.IsNullOrEmpty(ruta)) return null;
        string nombre = ruta.Substring(ruta.LastIndexOf('/') + 1);
        foreach (var grupo in mixer.FindMatchingGroups(ruta))
            if (grupo.name == nombre) return grupo;
        return null;
    }

    static string Familia(HopeAudioBus bus, string nombre, int nivel)
    {
        switch (bus)
        {
            case HopeAudioBus.Ambientes:
                if (nombre.Contains("arcos")) return "Arcos";
                if (nombre.Contains("Presion")) return "Presión de oscuridad";
                if (nombre.Contains("Viento")) return "Viento";
                if (nombre.Contains("Anticipacion")) return "Anticipación mano";
                if (nombre.Contains("Estampida")) return "Estampidas de piedra";
                break;
            case HopeAudioBus.Mecanicas:
                if (nombre.Contains("Checkpoint")) return "Checkpoint Faroles";
                if (nombre.Contains("Muerte")) return "Muerte";
                if (nivel == 1) return "Manos y plantas";
                break;
            case HopeAudioBus.Musica:
                if (nivel == 1) return nombre.Contains("Paso de Nivel") ? "Salida de nivel" : "Pulso";
                if (nivel == 3)
                {
                    if (nombre.StartsWith("Final")) return "Final";
                    if (nombre.Contains("Marimba")) return "Marimba";
                    if (nombre.StartsWith("Exterior")) return "Pulso exterior";
                    if (nombre.StartsWith("Interior")) return "Pulso interior";
                }
                break;
            case HopeAudioBus.Foley:
                if (nivel == 3)
                {
                    if (nombre.StartsWith("F - Pasos")) return "Pasos adoquín";
                    if (nombre.StartsWith("F - Apoyos")) return "Apoyos adoquín";
                    if (nombre.StartsWith("F - Saltos")) return "Saltos";
                    if (nombre.StartsWith("F - Capa")) return "Capa";
                    if (nombre.StartsWith("F - Lampara")) return "Lámpara";
                    if (nombre.StartsWith("F - Motor")) return "Motor";
                    if (nombre.StartsWith("F - Eco lejano")) return "Eco lejano de posguerra";
                    if (nombre.StartsWith("F - Faroles") || nombre.StartsWith("F - Gas fuego")) return "Faroles";
                }
                break;
        }
        return null;
    }
}
