# Hamacas — Nivel 2
Fuente: Hamacas.wav de la biblioteca local Hope Assets Audio Nivel 2/Ruidos Metalicos. Original conservado y versión de loop PCM24 con unión circular de 150 ms. Sin cambios de tono o normalización.

Art: nivel_2_frente.png. Tres cuerpos y cuatro tablones colgantes al inicio. Apoyos: ParallaxRoot/PisoBox/Plataformas/Platatorma1, Plataforma2, Plataforma3, Plataforma3 (1). No se modifican colliders, arte ni escenas.

HopeLevel2SwingAudio se instala al cargar únicamente Nivel_2. Fuente en Play bajo Hope/Pp: F - Hamacas nivel 2. Se activa al aterrizar o caminar sobre los colliders correspondientes, usando contacto de apoyo bajo los pies. No se activa por cercanía, golpes laterales o al pasar por debajo. Se desvanece también al detenerse; tras la última pisada se sostiene 0.10 s y hace fade suave de 0.75 s. Entrada 0.04 s. Pausa y reaparición contempladas.

Mixer: Hamacas (-6 dB iniciales) -> Foley N2 - PREMIX -> Master. Ajuste de duración y superficies en Resources/HopeLevel2SwingSettings. Mantiene el sonido de pasos metálicos como base. Los demás niveles y canales no cambian.
