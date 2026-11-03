using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class HopeBuild
{
    static string[] Escenas()
    {
        var lista = new List<string>();
        foreach (EditorBuildSettingsScene escena in EditorBuildSettings.scenes)
        {
            if (escena.enabled)
                lista.Add(escena.path);
        }
        return lista.ToArray();
    }

    public static void Windows()
    {
        Salir(Construir(BuildTarget.StandaloneWindows64, "build/Hope_FADU.exe"));
    }

    public static void Web()
    {
        Salir(Construir(BuildTarget.WebGL, "build_web"));
    }

    static BuildSummary Construir(BuildTarget target, string salida)
    {
        if (EditorUserBuildSettings.activeBuildTarget != target)
        {
            BuildTargetGroup grupo = BuildPipeline.GetBuildTargetGroup(target);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(grupo, target))
            {
                Debug.LogError("Hope: no se pudo cambiar el target a " + target);
                EditorApplication.Exit(1);
            }
        }

        var opciones = new BuildPlayerOptions
        {
            scenes = Escenas(),
            locationPathName = salida,
            target = target,
            targetGroup = BuildPipeline.GetBuildTargetGroup(target),
            options = BuildOptions.None
        };
        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        Debug.Log("Hope build " + target + ": " + reporte.summary.result + " en " + reporte.summary.totalTime.TotalSeconds.ToString("0") + "s");
        if (reporte.summary.result != BuildResult.Succeeded)
        {
            foreach (BuildStep step in reporte.steps)
            {
                foreach (BuildStepMessage mensaje in step.messages)
                {
                    if (mensaje.type == LogType.Error || mensaje.type == LogType.Exception)
                        Debug.LogError(mensaje.content);
                }
            }
        }
        return reporte.summary;
    }

    static void Salir(BuildSummary resumen)
    {
        EditorApplication.Exit(resumen.result == BuildResult.Succeeded ? 0 : 1);
    }
}
