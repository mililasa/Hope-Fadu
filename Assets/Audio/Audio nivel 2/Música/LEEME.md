# Música Nivel 2 — Diálogo pulso y piano V4
Dos capas originales PCM24 estéreo/48kHz de 51.2 s (2.457.600 muestras). Frase de cinco ataques de pulso y respuesta de dos notas de piano, 75 BPM, ciclo de 3.2 s. Se mantiene la mezcla relativa aprobada; no se normalizan capas por separado.

Al cargar únicamente Nivel_2 se crea Audio nivel 2 - Música. Las dos fuentes se programan en el mismo instante DSP (+0.2 s), pitch=1, loop=true. Inicio común y longitud idéntica impiden deriva rítmica. La pausa por timeScale pausa y reanuda las dos fuentes; AudioListener.pause mantiene el reloj DSP. Al recargar escena comienza de nuevo. Un respawn dentro de la misma escena permite continuar la frase.

Mixer Música nivel 2: Pulso Hope y Piano respuesta -> Música N2 - PREMIX (-12 dB inicial) -> Música del mixer general -> Master. Ajustar el premix para volumen global de esta pieza; ajustar los dos faders para balance interno. No hay ganancia automática ni modificación de los mixers existentes.

Se usa el loop WAV sin fades editoriales del MP3 de escucha. Los pasos y las mecánicas mantienen sus tiempos de acción; no se cambian físicas ni se retardan entradas para forzar una cuadrícula musical. La frase funciona como fondo a ese ritmo interactivo. La correspondencia estética y el balance final requieren escucha jugando.

Fuentes: síntesis original de Hope y fragmentos del motivo original Re–Mi♭–La de Nivel 1. Piano y secuencia originales V4. Sin cuerdas, platos ni fuentes musicales nuevas de terceros.
