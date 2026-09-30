# Unity Multiplayer Networking Lab

Este repositorio es un laboratorio universitario para construir y comprender un juego cliente-servidor con Unity. Compara el mismo gameplay mediante Netcode for GameObjects (NGO) y Netcode for Entities (NFE), y después lo lleva a un servidor Linux, contenedor, Kubernetes y Agones.

No es una solución multijugador terminada. El baseline local funciona y las escenas de red son bases estables con siete actividades `STUDENT TODO` que el estudiante debe completar.

## Qué aprenderás

- Input, movimiento y eventos de gameplay en Unity.
- Cliente, servidor, ownership, autoridad, UDP, puertos y RPC.
- GameObjects y `NetworkBehaviour` con NGO.
- Entities, Worlds, Ghosts, snapshots y predicción con NFE.
- Linux básico, SSH, Dedicated Server, Docker, Minikube y Agones.

## Requisitos

- Unity **6000.6.0f1**.
- Git para clonar el proyecto.
- La guía indica cuándo se necesita VirtualBox, Debian y herramientas de infraestructura. No instales esas herramientas antes de llegar a su sección.

El proyecto usa URP 17.6.0, Input System 1.20.0, NGO 2.13.3 embebido y Netcode for Entities/Entities 6.6.0 resueltos por Unity.

## Primeros pasos

1. Clona el repositorio y ábrelo con Unity 6000.6.0f1.
2. Abre `Assets/Scenes/LocalGameplayBaseline.unity`.
3. Presiona Play y verifica el movimiento de los cuatro jugadores y el mensaje de PowerUp.
4. Lee [la guía en Markdown](Docs/GUIA_LAB_UNITY_NETWORKING_LINUX.md) o abre `Docs/GUIA_LAB_UNITY_NETWORKING_LINUX.docx`.
5. Solo después estudia `NGOGameplay.unity` y `NFEGameplay.unity`.

## Escenas del laboratorio

| Escena | Propósito |
|---|---|
| `LocalGameplayBaseline.unity` | Referencia local terminada con cuatro jugadores en un teclado. |
| `NGOGameplay.unity` | Starter de GameObjects, `NetworkManager`, `UnityTransport` y UDP 7979. |
| `NFEGameplay.unity` | Starter ECS con ClientWorld, ServerWorld, Ghost y UDP 7980. |

## Trabajo deliberadamente pendiente

Los siete objetivos se conservan en código con el marcador `STUDENT TODO`:

1. NGO-01: enviar input del owner al servidor.
2. NGO-02: validar y aplicar movimiento autoritativo.
3. NGO-03: solicitar, validar y distribuir PowerUp mediante RPC.
4. NFE-01: abrir `Listen` y realizar `Connect`.
5. NFE-02: entrar InGame, crear Ghost y establecer ownership.
6. NFE-03: mover en la simulación predicha.
7. NFE-04: procesar PowerUp y distribuir el resultado mediante RPC.

La guía explica el código y cómo comprobar cada actividad. No copies una solución sin ejecutar y observar cada flujo.

## Local controls

| Player | Movement | PowerUp |
|---|---|---|
| Player 1 | W A S D | Space |
| Player 2 | Arrow keys | Right Ctrl |
| Player 3 | I J K L | O |
| Player 4 | Numpad 8, 4, 5, 6 | Numpad 0 |

## Alcance del starter

Incluido ahora:

- framework-neutral local gameplay;
- NGO `NetworkManager`, `UnityTransport`, network Player prefab, identity, connection UI, and extension code;
- NFE client/server Worlds, ECS components, systems, Ghost prefab, SubScene, connection UI, and presentation bridge;
- editable Address and Port fields;
- reference Dockerfiles, Minikube/Agones scripts, and GameServer manifests;
- the complete student lab guide.

Trabajo del estudiante:

- complete the seven `STUDENT TODO` activities;
- create Linux Dedicated Server builds;
- prepare Debian and host-only networking;
- build the server container;
- install Minikube and Agones;
- integrate the Agones lifecycle;
- deploy a GameServer and connect four clients.

Este starter no genera ni ejecuta un build Dedicated Server, una VM, una imagen de contenedor, un cluster Kubernetes ni un despliegue Agones.

## Limitación de arquitectura

El target Linux Server actual de Unity produce ejecutables x86-64. Una VM nativa de VirtualBox sobre Apple Silicon es ARM64 y no puede ejecutarlos de forma nativa. Windows x86-64 con Debian amd64 sí comparte arquitectura. La guía exige un artefacto ARM64 aprobado por el docente o un entorno x86-64 para el checkpoint end to end en Apple Silicon.
