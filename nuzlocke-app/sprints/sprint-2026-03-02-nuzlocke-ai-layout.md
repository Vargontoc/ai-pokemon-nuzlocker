## Sprint Review [2026-03-02-Nuzlocke-AI-Layout] (Completed)

### Objetivos Completados
- [x] Diseño interactivo en `NuzlockeView.vue`.
- [x] Layout dividido en dos secciones derecha, izquierda.
- [x] Empezar por la renderización de la pantalla principal:
    - [x] En la sección izquierda cuadro de renderización totalmente transparente, donde el usuario pondra el juego que esta ejecutando.
    - [x] El cuadro de renderización estará rodeado de un marco con estilos modernos, de neon. Con ligeras animaciones.
    - [x] El cuadro junto al marco es sizable por parte del usuario tanto en ancho como en alto.
    - [x] El requisito principal es que pueda verse el juego en la pantalla principal.
    - [x] No hay llamadas externas ni nada, solo es un renderizado de pantalla.     

### Aprobación Sprint review
- [x] Los tests del chat y Layout pasan.

### Sugerencias para iteraciones futuras
- [ ] Vía Emulador Web Integrado (iFrame PiP): Cargar ROM dentro del navegador (EmulatorJS).
- [ ] Vía Desktop App (Electron.js / Tauri): Empaquetar la app web permitiendo `transparent: true` transparente real a nivel OS.
- [ ] TTS interactivo leyendo los mensajes en voz alta.
