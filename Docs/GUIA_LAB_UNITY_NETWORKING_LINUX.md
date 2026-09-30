# GUÍA DE LABORATORIO
## Unity Multiplayer Networking con Linux Kubernetes y Agones

> **Proyecto:** NetworkingExample Starter Project
> **Versión:** Unity 6000.6.0f1
> **Estado:** starter docente con baseline local y bases NGO y NFE
> **Fecha de actualización:** 23 de septiembre de 2026
> **Hosts contemplados:** macOS Apple Silicon y Windows x86-64

## Cómo usar esta guía

El repositorio no es una solución multiplayer terminada. Contiene un gameplay local completo, una base funcional de Netcode for GameObjects y una base ECS de Netcode for Entities. Siete marcadores `STUDENT TODO` señalan el trabajo de programación que completa el estudiante. El Dedicated Server, la máquina virtual, el contenedor, Kubernetes y Agones son pasos del laboratorio y no se han ejecutado ni generado en el starter.

Esta guía usa tres etiquetas:

- **YA IMPLEMENTADO:** existe en el repositorio y compila.
- **ACTIVIDAD DEL ESTUDIANTE:** el estudiante modifica o configura el proyecto.
- **PASO DE LABORATORIO:** se ejecuta fuera del starter y debe demostrarse con evidencia.

Los valores entre `< >`, como `<IP_VM>`, son marcadores. Deben sustituirse por valores observados en el equipo.

> [!IMPORTANT]
> El objetivo final sí es conectar clientes Unity a un Dedicated Server Linux administrado por Kubernetes y Agones. La infraestructura no está preconstruida porque forma parte del aprendizaje. La guía indica qué ya existe y qué debe completar el estudiante.

## 1. Objetivo y resultado final

El laboratorio compara dos implementaciones del mismo comportamiento: NGO, basado en GameObjects y componentes, y NFE, basado en ECS, Worlds, Systems y Ghosts. En ambos casos, el servidor debe validar el movimiento y comunicar el evento PowerUp a todos los clientes.

```text
MISMO COMPUTADOR FÍSICO
│
├── Unity Editor y clientes
│   ├── Cliente 1
│   ├── Cliente 2
│   ├── Cliente 3
│   └── Cliente 4
│          │
│          │ Address UDP Port
│          ▼
└── VirtualBox
    └── Debian
        └── Minikube Kubernetes
            └── Agones GameServer
                └── Unity Dedicated Server
```

Al finalizar, el estudiante deberá demostrar conexión, aparición de jugadores, ownership, movimiento replicado, PowerUp comunicado y UI consistente.

## 2. Arquitectura de CPU y limitación en Apple Silicon

| Host | Arquitectura del host | Guest VirtualBox | ISO Debian | Server Player del starter |
|---|---|---|---|---|
| iMac o MacBook Apple Silicon | ARM64 | ARM64 | `arm64` | Linux x86-64, no nativo |
| Windows Intel o AMD | x86-64 | x86-64 | `amd64` | Linux x86-64, compatible |

`amd64` es el nombre de Debian para x86-64 y funciona en procesadores Intel y AMD. VirtualBox 7.2 sobre macOS ARM ejecuta guests ARM; no convierte una VM ARM64 en una VM x86-64. El target Linux Server estándar disponible en este proyecto produce un ejecutable x86-64.

Consecuencia para el aula:

```text
iMac M4 ARM64
└── Debian ARM64 y Minikube ARM64        disponibles
    └── Unity Linux Server x86-64       no ejecutable de forma nativa
```

En Windows x86-64 puede completarse la ruta end to end descrita. En Apple Silicon se completan el starter, Debian, red, SSH, contenedores y orquestación ARM64, pero la ejecución del servidor requiere un artefacto Linux ARM64 proporcionado por el docente mediante una plataforma Unity compatible o un entorno x86-64 autorizado. No se debe ocultar esta incompatibilidad ni presentar emulación no validada como solución.

## 3. Estado real del repositorio

### 3.1 Versiones y paquetes

| Elemento | Estado real |
|---|---|
| Unity | 6000.6.0f1 |
| Render pipeline | URP 17.6.0 |
| Input System | 1.20.0 |
| Multiplayer Play Mode | 3.0.0 instalado |
| Netcode for GameObjects | 2.13.3, embebido localmente por compatibilidad de ensamblados |
| Netcode for Entities | 6.6.0 resuelto por Unity |
| Entities | 6.6.0 resuelto por Unity |
| Unity Transport | 2.7.4 solicitado, 6.6.0 resuelto con NFE |
| Dedicated Server package | 3.0.0 resuelto por Unity |
| Dedicated Server build | No generado; paso del estudiante |
| Docker Kubernetes Agones | Plantillas incluidas; no ejecutadas |

### 3.2 Escenas y prefabs

| Recurso | Propósito |
|---|---|
| `Assets/Scenes/LocalGameplayBaseline.unity` | Gameplay local de referencia |
| `Assets/Scenes/NGOGameplay.unity` | Base NGO con NetworkManager, UnityTransport y UI de conexión |
| `Assets/Scenes/NFEGameplay.unity` | Presentación y UI de la base NFE |
| `Assets/Scenes/NFE/NFEGameplaySubScene.unity` | SubScene ECS con spawner y Ghost prefab |
| `Assets/Prefabs/NGOPlayer.prefab` | Player GameObject de red |
| `Assets/Prefabs/NFEPlayer.prefab` | Ghost authoring para conversión a Entity |

### 3.3 Estructura relevante

```text
Assets
├── Input/LocalPlayers.inputactions
├── Prefabs/NGOPlayer.prefab y NFEPlayer.prefab
├── Scenes/LocalGameplayBaseline.unity
├── Scenes/NGOGameplay.unity
├── Scenes/NFEGameplay.unity
├── Scenes/NFE/NFEGameplaySubScene.unity
└── Scripts
    ├── Player PowerUp UI
    ├── NGO/NgoConnectionUI.cs
    ├── NGO/NgoPlayerNetwork.cs
    ├── NFE/NfeBootstrap.cs
    ├── NFE/NfeComponents.cs
    ├── NFE/NfeConnectionUI.cs
    ├── NFE/NfeGameplaySystems.cs
    └── NFE/NfePresentationBridge.cs
Infrastructure
├── Containers/NGO/Dockerfile y NFE/Dockerfile
├── Kubernetes/Agones/ngo-gameserver.yaml y nfe-gameserver.yaml
└── Scripts de instalación y construcción para estudio
```

### 3.4 Qué contiene cada carpeta

El repositorio está organizado para que el estudiante pueda separar el gameplay común de las dos tecnologías de red.

| Carpeta | Qué contiene | Por qué debe observarla |
|---|---|---|
| `Assets/Input` | El asset `LocalPlayers.inputactions` | Define acciones y bindings sin escribir teclas en el controlador |
| `Assets/Scenes` | Baseline, escena NGO, escena NFE y SubScene | Permite estudiar cada arquitectura por separado |
| `Assets/Prefabs` | Los Players preparados para NGO y NFE | Muestra cómo cada stack representa un jugador de red |
| `Assets/Scripts/Player` | Input y movimiento local | Es la referencia de comportamiento que las versiones de red deben conservar |
| `Assets/Scripts/PowerUp` | Activación y evento común | Separa la acción de gameplay de su presentación |
| `Assets/Scripts/UI` | Mensaje visible durante dos segundos | Recibe el evento sin conocer si nació localmente o en la red |
| `Assets/Scripts/NGO` | Conexión y Player basado en GameObjects | Punto de partida de las actividades NGO |
| `Assets/Scripts/NFE` | Bootstrap, componentes ECS, sistemas y presentación | Punto de partida de las actividades NFE |
| `Infrastructure` | Dockerfiles, scripts y manifiestos Agones | Material que utilizará el estudiante después de generar el servidor |
| `Docs` | Fuente Markdown y guía Word | Instrucciones oficiales del laboratorio |

Las carpetas `Library`, `Temp`, `Logs` y `Builds` no forman parte del material fuente. Unity o el estudiante las generan localmente y Git las ignora.

### 3.5 Escenas que recibe el estudiante

#### LocalGameplayBaseline

**Ruta:** `Assets/Scenes/LocalGameplayBaseline.unity`.

Esta escena demuestra el comportamiento que debe conservarse. Sus GameObjects principales son `Ground`, `Main Camera`, `UI`, `EventSystem` y `Player 1` a `Player 4`. Al presionar Play, los cuatro Players comparten el mismo teclado. No existe conexión de red en esta escena.

**Qué observar:** cada cápsula tiene color y nombre; el movimiento permanece dentro del terreno; el PowerUp muestra el jugador correcto y desaparece aproximadamente después de dos segundos.

#### NGOGameplay

**Ruta:** `Assets/Scenes/NGOGameplay.unity`.

La escena presenta el enfoque basado en GameObjects. Contiene `NetworkManager`, `UnityTransport`, una cámara, terreno, UI y el panel de conexión. El `NetworkManager` conoce `NGOPlayer.prefab` y puede iniciar Server, Host o Client. El movimiento autoritativo y el PowerUp distribuido permanecen incompletos de forma deliberada.

**Qué observar:** los campos Address y Port, el estado de conexión, los botones y los componentes de `NGOPlayer.prefab`.

#### NFEGameplay y NFEGameplaySubScene

**Rutas:** `Assets/Scenes/NFEGameplay.unity` y `Assets/Scenes/NFE/NFEGameplaySubScene.unity`.

La escena principal contiene la cámara, la UI, el panel de conexión y `NfePresentationBridge`. La SubScene contiene `NfePlayerSpawnerAuthoring` y la referencia al Ghost prefab. Unity convierte el contenido de la SubScene en Entities para los Worlds de NetCode.

**Qué observar:** la escena principal presenta información; la SubScene prepara los datos ECS. `NFEPlayer.prefab` no se controla como un Player GameObject tradicional durante la simulación.

### 3.6 Estado funcional confirmado

| Área | Estado | Explicación |
|---|---|---|
| Baseline Input movimiento PowerUp UI | IMPLEMENTED | Es la referencia local completa |
| NGO conexión identidad prefab y transporte | IMPLEMENTED | La infraestructura fundamental está preparada |
| NGO movimiento de red y PowerUp de red | PARTIAL | Corresponde a NGO-01 a NGO-03 |
| NFE Worlds componentes Ghost e input base | IMPLEMENTED | La estructura ECS real está preparada |
| NFE conexión spawn movimiento y PowerUp | PARTIAL | Corresponde a NFE-01 a NFE-04 |
| Dedicated Server build | NOT IMPLEMENTED | Lo genera el estudiante |
| VM container Kubernetes Agones GameServer | NOT IMPLEMENTED | Pasos de laboratorio no ejecutados |
| Address y Port editables | IMPLEMENTED | NGO usa 7979 y NFE 7980 como valores iniciales |

## 4. Gameplay local ya implementado

