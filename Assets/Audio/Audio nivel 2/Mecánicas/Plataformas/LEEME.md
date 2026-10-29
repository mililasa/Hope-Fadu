# Plataformas móviles de Nivel 2
Cuatro plataformas verticales y una entre puntos horizontales. Cada una obtiene HopeLevel2PlatformAudio al cargar Nivel_2. No modifica escenas, físicas ni scripts de movimiento.

Tres capas independientes (Metal, Motor, Electricidad) y una conmutación breve de inversión que sale por Electricidad. Material de las pruebas V1, exportado sin envolventes ni descargas temporizadas: el volumen se regula con el desplazamiento real del Rigidbody2D. Se registra inversión al cambiar la dirección, con protección contra teletransporte y disparos duplicados. El servo afloja en los extremos; si se detiene la plataforma, las capas se apagan suavemente.

Atenuación respecto de Hope: plena hasta 2 unidades, inaudible a 11. Paneo moderado ±0.55. Fade de entrada 0.12 s y salida 0.4 s. Pausa incluida. No dispara un sonido de salto al pulsar espacio: representa la maquinaria móvil.

Mixer Plataformas nivel 2: Metal, Motor y Electricidad -> Plataformas N2 - PREMIX -> Mecánicas del mixer general -> Master. Faders de capas en 0 dB, premix compensado por el gain actual del bus global más -3 dB para primera prueba. No se cambia ningún fader previo. Recursos y distancias en Resources/HopeLevel2PlatformSettings.

Fuentes y procesos en Fuentes_y_tratamiento.json. Los loops de Unity no llevan la temporización fija de los audios demostrativos.

## Prueba sin loop
Las tres capas y la inversión usan loop=false. Cada capa se dispara una sola vez por recorrido, al empezar a moverse dentro del radio audible. Solo se rearma cuando cambia de dirección o se detiene/deshabilita el mecanismo. El final del archivo queda en silencio: no se reinicia por isPlaying=false ni por salir y volver al radio durante el mismo recorrido. Reinicio con fade breve de 60 ms para evitar cortes; controles de mezcla sin cambios.
