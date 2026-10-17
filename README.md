# TRUST.EXE — juego cooperativo y escenario final Núcleo

Unity **6000.5.5f1**, Windows, Input System. Rama de trabajo: `work-integration` (integrada desde `escena-vigilancia` + `menu-y-seleccion-de-personajes`).

## Probar

1. Abrir este directorio como proyecto en Unity y esperar la importación.
2. Abrir `Assets/00_Scenes/MainMenu.unity` y pulsar Play. En la PC de desarrollo, Play también está configurado para iniciar esta escena desde SampleScene.
3. Pulsar PLAY. P1 elige con WASD y confirma con E; P2 usa flechas y Enter (también Enter numérico).
4. R deshace P1; Shift derecho deshace P2. Escape deshace confirmaciones o vuelve. Con ratón, asignarlo primero a P1/P2, elegir una tarjeta y pulsar CONFIRMAR / DESHACER.
5. Al confirmar dos avatares distintos se reproduce su entrada y comienza la historia. Espacio/Enter revela el texto o avanza. SALTAR INTRO llega a la pantalla de equipo listo.

## Incluido

- Los 12 avatares originales; elección simultánea con cursores independientes y roles físico/virtual.
- Animación Think, secuencia de caminata, entrada lateral, ampliación de impacto y banda con nombre durante 1,5 segundos al confirmar.
- E.V.A. animada con cuatro frames, fondo con líneas de datos, título flotante, transiciones narrativas y texto progresivo.
- Respuesta sonora sintetizada localmente, sin descargas ni dependencias adicionales.
- Interfaz de referencia 1600×900 centrada y adaptada al espacio disponible, con márgenes en otras proporciones.
- Catálogo serializado con referencias directas a las texturas originales. No mueve, recorta ni cambia los pivotes de los sprites del equipo.

## Flujo de pantallas

El orden es el solicitado: **Menú → Selección de personaje → Historia → Tutorial → Acceso → Vigilancia → Núcleo → Victoria**, con la pantalla de **Derrota** disponible cuando se pierden las vidas compartidas en cualquier nivel, y botones «Reintentar nivel» / «Volver al menú».

## Conectar el Tutorial

El proyecto incluye MainMenu, Tutorial, Acceso, Vigilancia, Núcleo y Resultado. Los escenarios se conectan mediante sus salidas cooperativas. La escena `Resultado` se abre automáticamente al ganar el Núcleo (Victoria) o al agotar las vidas compartidas (Derrota).

Las escenas están habilitadas en Build Profiles. Desde el menú se entra al Tutorial después de seleccionar la pareja y completar o saltar la introducción. Los checkpoints disponibles permiten continuar desde el sector más avanzado.

## Vidas compartidas y derrota

Cada nivel posee **3 vidas compartidas** para ambos jugadores (`TrustEXE.Core.GameSession`). En Vigilancia, ser detectado por E.V.A. (alarma) consume una vida; en Núcleo, agotar el tiempo de una etapa o colapsar la integridad consume una vida. Al llegar a cero, se abre la pantalla de **Derrota** especificando la causa exacta y ofreciendo «Reintentar nivel» (restaura vidas y reaparece en el sector) o «Volver al menú». Los niveles sin fallo fatal (Tutorial, Acceso) solo muestran el contador de vidas.

## E.V.A. animada

E.V.A. aparece **animada** (frames de Neutral/Warning/Alarm) sobre el jugador que recibe cada pista o aviso, mediante `TrustEXE.Core.EvaPresenter`, en lugar de limitarse al mensaje de la barra inferior. Conserva el pequeño retrato animado permanente de cada HUD.

El código de generación de jugadores debe leer `TrustEXE.Menu.LocalParty.Explorer` y `LocalParty.Operator`. Son los identificadores de avatar, independientes del rol. `IsReady` valida que hay una pareja. Se conservan entre escenas y se limpian al volver al inicio o iniciar otra sesión; no son un checkpoint persistente.

## Estructura

- `Assets/00_Scenes/MainMenu.unity`: escena inicial incluida en el build.
- `Assets/01_Script/Menu/TrustMenuController.cs`: interfaz, controles, animación, historia y transición.
- `TrustMenuCatalog.cs` y `MenuCatalog.asset`: datos de avatares y E.V.A.; estado local de la pareja.
- `Editor/TrustMenuSetup.cs`: reconstrucción explícita del catálogo y validación de flujo. El menú TRUST.EXE del editor permite abrir/preparar la escena. La interfaz se construye al ejecutar Play.