El baseline es el punto de partida porque utiliza conceptos que el estudiante ya conoce: escena, GameObject, componente, script y Play Mode. Antes de estudiar la red, ejecute `Assets/Scenes/LocalGameplayBaseline.unity` y confirme el comportamiento.

| Jugador | Movimiento | PowerUp |
|---|---|---|
| Player 1 | W A S D | Space |
| Player 2 | Flechas | Right Ctrl |
| Player 3 | I J K L | O |
| Player 4 | Numpad 8 4 5 6 | Numpad 0 |

### 4.1 Input System en palabras sencillas

Una **Input Action** describe una intención del jugador, por ejemplo `Move` o `PowerUp`. Un **binding** relaciona esa intención con una tecla concreta. Esta separación permite cambiar WASD por otras teclas sin reescribir el movimiento.

`Assets/Input/LocalPlayers.inputactions` contiene cuatro Action Maps. Cada mapa representa un jugador local y contiene las mismas acciones con bindings distintos.

```text
Tecla física
    ↓ binding
Input Action Move o PowerUp
    ↓ lectura del script
Gameplay
```

### 4.2 PlayerInputSource

#### Para qué sirve

Este script traduce Input System a datos sencillos que el gameplay puede consumir. Está en `Assets/Scripts/Player/PlayerInputSource.cs` y se utiliza en cada Player del baseline.

#### Información que guarda

Guarda el `InputActionAsset`, el nombre del Action Map y referencias resueltas a `Move` y `PowerUp`. No guarda una tecla específica dentro del código.

#### Acciones principales

```csharp
public Vector2 Movement => moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
public event Action PowerUpPressed;
```

`Movement` produce una dirección bidimensional. `PowerUpPressed` avisa que la acción ocurrió. El script no mueve la cápsula ni modifica la UI; esa separación permite reutilizar el gameplay con diferentes fuentes de input.

### 4.3 PlayerController

#### Para qué sirve

`Assets/Scripts/Player/PlayerController.cs` transforma la dirección recibida en desplazamiento sobre el plano. El componente está en los cuatro Players locales.

#### Qué ocurre en Play Mode

En cada frame lee `PlayerInputSource.Movement`, limita su magnitud, calcula un desplazamiento usando velocidad y tiempo, y mantiene la posición dentro de los límites del terreno.

```text
Teclado
  ↓
Input System
  ↓
PlayerInputSource.Movement
  ↓
PlayerController
  ↓
Transform.position local
```

El controlador no conoce NGO, NFE, ownership ni RPC. En las escenas de red, ese mismo problema se divide entre cliente y servidor.

### 4.4 PlayerPowerUp y PowerUpEvents

`Assets/Scripts/PowerUp/PlayerPowerUp.cs` escucha `PowerUpPressed`. Cuando se activa, llama `ActivatePowerUp` y publica el nombre del jugador mediante `PowerUpEvents`.

`Assets/Scripts/PowerUp/PowerUpEvents.cs` es un canal de eventos común:

```csharp
public static event Action<string> Activated;
```

El evento no sabe si la activación vino del teclado local, de un RPC NGO o de una entidad RPC NFE. Esto permite conservar la UI mientras cambia el origen de la información.

### 4.5 PowerUpMessageUI

`Assets/Scripts/UI/PowerUpMessageUI.cs` se suscribe a `PowerUpEvents.Activated`. Escribe `Player N activated PowerUp`, reinicia el temporizador si llega otro evento y borra el texto aproximadamente dos segundos después.

```text
PowerUp Input
    ↓
PlayerPowerUp.ActivatePowerUp
    ↓
PowerUpEvents.Activated
    ↓
PowerUpMessageUI
```

**Problema que resolverá la red:** este evento existe inicialmente dentro de un solo proceso. Cuando haya varios clientes, el servidor deberá comunicar el resultado para que cada proceso publique el mismo evento local de UI.

> [!CONCEPT]
> CONCEPTO CLAVE  Estado compartido. En palabras sencillas, varios procesos deben representar el mismo resultado del juego. En esta práctica, el servidor decide el movimiento aceptado y qué Player activó PowerUp; los clientes presentan esa decisión.

[CAPTURA SUGERIDA: escena LocalGameplayBaseline en Play Mode con los cuatro Players y el mensaje de PowerUp]

## 5. Base NGO incluida

NGO es la solución de networking de Unity basada en la forma tradicional de trabajar con GameObjects y Components. El starter incluye la estructura suficiente para estudiar la arquitectura sin resolver las tres actividades centrales.

### 5.1 Conceptos antes de abrir la escena

> [!CONCEPT]
> CONCEPTO CLAVE  Cliente y servidor. Un cliente lee input y presenta el juego. El servidor acepta conexiones, valida acciones y mantiene el estado compartido. Ambos son ejecuciones distintas del proyecto.

- **NetworkManager:** coordina el inicio de Server, Host o Client y registra prefabs de red.
- **UnityTransport:** envía y recibe los paquetes UDP utilizados por NGO.
- **NetworkObject:** da identidad de red a un GameObject.
- **NetworkBehaviour:** permite que un componente conozca si corre como cliente, servidor u owner y utilice APIs de red.
- **Ownership:** indica qué cliente puede originar el input de un objeto.
- **RPC:** llamada que cruza procesos; no es una llamada local ordinaria.
- **NetworkTransform:** replica a clientes el Transform decidido por la autoridad configurada.

### 5.2 NgoConnectionUI

#### Para qué sirve y dónde está

`Assets/Scripts/NGO/NgoConnectionUI.cs` está en la UI de `NGOGameplay.unity`. Es el punto visible donde el estudiante escribe Address y Port e inicia Server, Host o Client.

#### Información que guarda

Mantiene referencias a los `InputField` de Address y Port, al texto de estado y al texto del cliente local. Obtiene `NetworkManager.Singleton` en `Awake`.

#### Código relevante

```csharp
transport.SetConnectionData(address, ReadPort(), server ? "0.0.0.0" : null);
```

Para un cliente, `address` identifica la máquina del servidor y `ReadPort()` identifica el servicio UDP. Para el servidor, `0.0.0.0` significa escuchar en todas sus interfaces disponibles. El valor inicial es UDP 7979.

`StartServer`, `StartHost`, `ConnectClient` y `Shutdown` ya están implementados. También se admiten `-address`, `-port` y `-client` desde línea de comandos. El estudiante no debe reescribir esta UI para completar los TODO.

### 5.3 NGOPlayer prefab

`Assets/Prefabs/NGOPlayer.prefab` contiene representación visual, `NetworkObject`, `NetworkTransform` y `NgoPlayerNetwork`. `NetworkObject` permite que el servidor cree una instancia y que NGO relacione sus copias en todos los clientes.

### 5.4 NgoPlayerNetwork

#### Para qué sirve y dónde se utiliza

`Assets/Scripts/NGO/NgoPlayerNetwork.cs` hereda de `NetworkBehaviour` y está en `NGOPlayer.prefab`. Reúne identidad, lectura del input del owner y puntos de extensión de movimiento y PowerUp.

#### Información importante

```csharp
private readonly NetworkVariable<int> playerNumber = new(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
```

Todos pueden leer `playerNumber`, pero solo el servidor puede escribirlo. En `OnNetworkSpawn`, el servidor asigna un número, posición inicial, etiqueta y color. El cliente owner habilita el Action Map `Player 1`, que representa el input de ese proceso cliente.

#### Flujo al ejecutar

```text
NetworkManager crea NGOPlayer
    ↓
Servidor asigna playerNumber
    ↓
Owner habilita input
    ↓
NGO-01 envía dirección
    ↓
NGO-02 mueve en servidor
    ↓
NetworkTransform replica
```

#### Qué ya está implementado

Identidad, permisos de `NetworkVariable`, spawn position, input del owner, RPC receptor `SubmitMoveRpc`, límites, vista previa local y presentación.

#### Qué completa el estudiante

`NGO-01` invoca el RPC con el input; `NGO-02` aplica movimiento autoritativo en servidor; `NGO-03` sustituye la vista previa local de PowerUp por solicitud y broadcast.

> [!CONCEPT]
> CONCEPTO CLAVE  Ownership. Ser owner permite a un cliente originar el input de su Player. No significa que pueda decidir cualquier posición válida; el servidor conserva la decisión autoritativa.

[CAPTURA SUGERIDA: Inspector del GameObject NetworkManager y lista Network Prefabs en NGOGameplay]

## 6. Base NFE incluida

NFE resuelve el mismo laboratorio usando ECS. Para partir de algo conocido, un GameObject reúne datos y comportamiento en Components. En ECS, una Entity es un identificador; los Components contienen datos y los Systems procesan conjuntos de Entities.

### 6.1 Conceptos ECS en palabras sencillas

- **Entity:** identificador ligero de un elemento del juego.
- **Component:** datos asociados a una Entity. No es el mismo tipo de componente MonoBehaviour.
- **System:** código que consulta y modifica entidades con componentes concretos.
- **World:** conjunto aislado de entidades y sistemas. NFE utiliza ClientWorld y ServerWorld.
- **Ghost:** entidad cuya información NetCode replica entre servidor y clientes.
- **Snapshot:** representación del estado de Ghost enviada desde servidor.
- **Input command:** input del cliente asociado a ticks de simulación.

> [!CONCEPT]
> CONCEPTO CLAVE  World. ClientWorld y ServerWorld pueden existir en el mismo Editor para pruebas, pero representan responsabilidades distintas. En un Dedicated Server solo debe existir el mundo servidor.

### 6.2 NfeBootstrap

`Assets/Scripts/NFE/NfeBootstrap.cs` decide qué Worlds crear. Solo activa NFE si la escena comienza por `NFE` o si la línea de comandos contiene `-nfe`. Un proceso con `-client` crea ClientWorld; batch mode crea ServerWorld; el Editor normal crea ambos para pruebas locales.

`AutoConnectPort = 0` evita una conexión automática oculta. Por eso `NfeConnectionUI` es responsable de iniciar Listen o Connect de forma explícita.

### 6.3 NfeComponents

`Assets/Scripts/NFE/NfeComponents.cs` define los datos principales:

```csharp
public struct NfePlayerState : IComponentData
{
    [GhostField] public int PlayerNumber;
    [GhostField(Quantization = 1000)] public float2 Position;
}

public struct NfePlayerInput : IInputComponentData
{
    public float2 Move;
    public InputEvent PowerUp;
}
```

`GhostField` indica datos que deben aparecer en snapshots. La cuantización representa posición con una precisión definida en lugar de enviar precisión ilimitada. `IInputComponentData` identifica datos enviados como input de predicción.

`NfePlayerAuthoring` y su Baker convierten `NFEPlayer.prefab` en una Entity. `NfePlayerSpawnerAuthoring` convierte la referencia al prefab en `NfePlayerSpawner` dentro de la SubScene.

### 6.4 NfeConnectionUI

