# CONTEXT.md — Unity Multiplayer Networking Lab (ServidorVideojuegos)

> Archivo de contexto para agentes IA. Léelo completo antes de operar sobre el repo.
> Última actualización: 2026-10-13. Idioma del proyecto: español.

## 0. TL;DR (qué es y en qué estado está)

- Proyecto **Unity 6000.6.0f1** que es un laboratorio universitario cliente-servidor.
- Compara el **mismo gameplay** implementado dos veces: con **NGO** (Netcode for GameObjects) y con **NFE** (Netcode for Entities / ECS).
- El objetivo final es llevar los Dedicated Servers a **Linux + Docker + Minikube/Kubernetes + Agones** con 4 clientes conectados.
- Hay un **baseline local** totalmente funcional (4 jugadores en un teclado) y **dos bases de red** con puntos de extensión marcados `STUDENT TODO`.
- **ESTADO CRÍTICO A VERIFICAR:** los 7 `STUDENT TODO` (NGO-01..03, NFE-01..04) **ya tienen código de solución escrito** en el repo (ver §7). Es decir, el "starter" parece haber sido resuelto por el estudiante. Los comentarios `STUDENT TODO` permanecen en el código pero la implementación ya está presente. No asumas que es un starter vacío.
- No existe carpeta `Builds/` (no se ha generado Dedicated Server). Infraestructura (Docker/K8s/Agones) está como plantillas, **no ejecutadas**.

## 1. Stack y versiones (verificado)

| Componente | Versión real |
|---|---|
| Unity Editor | `6000.6.0f1` (`ProjectSettings/ProjectVersion.txt`) |
| Render Pipeline | URP `17.6.0` |
| Input System | `1.20.0` |
| Multiplayer Play Mode | `3.0.0` |
| **NGO** (Netcode for GameObjects) | `2.13.3` — **paquete EMBEBIDO** en `Packages/com.unity.netcode.gameobjects/` (declara `com.unity.transport 2.6.0` internamente) |
| **NFE** (Netcode for Entities) | `6.6.0` (resuelto "builtin" en `packages-lock.json`) |
| Entities | `6.6.0` (resuelto; el `manifest.json` pide `1.4.8` pero el lock resuelve 6.6.0) |
| Unity Transport | `6.6.0` resuelto; `manifest.json` pide `2.7.4` |
| Dedicated Server pkg | `2.0.2` |
| AI Assistant / Inference | `2.19.0-pre.2` / `2.6.1` (presentes, no usados por el lab) |

> Discrepancia conocida y deliberada: `Packages/manifest.json` lista versiones antiguas (`entities 1.4.8`, `netcode 1.14.3`, `transport 2.7.4`) pero `packages-lock.json` resuelve 6.6.0 para el stack Entities/NFE. **No actualizar paquetes** sin abrir Unity y validar la matriz de compatibilidad.

## 2. Estructura de carpetas (solo material fuente relevante)

