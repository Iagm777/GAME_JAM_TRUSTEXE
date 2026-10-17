# TRUST.EXE — Flujo de pantallas, vidas compartidas, derrota, Núcleo y E.V.A.

## Cabeza

- Base: rama `work-integration` (origen `escena-vigilancia`), Unity 6000.5.5f1.
- Objetivo: cerrar los 5 pedidos del usuario sobre el juego jugable existente.

## Specs

- **S1 (Flujo)**: Orden de pantallas exacto: Menú → Selección de personaje → Historia → Tutorial → Escena Acceso → Escena Vigilancia → Escena Núcleo → Pantalla de Victoria; Pantalla de Derrota en caso de perder en cualquiera de los niveles.
- **S2 (Vidas)**: Vidas compartidas por nivel entre ambos jugadores. Al perderse todas → pantalla de Derrota con botones "Reintentar nivel" y "Volver al menú". Las vidas se reinician por nivel.
- **S3 (Bug personaje)**: Al elegir un personaje en la selección debe poder jugarse con ese personaje (reproducir, corregir causa raíz y verificar con ambos personajes y varias combinaciones).
- **S4 (Núcleo)**: Ventana por interruptor/nodo de energía 7 s → 20 s; límites de etapa +80 s (100→180, 90→170, 75→155 s). Al perder en Núcleo se abre Derrota y el texto debe especificar la causa exacta (tiempo agotado, integridad agotada, vidas agotadas).
- **S5 (E.V.A.)**: E.V.A. debe aparecer animada cada vez que se entrega una pista o cambio de instrucción a cualquier personaje (no solo el mensaje de la barra). Todos los escenarios deben tener sonido (música/efectos) y las pantallas de resultado también.

## Tasks

- T1: Fundación compartida (`Assets/01_Script/Core/`): `GameSession` (vidas compartidas por nivel, causa de derrota, flujo), escena `Resultado` (Victoria/Derrota) + setup de editor, componente presentador E.V.A. animado. (S1,S2,S5)
- T2: Integrar menú: orden de pantallas, reset de vidas, sonido de menú. (S1,S2)
- T3: Integrar Tutorial: vidas compartidas en nivel, reinicio limpio, E.V.A. animada en pistas, sonido. (S2,S5)
- T4: Integrar Acceso: vidas, E.V.A. animada en pistas/engaño, sonido. (S2,S5)
- T5: Integrar Vigilancia: vidas (alarma consume vida), Derrota con causa, E.V.A. animada, sonido. (S2,S5)
- T6: Integrar Núcleo: timers 20 s / +80 s, derrota con causa, victoria → pantalla Victoria, E.V.A. animada. (S1,S4,S5)
- T7: Fix bug de personaje seleccionado (causa raíz) + verificación multi-personaje. (S3)
- T8: Compilación limpia + validaciones de todas las escenas + build Windows. (S1-S5)

## Log

- L2: Contexto descubierto: main del repo está vacío; el juego completo vive en la rama `escena-vigilancia` (menú+selección+historia en `menu-y-seleccion-de-personajes`). Rama de trabajo: `work-integration`.
- L3: Licencia de Unity 6000.5.5f1 invalidada en la máquina (batch y GUI: "No valid Unity Editor license found", 0 entitlements). Compilación/validación en el editor QUEDAN PENDIENTES de que el usuario reactive la licencia en Unity Hub. Mientras tanto, autoré la escena `Resultado.unity` a mano + `.meta` con GUIDs consistentes.
- L4: Diseño de GameSession (TrustEXE.Core): 3 vidas compartidas por nivel (BeginLevel reinicia solo si cambia de nivel; RetryLevel de la pantalla de Derrota restaura vidas y recarga el sector). Derrota con causa exacta; Victoria con stats.
- L5: Derrota real solo en Vigilancia (alarma consume vida) y Núcleo (tiempo de etapa o integridad consumen vida). Tutorial/Acceso: solo muestran contador (fallos suaves).
- L6: Núcleo: ventana de energía 7s→20s, límites 100/90/75s→180/170/155s; NucleoValidation actualizado (expiry 30s/31s). Mensaje de "20 segundos" que ya decía 20 s coincide con la ventana real.
- L7: Fix del bug de personaje (T7): causa raíz clase = sustitución silenciosa (Tutorial usaba `?? assets.characters[p]`) y excepción en Acceso/Vigilancia/Núcleo (`.First()` lanza si el id no está en TutorialAssets compartido, p.ej. datos desactualizados). Ahora `CharacterFor()`/`IdentityFallback()` resuelven con fallback por rol (Mara/Ren) + `Debug.LogError` sonoro; `TutorialValidation` verifica invariante ids catálogo == ids personajes y resolución de selección; `TrustMenuSetup` valida propagación de selección arbitraria (Zoro/Nova) a LocalParty.
- L8: E.V.A. animada vía `EvaPresenter` (overlay uGUI en la mitad del jugador o centrada, frames 8fps, chime/tono, slide-in, fade; sin consumir input). Disparadores por escena en cada pista/aviso (spec L del usuario). Colisión de nombres detectada y corregida: método auxiliar `Eva(` renombrado a `EvaShow(` (la propiedad `Eva` ya existía).
- L9: El usuario autorizó subir e integrar todo al repo (`escena-*`, `menu-*`, `main` → commit integrado).
- Slice S1 (commit): fundación compartida (GameSession, EvaPresenter, Resultado scene + setup + build settings). Siguientes: S2 Tutorial+Acceso, S3 Vigilancia+Núcleo+fix personaje+docs.

- L1: Solicitud original del usuario: "revisa el archivo de instrucciones y el github y tenemos que hacer esto menu, seleccionar personaje, historia, tutorial, escena acceso, escena vigilancia, escena nucleo, pantalla de victoria, y pantalla de derrota en caso de que pierdan, en alguno de los niveles, 1. arreglar que se las pantallas en el siguiente orden: menu, seleccionar personaje, historia, tutorial, escena acseso, escena vigilancia, escena nucleo, pantalla de victoria, y pantalla de derrota en caso de q pierdan, en alguno de los niveles, 2. necesitamos agregar vidas compartidas por nivel, y un botón de reiniciar nivel o menu en caso de q pierdan. 3. hay un error q cuando escogemos un personaje, no me deja jugar con dicho personaje, 4. el juego del ultimo nivel q es escena núcleo, el tiempo es muy poco esta con 7 segundos para cada interruptor, y hay cierto tiempo para completar todo, quiero q sea un poco mas de tiempo tipo 20segundos por interruptor y para terminar toda las prueba unos 80 segundos mas, en caso q no se logro sale la ventana de perdiste y siempre q pierdes debe especificar por q perdiste 5. todos los escenarios deben tener sonido y E.V.A. debe aparecer mas seguido, tipo cada q se da un pista a cualquier personaje E.V.A. debe estar ahi animada, no como ahora solo el mensaje y ya, debe estar la animacion de E.V.A."