## Validación reproducible

Con MainMenu en Play Mode, escribir `validate` en `Library/TrustMenuRequest.txt`. El editor ejecuta comprobaciones de catálogo, navegación por los doce avatares, wrap, duplicados, bloqueo, deshacer, identidad de sesión, revelado/avance de historia, recorrido completo, salto y reinicio. El resultado se escribe en `Library/TrustMenuValidation.txt`; la prueba regresa al menú.

Comprobar también visualmente la entrada de ambos personajes, clic de tarjetas, teclado compartido, historia y salida a inicio. La prueba automatizada de lógica no verifica ghosting del teclado físico. Animaciones y tonos se ejecutan con tiempo no escalado.

La documentación maestra se usó como referencia narrativa y de roles. Sus prompts para construir todo el juego no forman parte de esta entrega.

## Escenario final Núcleo

Integrado en este mismo proyecto Unity. `Vigilancia` ofrece **ENTRAR AL NÚCLEO** al completar el ascensor. La pareja seleccionada se mantiene. También se puede abrir `Assets/00_Scenes/Nucleo.unity` para probar directamente (Mara y Ren si no hay selección).

- Energía cruzada: tres nodos, pulsos de ambos jugadores separados por un máximo de **20 segundos**, límite de etapa de **180 segundos**.
- Verificación: ambos consultan el registro de entrada. P1 comunica III > I > II y P2 lo introduce físicamente en sus nodos. La información de E.V.A. está marcada como caché antigua. Límite de **170 segundos**.
- Sincronización: ambos recorren III > I > II con pulsos separados por un máximo de 2,5 segundos. Límite de **155 segundos**.
- Láseres alternan libre, aviso de 1,5 segundos y daño; patrullas recorren el corredor. Cuatro impactos cuestan una vida compartida. Hay dos segundos de inmunidad tras daño y tres al reaparecer. Las etapas anteriores se conservan.
- Fallos: agotar el tiempo de etapa o la integridad consume una vida compartida; al agotarlas, la pantalla de Derrota indica el motivo exacto (tiempo agotado en la etapa de Energía/Verificación/Sincronización o colapso del Núcleo).
- Sala final: ambos eligen AISLAR o RESTAURAR. Pueden cambiar su decisión si no coincide. No hay límite de tiempo ni daño en esta sala.
- Victoria: al completar el Núcleo se abre la escena **Resultado** con la pareja elegida en primer plano, los diez personajes restantes al fondo, desenlace, tiempo, reinientos, consultas y cooperación; botón «Volver al menú».
- Animación direccional de los doce personajes, núcleo rotatorio, nodos pulsantes, patrullas, E.V.A. neutral/advertencia/alarma, música original a 150 BPM y efectos estéreo.

Controles: P1 WASD/E/Q/R; P2 flechas/Enter/Ctrl derecho/Shift derecho. Q y Ctrl derecho abren notas; el tiempo sigue corriendo. Escape pausa ambas capas. La pausa permite ajustar música y efectos, reintentar o recuperar. El menú principal da prioridad a CONTINUAR NÚCLEO si existe un checkpoint. El checkpoint de Núcleo es independiente de los sectores anteriores y se guarda al cambiar de etapa, reintentar y terminar; recuperar comienza en la entrada con tiempo e integridad renovados.

El menú del Editor `TRUST.EXE/Nucleo/Preparar escenario final` prepara recursos y escena sin reemplazar los existentes. Para validación: escribir `validate` en `Library/TrustNucleoRequest.txt`; `play` inicia Núcleo; `runtime` ejecuta el recorrido automatizado en Play y usa un guardado de prueba separado; `capture` captura la vista; `stop` termina Play; `menu` restablece MainMenu como inicio de Play; `build` compila todas las escenas habilitadas en `Builds/Nucleo/TRUST_EXE.exe`. Resultados en `Library/NucleoValidation.txt`, `Library/NucleoRuntimeValidation.txt` y `Library/NucleoBuild.txt`.
