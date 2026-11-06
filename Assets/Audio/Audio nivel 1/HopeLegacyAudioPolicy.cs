using UnityEngine;

// Nivel_2 starts a separate sound design. Only the legacy auto-installation
// is skipped; new level-specific audio can keep using HopeAudioRouting.
public static class HopeLegacyAudioPolicy
{
    public static bool Permitir(Component origen)
    {
        return origen != null && origen.gameObject.scene.name != "Nivel_2";
    }
}