`Assets/Scripts/NFE/NfeConnectionUI.cs` está en `NFEGameplay.unity`. Prepara Address y UDP 7980, localiza ClientWorld o ServerWorld y muestra el `NetworkId` cuando existe conexión.

La creación del endpoint ya está implementada:

```csharp
NetworkEndpoint endpoint = NetworkEndpoint.Parse(addressInput.text.Trim(), ReadPort());
```

`NFE-01` completa las llamadas reales a `NetworkStreamDriver.Listen` y `Connect`. Esta actividad existe porque crear un endpoint no abre por sí mismo un socket ni establece una conexión.

### 6.5 NfeGameplaySystems

`Assets/Scripts/NFE/NfeGameplaySystems.cs` contiene sistemas separados por responsabilidad y World.

- `NfeRpcRegistrationSystem` habilita el registro dinámico de los RPC del starter.
- `NfeGoInGameClientSystem` y `NfeGoInGameServerSystem` forman `NFE-02`.
- `NfeGatherInputSystem` ya lee WASD y Space en el ClientWorld owner.
- `NfeMovementSystem` es el punto de `NFE-03` dentro de `PredictedSimulationSystemGroup`.
- `NfePowerUpServerSystem` y `NfePowerUpClientSystem` forman `NFE-04`.

```text
ClientWorld reúne NfePlayerInput
    ↓ comandos por tick
ServerWorld simula y actualiza NfePlayerState
    ↓ snapshots del Ghost
ClientWorld recibe estado
```

### 6.6 NfePresentationBridge

`Assets/Scripts/NFE/NfePresentationBridge.cs` lee `NfePlayerState` y crea cápsulas y etiquetas GameObject para que el estudiante vea el estado ECS. Esta presentación no sustituye la Entity ni ejecuta la autoridad de gameplay. Es un puente visual apropiado para un laboratorio sencillo.

### 6.7 Qué completa el estudiante

`NFE-01` abre y conecta endpoints; `NFE-02` entra en juego, hace spawn y asigna `GhostOwner`; `NFE-03` mueve en la simulación predicha; `NFE-04` comunica PowerUp mediante RPC Entities.

[CAPTURA SUGERIDA: Entities Hierarchy mostrando ClientWorld ServerWorld y un Ghost con GhostOwner]

## 7. Comparación de las bases

| Aspecto | NGO | NFE |
|---|---|---|
| Escena | `NGOGameplay.unity` | `NFEGameplay.unity` y SubScene |
| Representación | GameObject y componentes | Entity y componentes de datos |
| Player | `NGOPlayer.prefab` | Ghost `NFEPlayer.prefab` |
| Identidad | `NetworkVariable<int>` | `GhostField PlayerNumber` y `GhostOwner` |
| Input | Input System en `NetworkBehaviour` | `IInputComponentData` en ClientWorld |
| Movimiento | servidor más `NetworkTransform` | sistema predicho y Ghost replication |
| PowerUp | Server RPC y broadcast RPC | `IRpcCommand` y sistemas por World |
| Endpoint inicial | UDP 7979 | UDP 7980 |

Ninguna columna se presenta como mejor. La práctica busca que el estudiante observe cómo dos arquitecturas producen el mismo comportamiento.

## 8. Actividades de programación del estudiante

============================================================

A PARTIR DE AQUÍ COMIENZA EL TRABAJO DE IMPLEMENTACIÓN DEL ESTUDIANTE

============================================================

Existen exactamente siete identificadores `STUDENT TODO`. Cada actividad siguiente corresponde a un identificador único, aunque un mismo identificador aparezca en los lados cliente y servidor.

### ACTIVIDAD STUDENT TODO NGO-01 Enviar input del owner

**OBJETIVO:** enviar el Vector2 del cliente propietario al servidor.
**ARCHIVO:** `Assets/Scripts/NGO/NgoPlayerNetwork.cs`.
**COMPONENTE:** método `Update` y RPC `SubmitMoveRpc`.
**IDEA:** un cliente puede afirmar cualquier posición. Por eso no transmite su `Transform`; transmite la intención de movimiento. La autoridad para decidir la posición queda en el servidor.

**ANTES:** `Update` ya obtiene y limita `move`, pero no lo envía.

**ESCRIBA/MODIFIQUE:** después de calcular `move`, añada una sola línea.

```csharp
SubmitMoveRpc(move);
```

`SubmitMoveRpc` ya tiene `[Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, InvokePermission = RpcInvokePermission.Owner)]`.

- `SendTo.Server` cruza del proceso cliente al proceso servidor.
- `InvokePermission.Owner` impide que otro cliente invoque el RPC de un `NetworkObject` ajeno.
- `Unreliable` es apropiado para input continuo: si un paquete se pierde, el siguiente input más reciente lo reemplaza. No es apropiado para un evento único como PowerUp.
- El servidor limita de nuevo el vector en el RPC: la red nunca reemplaza la validación.

**VERIFIQUE:** inicie Host y un Client. Con un breakpoint o un log temporal en `SubmitMoveRpc`, confirme que el método se ejecuta en el servidor y que el valor recibido coincide con la dirección pulsada. Retire logs por frame antes de continuar.
**RESULTADO ESPERADO:** el servidor actualiza `pendingServerInput`.

### ACTIVIDAD STUDENT TODO NGO-02 Movimiento autoritativo

**OBJETIVO:** mover solo en servidor y replicar el resultado.
**ARCHIVO:** `Assets/Scripts/NGO/NgoPlayerNetwork.cs`.
**COMPONENTE:** `FixedUpdate`, `pendingServerInput`, `NetworkTransform`.
**IDEA:** el cliente pide; el servidor decide; `NetworkTransform` comparte la decisión. Mientras exista `ApplyLocalPreview`, el cliente owner verá una respuesta inmediata, pero el estado final sigue siendo el que replique el servidor.

**ANTES:** `FixedUpdate` sale si no es servidor y contiene el TODO.

**ESCRIBA/MODIFIQUE:** reemplace el TODO por este bloque.

```csharp
Vector2 move = Vector2.ClampMagnitude(pendingServerInput, 1f);
Vector3 next = transform.position +
    new Vector3(move.x, 0f, move.y) * (moveSpeed * Time.fixedDeltaTime);
next.x = Mathf.Clamp(next.x, -horizontalBounds.x, horizontalBounds.x);
next.z = Mathf.Clamp(next.z, -horizontalBounds.y, horizontalBounds.y);
transform.position = next;
```

- `pendingServerInput` es la última intención aceptada del owner.
- `ClampMagnitude` evita que una diagonal o un dato alterado supere velocidad 1.
- `Time.fixedDeltaTime` hace que la simulación del servidor use el paso fijo de física.
- Los dos `Mathf.Clamp` conservan los límites del baseline local.
- Al escribir el `Transform` desde el servidor, el `NetworkTransform` del prefab replica la posición a los clientes.

**VERIFIQUE:** abra Host y Client. Mueva el jugador del Client; ambas ventanas deben mostrar la misma posición final. Intente mantener una dirección contra el borde: el jugador no debe atravesarlo. Pruebe que un cliente no puede mover el jugador del otro.
**RESULTADO ESPERADO:** todos los clientes observan la posición autoritativa.

### ACTIVIDAD STUDENT TODO NGO-03 PowerUp por RPC

**OBJETIVO:** sustituir el evento local por solicitud, validación y broadcast.
**ARCHIVO:** `Assets/Scripts/NGO/NgoPlayerNetwork.cs`.
**COMPONENTE:** `OnPowerUpPerformed` y nuevos RPC.
**IDEA:** PowerUp es un evento único. Debe usar entrega fiable y el servidor debe ser quien anuncie el resultado a todos los procesos.

**ESCRIBA/MODIFIQUE:** sustituya el cuerpo de `OnPowerUpPerformed` y agregue estos dos métodos dentro de `NgoPlayerNetwork`.

```csharp
private void OnPowerUpPerformed(InputAction.CallbackContext context)
{
    RequestPowerUpRpc();
}

[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
private void RequestPowerUpRpc()
{
    if (!IsSpawned || playerNumber.Value <= 0)
    {
        return;
    }

    AnnouncePowerUpRpc(playerNumber.Value);
}

[Rpc(SendTo.Clients)]
private void AnnouncePowerUpRpc(int number)
{
    PowerUpEvents.RaiseActivated($"Player {number}");
}
```

- El owner solicita la acción; ya no actualiza directamente su UI.
- La validación ocurre en el servidor, que es el único que ejecuta `RequestPowerUpRpc`.
- El RPC de servidor a clientes transporta el número ya asignado por el servidor.
- Cada cliente recibe `AnnouncePowerUpRpc` y publica su evento local para su propia `PowerUpMessageUI`.

**VERIFIQUE:** conecte dos o más clientes. Pulse PowerUp solo en uno y observe un mensaje idéntico, una sola vez, en cada ventana; debe desaparecer tras dos segundos.
**RESULTADO ESPERADO:** cuatro clientes muestran `Player N activated PowerUp` durante unos dos segundos.

### ACTIVIDAD STUDENT TODO NFE-01 Listen y Connect

**OBJETIVO:** abrir el endpoint del ServerWorld y conectar el ClientWorld.
**ARCHIVO:** `Assets/Scripts/NFE/NfeConnectionUI.cs`.
**COMPONENTE:** `StartServer` y `ConnectClient`.
**IDEA:** crear un `NetworkEndpoint` solo describe una dirección. `Listen` crea el socket del servidor y `Connect` inicia una conexión desde el cliente.

**ESCRIBA/MODIFIQUE:** después del texto de estado, añada en cada método el bloque correspondiente.

```csharp
// StartServer
server.EntityManager.GetComponentDataRW<NetworkStreamDriver>()
    .ValueRW.Listen(NetworkEndpoint.AnyIpv4.WithPort(ReadPort()));

// ConnectClient
client.EntityManager.GetComponentDataRW<NetworkStreamDriver>()
    .ValueRW.Connect(client.EntityManager, endpoint);
```

- `ServerWorld` y `ClientWorld` son entidades de ejecución distintas, aunque el editor pueda contener ambas.
- `AnyIpv4` escucha en las interfaces IPv4 disponibles del proceso servidor.
- `endpoint` usa Address y Port de la UI; en una VM, Address debe ser la IP Host-Only de Debian, no `127.0.0.1` del host.
- `NetworkStreamDriver` es el componente que posee el transporte de NFE en ese World.

**VERIFIQUE:** inicie Host. La UI debe mostrar un `Local client ID`; en Linux, `ss -lunp` debe mostrar UDP 7980 cuando el TODO de servidor esté activo.
**RESULTADO ESPERADO:** aparece `NetworkId` en ClientWorld.

### ACTIVIDAD STUDENT TODO NFE-02 GoInGame y spawn

