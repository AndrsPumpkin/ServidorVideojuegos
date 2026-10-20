using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine.InputSystem;
using NetworkingLab.PowerUp;

namespace NetworkingLab.NFE
{
    public struct NfeGoInGameRequest : IRpcCommand { }

    public struct NfePowerUpResultRpc : IRpcCommand
    {
        public int PlayerNumber;
    }

    public struct NfeGameOverRpc : IRpcCommand
    {
        public int WinnerPlayerNumber;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [CreateAfter(typeof(RpcSystem))]
    public partial struct NfeRpcRegistrationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            SystemAPI.GetSingletonRW<RpcCollection>().ValueRW.DynamicAssemblyList = true;
            state.Enabled = false;
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct NfeGoInGameClientSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NfePlayerSpawner>();
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>().WithNone<NetworkStreamInGame>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        public void OnUpdate(ref SystemState state)
        {
            // STUDENT TODO NFE-02: mark the connection InGame and send NfeGoInGameRequest.
            // Use an EntityCommandBuffer and SendRpcCommandRequest targeted at the connection.
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
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct NfeGoInGameServerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NfePlayerSpawner>();
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NfeGoInGameRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        public void OnUpdate(ref SystemState state)
        {
            // STUDENT TODO NFE-02: receive NfeGoInGameRequest, instantiate PlayerPrefab,
            // assign GhostOwner/PlayerNumber, and link the player to its connection.
            EntityCommandBuffer ecb = new(Allocator.Temp);
            Entity prefab = SystemAPI.GetSingleton<NfePlayerSpawner>().PlayerPrefab;
            ComponentLookup<NetworkId> networkIds = state.GetComponentLookup<NetworkId>(true);
            networkIds.Update(ref state);

            if (!SystemAPI.HasSingleton<NfeGameState>())
            {
                Entity gameState = ecb.CreateEntity();
                ecb.AddComponent(gameState, new NfeGameState
                {
                    GameOver = 0,
                    WinnerPlayerNumber = 0
                });
            }

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
                ecb.SetComponent(player, new NfePlayerLives { Lives = 3, IsDead = 0 });
                ecb.AppendToBuffer(connection, new LinkedEntityGroup { Value = player });
                ecb.DestroyEntity(rpcEntity);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
 
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct NfeGatherInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<NfePlayerInput>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            float2 movement = float2.zero;
            if (keyboard.aKey.isPressed) movement.x -= 1f;
            if (keyboard.dKey.isPressed) movement.x += 1f;
            if (keyboard.sKey.isPressed) movement.y -= 1f;
            if (keyboard.wKey.isPressed) movement.y += 1f;
            movement = math.normalizesafe(movement);
            bool powerUp = keyboard.spaceKey.wasPressedThisFrame;

            foreach (RefRW<NfePlayerInput> input in
                     SystemAPI.Query<RefRW<NfePlayerInput>>().WithAll<GhostOwnerIsLocal>())
            {
                input.ValueRW = default;
                input.ValueRW.Move = movement;
                if (powerUp) input.ValueRW.PowerUp.Set();
            }
        }
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct NfeMovementSystem : ISystem
    {
        private const float TurnSpeedDegrees = 720f;

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<NfeGameState>())
            {
                if (SystemAPI.GetSingleton<NfeGameState>().GameOver == 1)
                {
                    return;
                }
            }

            // STUDENT TODO NFE-03: for simulated ghosts, apply NfePlayerInput.Move to
            // NfePlayerState.Position with DeltaTime and the baseline movement bounds.
            foreach (var (input, player, lives) in SystemAPI
                .Query<RefRO<NfePlayerInput>, RefRW<NfePlayerState>, RefRO<NfePlayerLives>>()
                .WithAll<Simulate>())
            {
                if (lives.ValueRO.IsDead == 1 || lives.ValueRO.Lives <= 0)
                {
                    continue;
                }

                float2 raw = input.ValueRO.Move;
                float2 move = math.normalizesafe(raw);
                float2 next = player.ValueRO.Position + move * (5f * SystemAPI.Time.DeltaTime);
                next.x = math.clamp(next.x, -10.5f, 10.5f);
                next.y = math.clamp(next.y, -7.5f, 7.5f);
                player.ValueRW.Position = next;

                if (math.lengthsq(raw) > 0.0001f)
                {
                    float targetYaw = math.degrees(math.atan2(move.x, move.y));
                    float delta = math.fmod(targetYaw - player.ValueRO.RotationY + 540f, 360f) - 180f;
                    float maxDelta = TurnSpeedDegrees * SystemAPI.Time.DeltaTime;
                    player.ValueRW.RotationY += math.clamp(delta, -maxDelta, maxDelta);
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(NfeMovementSystem))]
    public partial struct NfePowerUpServerSystem : ISystem
    {
        private const float FireCooldownSeconds = 0.3f;
        private const float BulletSpeed = 12f;
        private const float MuzzleSideOffset = 0.6f;
        private const float MuzzleHeightOffset = 1.5f;
        private const float PlayerHeight = 0.75f;

        public void OnUpdate(ref SystemState state)
        {
            // STUDENT TODO NFE-04: detect InputEvent PowerUp on the server and broadcast
            // NfePowerUpResultRpc containing the replicated PlayerNumber.
            if (SystemAPI.HasSingleton<NfeGameState>())
            {
                if (SystemAPI.GetSingleton<NfeGameState>().GameOver == 1)
                {
                    return;
                }
            }

            double elapsed = SystemAPI.Time.ElapsedTime;
            bool hasSpawner = SystemAPI.TryGetSingleton<NfeBulletSpawner>(out NfeBulletSpawner spawner);
            EntityCommandBuffer ecb = new(Allocator.Temp);
            foreach (var (input, player, lives) in SystemAPI
                .Query<RefRO<NfePlayerInput>, RefRW<NfePlayerState>, RefRO<NfePlayerLives>>()
                .WithAll<Simulate>())
            {
                if (lives.ValueRO.IsDead == 1 || lives.ValueRO.Lives <= 0)
                {
                    continue;
                }

                if (!input.ValueRO.PowerUp.IsSet) continue;
                Entity rpc = ecb.CreateEntity();
                ecb.AddComponent(rpc, new NfePowerUpResultRpc
                {
                    PlayerNumber = player.ValueRO.PlayerNumber
                });
                ecb.AddComponent(rpc, new SendRpcCommandRequest
                {
                    TargetConnection = Entity.Null
                });

                if (!hasSpawner || spawner.BulletPrefab == Entity.Null) continue;
                if (elapsed < player.ValueRO.FireCooldown) continue;

                player.ValueRW.FireCooldown = (float)(elapsed + FireCooldownSeconds);
                float yawRadians = math.radians(player.ValueRO.RotationY);
                float3 right = new float3(math.cos(yawRadians), 0f, -math.sin(yawRadians));
                float3 origin = new float3(player.ValueRO.Position.x, PlayerHeight, player.ValueRO.Position.y);
                float3 spawnPosition = origin + right * MuzzleSideOffset + new float3(0f, MuzzleHeightOffset, 0f);

                Entity bullet = ecb.Instantiate(spawner.BulletPrefab);
                ecb.SetComponent(bullet, new NfeBulletState
                {
                    Position = spawnPosition,
                    Velocity = right * BulletSpeed,
                    Lifetime = 2f,
                    OwnerPlayerNumber = player.ValueRO.PlayerNumber
                });
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct NfePowerUpClientSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // STUDENT TODO NFE-04: consume NfePowerUpResultRpc on clients, raise
            // PowerUpEvents, then destroy the received RPC entity.
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
        }
    }
}
