using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Transforms;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using ProjectDawn.Navigation;

/// <summary>
/// System that steers agent towards destination.
/// </summary>
[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(AgentSeekingSystemGroup))]
public partial struct InfantrySeekingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new InfantrySteeringJob().ScheduleParallel();
    }

    [BurstCompile]
    partial struct InfantrySteeringJob : IJobEntity
    {
        public void Execute(ref AgentBody body, in InfantryLocomotion locomotion, in LocalTransform transform)
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