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
public partial struct AircraftLocomotionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new AircraftLocomotionJob
        {
            DeltaTime = state.WorldUnmanaged.Time.DeltaTime
        }.ScheduleParallel();
    }

    [BurstCompile]
    partial struct AircraftLocomotionJob : IJobEntity
    {
        public float DeltaTime;
        public float3 tempVelocity;
        public float speedMultiplier;

        public void Execute(ref LocalTransform transform, ref AgentBody body, in AircraftLocomotion locomotion, in AgentShape shape)
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

            // Update rotation
            if (shape.Type == ShapeType.Circle)
            {
                float angle = math.atan2(body.Velocity.x, body.Velocity.y);
                transform.Rotation = math.slerp(transform.Rotation, quaternion.RotateZ(-angle), DeltaTime * locomotion.AngularSpeed);
            }
            else if (shape.Type == ShapeType.Cylinder)
            {
                float angle = math.atan2(body.Velocity.x, body.Velocity.z);
                transform.Rotation = math.slerp(transform.Rotation, quaternion.RotateY(angle), DeltaTime * locomotion.AngularSpeed);
            }

            float3 direction = math.normalizesafe(body.Velocity);
            float3 facing = math.mul(transform.Rotation, new float3(1, 0, 0));

            Quaternion rotationLook = Quaternion.LookRotation(direction, facing);

            // Interpolate velocity
            body.Velocity = math.lerp(body.Velocity, body.Force * maxSpeed, DeltaTime * locomotion.Acceleration);

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
        }
    }
}