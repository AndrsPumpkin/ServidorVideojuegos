# Auditoría del starter

Fecha de auditoría: 29 de septiembre de 2026.

## Resultado

La estructura del starter es consistente y se conserva. El gameplay local, los prefabs, escenas, assemblies y puntos de extensión de NGO y NFE mantienen una separación clara entre referencia terminada y trabajo del estudiante.

| Área | Evidencia | Estado |
|---|---|---|
| Unity | `ProjectSettings/ProjectVersion.txt`: 6000.6.0f1 | PASS |
| Gameplay local | Cuatro `PlayerController`, Input Actions y pruebas Play Mode | PASS estático |
| NGO | `NetworkManager`, `UnityTransport`, `NetworkObject`, `NetworkTransform` y tres TODO | PASS estático |
| NFE | Bootstrap, Worlds, Ghost authoring, SubScene y cuatro TODO | PASS estático |
| TODO | Siete identificadores únicos: NGO-01..03 y NFE-01..04 | PASS |
| Infraestructura | Dockerfiles, scripts y manifiestos con puertos coherentes | PASS estático |
| Build Profiles | No hay perfiles de build serializados en el repositorio | NOT_TESTED |
| Build Linux | No existe artefacto `Builds/` en el repositorio | NOT_TESTED |

## Versiones observadas

| Componente | Valor observado |
|---|---|
| NGO | 2.13.3, paquete embebido `Packages/com.unity.netcode.gameobjects` |
| Netcode for Entities | 6.6.0 |
| Entities | 6.6.0 |
| Transport de NFE | 6.6.0 resuelto por Unity |
| Transport declarado para NGO | 2.6.0 como dependencia del paquete embebido |

El `manifest.json` solicita `com.unity.transport` 2.7.4, mientras el lockfile resuelve 6.6.0 para el stack Entities y NGO declara 2.6.0 internamente. No se cambia este conjunto porque una actualización de paquetes requiere abrir Unity y validar la matriz de compatibilidad de ambos stacks.

## Ajustes de publicación realizados

- Se elimina `Assets/TutorialInfo/Layout.wlt`: incluía una ruta absoluta de un equipo ajeno y es una preferencia de editor, no material del laboratorio.
- `.gitignore` ahora evita que ese layout vuelva a ser versionado.
- Se elimina `Docs/update_starter_guide.py`: duplicaba una versión anterior del texto; `generate_guide_docx.py` usa directamente la fuente Markdown vigente.
- README y guía describen el starter sin afirmar que infraestructura o builds no ejecutados estén validados.

## Limitaciones verificadas

- Los Dockerfiles y scripts esperan ejecutables Linux x86-64 y las imágenes Agones seleccionan nodos `amd64`.
- Una VM Debian ARM64 en Apple Silicon no puede ejecutar de forma nativa ese artefacto x86-64.
- El ciclo `Ready` y `Health` de Agones es una actividad posterior; los manifiestos no lo implementan por sí solos.
