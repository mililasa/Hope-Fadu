# Hope: primera integracion de audio

Rama de trabajo: nahuel-audio. No requiere paquetes nuevos ni cambios manuales en SampleScene.

## Probar

1. Abrir SampleScene y esperar a que Unity importe los archivos y compile.
2. Pulsar Play. PlayerController2D agrega HopePlayerAudio al personaje durante el juego; la configuracion se carga desde Resources/HopeAudioSettings.
3. Caminar y detenerse: deben sonar variantes de tierra solo al desplazarse en el suelo. Saltar, hacer doble salto y aterrizar. Los pasos se suspenden en el aire y en la escalera. La cadencia inicial usa distancia recorrida; queda pendiente afinarla con los apoyos exactos de la animacion.
4. Probar los saltadores: usan tres variantes de rebote en lugar del salto normal.
5. Acercarse a un farol y pulsar E: chispa, encendido, motivo de luz y llama. Una nueva pulsacion sobre el farol ya encendido no repite la secuencia. La llama pierde volumen y cambia de panorama al alejarse. Se calcula la distancia al personaje en el plano 2D, sin depender de la distancia Z de la camara.
6. Caer o dejar que la mano capture al personaje: al volver al checkpoint se reinician las llamas y acciones; la musica mantiene continuidad. En la transicion de nivel la musica se desvanece y vuelve al retornar.
7. Salir de Play y volver a entrar: no deben quedar fuentes duplicadas de la sesion anterior.

## Ajustar

Seleccionar **Assets/HopeAudio/Resources/HopeAudioSettings.asset** en Project. Contiene niveles de Musica, Foleys y Mecanicas, clips y parametros de pasos/faroles. Ajustar fuera de Play para mantener un flujo claro de guardado. Es un asset compartido: cambios realizados sobre el asset durante Play tambien pueden persistir en el editor.

Esta tanda usa AudioSources y ganancias de configuracion. Todavia no crea un AudioMixer con buses/snapshots; los ambientes y las criaturas quedan para la siguiente tanda. Los niveles son un punto de partida para jugar y ajustar; no son una reconstruccion automatica de los efectos y automatizaciones de Reaper.

La musica toma un ciclo interior de 25.6 s del archivo musical usado en Reaper. La llama tiene una union circular con fundido. Los demas clips conservan los WAV fuente de la biblioteca. PROCEDENCIA.json registra cada origen. Los assets se importan inicialmente como PCM a su frecuencia original para esta prueba pequena; compresion y consumo de memoria se ajustaran segun la plataforma de entrega.

## Verificacion realizada

Los 12 scripts del proyecto compilan con las referencias de Unity 2022.3. Se verificaron los 15 WAV, las 16 referencias de la configuracion y la presencia de AudioListener en la escena. Falta comprobar reproduccion, balance y sincronizacion jugando en el editor. No se modificaron las escenas ni se hizo commit o push.

## Creditos para distribuir el juego

- **qubodup — Fire Loop**, https://opengameart.org/content/fire-loop, **CC BY 3.0**, https://creativecommons.org/licenses/by/3.0/ . Modificado mediante filtros, edicion, repeticiones y fundido circular para representar una llama pequena. Este credito debe conservarse en la distribucion del juego.
- **Kenney — Impact Sounds**, CC0: https://kenney.nl/assets/impact-sounds . Pasos, apoyos y lamina para rebotes; filtrados y combinados.
- **TinyWorlds — Different steps**, CC0: https://opengameart.org/content/different-steps-on-wood-stone-leaves-gravel-and-mud . Grava incorporada a pasos y apoyos.
- **dawith / qubodup — Zippo click sound**, CC0: https://opengameart.org/content/zippo-click-sound . Capa editada de chispa.
- **themightyglider / qubodup — Catching fire**, CC0: https://opengameart.org/content/catching-fire . Encendido filtrado.
- Pulso, motivo de luz, roce de salto y resonancia adicional de rebote: sintesis preparada para la maqueta de Hope.
