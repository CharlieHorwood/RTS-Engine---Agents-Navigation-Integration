using Unity.Burst;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using ProjectDawn.Navigation;
using System;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(AgentLocomotionSystemGroup))]
public partial struct TrackedLocomotionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new TrackedLocomotionJob
        {
            DeltaTime = state.WorldUnmanaged.Time.DeltaTime
        }.ScheduleParallel();
    }

    [BurstCompile]
    partial struct TrackedLocomotionJob : IJobEntity
    {
        public float DeltaTime;
        public float3 tempVelocity;
        public float speedMultiplier;

        public void Execute(ref LocalTransform transform, ref AgentBody body, in TrackedLocomotion locomotion, in AgentShape shape)
        {
            if (body.IsStopped)
                return;

            // Check, if we reached the destination
            //float slowDistance = locomotion.ReduceSpeedDistance;
            float remainingDistance = body.RemainingDistance;

            if (remainingDistance <= locomotion.StoppingDistance + 1e-3f)
            {
                body.Velocity = 0;
                body.IsStopped = true;
                return;
            }
            float maxSpeed = locomotion.Speed;


            // Start breaking if close to destination
            if (locomotion.AutoBreaking)
            {
                float breakDistance = shape.Radius * 4 + locomotion.StoppingDistance;
                if (remainingDistance <= breakDistance)
                {
                    maxSpeed = math.lerp(locomotion.Speed, 0, DeltaTime * locomotion.Acceleration);
                }
            }

            // Force force to be maximum of unit length, but can be less
            float forceLength = math.length(body.Force);
            if (forceLength > 1)
                body.Force = body.Force / forceLength;

            // Tank should only move, if facing direction and movement direction is within certain degrees
            const float maxVelocityMagnitudeForSteering = 3f;
            speedMultiplier = 1;

            var velocityMagnitudeForSteering = math.clamp(math.length(body.Velocity), 0, maxVelocityMagnitudeForSteering);
            var magnitudeForSteering = velocityMagnitudeForSteering / maxVelocityMagnitudeForSteering;
            var forwardMultiplier = math.lerp(0, 1, magnitudeForSteering);

            var directionSign = 1;
            var adjustment = locomotion.Acceleration;

            //Debug.Log($" z rotation: {math.length(body.Velocity)}");

            if (math.dot(transform.Forward(), math.normalize(body.Force)) < 0 && math.length(body.Velocity) <
            maxVelocityMagnitudeForSteering)
            {
                directionSign = 1;
                adjustment = locomotion.Acceleration / 6;
            }
            var designatedForce = directionSign * math.lerp(transform.Forward() * math.length(body.Force), body.Force, forwardMultiplier);
            body.Velocity = math.lerp(body.Velocity, designatedForce * maxSpeed, math.saturate(DeltaTime * adjustment));

            float3 direction = math.normalizesafe(body.Velocity);
            float3 facing = math.mul(transform.Rotation, new float3(1, 0, 0));

            Quaternion rotationLook = Quaternion.LookRotation(direction, facing);

            float speed = math.length(body.Velocity);

            // Early out if steps is going to be very small
            if (speed < 1e-3f)
                return;

            // Avoid over-stepping the destination
            if (speed * DeltaTime > remainingDistance)
            {
                transform.Position += (body.Velocity / speed) * remainingDistance;
                return;
            }

            // Update position
            transform.Position += DeltaTime * body.Velocity;

            float angle = math.atan2(body.Velocity.x, body.Velocity.z);
            transform.Rotation = math.slerp(transform.Rotation, quaternion.RotateY(angle), DeltaTime * locomotion.AngularSpeed);
        }
    }
}