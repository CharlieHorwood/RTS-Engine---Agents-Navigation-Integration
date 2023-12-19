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
public partial struct WheeledSeekingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new WheeledSteeringJob().ScheduleParallel();
    }

    [BurstCompile]
    partial struct WheeledSteeringJob : IJobEntity
    {
        public void Execute(ref AgentBody body, in WheeledLocomotion locomotion, in LocalTransform transform)
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
