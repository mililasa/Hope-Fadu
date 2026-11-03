# Canales 17 y 18 — motor y eco de posguerra

Secuencias ilimitadas por tiempo de juego, independientes de la posicion de Hope. Primera a los 5 s; siguientes con intervalo aleatorio entre 10 y 20 s desde el inicio anterior. Paneos de ambas voces: -0,5 y +0,5 alternados indefinidamente. No superpone secuencias entre si, incluso si se acortan los intervalos en configuracion. El reloj usa Time.deltaTime y se congela con timeScale=0 o pausa del Editor. Continuan durante todo el nivel; no hay limite de repeticiones.

Motor de 6,9 s, entrada 1,2 s, salida 2 s, ganancia 0,025119. Eco de 4,8 s a +5,15 s, entrada 0,06 s, salida 1,1 s, ganancia 0,044668. Valores de items Reaper V2; paneo sustituido por lo solicitado. WAV sin normalizar. Secuencia total 9,95 s. Master general omitido como el resto del audio del juego. Al morir/reiniciar se cancelan voces y reinicia la cuenta, con otra espera de 5 s.

Configuracion: Motor/Resources/HopeDistantSettings. WAV de eco en la carpeta hermana Eco lejano de posguerra. En Play las fuentes aparecen debajo de Pp. Probar primer disparo, mezcla motor-eco, esperas sin mover al personaje y checkpoint; ajustes de balance deben validarse escuchando.