```
ServidorVideojuegos/
├── Assets/
│   ├── Input/LocalPlayers.inputactions        # 4 Action Maps (Player 1..4), Move + PowerUp
│   ├── Materials/                             # materiales generados por los builders
│   ├── Modelos/Barco/                         # Ship_01.fbx + texturas (material temático, no usado por el lab de red)
│   ├── Prefabs/
│   │   ├── NGOPlayer.prefab                    # Player de red NGO (NetworkObject+NetworkTransform+NgoPlayerNetwork)
│   │   ├── NFEPlayer.prefab                    # Ghost prefab NFE (NfePlayerAuthoring + GhostAuthoringComponent)
│   │   ├── NGOPlayer (Original).prefab         # backup
│   │   └── Ship_01.prefab, Ships_Pruebas.prefab# prefabs temáticos no usados por scripts de red
│   ├── Scenes/
│   │   ├── LocalGameplayBaseline.unity         # referencia local completa, 4 jugadores
│   │   ├── NGOGameplay.unity                   # escena NGO (NetworkManager + UnityTransport + UI)
│   │   ├── NFEGameplay.unity                   # escena NFE (cámara + UI + NfePresentationBridge + SubScene)
│   │   ├── NFE/NFEGameplaySubScene.unity       # SubScene ECS (spawner + Ghost prefab)
│   │   └── SampleScene.unity                   # sobrante de plantilla Unity
│   ├── Scripts/
│   │   ├── NetworkingLab.asmdef                # núcleo framework-neutral
│   │   ├── Core/                               # VACÍO
│   │   ├── Player/    PlayerInputSource.cs, PlayerController.cs
│   │   ├── PowerUp/   PlayerPowerUp.cs, PowerUpEvents.cs
│   │   ├── UI/        PowerUpMessageUI.cs
│   │   ├── NGO/       NgoConnectionUI.cs, NgoPlayerNetwork.cs  (+ NetworkingLab.NGO.asmdef)
│   │   ├── NFE/       NfeBootstrap.cs, NfeComponents.cs, NfeConnectionUI.cs,
│   │   │              NfeGameplaySystems.cs, NfePlayerAuthoring.cs,
│   │   │              NfePlayerSpawnerAuthoring.cs, NfePresentationBridge.cs  (+ NetworkingLab.NFE.asmdef)
│   │   └── Editor/    LocalBaselineSceneBuilder.cs, NetworkLabSceneBuilder.cs  (+ NetworkingLab.Editor.asmdef)
│   ├── Settings/  UI/  TutorialInfo/  SceneDependencyCache/
│   └── Tests/PlayMode/LocalBaselinePlayModeTests.cs (+ NetworkingLab.PlayModeTests.asmdef)
├── Docs/
│   ├── GUIA_LAB_UNITY_NETWORKING_LINUX.md      # guía oficial completa (1738 líneas, 29 secciones)
│   ├── GUIA_LAB_UNITY_NETWORKING_LINUX.docx    # misma guía en Word
│   ├── generate_guide_docx.py                  # convierte el .md a .docx (python-docx)
│   └── AUDITORIA_PROYECTO.md                   # auditoría del starter (2026-09-29)
├── Infrastructure/
│   ├── README.md                               # flujo amd64
│   ├── Containers/NGO/Dockerfile               # debian:13-slim, expone 7979/udp
│   ├── Containers/NFE/Dockerfile               # debian:13-slim, expone 7980/udp
│   ├── Kubernetes/Agones/ngo-gameserver.yaml   # GameServer UDP 7979, nodeSelector amd64
│   ├── Kubernetes/Agones/nfe-gameserver.yaml   # GameServer UDP 7980, nodeSelector amd64
│   ├── Scripts/build-images.sh                 # docker build + minikube image load (perfil "agones")
│   └── Scripts/install-minikube-agones.sh      # Minikube 1.39.0, K8s 1.34.6, Agones 1.60.0
├── Packages/  (manifest.json, packages-lock.json, com.unity.netcode.gameobjects/ embebido)
├── ProjectSettings/  (ProjectVersion.txt = 6000.6.0f1)
└── README.md
```

## 3. Arquitectura de gameplay (núcleo framework-neutral)

Flujo local (baseline), independiente de la red:

```
Teclado → Input System (LocalPlayers.inputactions)
       → PlayerInputSource.Movement (Vector2) + event PowerUpPressed
       → PlayerController.Move() (mueve transform, velocidad 5, bounds ±10.5 x / ±7.5 z)
       → PlayerPowerUp.ActivatePowerUp()
       → PowerUpEvents.RaiseActivated("Player N")   (evento estático global)
       → PowerUpMessageUI.ShowMessage()             (texto 2 s y se limpia)
```

**Reglas de gameplay compartidas (deben coincidir en las 3 versiones):**
- Velocidad de movimiento: `5f`.
- Límites: `x ∈ [-10.5, 10.5]`, `z ∈ [-7.5, 7.5]` (NFE usa `float2` con `y` = `z`).
- Colores/posiciones de spawn: 4 jugadores en `(-5,3) (5,3) (-5,-3) (5,-3)`, colores azul/rojo/verde/naranja.
- Velocidad de simulación servidor NGO: `Time.fixedDeltaTime`; NFE: `SystemAPI.Time.DeltaTime`.