**OBJETIVO:** marcar la conexión InGame y crear un Ghost con owner.
**ARCHIVO:** `Assets/Scripts/NFE/NfeGameplaySystems.cs`.
**COMPONENTE:** `NfeGoInGameClientSystem` y `NfeGoInGameServerSystem`.
**IDEA:** estar conectado no significa estar jugando. `NetworkStreamInGame` habilita el flujo de Ghosts. El servidor crea el jugador y define quién es su dueño.

**ESCRIBA/MODIFIQUE:** en el sistema cliente, para cada conexión sin `NetworkStreamInGame`, cree una entidad RPC y marque la conexión.

```csharp
EntityCommandBuffer ecb = new(Allocator.Temp);
foreach (var (_, connection) in SystemAPI.Query<RefRO<NetworkId>>()
             .WithEntityAccess().WithNone<NetworkStreamInGame>())
{
    ecb.AddComponent<NetworkStreamInGame>(connection);
    Entity request = ecb.CreateEntity();
    ecb.AddComponent(request, new NfeGoInGameRequest());
    ecb.AddComponent(request, new SendRpcCommandRequest { TargetConnection = connection });
}
ecb.Playback(state.EntityManager);
ecb.Dispose();
```

En el sistema servidor, use el mismo `EntityCommandBuffer` y el siguiente patrón. Antes del bucle, obtenga `Entity prefab = SystemAPI.GetSingleton<NfePlayerSpawner>().PlayerPrefab;` y `ComponentLookup<NetworkId> networkIds = state.GetComponentLookup<NetworkId>(true);`.

```csharp
foreach (var (request, rpcEntity) in SystemAPI
             .Query<RefRO<ReceiveRpcCommandRequest>>()
             .WithAll<NfeGoInGameRequest>().WithEntityAccess())
{
    Entity connection = request.ValueRO.SourceConnection;
    int networkId = networkIds[connection].Value;
    ecb.AddComponent<NetworkStreamInGame>(connection);
    Entity player = ecb.Instantiate(prefab);
    ecb.SetComponent(player, new GhostOwner { NetworkId = networkId });
    ecb.SetComponent(player, new NfePlayerState { PlayerNumber = networkId });
    ecb.AppendToBuffer(connection, new LinkedEntityGroup { Value = player });
    ecb.DestroyEntity(rpcEntity);
}
```

Actualice `networkIds` con `networkIds.Update(ref state);` antes del bucle. `AppendToBuffer` liga el Ghost a la conexión: NFE lo destruye cuando esa conexión desaparece.

- El cliente solicita entrar; no crea su propio jugador de red.
- `ReceiveRpcCommandRequest` informa al servidor qué conexión originó la solicitud.
- `GhostOwner.NetworkId` permite que NFE añada `GhostOwnerIsLocal` solo en el cliente adecuado.
- El número de jugador debe originarse en el servidor y formar parte de `NfePlayerState`, que ya está marcado con `GhostField`.

**VERIFIQUE:** conecte dos clientes. Entities Hierarchy debe mostrar un Ghost por conexión, y cada cliente debe tener `GhostOwnerIsLocal` únicamente en su propio Ghost.
**RESULTADO ESPERADO:** un Ghost distinto por conexión.

### ACTIVIDAD STUDENT TODO NFE-03 Movimiento predicho

**OBJETIVO:** aplicar `NfePlayerInput.Move` dentro de la simulación predicha.
**ARCHIVO:** `Assets/Scripts/NFE/NfeGameplaySystems.cs`.
**COMPONENTE:** `NfeMovementSystem`.
**IDEA:** el owner predice para no esperar la respuesta de red. El servidor ejecuta la misma regla y el snapshot autoritativo corrige diferencias.

**ESCRIBA/MODIFIQUE:** mantenga el sistema en `PredictedSimulationSystemGroup` y aplique este patrón.

```csharp
foreach (var (input, player) in SystemAPI
             .Query<RefRO<NfePlayerInput>, RefRW<NfePlayerState>>()
             .WithAll<Simulate>())
{
    float2 move = math.normalizesafe(input.ValueRO.Move);
    float2 next = player.ValueRO.Position + move * (5f * SystemAPI.Time.DeltaTime);
    next.x = math.clamp(next.x, -10.5f, 10.5f);
    next.y = math.clamp(next.y, -7.5f, 7.5f);
    player.ValueRW.Position = next;
}
```

- `Simulate` limita la consulta a Ghosts que este tick debe simular.
- `IInputComponentData` hace disponible el mismo input para predicción y servidor.
- Las velocidades y límites coinciden con `PlayerController` para que las dos versiones representen el mismo gameplay.
- `GhostField` en `NfePlayerState` hace que el servidor envíe snapshots de esta posición.

**VERIFIQUE:** conecte dos clientes. Cada persona solo controla su Ghost; ambos ven los movimientos converger y ningún Ghost puede salir del terreno.
**RESULTADO ESPERADO:** cliente propietario predice y servidor confirma el estado.

### ACTIVIDAD STUDENT TODO NFE-04 PowerUp RPC

**OBJETIVO:** convertir el InputEvent en un resultado de servidor para todos los clientes.
**ARCHIVO:** `Assets/Scripts/NFE/NfeGameplaySystems.cs`.
**COMPONENTE:** `NfePowerUpServerSystem`, `NfePowerUpClientSystem` y `NfePowerUpResultRpc`.
**IDEA:** un `InputEvent` solo pide la acción. El servidor emite `NfePowerUpResultRpc`; cada cliente convierte ese resultado de red en su evento local de UI.

**ANTES:** añada `using NetworkingLab.PowerUp;` al inicio del archivo para que el sistema cliente pueda publicar el evento común.

**ESCRIBA/MODIFIQUE:** en servidor, consulte `NfePlayerInput` y `NfePlayerState`; cuando `PowerUp.IsSet`, cree una entidad con `NfePowerUpResultRpc` y `SendRpcCommandRequest`.

```csharp
EntityCommandBuffer ecb = new(Allocator.Temp);
foreach (var (input, player) in SystemAPI
             .Query<RefRO<NfePlayerInput>, RefRO<NfePlayerState>>()
             .WithAll<Simulate>())
{
    if (!input.ValueRO.PowerUp.IsSet)
    {
        continue;
    }

    Entity rpc = ecb.CreateEntity();
    ecb.AddComponent(rpc, new NfePowerUpResultRpc
    {
        PlayerNumber = player.ValueRO.PlayerNumber
    });
    ecb.AddComponent(rpc, new SendRpcCommandRequest
    {
        TargetConnection = Entity.Null,
        BroadcastTargets = RpcBroadcastTargets.All
    });
}
ecb.Playback(state.EntityManager);
ecb.Dispose();
```

En el cliente, el consumo y limpieza es:

```csharp
EntityCommandBuffer ecb = new(Allocator.Temp);
foreach (var (result, rpcEntity) in SystemAPI
             .Query<RefRO<NfePowerUpResultRpc>>()
             .WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
{
    PowerUpEvents.RaiseActivated($"Player {result.ValueRO.PlayerNumber}");
    ecb.DestroyEntity(rpcEntity);
}
ecb.Playback(state.EntityManager);
ecb.Dispose();
```

- El servidor decide el número incluido en el resultado; el cliente no lo inventa.
- La entidad RPC es temporal: destruirla después de consumirla evita reproducir el mismo evento en el siguiente frame.
- `PowerUpEvents` conserva el desacoplamiento con `PowerUpMessageUI`: la UI no necesita saber si el resultado nació en NGO o NFE.

**VERIFIQUE:** pulse Espacio en un cliente. Cada ventana conectada debe mostrar una sola vez el mismo Player y limpiar el mensaje después de dos segundos.
**RESULTADO ESPERADO:** UI idéntica en todos los clientes sin duplicados.

## Parte II Implementación del laboratorio por el estudiante

Hasta aquí se explicó el proyecto entregado. A partir de este punto el estudiante prepara el entorno Linux, completa el networking y demuestra el resultado. Cada nueva herramienta resuelve un problema concreto:

```text
Unity Client
    ↓ necesita otra ejecución que mantenga el estado
Dedicated Server
    ↓ debe ejecutarse en Linux aislado del host
Debian en VirtualBox
    ↓ debe empaquetarse de forma repetible
Container
    ↓ debe ser administrado
Kubernetes
    ↓ necesita funciones específicas de game server
Agones GameServer
```

No avance solo porque un comando terminó. En cada etapa interprete el resultado y complete el checkpoint correspondiente.

## 9. Preparación de VirtualBox

Nuestro servidor necesita Linux, pero no otro computador físico. Una máquina virtual permite ejecutar Debian dentro del host. El host es macOS o Windows; el guest es Debian; VirtualBox es el hipervisor que asigna CPU virtual, RAM, disco y adaptadores de red. Una máquina virtual simula otro computador utilizando recursos del host. RAM, CPU y disco asignados a la VM dejan de estar disponibles total o parcialmente para el host mientras la VM está encendida.

