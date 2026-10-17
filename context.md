# CONTEXT.md — Unity Multiplayer Networking Lab (ServidorVideojuegos)
> Archivo de contexto para agentes IA. Léelo completo antes de operar sobre el repo.
> Última actualización: 2026-10-16. Idioma del proyecto: español.
> Última actualización: 2026-10-16. Idioma del proyecto: español.
> Archivo de contexto para agentes IA. Léelo completo antes de operar sobre el repo.
> Última actualización: 2026-10-16. Idioma del proyecto: español.

## 0. TL;DR (qué es y en qué estado está)

- Proyecto **Unity 6000.6.0f1** que es un laboratorio universitario cliente-servidor.
- Compara el **mismo gameplay** implementado dos veces: con **NGO** (Netcode for GameObjects) y con **NFE** (Netcode for Entities / ECS).
- El objetivo final es llevar los Dedicated Servers a **Linux + Docker + Minikube/Kubernetes + Agones** con 4 clientes conectados.
- Hay un **baseline local** totalmente funcional (4 jugadores en un Teclado) y **dos bases de red** con puntos de extensión marcados `STUDENT TODO`.
- **ESTADO crítico A VERIFICAR:** los 7 STUDENT TODO (NGO-01..03, NFE-01..04) **ya tienen código de solución escrito** en el repo (ver §7).
- No existe carpeta Builds/ (no se ha generado Dedicated Server). Infraestructura (Docker/K8s/Agones) está como plantillas, **no ejecutadas**.
- **Trabajo reciente (2026-10-16):** la escena NFE ahora muestra el **barco** (NGOPlayer.prefab) como visual, y el color del barco por jugador se corrigió en **NGO y NFE** (ver §18).