Archivos clave:
- `Assets/Scripts/Player/PlayerInputSource.cs` — adapta InputActionAsset (`Move`, `PowerUp`) a `Movement`/`PowerUpPressed`.
- `Assets/Scripts/Player/PlayerController.cs` — `Move(Vector2, deltaTime)`, no conoce teclas ni red.
- `Assets/Scripts/PowerUp/PowerUpEvents.cs` — canal estático `static event Action<string> Activated`.
- `Assets/Scripts/UI/PowerUpMessageUI.cs` — suscrito a `PowerUpEvents`, muestra `"{player} activated PowerUp"` ~2 s.

## 4. NGO (Netcode for GameObjects)

- **Escena:** `Assets/Scenes/NGOGameplay.unity`. Contiene `NetworkManager` (con `UnityTransport`, `TickRate=30`, `EnableSceneManagement=true`, `PlayerPrefab=NGOPlayer.prefab`) + cámara + terreno + Canvas UI.
- **Puerto por defecto:** UDP **7979** (`NgoConnectionUI.DefaultPort`).
- **UI:** `NgoConnectionUI.cs` — campos Address/Port editables, botones Start Server / Start Host / Connect Client / Shutdown. Acepta args CLI `-address`, `-port`, `-client`. En `Application.isBatchMode` arranca servidor automáticamente.
- **Player:** `NGOPlayer.prefab` = capsule + `NetworkObject` + `NetworkTransform` (AuthorityMode=Server) + `NgoPlayerNetwork` + `TextMesh` label.
- **`NgoPlayerNetwork.cs`** (hereda `NetworkBehaviour`):
  - `NetworkVariable<int> playerNumber` — lectura Everyone, escritura Server. El servidor asigna número, posición, color y etiqueta en `OnNetworkSpawn`.
  - El owner-client habilita su Action Map (por defecto "Player 1").
  - `Update`: lee input, `ApplyLocalPreview(move)` (predicción local) y llama `SubmitMoveRpc(move)`.
  - `FixedUpdate`: aplica `pendingServerInput` sobre `transform.position` (autoridad de servidor) y `NetworkTransform` replica.
  - `SubmitMoveRpc` → `[Rpc(SendTo.Server, Delivery=Unreliable, InvokePermission=Owner)]`, valida con `ClampMagnitude`.
  - `OnPowerUpPerformed` → `RequestPowerUpRpc` → `AnnouncePowerUpRpc` (`[Rpc(SendTo.ClientsAndHost)]`) → `PowerUpEvents.RaiseActivated`.

## 5. NFE (Netcode for Entities / ECS)

- **Escenas:** `NFEGameplay.unity` (presentación + UI + `NfePresentationBridge`) y `NFE/NFEGameplaySubScene.unity` (SubScene con `NfePlayerSpawnerAuthoring` + Ghost prefab).
- **Puerto por defecto:** UDP **7980** (`NfeConnectionUI.DefaultPort`).
- **Worlds:** `NfeBootstrap.cs` (hereda `ClientServerBootstrap`). Solo activa NFE si la escena empieza por `NFE` o si hay `-nfe`. `AutoConnectPort=0`. Con `-client` crea solo ClientWorld; en batch mode solo ServerWorld; en Editor normal crea ambos.
- **Componentes** (`NfeComponents.cs`):
  - `NfePlayerTag : IComponentData`
  - `NfePlayerState : IComponentData` — `[GhostField] int PlayerNumber`, `[GhostField(Quantization=1000)] float2 Position`
  - `NfePlayerInput : IInputComponentData` — `float2 Move`, `InputEvent PowerUp`
  - `NfePlayerSpawner : IComponentData` — `Entity PlayerPrefab`
