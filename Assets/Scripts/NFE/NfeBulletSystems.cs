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
                foreach (var player in SystemAPI.Query<RefRO<NfePlayerState>>().WithAll<NfePlayerTag>())
                {
                    if (player.ValueRO.PlayerNumber == bullet.ValueRO.OwnerPlayerNumber) continue;
                    if (math.distancesq(player.ValueRO.Position, flat) > HitRadiusXz * HitRadiusXz) continue;

                    Entity rpc = ecb.CreateEntity();
                    ecb.AddComponent(rpc, new NfePlayerHitRpc
                    {
                        VictimPlayerNumber = player.ValueRO.PlayerNumber
                    });
                    ecb.AddComponent(rpc, new SendRpcCommandRequest
                    {
                        TargetConnection = Entity.Null
                    });
                    ecb.DestroyEntity(entity);
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
