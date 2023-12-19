using System;
using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using ProjectDawn.Navigation;
using Unity.Mathematics;
using Unity.Transforms;
/// <summary>
/// System that steers agent towards destination.
/// </summary>
[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(AgentSeekingSystemGroup))]
public partial struct TrackedSeekingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new TrackedSteeringJob().ScheduleParallel();
    }

    [BurstCompile]
    partial struct TrackedSteeringJob : IJobEntity
    {
        public void Execute(ref AgentBody body, in TrackedLocomotion locomotion, in LocalTransform transform)
        {
            if (body.IsStopped)
                return;

            float3 towards = body.Destination - transform.Position;
            float distance = math.length(towards);
            float3 desiredDirection = distance > math.EPSILON ? towards / distance : float3.zero;
            body.Force = desiredDirection;
            body.RemainingDistance = distance;
        }
    }
}