- **Authoring:** `NfePlayerAuthoring` (Baker añade Tag/State/Input); `NfePlayerSpawnerAuthoring` (Baker añade `NfePlayerSpawner` con el prefab).
- **Sistemas** (`NfeGameplaySystems.cs`):
  - `NfeRpcRegistrationSystem` — habilita `RpcCollection.DynamicAssemblyList`.
  - `NfeGoInGameClientSystem` — marca conexión `NetworkStreamInGame` y envía `NfeGoInGameRequest` (RPC).
  - `NfeGoInGameServerSystem` — recibe RPC, instancia prefab, asigna `GhostOwner{NetworkId}` + `NfePlayerState{PlayerNumber}`, enlaza a conexión con `LinkedEntityGroup`.
  - `NfeGatherInputSystem` — en `GhostInputSystemGroup`, lee WASD/Space y llena `NfePlayerInput` de entidades con `GhostOwnerIsLocal`. **Nota: hardcodea WASD/Space, no usa los 4 Action Maps.**
  - `NfeMovementSystem` — en `PredictedSimulationSystemGroup`, aplica `NfePlayerInput.Move` a `NfePlayerState.Position`.
  - `NfePowerUpServerSystem` — detecta `PowerUp.IsSet` y emite `NfePowerUpResultRpc` broadcast.
  - `NfePowerUpClientSystem` — consume `NfePowerUpResultRpc`, llama `PowerUpEvents.RaiseActivated` y destruye la entidad RPC.
- **Presentación:** `NfePresentationBridge.cs` — lee `NfePlayerState` del ClientWorld y crea cápsulas GameObject + labels (solo visual, sin autoridad).

## 6. Comparación rápida NGO vs NFE

| Aspecto | NGO | NFE |
|---|---|---|
| Escena | `NGOGameplay.unity` | `NFEGameplay.unity` + SubScene |
| Representación | GameObject + `NetworkBehaviour` | Entity + Components ECS |
| Player | `NGOPlayer.prefab` | Ghost `NFEPlayer.prefab` |
| Identidad | `NetworkVariable<int>` | `GhostField PlayerNumber` + `GhostOwner` |
| Input | Input System en `NetworkBehaviour` | `IInputComponentData` en ClientWorld |
| Movimiento | servidor + `NetworkTransform` | `PredictedSimulationSystemGroup` + snapshots |
| PowerUp | ServerRPC + ClientRPC broadcast | `IRpcCommand` + sistemas por World |
| Puerto | UDP 7979 | UDP 7980 |

## 7. Las 7 actividades `STUDENT TODO` (estado real del código)

> El código de solución ya está presente en el repo. Los comentarios `// STUDENT TODO ...` siguen ahí. Verifica si el usuario quiere el starter original (vaciado) o la solución.

| ID | Archivo | Qué implementa | Estado en repo |
|---|---|---|---|
| **NGO-01** | `NgoPlayerNetwork.cs` `Update` | `SubmitMoveRpc(move)` (input owner→servidor) | IMPLEMENTADO (línea ~71) |
| **NGO-02** | `NgoPlayerNetwork.cs` `FixedUpdate` | Movimiento autoritativo en servidor | IMPLEMENTADO (~74-85) |
| **NGO-03** | `NgoPlayerNetwork.cs` `OnPowerUpPerformed` | Request/Announce PowerUp por RPC | IMPLEMENTADO (~93-111) |
| **NFE-01** | `NfeConnectionUI.cs` `StartServer`/`ConnectClient` | `NetworkStreamDriver.Listen` / `.Connect` | IMPLEMENTADO (~79-101) |
| **NFE-02** | `NfeGameplaySystems.cs` GoInGame client/server | InGame + spawn Ghost + `GhostOwner` | IMPLEMENTADO (~40-94) |
| **NFE-03** | `NfeGameplaySystems.cs` `NfeMovementSystem` | Movimiento predicho | IMPLEMENTADO (~129-147) |
| **NFE-04** | `NfeGameplaySystems.cs` PowerUp server/client | RPC PowerUp + `PowerUpEvents` | IMPLEMENTADO (~149-197) |

Verificación de que siguen pendientes (issue abierto): confirmar con el usuario si esto debe revertirse al estado starter.

## 8. Controles locales (baseline)

| Jugador | Movimiento | PowerUp |
|---|---|---|
| Player 1 | W A S D | Space |
| Player 2 | Flechas | Right Ctrl |
| Player 3 | I J K L | O |
| Player 4 | Numpad 8 4 5 6 | Numpad 0 |

