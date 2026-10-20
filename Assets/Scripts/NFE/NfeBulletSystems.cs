using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace NetworkingLab.NFE
{
    public struct NfePlayerHitRpc : IRpcCommand
    {
        public int VictimPlayerNumber;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(NfePowerUpServerSystem))]
    public partial struct NfeBulletSimulationSystem : ISystem
    {
        private const float Lifetime = 2f;
        private const float HitRadiusXz = 1f;
        private const float BoundX = 10.5f;
        private const float BoundZ = 7.5f;

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            EntityCommandBuffer ecb = new(Allocator.Temp);

            if (!SystemAPI.HasSingleton<NfeGameState>())
            {
                Entity gameState = ecb.CreateEntity();
                ecb.AddComponent(gameState, new NfeGameState
                {
                    GameOver = 0,
                    WinnerPlayerNumber = 0
                });
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
                return;
            }

            RefRW<NfeGameState> gameStateRef = SystemAPI.GetSingletonRW<NfeGameState>();
            if (gameStateRef.ValueRO.GameOver == 1)
            {
                ecb.Dispose();
                return;
            }

            ComponentLookup<NfePlayerLives> livesLookup = state.GetComponentLookup<NfePlayerLives>(false);
            livesLookup.Update(ref state);
            ComponentLookup<NfePlayerState> playerStateLookup = state.GetComponentLookup<NfePlayerState>(true);
            playerStateLookup.Update(ref state);

            foreach (var (bullet, entity) in SystemAPI
                .Query<RefRW<NfeBulletState>>()
                .WithAll<NfeBulletTag, Simulate>()
                .WithEntityAccess())
            {
                float3 position = bullet.ValueRO.Position + bullet.ValueRO.Velocity * deltaTime;
                float age = bullet.ValueRO.Age + deltaTime;
                bullet.ValueRW.Position = position;
                bullet.ValueRW.Age = age;

                if (age >= Lifetime ||
                    math.abs(position.x) > BoundX ||
                    math.abs(position.z) > BoundZ)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                float2 flat = new float2(position.x, position.z);
                foreach (var playerPair in SystemAPI.Query<RefRO<NfePlayerState>, RefRW<NfePlayerLives>>().WithAll<NfePlayerTag>())
                {
                    if (playerPair.Item1.ValueRO.PlayerNumber == bullet.ValueRO.OwnerPlayerNumber) continue;
                    if (playerPair.Item2.ValueRO.IsDead == 1) continue;
                    if (math.distancesq(playerPair.Item1.ValueRO.Position, flat) > HitRadiusXz * HitRadiusXz) continue;

                    int victimNumber = playerPair.Item1.ValueRO.PlayerNumber;
                    int newLives = playerPair.Item2.ValueRO.Lives - 1;
                    if (newLives < 0) newLives = 0;
                    playerPair.Item2.ValueRW.Lives = newLives;
                    if (newLives <= 0)
                    {
                        playerPair.Item2.ValueRW.IsDead = 1;
                    }

                    Entity rpc = ecb.CreateEntity();
                    ecb.AddComponent(rpc, new NfePlayerHitRpc
                    {
                        VictimPlayerNumber = victimNumber
                    });
                    ecb.AddComponent(rpc, new SendRpcCommandRequest
                    {
                        TargetConnection = Entity.Null
                    });
                    ecb.DestroyEntity(entity);

                    // Check win condition
                    int aliveCount = 0;
                    int lastAlive = 0;
                    foreach (var p in SystemAPI.Query<RefRO<NfePlayerState>, RefRO<NfePlayerLives>>().WithAll<NfePlayerTag>())
                    {
                        if (p.Item2.ValueRO.IsDead == 0 && p.Item2.ValueRO.Lives > 0)
                        {
                            aliveCount++;
                            lastAlive = p.Item1.ValueRO.PlayerNumber;
                        }
                    }

                    if (aliveCount <= 1)
                    {
                        gameStateRef.ValueRW.GameOver = 1;
                        gameStateRef.ValueRW.WinnerPlayerNumber = aliveCount == 1 ? lastAlive : 0;
                        Entity overRpc = ecb.CreateEntity();
                        ecb.AddComponent(overRpc, new NfeGameOverRpc
                        {
                            WinnerPlayerNumber = gameStateRef.ValueRW.WinnerPlayerNumber
                        });
                        ecb.AddComponent(overRpc, new SendRpcCommandRequest
                        {
                            TargetConnection = Entity.Null
                        });
                        // Do not touch entity buffers/LinkedEntityGroup here; just mark game over
                    }

                    break;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct NfePlayerHitClientSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            foreach (var (hit, rpcEntity) in SystemAPI
                .Query<RefRO<NfePlayerHitRpc>>()
                .WithAll<ReceiveRpcCommandRequest>()
                .WithEntityAccess())
            {
                NfePresentationBridge.RequestHitFlash(hit.ValueRO.VictimPlayerNumber);
                ecb.DestroyEntity(rpcEntity);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