VirtualBox es un hipervisor de escritorio. La descarga oficial es [Oracle VirtualBox Downloads](https://www.virtualbox.org/wiki/Downloads). La versión verificada al redactar esta guía fue 7.2.20. Use la última versión estable 7.2 disponible para el aula y no versiones Beta/RC.

### macOS — Apple Silicon

1. Descargue **macOS / Apple Silicon hosts**.
2. Abra el `.dmg` y ejecute el instalador.
3. Autorice los componentes solicitados por macOS.
4. Reinicie si el instalador lo requiere.
5. Abra VirtualBox y compruebe **Help > About VirtualBox**.

No descargue `macOS / Intel hosts` para un iMac M4.

[CAPTURA: página oficial de VirtualBox]

[CAPTURA: selección macOS Apple Silicon]

### Windows — x86-64

1. Descargue **Windows hosts**.
2. Ejecute el instalador como usuario con privilegios de instalación.
3. Mantenga habilitados los componentes de red; Host-Only depende de ellos.
4. Acepte la interrupción breve de red que advierte el instalador.
5. Abra VirtualBox y compruebe **Help > About VirtualBox**.

[CAPTURA: selección Windows]


---

## 10. Descarga de Debian

Debian es una distribución Linux. Usamos la edición estable porque prioriza cambios controlados y soporte predecible. Una ISO es la imagen de instalación que VirtualBox conecta como si fuera un disco. Debian estable verificado: **Debian 13 “trixie”**, actualización 13.7. Use una imagen `netinst`: es pequeña y descarga paquetes durante la instalación.

Fuentes oficiales:

- [Información de Debian estable](https://www.debian.org/releases/stable/)
- [Medios oficiales de instalación](https://www.debian.org/CD/)
- [Manual de instalación](https://www.debian.org/releases/stable/installmanual.en.html)

### macOS — Apple Silicon

Descargue la ISO `debian-13.x.x-arm64-netinst.iso` desde el directorio oficial de imágenes **arm64**. No descargue `amd64`: VirtualBox en Apple Silicon no ejecuta guests x86.

### Windows — x86-64

Descargue `debian-13.x.x-amd64-netinst.iso` desde el directorio oficial de imágenes **amd64**.

> [!WARNING]
> Verifique siempre las letras de arquitectura en el nombre del archivo antes de crear la VM.

---

## 11. Creación de la máquina virtual

Configuración recomendada:

| Parámetro | Valor | Explicación |
|---|---:|---|
| Nombre | `Debian-Unity-Server` | Identifica la VM |
| Tipo | Linux / Debian 64-bit | Sistema guest |
| CPU | 2 vCPU | Suficiente para el laboratorio inicial |
| RAM | 4 GB | Cómodo para Debian mínimo y futuro servidor; 2 GB es el mínimo práctico |
| Disco | 25 GB dinámico | Crece al usarlo, hasta el máximo |
| Adaptador 1 | NAT | Permite a Debian salir a Internet |
| Adaptador 2 | Host-Only Network | Comunicación host ↔ guest sin depender de Wi-Fi |

Pasos:

1. Seleccione **New**.
2. Nombre la VM `Debian-Unity-Server`.
3. Seleccione la ISO correcta.
4. Desactive instalación desatendida para seguir manualmente la práctica.
5. Asigne 4096 MB de RAM y 2 CPU sin entrar en la zona roja del indicador.
6. Cree un disco virtual de 25 GB, asignación dinámica.
7. Finalice, pero no inicie todavía.

### Configurar las dos interfaces

1. Abra **File > Tools > Network**.
2. Cree una **Host-Only Network** si no existe.
3. Mantenga DHCP habilitado. Un rango típico de VirtualBox es `192.168.56.0/24`, pero registre el rango real mostrado.
4. Abra **Settings > Network** de la VM.
5. Adapter 1: habilitado, Attached to **NAT**.
6. Adapter 2: habilitado, Attached to **Host-Only Network**.
7. En macOS reciente elija **Host-Only Network**, no la opción heredada Host-Only Adapter.

## 12. Instalación mínima de Debian

1. Inicie la VM.
2. Si se solicita un disco, seleccione la ISO descargada.
3. En el menú elija **Install** o **Graphical install**. Ambos producen el mismo sistema; Graphical install facilita el uso inicial.
4. Seleccione idioma, país y teclado que correspondan al estudiante.
5. Espere la detección de red.
6. Hostname: `unity-server`.
7. Domain name: déjelo vacío para este laboratorio.
8. Cuando se solicite la contraseña de `root`, déjela vacía. El instalador deshabilitará el acceso directo de `root` y permitirá al primer usuario administrar con `sudo`, que es el método usado en esta guía.
9. Cree un usuario normal, por ejemplo `student`. Use una contraseña de laboratorio que no reutilice en otros servicios.
10. Particionado: **Guided — use entire disk**. El “entire disk” es el disco virtual de 25 GB, no el disco físico del host.
11. Seleccione **All files in one partition**.
12. Confirme **Finish partitioning and write changes to disk**.
13. Configure un mirror de Debian cuando el instalador lo solicite.
14. En selección de software:
    - desmarque **Debian desktop environment** y cualquier escritorio;
    - marque **SSH server**;
    - marque **standard system utilities**.
15. Instale GRUB en el disco virtual cuando se solicite.
16. Finalice y reinicie.
17. Si regresa al instalador, apague la VM, retire la ISO de la unidad óptica virtual y vuelva a iniciar.
18. Inicie sesión con el usuario normal.

Una instalación sin escritorio consume menos RAM, disco y CPU. El servidor no necesita ventanas; además, la terminal hace visibles los procesos, puertos y logs que se estudian.


---

## 13. Primera introducción a la terminal Linux

El prompt no se copia. En `student@unity-server:~$`, el símbolo `$` indica un usuario normal. Los comandos distinguen mayúsculas de minúsculas.

### `pwd`

**QUÉ HACE:** muestra el directorio actual.
**COMANDO:**

```bash
pwd
```

**EJEMPLO:** `/home/student`

### `ls` y `ls -la`

**QUÉ HACEN:** `ls` lista archivos; `-l` muestra detalles y permisos; `-a` incluye nombres ocultos.

```bash
ls
ls -la
```

### `cd`

**QUÉ HACE:** cambia de directorio. `~` representa el home del usuario y `..` el directorio padre.

```bash
cd ~
cd ..
```

### `mkdir`

**QUÉ HACE:** crea directorios.

```bash
mkdir -p ~/server-build
```

`-p` crea los directorios intermedios necesarios y no falla si ya existen.

### `cp`

**QUÉ HACE:** copia. `-r` copia un directorio y su contenido.

```bash
cp archivo.txt copia.txt
cp -r carpeta carpeta-copia
```

### `mv`

**QUÉ HACE:** mueve o renombra.

```bash
mv nombre-antiguo.txt nombre-nuevo.txt
```

### `rm`

**QUÉ HACE:** elimina archivos. La terminal normalmente no ofrece papelera.

```bash
rm archivo-temporal.txt
```

> [!WARNING]
> `rm` es destructivo. Compruebe `pwd` y `ls` antes de usarlo. No use `rm -rf` en esta práctica.

### `cat`

**QUÉ HACE:** imprime el contenido de un archivo de texto; es útil para logs pequeños.

```bash
cat archivo.log
```

### `less` y `nano`

**QUÉ HACEN:** `less` lee archivos largos por páginas; `nano` es un editor de texto sencillo en la terminal.

```bash
less archivo-largo.log
nano notas-del-laboratorio.txt
```

En `less`, use las flechas o Espacio para avanzar y `q` para salir. En `nano`, la banda inferior muestra atajos; `Ctrl+O` guarda, Enter confirma el nombre y `Ctrl+X` sale. Los usaremos para leer logs y editar archivos de configuración pequeños. Antes de modificar, haga una copia con `cp`.

### `clear`

**QUÉ HACE:** limpia visualmente la terminal; no borra archivos ni historial.

```bash
clear
```

### `whoami`, `hostname` y `uname`

```bash
whoami
hostname
uname -m
```

- `whoami`: usuario actual.
- `hostname`: nombre de la máquina.
- `uname -m`: arquitectura. Debe mostrar `aarch64` en Debian ARM64 o `x86_64` en Debian amd64.

### `ip`

**QUÉ HACE:** consulta/configura red. En la práctica solo se usa para leer información.

```bash
ip -br addr
ip route
```

`-br` produce una salida breve; `addr` muestra direcciones. `ip route` indica rutas, incluida la salida predeterminada a Internet.

### `ping`

**QUÉ HACE:** envía mensajes ICMP para comprobar alcance. `-c 4` envía cuatro y termina.

```bash
ping -c 4 1.1.1.1
```

### `chmod`

**QUÉ HACE:** cambia permisos.

```bash
chmod +x <EJECUTABLE_REAL>
```

`+x` añade permiso de ejecución. No use `chmod 777`.

### `ps`, `pgrep` y `kill`

```bash
ps -ef
pgrep -af <NOMBRE_REAL>
kill <PID>
```

- `ps -ef`: fotografía de procesos.
- `pgrep -af`: busca por nombre y muestra argumentos.
- `kill PID`: solicita una terminación normal. Evite `kill -9` salvo diagnóstico avanzado.

### `sudo`

**QUÉ HACE:** ejecuta un único comando administrativo. Solicita la contraseña del usuario.

```bash
sudo apt update
```

No trabaje permanentemente como `root`. Use `sudo` solo para paquetes, servicios o firewall.

### `apt`

**QUÉ HACE:** instala y actualiza paquetes desde repositorios configurados.

```bash
sudo apt update
sudo apt upgrade
```

- **Repositorio:** servidor que publica paquetes firmados.
- **Índice:** catálogo local de versiones disponibles.
- **Paquete:** archivo instalable de software y metadatos.
- `apt update`: actualiza el índice; no actualiza todavía los programas.
- `apt upgrade`: instala versiones más nuevas de paquetes ya instalados.

Revise la lista antes de confirmar con `Y`.

### `systemctl`

**QUÉ HACE:** consulta y controla servicios gestionados por systemd.

```bash
sudo systemctl status ssh
sudo systemctl enable --now ssh
```

- `status`: muestra estado y logs recientes.
- `enable`: programa inicio automático.
- `--now`: además lo inicia inmediatamente.

### `ss` y `journalctl`

**QUÉ HACEN:** `ss` muestra sockets de red; `journalctl` consulta el registro de servicios de Linux.

```bash
sudo ss -lunp
sudo journalctl -u ssh --since "10 minutes ago"
```

- `-l` muestra servicios que escuchan; `-u` en `ss` selecciona UDP; `-n` evita resolver nombres; `-p` intenta mostrar el proceso.
- `journalctl -u ssh` limita la consulta al servicio SSH. Sustituya `ssh` por el servicio que esté diagnosticando.
- Un **socket** es el extremo del sistema operativo que un proceso usa para comunicarse. En este laboratorio, `0.0.0.0:7979/udp` significa que el proceso está esperando datagramas UDP para NGO en el puerto 7979.


---

## 14. Configuración de red

Ahora Debian funciona, pero Unity todavía necesita una ruta para alcanzarlo. Configuraremos una interfaz NAT para que Debian descargue paquetes y una interfaz Host Only para que el host se comunique con la VM sin depender del Wi-Fi del aula.


### 14.1 Vocabulario

No memorice estas definiciones aisladas. Utilícelas para dibujar la ruta real entre Unity y Debian.


- **IP:** dirección de una interfaz dentro de una red.
- **Puerto:** número que permite dirigir tráfico a un proceso concreto en una IP.
- **Interfaz:** conexión de red concreta de una máquina, física o virtual. Una misma VM puede tener una IP NAT y otra Host-Only.
- **Socket:** combinación que usa un proceso para enviar o recibir; en una escucha se identifica habitualmente por protocolo, dirección y puerto.
- **TCP:** protocolo orientado a conexión que confirma orden y entrega. SSH usa TCP, normalmente en el puerto 22.
- **UDP:** protocolo de datagramas sin confirmación incorporada. NGO y NFE lo usan porque el juego puede enviar información nueva antes de que una posición antigua siga siendo útil.
- **127.0.0.1 / localhost:** loopback; siempre significa “esta misma máquina/proceso de sistema operativo”.
- **Adaptador virtual:** tarjeta de red que VirtualBox presenta a Debian.
- **NAT:** permite que la VM salga a Internet; por defecto no facilita que el host inicie conexiones directas al guest.
- **Host-Only:** red privada entre host y VMs, independiente del Wi-Fi.
- **Bridged:** coloca la VM en la red física; depende de políticas del aula y no es la opción principal.
- **Firewall:** reglas que permiten o bloquean tráfico.

Una dirección completa, por ejemplo `192.168.56.101:7979`, se lee así: `192.168.56.101` identifica una interfaz de la VM y `7979` identifica el servicio UDP de NGO dentro de esa VM. No es una dirección web y no incluye el nombre de un archivo.

### 14.2 Diseño recomendado

```text
                           Internet
                               ▲
                               │ Adapter 1: NAT
HOST macOS/Windows             │
┌──────────────────────┐   ┌───┴───────────────┐
│ Unity / Terminal     │   │ Debian VM         │
│ Host-Only IP         ├───┤ Host-Only IP      │
└──────────────────────┘   └───────────────────┘
        Adapter 2: Host-Only Network
```

NAT se usa para `apt`. Host-Only se usa para SSH y, en el futuro, para tráfico del juego. Esta combinación evita depender de Wi-Fi y no expone el servidor al resto del aula.

### 14.3 Encontrar la IP de Debian

```bash
ip -br addr
```

Ejemplo ilustrativo, no valores garantizados:

```text
lo       UNKNOWN  127.0.0.1/8
enp0s3   UP       10.0.2.15/24
enp0s8   UP       192.168.56.101/24
```

- `lo` es localhost.
- `enp0s3` suele corresponder a NAT.
- `enp0s8` suele corresponder a Host-Only.
- Use la IP Host-Only, en este ejemplo `192.168.56.101`, para conexiones desde el host.

Los nombres e IP pueden variar. Confirme también:

```bash
ip route
```

### macOS — comprobar comunicación

Abra Terminal en macOS:

```bash
ping -c 4 <IP_VM>
```

### Windows — comprobar comunicación

Abra PowerShell:

```powershell
ping <IP_VM>
```

En Windows `ping` envía cuatro solicitudes por defecto.

### Por qué Unity no debe usar localhost

Si Unity corre en macOS/Windows, `127.0.0.1` apunta al host. El servidor corre en Debian, otro sistema operativo con su propia interfaz loopback. Unity deberá usar la IP Host-Only de Debian.

### ACTIVIDAD — mapa de red

Sin copiar los ejemplos, registre:

1. nombre de la interfaz NAT;
2. IP NAT;
3. nombre de la interfaz Host-Only;
4. IP Host-Only;
5. ruta predeterminada;
6. resultado del ping desde el host.


---

## 15. Preparación de SSH

Ya podemos alcanzar Debian por red. SSH aprovechará esa ruta para abrir una terminal remota cifrada desde el host. Un servicio SSH escucha en Debian; el cliente `ssh` se ejecuta en Terminal de macOS o PowerShell de Windows.


SSH crea una terminal cifrada hacia otra máquina. Así puede controlar Debian desde Terminal o PowerShell sin trabajar en la pequeña consola de VirtualBox. `scp` utiliza SSH para transferir archivos.

Si “SSH server” fue seleccionado durante la instalación, ya debe existir. Para asegurar el paquete:

```bash
sudo apt update
sudo apt install openssh-server
sudo systemctl enable --now ssh
sudo systemctl status ssh
```

Busque `active (running)`. Presione `q` para salir de la vista de estado.

Compruebe el puerto SSH estándar:

```bash
sudo ss -lntp | grep ':22'
```

### macOS — conexión

```bash
ssh student@<IP_VM>
```

### Windows — conexión

PowerShell moderno incluye cliente OpenSSH:

```powershell
ssh student@<IP_VM>
```

La primera vez, SSH pregunta si confía en la huella de la VM. Compare la IP, escriba `yes` y luego la contraseña. La contraseña no muestra asteriscos mientras se escribe; es normal.

Para cerrar:

```bash
exit
```


---

## 16. Generación del Dedicated Server

Volvemos a Unity. Un Dedicated Server es otra forma de ejecutar el proyecto: conserva simulación y networking, pero no necesita cámara, interfaz de jugador ni renderizado normal. Primero se prueba como proceso Linux directo; solo después se introduce el container.


Este paso lo realiza el estudiante después de completar y probar uno de los stacks. NGO y NFE necesitan builds separados porque usan escenas, bootstrap y puertos distintos.

### 16.1 Preparar un perfil NGO

1. Abra `File > Build Profiles`.
2. Seleccione `Add Build Profile` y luego `Linux Server`.
3. Instale el módulo desde Unity Hub solo si Unity indica que falta.
4. Active únicamente `Assets/Scenes/NGOGameplay.unity` para este perfil.
5. Elija la carpeta de salida `Builds/NGO-LinuxServer`.
6. Use `Build`, no `Build and Run` durante la preparación.

### 16.2 Preparar un perfil NFE

Repita el proceso con `Assets/Scenes/NFEGameplay.unity` y salida `Builds/NFE-LinuxServer`. La SubScene es una dependencia de la escena principal. No mezcle la escena NGO en este build.

### 16.3 Verificar el artefacto

En el host, identifique el ejecutable y su arquitectura:

```bash
file Builds/NGO-LinuxServer/<EJECUTABLE>
```

El build Linux Server estándar debe informar ELF x86-64. Registre el nombre real; no reemplace `<EJECUTABLE>` hasta observarlo.

## 17. Transferencia y ejecución en Debian

Copie el directorio completo mediante SSH:

```bash
scp -r Builds/NGO-LinuxServer student@<IP_VM>:/home/student/ngo-server
```

En Debian:

```bash
cd /home/student/ngo-server
ls -la
chmod +x <EJECUTABLE>
./<EJECUTABLE> -batchmode -nographics -port 7979
```

Para NFE use su directorio, `-nfe` cuando el arranque lo requiera y puerto 7980. `chmod +x` concede permiso de ejecución; no use `chmod 777`. `./` ejecuta el archivo del directorio actual.

Verifique proceso y socket:

```bash
pgrep -af <EJECUTABLE>
sudo ss -lunp | grep -E '7979|7980'
```

`ss -lunp` muestra sockets UDP en escucha con valores numéricos y proceso. `pgrep -af` demuestra que el proceso existe; `ss` demuestra que abrió un puerto.

## 18. Contenedor del servidor

El servidor ya debe funcionar directamente antes de empaquetarlo. Un container no corrige un ejecutable roto; solo ofrece un entorno repetible para ejecutarlo.

> [!CONCEPT]
> Una **imagen** es una plantilla inmutable con archivos y configuración. Un **container** es un proceso creado desde esa imagen. Un **Dockerfile** es la receta para construirla. Un **runtime**, en este caso Docker, crea y controla containers. Un **registry** almacena imágenes; no se necesita publicar ninguna imagen para las primeras pruebas locales.

### 18.1 Instalar y comprobar Docker en Debian

En la VM Debian, actualice el índice e instale el paquete mantenido por Debian:

```bash
sudo apt update
sudo apt install docker.io
sudo systemctl enable --now docker
sudo systemctl status docker
```

- `docker.io` instala el motor y el cliente de Docker.
- `enable --now` inicia el servicio ahora y en futuros arranques.
- `status` debe mostrar `active (running)`; presione `q` para salir.

Para evitar usar `sudo` en cada comando, agregue **su usuario actual** al grupo `docker`:

```bash
sudo usermod -aG docker "$USER"
```

`-aG` agrega el usuario a un grupo complementario sin eliminar sus otros grupos. Cierre completamente la sesión SSH y vuelva a entrar para que el cambio se aplique. Compruebe:

```bash
docker version
docker run --rm hello-world
```

> [!WARNING]
> Pertenecer al grupo `docker` concede privilegios elevados sobre la máquina. Hágalo solo en la VM del laboratorio y no exponga el socket de Docker por red.

### 18.2 Leer los Dockerfiles reales

El repositorio incluye `Infrastructure/Containers/NGO/Dockerfile` y `Infrastructure/Containers/NFE/Dockerfile`. Ambos esperan que el estudiante haya generado previamente el build correspondiente. No se construyeron en el starter.

| Instrucción real | Qué hace en esta práctica |
|---|---|
| `FROM debian:13-slim` | Comienza con una base Debian mínima. La arquitectura se elige según el host de construcción. |
| `RUN apt-get ...` | Instala certificados y bibliotecas requeridas; luego elimina el índice temporal para reducir tamaño. |
| `useradd ... unity` | Crea un usuario sin privilegios para ejecutar el servidor. |
| `WORKDIR /home/unity/server` | Define el directorio desde el que se copiará y ejecutará el build. |
| `COPY Builds/... ./` | Copia **toda** la carpeta del build desde el contexto de construcción. |
| `RUN chmod ... && chown ...` | Concede ejecución al binario y entrega los archivos al usuario `unity`. |
| `USER unity` | Evita ejecutar el juego como `root`. |
| `EXPOSE 7979/udp` o `7980/udp` | Documenta el puerto UDP esperado; por sí solo no lo publica al host. |
| `ENTRYPOINT [...]` | Define el ejecutable y los argumentos que arrancan automáticamente. |

El Dockerfile NGO espera exactamente `NetworkingLabNGOServer.x86_64`; el NFE espera `NetworkingLabNFEServer.x86_64`. Si Unity produjo otro nombre, corrija el perfil o la referencia antes de construir.

### 18.3 Construir, inspeccionar y ejecutar

En Debian amd64, desde la raíz transferida del repositorio:

```bash
docker build -f Infrastructure/Containers/NGO/Dockerfile -t networking-lab-ngo:local .
docker image inspect networking-lab-ngo:local
```

- `-f` selecciona el Dockerfile.
- `-t` asigna el nombre `networking-lab-ngo` y la etiqueta `local`.
- El punto final `.` es el **contexto**: la raíz desde la que `COPY` puede leer `Builds/NGO-LinuxServer`.
- `inspect` devuelve JSON. Busque `Architecture`, `Config.Entrypoint` y `Config.ExposedPorts`.

Ejecute una prueba directa publicando el mismo puerto UDP:

```bash
docker run --rm --name ngo-server-test -p 7979:7979/udp networking-lab-ngo:local
```

`--rm` elimina el container al terminar, `--name` le asigna un nombre y `-p` enlaza `PUERTO_HOST:PUERTO_CONTAINER/protocolo`. En otra sesión SSH:

```bash
docker ps
docker logs ngo-server-test
sudo ss -lunp | grep ':7979'
```

Detenga la prueba de forma ordenada:

```bash
docker stop ngo-server-test
```

Para NFE sustituya archivo, nombre de imagen y puerto por los valores NFE reales. Antes de construir, confirme la ruta `COPY`, el ejecutable y el puerto. En Apple Silicon, una imagen ARM64 no puede ejecutar un binario Unity x86-64 de forma nativa: la arquitectura de la imagen y la del ejecutable deben coincidir.

## 19. Kubernetes single node con Minikube

Hasta ahora el estudiante inicia un container manualmente. Kubernetes introduce un plano de control que decide dónde y cómo ejecutar containers. Para esta práctica usaremos un cluster pequeño de un solo Node dentro de Debian.


Esta guía selecciona Minikube con driver Docker porque crea un cluster de un nodo, funciona en Linux amd64 y arm64 y es apropiado para un laboratorio. Agones documenta Minikube para evaluación local. No se instala en el starter.

> [!CONCEPT]
> Kubernetes recibe una descripción del estado deseado y trabaja continuamente para alcanzarlo. El estudiante no inicia directamente el proceso Unity: crea un recurso, y Kubernetes programa un Pod que contiene el container.

Conceptos:

| Término | Función en el laboratorio |
|---|---|
| Cluster | conjunto administrado por Kubernetes |
| Node | Debian o nodo Minikube que ejecuta Pods |
| Pod | unidad que contiene el servidor de juego |
| Container | proceso aislado creado desde la imagen |
| YAML | descripción declarativa de recursos |
| kubectl | cliente para consultar y aplicar recursos |

El script `Infrastructure/Scripts/install-minikube-agones.sh` es material de referencia. Léalo antes de ejecutarlo: descarga binarios, usa `sudo install` y aplica manifiestos remotos. La combinación documentada al preparar el starter es Minikube 1.39.0, Kubernetes 1.34.6 y Agones 1.60.0.

### 19.1 Instalar Minikube según la arquitectura

El script detecta la arquitectura de Debian mediante:

```bash
dpkg --print-architecture
```

El resultado debe ser `amd64` o `arm64`. Después descarga el binario correspondiente y lo instala en `/usr/local/bin/minikube`. No copie literalmente una URL de otra arquitectura. Verifique al final:

```bash
minikube version
```

### 19.2 Crear el cluster del laboratorio

El perfil usado por el script se llama `agones`:

```bash
minikube start -p agones --driver=docker --kubernetes-version=v1.34.6 --cpus=4 --memory=6144
minikube kubectl -p agones -- get nodes -o wide
```

- `-p agones`: crea o selecciona el perfil del laboratorio.
- `--driver=docker`: ejecuta el nodo de Minikube mediante Docker.
- `--kubernetes-version`: fija la versión comprobada para la práctica.
- `--cpus` y `--memory`: reservan recursos; la VM debe disponer de ellos.
- `minikube kubectl ... --`: ejecuta `kubectl` contra ese perfil aunque no exista un binario `kubectl` separado.

Salida conceptual de `get nodes`:

| Columna | Cómo interpretarla |
|---|---|
| `NAME` | nombre del nodo creado por Minikube |
| `STATUS` | debe ser `Ready` antes de continuar |
| `ROLES` | función de control plane del nodo único |
| `AGE` | tiempo desde que se creó |
| `VERSION` | versión real de Kubernetes |
| `INTERNAL-IP` | IP interna del nodo; todavía no prueba acceso desde el host físico |

Si aparece `NotReady`, consulte y lea la sección `Conditions` y los `Events` finales:

```bash
minikube status -p agones
minikube kubectl -p agones -- describe node
```

No continúe a Agones hasta obtener `Ready`.

## 20. Agones y GameServer

Kubernetes sabe ejecutar aplicaciones generales. Agones agrega recursos para representar y administrar instancias de servidores de videojuegos. El recurso central de esta práctica es `GameServer`. Agones no transporta el movimiento ni el PowerUp.


Kubernetes administra workloads. Agones añade recursos y controladores para servidores de juegos. Agones no reemplaza Unity Transport ni NGO o NFE.

> [!CONCEPT]
> Un **namespace** agrupa recursos. Un **controller** observa recursos y actúa para acercar el cluster al estado solicitado. Un **CRD** enseña a Kubernetes un tipo nuevo de recurso. Agones instala la CRD `GameServer`; cada instancia crea y supervisa un Pod para un servidor de juego.

```text
Unity Client ── UDP ──► Unity Dedicated Server
                              ▲
                              │ administrado como GameServer
                           Agones
                              ▲
                         Kubernetes
```

### 20.1 Instalar y verificar Agones

El script crea el namespace y aplica el manifiesto oficial de la versión indicada. Después espera hasta cinco minutos a que el controller esté disponible. Si el docente autoriza ejecutar el script completo:

```bash
bash Infrastructure/Scripts/install-minikube-agones.sh
```

Verifique explícitamente:

```bash
minikube kubectl -p agones -- get pods -n agones-system
minikube kubectl -p agones -- get crd gameservers.agones.dev
```

En la primera salida, `READY` muestra containers listos frente al total y `STATUS` debe ser `Running` o `Completed` según la función. La segunda salida debe incluir la CRD `gameservers.agones.dev`. Si faltan recursos, no aplique todavía un GameServer.

### 20.2 Leer el manifiesto GameServer

Las plantillas están en `Infrastructure/Kubernetes/Agones`. Antes de aplicar, el estudiante debe sustituir la imagen, confirmar `containerPort`, `hostPort` o política de asignación, protocolo UDP y puerto del stack.

| Campo real | Significado |
|---|---|
| `apiVersion: agones.dev/v1` | usa la API instalada por la CRD de Agones |
| `kind: GameServer` | solicita una instancia administrada de servidor de juego |
| `metadata.name` | nombre con el que se consultará el recurso |
| `portPolicy: Static` | conserva el `hostPort` escrito; no solicita asignación dinámica |
| `containerPort` | puerto donde escucha Unity dentro del container |
| `hostPort` | puerto UDP publicado en el nodo para llegar al container |
| `protocol: UDP` | protocolo real de Unity Transport en esta práctica |
| `health` | tiempos y tolerancia del ciclo de salud de Agones |
| `nodeSelector` | exige nodo `amd64`; debe revisarse para un artefacto ARM64 real |
| `image` | nombre de la imagen que debe existir para Minikube |
| `imagePullPolicy` | `IfNotPresent` reutiliza una imagen local si está disponible |
| `resources` | CPU y memoria solicitadas y máximas |

La imagen construida en Debian no aparece automáticamente dentro del runtime de Minikube. Cárguela en el perfil antes de aplicar el manifiesto:

```bash
minikube image load -p agones networking-lab-ngo:local
minikube image ls -p agones | grep networking-lab-ngo
```

### 20.3 Crear y observar el GameServer

```bash
minikube kubectl -p agones -- apply -f Infrastructure/Kubernetes/Agones/ngo-gameserver.yaml
minikube kubectl -p agones -- get gameserver
minikube kubectl -p agones -- describe gameserver <NOMBRE>
minikube kubectl -p agones -- get pods -o wide
```

`apply` crea o actualiza el recurso. `get` resume su estado. `describe` muestra configuración y eventos; los eventos finales suelen explicar por qué un Pod no inicia. `get pods -o wide` relaciona el GameServer con su Pod y nodo.

### 20.4 Ciclo Ready y Health

Un GameServer solo alcanza `Ready` cuando el proceso integra el ciclo de vida requerido por Agones. Las plantillas no incorporan silenciosamente esa integración. El estudiante debe implementar el uso del SDK o del REST sidecar expuesto mediante `AGONES_SDK_HTTP_PORT`:

1. Unity inicia y abre el socket de transporte.
2. El proceso llama `POST /ready` para indicar que acepta jugadores.
3. Mientras esté sano llama periódicamente `POST /health`.
4. Agones actualiza el estado y puede retirar una instancia que deja de reportar salud.

Esta es una actividad de infraestructura, no uno de los siete `STUDENT TODO` de gameplay. No marque el GameServer como operativo solamente porque el Pod exista.

## 21. Address y Port desde el host

El cliente está en macOS o Windows; el Pod está dentro de Minikube en Debian. La IP interna del Pod no es el endpoint que debe escribirse en Unity.

Para este laboratorio, el GameServer debe publicar un `hostPort` UDP en el nodo Minikube y la red Host Only debe permitir llegar a Debian. Existen tres niveles de dirección:

```text
macOS o Windows
    │ usa IP Host-Only de Debian y puerto publicado
    ▼
Debian VM
    │ reenvía o enruta hacia el Node de Minikube
    ▼
Node Minikube ── hostPort UDP ──► Pod ── containerPort ──► Unity Server
```

Obtenga datos reales:

```bash
minikube kubectl -p agones -- get gameserver <NOMBRE> -o jsonpath='{.status.address}{"\n"}{.status.ports[0].port}{"\n"}'
minikube ip -p agones
```

`jsonpath` extrae dos campos sin imprimir todo el YAML: primera línea, `status.address`; segunda línea, primer puerto publicado. `minikube ip` muestra la IP interna del nodo. Registre además la IP Host-Only con `ip -br addr` en Debian.

Si Agones publica la IP del nodo interno de Minikube, el host físico puede no enrutarla. En ese caso configure un reenvío UDP explícito desde la IP Host Only de Debian al endpoint del nodo, o ejecute Minikube con una configuración de red validada por el docente. No use la IP del Pod ni asuma que `localhost` cruza la VM.

Los campos de Unity son:

- NGO: Address más UDP 7979 por defecto.
- NFE: Address más UDP 7980 por defecto.

Reemplace esos puertos por el puerto asignado que muestre `GameServer.status` si Agones lo cambia.

> [!WARNING]
> La guía no afirma que exista una ruta automática entre el host físico y la red interna de Minikube. El checkpoint de routing exige demostrarla antes de conectar Unity. Abra solo el puerto UDP del stack que esté probando; no desactive el firewall completo.

## 22. Multiplayer Play Mode y cuatro clientes

Multiplayer Play Mode 3.0 está instalado. Permite ejecutar instancias adicionales del Editor para representar clientes separados. No convierte los cuatro Players del baseline en clientes de red.

### 22.1 Configurar el endpoint en la escena correcta

- NGO: abra `Assets/Scenes/NGOGameplay.unity`, localice el panel de `NgoConnectionUI`, escriba Address y Port reales y use **Client**. El servidor externo ya debe estar activo; no pulse Host ni Server.
- NFE: abra `Assets/Scenes/NFEGameplay.unity`, localice `NfeConnectionUI`, escriba el mismo endpoint publicado para el stack NFE y use **Client**.

### 22.2 Probar primero un solo cliente

1. Observe `GameServer`, Pod y logs del servidor.
2. Inicie Play en el Editor principal.
3. Pulse Client una vez.
4. Confirme estado conectado y aparición del Player o Ghost correspondiente.
5. Compruebe en logs del servidor que existe una conexión.

Solo después de aprobar este recorrido configure múltiples instancias.

### 22.3 Crear cuatro clientes

1. Abra `Window > Play Mode > Scenarios`.
2. Cree un escenario para el stack que está probando.
3. Añada hasta tres **Additional Editor Instances**, además del Editor principal.
4. Asegure que las cuatro instancias usan la misma escena, Address y Port.
5. Ejecute y conecte una instancia a la vez.
6. Compare logs de cada cliente, del Unity Server y de Agones.

La evidencia correcta muestra cuatro procesos cliente y cuatro identidades de red. Cada Player debe aceptar input solamente de su owner; los demás reciben estado replicado. Para PowerUp, una activación se valida en el servidor y llega una sola vez a la UI de cada cliente.

## 23. Checkpoints del laboratorio

Los checkpoints son comprobaciones manuales, no tests automáticos. Cada uno cierra una etapa que cambia el tipo de problema a resolver; por eso se consolidan en lugar de contar acciones menores. Registre `PASS`, `FAIL`, `BLOCKED` o `NOT_TESTED` y no continúe sobre un fallo de red no entendido.

| Checkpoint | Qué comprobamos | Procedimiento | PASS significa | Si falla |
|---|---|---|---|---|
| 01 Proyecto y baseline | El punto de partida abre y funciona | Abra la escena local, pruebe cuatro controles y PowerUp | Cuatro jugadores se mueven, respetan límites y la UI se limpia | Revise Input Actions, componentes y tests Play Mode |
| 02 Starter de red | Se entiende el límite entre código entregado y TODO | Ubique los siete TODO, `NetworkManager` y SubScene | Puede explicar las tres tareas NGO y las cuatro NFE | Vuelva a secciones 5 a 8 |
| 03 VM y arquitectura | Guest e ISO corresponden al host | Compruebe la ISO y ejecute `uname -m` | ARM64 en M4 o x86-64 en Windows | Reinstale con la ISO correcta |
| 04 Red host VM | Hay Internet y canal privado | Pruebe `apt update`, `ip -br addr` y ping Host-Only | NAT y Host-Only funcionan | Revise adaptadores, DHCP y firewall |
| 05 SSH | Administración remota segura | `systemctl status ssh` y `ssh usuario@IP_VM` | Prompt remoto por puerto 22 | Revise servicio, IP y credenciales |
| 06 Código de red | La actividad implementada compila y tiene el flujo correcto | Complete una actividad y pruebe Host más Client | Input, servidor y réplica siguen la arquitectura explicada | Corrija el primer error de compilación o flujo |
| 07 Dedicated Server directo | El servidor es ejecutable antes de contenerizar | Copie el build, ejecútelo y consulte `pgrep`/`ss` | Proceso y puerto UDP activos | Revise arquitectura, dependencias y argumentos |
| 08 Container | La imagen conserva el comportamiento directo | Construya, ejecute y lea `docker logs` | El container sigue activo y expone el puerto esperado | Revise Dockerfile, rutas y nombre del ejecutable |
| 09 Cluster | Kubernetes puede ejecutar cargas | Inicie Minikube y consulte Nodes | El Node está `Ready` | Use `describe node` y resuelva Docker/recursos |
| 10 Agones | Agones reconoce GameServer y sus controles | Instale, consulte Pods y aplique manifiesto | Controlador disponible y GameServer creado | Lea eventos, versiones e imagen |
| 11 Ruta al GameServer | El endpoint publicado es alcanzable desde el host | Compare IP Host-Only, Node, estado y puerto | La ruta UDP está demostrada | No use IP del Pod; valide reenvío/ruta con docente |
| 12 Un cliente | Una conexión completa llega al servidor | Conecte una instancia y revise UI y logs | Identidad y jugador/Ghost aparecen | Revise Address, UDP y TODO de conexión |
| 13 Cuatro clientes y ownership | Cuatro procesos reciben identidades distintas | Conecte uno a uno y pruebe cada ventana | Cada uno controla solo su jugador | Revise ownership y spawn |
| 14 Estado compartido | Movimiento y PowerUp llegan a todos | Mueva y active PowerUp desde cada cliente | Posición autoritativa y un mensaje por cliente | Separe input, validación y broadcast |

## 24. Diagnóstico de problemas

| Problema | Posible causa | Verificación | Solución segura |
|---|---|---|---|
| Unity no compila | TODO incompleto o API incorrecta | Console y primer error C# | Corregir el primer error antes de continuar |
| NGO no conecta | Address o UDP 7979 incorrecto | UI y `ss -lunp` | Usar endpoint real y confirmar servidor |
| Port forwarding no funciona | Regla NAT apunta a IP o puerto incorrecto | Revise protocolo dirección y puertos de la regla | Corregir una sola regla específica |
| `localhost` conecta al proceso equivocado | Se usa loopback del host | Compare ubicación del cliente y servidor | Usar IP Host Only de Debian |
| NFE no conecta | NFE 01 incompleto | NetworkId y logs | Completar Listen y Connect |
| No aparece Player NGO | prefab no registrado o spawn falló | NetworkManager Prefabs y logs | Restaurar registro y revisar servidor |
| No aparece Ghost NFE | NFE 02 incompleto | Entities Hierarchy | Completar GoInGame y owner |
| VM no inicia | ISO con arquitectura incorrecta | nombre de ISO | usar arm64 en Mac ARM o amd64 en Windows x64 |
| Binario no ejecuta | x86-64 dentro de Debian ARM64 | `uname -m` y `file` | usar entorno o artefacto compatible definido por docente |
| Debian sin Internet | NAT desconectado | `ip route` y ping | habilitar Adapter 1 NAT |
| Host no llega a VM | Host Only ausente | `ip -br addr` | habilitar Adapter 2 Host Only |
| SSH falla | servicio detenido o IP incorrecta | `systemctl status ssh` | habilitar servicio y usar IP Host Only |
| Permission denied | falta permiso x | `ls -l` | `chmod +x` al ejecutable |
| Dependencia Linux ausente | El ejecutable informa biblioteca no encontrada | Leer salida y usar `ldd` cuando corresponda | Instalar solo la dependencia identificada |
| Container cierra | ruta o ejecutable incorrecto | `docker logs` | revisar Dockerfile y arquitectura |
| `kubectl` no conecta | Contexto o cluster detenido | `kubectl config current-context` y `minikube status` | Seleccionar contexto correcto o iniciar Minikube |
| Node NotReady | Minikube o runtime incompleto | `kubectl describe node` | resolver condición antes de Agones |
| Agones Pods fallan | versión incompatible o recursos | Pods de agones-system | revisar eventos y matriz de versiones |
| Pod no inicia | Imagen no disponible o comando incorrecto | `kubectl describe pod` y `kubectl logs` | Corregir imagen ENTRYPOINT o carga local |
| GameServer no llega a Ready | falta ciclo de vida Agones | describe y logs | implementar Ready y Health mediante SDK o REST |
| Address no es alcanzable | dirección interna de Minikube | ruta y ping | publicar o reenviar UDP por IP Host Only |
| Puerto no aparece | transporte no escucha | `ss -lunp` | revisar puerto y logs del servidor |
| Movimiento solo local | NGO 01/02 o NFE 03 incompletos | comparar ventanas | completar autoridad y réplica |
| PowerUp solo local | NGO 03 o NFE 04 incompleto | logs y UI | completar solicitud y broadcast |
| Firewall bloquea UDP | regla específica ausente | `nft list ruleset` | abrir solo el puerto UDP requerido |

## 25. Actividades de análisis

1. Explique por qué `127.0.0.1` del host no apunta a Debian.
2. Distinga NetworkObject de Ghost usando archivos reales del starter.
3. Explique por qué el input no debe otorgar autoridad ilimitada al cliente.
4. Compare un RPC de PowerUp con la réplica frecuente de posición.
5. Explique la función de `GhostOwner` y de `NetworkVariable<int>`.
6. ¿Qué demuestra `ss` que `ps` no demuestra?
7. ¿Por qué el Address de un Pod no suele ser el endpoint del host?
8. ¿Qué administra Agones y qué sigue administrando Unity Transport?
9. ¿Por qué una imagen ARM64 no corrige un ejecutable x86-64 dentro de ella?
10. ¿Qué cambia al cerrar el Dedicated Server mientras los clientes están conectados?

Deje espacio para responder en una hoja separada o en la copia digital. No consulte una respuesta automática antes de justificarla con evidencias del laboratorio.

[CAPTURA SUGERIDA: salida de kubectl get nodes con STATUS Ready]

[CAPTURA SUGERIDA: kubectl get gameserver mostrando State Address y Port]

## 26. Evidencias a entregar

- ☐ Baseline con cuatro jugadores y PowerUp local
- ☐ Siete TODO identificados y código comentado
- ☐ Compilación NGO y NFE sin errores
- ☐ Dedicated Server generado y arquitectura identificada
- ☐ Debian instalado y `uname -m`
- ☐ IP Host Only identificada y ping
- ☐ SSH funcionando
- ☐ Build transferido
- ☐ Imagen de container inspeccionada
- ☐ Node Kubernetes Ready
- ☐ Agones operativo
- ☐ GameServer y Pod observados
- ☐ Address y Port reales
- ☐ Primer cliente conectado
- ☐ Cuatro clientes conectados
- ☐ Movimiento replicado
- ☐ PowerUp visible en cuatro clientes
- ☐ Preguntas de análisis respondidas

No entregue capturas simuladas. Incluya comandos y resultados suficientes para que el docente pueda relacionar cada evidencia con su checkpoint.

## 27. Seguridad y buenas prácticas

- Use `sudo` solo para administración del sistema.
- No ejecute el servidor como root.
- No use `chmod 777`.
- No desactive permanentemente el firewall.
- Abra únicamente el puerto UDP observado.
- Revise rutas antes de `rm`; el borrado desde terminal puede ser irreversible.
- No publique contraseñas, tokens ni kubeconfig.
- Detenga primero con `kill <PID>` y use señales forzadas solo después de diagnosticar.

## 28. Fuentes oficiales

- [Unity Dedicated Server build](https://docs.unity3d.com/6000.0/Documentation/Manual/dedicated-server-build.html)
- [Unity Multiplayer Play Mode 3.0](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@3.0/manual/index.html)
- [Netcode for GameObjects](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/manual/index.html)
- [Netcode for Entities](https://docs.unity3d.com/Packages/com.unity.netcode@6.6/manual/index.html)
- [Oracle VirtualBox Downloads](https://www.virtualbox.org/wiki/Downloads)
- [Oracle VirtualBox User Manual](https://www.virtualbox.org/manual/)
- [Debian stable](https://www.debian.org/releases/stable/)
- [Debian netinst amd64 y arm64](https://www.debian.org/distrib/netinst.html)
- [Docker documentation](https://docs.docker.com/)
- [Kubernetes documentation](https://kubernetes.io/docs/home/)
- [Minikube documentation](https://minikube.sigs.k8s.io/docs/)
- [Agones install on Minikube](https://agones.dev/site/docs/installation/creating-cluster/minikube/)
- [Agones GameServer specification](https://agones.dev/site/docs/reference/gameserver/)
- [Agones REST SDK](https://agones.dev/site/docs/guides/client-sdks/rest/)

## 29. Cierre

El starter conserva el gameplay local y añade dos bases reales sin resolver los ejercicios centrales. NGO deja lista la conexión, identidad y réplica de Transform; NFE deja listos Worlds, componentes, Ghost y sistemas de extensión. El estudiante completa siete actividades y después construye, despliega y prueba el servidor mediante Debian, Minikube y Agones.

El resultado final se considera completo solo cuando la evidencia demuestra conectividad, ownership, movimiento y PowerUp en cuatro clientes. La incompatibilidad x86-64 frente a ARM64 debe resolverse con un artefacto o entorno aprobado, no con una suposición.