Definidos en `Assets/Input/LocalPlayers.inputactions` (generado por `LocalBaselineSceneBuilder.cs`).

## 9. Scene Builders (editor)

Dos utilidades en `Assets/Scripts/Editor/` (namespace `NetworkingLab.Editor`), accesibles por menú **`Networking Lab/...`**:

- **`LocalBaselineSceneBuilder.Build()`** — menú "Networking Lab/Build Local Baseline". Regenera `LocalPlayers.inputactions`, la escena `LocalGameplayBaseline.unity`, materiales y la añade a Build Settings.
- **`NetworkLabSceneBuilder.BuildAll()`** — menú "Networking Lab/Build NGO and NFE Scenes". Genera `NGOPlayer.prefab`, `NGOGameplay.unity`, `NFEPlayer.prefab`, `NFEGameplaySubScene.unity`, `NFEGameplay.unity` y configura Build Settings (orden: baseline, NGO, NFE).
- Son **builders programáticos**: las escenas/prefabs se pueden regenerar desde código; si editas escenas a mano, ten en cuenta que el builder borra y recrea.

## 10. Assemblies (asmdef)

| Assembly | Plataformas | Depende de |
|---|---|---|
| `NetworkingLab` | todas | InputSystem, ugui |
| `NetworkingLab.NGO` | todas | NetworkingLab, InputSystem, Netcode.Runtime, Netcode.Components, ugui |
| `NetworkingLab.NFE` | todas | NetworkingLab, Collections, Entities, Entities.Hybrid, Mathematics, NetCode, NetCode.Authoring.Hybrid, Networking.Transport, Scenes, Transforms, InputSystem, Jobs, ugui |
| `NetworkingLab.Editor` | **Editor** | los 3 anteriores + authoring/scenes editor |
| `NetworkingLab.PlayModeTests` | todas | NetworkingLab, InputSystem + TestAssemblies |

## 11. Tests

`Assets/Tests/PlayMode/LocalBaselinePlayModeTests.cs` (NUnit, PlayMode, carga escena `LocalGameplayBaseline`):
1. `SceneContainsFourPlayersGroundCameraAndUi` — 4 `PlayerController`, Ground, Camera, `PowerUpMessageUI`.
2. `EveryPlayerMovesFromItsConfiguredInput` — inyecta W/Up/I/Numpad8 y verifica desplazamiento.
3. `BindingsMatchTheFourLocalPlayers` — verifica los 16 bindings esperados.
4. `EveryConfiguredPowerUpInputIdentifiesPlayerAndMessageClears` — PowerUp por jugador + limpieza a los ~2 s.

Solo cubren el **baseline local**. No hay tests automáticos de NGO/NFE ni de infraestructura.

## 12. Infraestructura y despliegue

**Puertos:** NGO UDP **7979**, NFE UDP **7980**.

**Dockerfiles** (`Infrastructure/Containers/{NGO,NFE}/Dockerfile`): base `debian:13-slim`, instalan `ca-certificates libatomic1 libunwind8`, crean usuario `unity` (uid 10001), copian `Builds/NGO-LinuxServer/` o `Builds/NFE-LinuxServer/`, esperan el ejecutable exacto:
- NGO: `NetworkingLabNGOServer.x86_64` → ENTRYPOINT `-batchmode -nographics -port 7979`
- NFE: `NetworkingLabNFEServer.x86_64` → ENTRYPOINT `-batchmode -nographics -nfe -port 7980`

**Scripts:**
- `build-images.sh` — exige `uname -m == x86_64`; `docker build` de ambas imágenes y `minikube image load -p agones`.
- `install-minikube-agones.sh` — instala Minikube **1.39.0**, crea perfil `agones` (K8s **1.34.6**, 4 CPU, 6144 MB), instala Agones **1.60.0** vía manifiesto oficial en namespace `agones-system`.

**Manifiestos Agones:** `GameServer` con `portPolicy: Static`, `nodeSelector.kubernetes.io/arch: amd64`, imagen `networking-lab-{ngo,nfe}:local` + `IfNotPresent`, bloque `health`.
> El ciclo `Ready`/`Health` de Agones **NO está implementado** en el código Unity (falta SDK/REST `POST /ready` y `POST /health`). Es actividad de infraestructura del estudiante.

## 13. Limitación de arquitectura (importante)

- El target **Linux Server** de Unity produce binarios **ELF x86-64**.
- Imágenes y manifiestos son **linux/amd64**.
- En **Apple Silicon (ARM64)**, la VM Debian nativa es ARM64 y **no ejecuta** el binario x86-64. La ruta end-to-end completa solo funciona en **Windows x86-64 + Debian amd64**, o con un artefacto ARM64 aprobado por el docente (hay módulos `com.unity.sdk.linux-arm64` y `toolchain.*` instalados pero el lab no los usa).
- La guía exige demostrar el entorno correcto, no asumir emulación.

## 14. Flujo de laboratorio (Parte II de la guía)

`Unity Client → Dedicated Server → Debian en VirtualBox → Container → Kubernetes (Minikube) → Agones GameServer`

Pasos del estudiante: completar los 7 TODO → generar builds Linux Server (perfiles separados NGO y NFE) → preparar VM Debian + red Host-Only/NAT → SSH → Docker → Minikube → Agones → publicar endpoint (IP Host-Only de Debian, NO `localhost`) → conectar hasta 4 clientes con Multiplayer Play Mode.

Los **14 checkpoints** de la guía (sección 23) cubren: baseline, starter, VM/arquitectura, red, SSH, código de red, dedicated server, container, cluster, Agones, ruta al GameServer, 1 cliente, 4 clientes/ownership, estado compartido (movimiento + PowerUp).

## 15. Documentación oficial

- `README.md` (raíz) — resumen del lab, requisitos, escenas, controles, alcance.
- `Docs/GUIA_LAB_UNITY_NETWORKING_LINUX.md` — guía completa (fuente de verdad). Secciones 1–29.
- `Docs/GUIA_LAB_UNITY_NETWORKING_LINUX.docx` — versión Word generada desde el `.md`.
- `Docs/generate_guide_docx.py` — regenera el `.docx` desde el `.md` (requiere `python-docx`). Ejecutar: `python Docs/generate_guide_docx.py`.
- `Docs/AUDITORIA_PROYECTO.md` — auditoría (fecha 2026-09-29, PASS estático).
- `Infrastructure/README.md` — flujo amd64 y puertos.

## 16. Convenciones y advertencias para el agente

- **No añadir comentarios** al código salvo que se pida. El código ya usa XML-doc resumido.
- **No actualizar paquetes**: la combinación NGO embebido + NFE 6.6.0 es frágil.
- **No hay git en PATH** en este entorno Windows (verificado). No se puede usar `git` desde el shell.
- Carpetas `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/` son generadas e ignoradas por `.gitignore`; no son fuente.
- `.dockerignore` excluye `Library/Temp/Logs/obj/.git/Docs/UserSettings` y la doc del paquete NGO.
- Las escenas y prefabs de red se **regeneran** con los Scene Builders del editor; ediciones manuales pueden perderse al reconstruir.
- Al modificar movimiento: mantener velocidad `5` y bounds `±10.5 / ±7.5` coherentes entre baseline, NGO y NFE.
- Los `STUDENT TODO` están resueltos en el código; confirmar con el usuario la intención antes de "revertir" o "completar".

## 17. Problemas / deuda técnica conocidos

- Los TODO están implementados aunque la documentación los describe como pendientes.
- `NfeGatherInputSystem` solo lee WASD/Space del teclado (no respeta los 4 Action Maps del `.inputactions`), a diferencia del baseline.
- No hay perfil de build serializado ni `Builds/` (auditoría: `NOT_TESTED`).
- Ciclo de vida Agones (`Ready`/`Health`) sin integrar en Unity.
- `Assets/Scenes/SampleScene.unity` y prefabs/modelos temáticos (`Ship_01`, `Barco`) son sobrantes ajenos al lab de red.
- `Assets/Scripts/Core/` está vacío.
