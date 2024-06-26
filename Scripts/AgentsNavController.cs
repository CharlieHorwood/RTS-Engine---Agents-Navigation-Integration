﻿using ProjectDawn.Navigation.Hybrid;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Movement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

    public class AgentsNavController : MonoBehaviour, IMovementController
    {
        #region Attributes
        public bool Enabled
        {
            set { navAgent.enabled = value; }
            get => navAgent.enabled;
        }

        public bool IsActive
        {
            set
            {
                var currentBody = navAgent.EntityBody;
                currentBody.IsStopped = !value;
                navAgent.EntityBody = currentBody;
            }
            get => !navAgent.EntityBody.IsStopped;
        }

    public AgentAuthoring navAgent;
    public AgentCylinderShapeAuthoring agentShape;
    public AgentNavMeshAuthoring agentNavmesh;
    public AgentAvoidAuthoring agentAvoidance;
        //private NavMeshPath navPath;

        private MovementControllerData data;

        // Used to save the NavMeshAgent velocity to re-assign it when the game is paused and the unit resumes movement
        private Vector3 cachedVelocity;
        public IEntity Entity { private set; get; }
        public MovementControllerData Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;

                var currentSteering = navAgent.EntitySteering;

                currentSteering.Speed = data.speed;
                currentSteering.Acceleration = data.acceleration;
                //currentSteering.AngularSpeed = data.angularSpeed;
                currentSteering.StoppingDistance = data.stoppingDistance;

                navAgent.EntitySteering = currentSteering;

                var currentBody = navAgent.EntityBody;
                // If the speed value is positive and the movement was stopped (due to a game pause for example) before this assignment
                if (currentSteering.Speed > 0)
                {
                    if (currentBody.IsStopped)
                    {
                        currentBody.Velocity = cachedVelocity; // Assign velocity before the isStopped is enabled.
                        currentBody.IsStopped = false; // Enable movement
                    }
                }
                else
                {
                    if (!currentBody.IsStopped)
                    {
                        cachedVelocity = currentBody.Velocity; // Cache current velocity of unit. 
                        currentBody.IsStopped = true; // Disable movement
                        currentBody.Velocity = Vector3.zero; // Nullify velocity to stop any in progress movement
                    }
                }
                navAgent.EntityBody = currentBody;
            }
        }

        public LayerMask NavigationAreaMask => agentNavmesh.DefaulPath.AreaMask;

        public float Radius => agentShape.EntityShape.Radius;

        public Vector3 NextPathTarget => navAgent.transform.position;

        public MovementSource LastSource { get; private set; }
        public Vector3 LastDestination { get; private set; }

        public Vector3 Destination => navAgent.EntityBody.Destination;

        // Game services
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }

        // Other components
        protected IGameManager gameMgr { private set; get; }
        protected IMovementComponent mvtComponent { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, IMovementComponent mvtComponent, MovementControllerData data)
        {
            this.gameMgr = gameMgr;
            this.mvtComponent = mvtComponent;

            this.logger = gameMgr.GetService<IGameLoggingService>();

            Entity = mvtComponent?.Entity;
            if (!logger.RequireValid(Entity
                , $"[{GetType().Name}] Can not initialize without a valid Unit instance."))
                return;

            navAgent = Entity.gameObject.GetComponent<AgentAuthoring>();
            if (!logger.RequireValid(navAgent,
                $"[{GetType().Name} - '{Entity.Code}'] '{typeof(AgentAuthoring).Name}' component must be attached to the unit."))
                return;
            navAgent.enabled = true;

            agentShape = Entity.gameObject.GetComponent<AgentCylinderShapeAuthoring>();
            if (!logger.RequireValid(agentShape,
                $"[{GetType().Name} - '{Entity.Code}'] '{typeof(AgentCylinderShapeAuthoring).Name}' component must be attached to the unit."))
                return;
            agentShape.enabled = true;

            agentNavmesh = Entity.gameObject.GetComponent<AgentNavMeshAuthoring>();
            if (!logger.RequireValid(agentNavmesh,
                $"[{GetType().Name} - '{Entity.Code}'] '{typeof(AgentNavMeshAuthoring).Name}' component must be attached to the unit."))
                return;
            agentNavmesh.enabled = true;

            agentAvoidance = Entity.gameObject.GetComponent<AgentAvoidAuthoring>();
            if (!logger.RequireValid(agentAvoidance,
                $"[{GetType().Name} - '{Entity.Code}'] '{typeof(AgentAvoidAuthoring).Name}' component must be attached to the unit."))
                return;
            agentAvoidance.enabled = true;

            this.Data = data;

            mvtComponent.MovementStart += HandleMovementStart;
            mvtComponent.MovementStop += HandleMovementStop;
        }

        public void Disable()
        {
            mvtComponent.MovementStart -= HandleMovementStart;
            mvtComponent.MovementStop -= HandleMovementStop;
        }
        #endregion

        #region Preparing/Launching Movement
        public void Prepare(Vector3 destination, MovementSource source)
        {
            this.LastSource = source;
            this.LastDestination = destination;
            navAgent.SetDestination(destination);
        }

        public void Launch()
        {
            IsActive = true;
            navAgent.SetDestination(LastDestination);
        }
        #endregion

        #region Handling Movement Stop
        // When movement is stopped, it can stop and the velocity of the agent is still non-zero
        // When this happens, the unit will continue moving for a bit more before it fully stops
        // The logic below allows to launch a coroutine that keeps resetting the marker position until the velocity hits 0 and the unit fully stops
        private void HandleMovementStart(IMovementComponent sender, MovementEventArgs args)
        {
            if (markerResetPositionCoroutine != null)
                StopCoroutine(markerResetPositionCoroutine);
        }

        private void HandleMovementStop(IMovementComponent sender, EventArgs args)
        {
            markerResetPositionCoroutine = StartCoroutine(MarkerResetPositionCoroutine());
        }

        private Coroutine markerResetPositionCoroutine;
        private const float markerResetPositionDelay = 0.1f;
        private IEnumerator MarkerResetPositionCoroutine()
        {
            while (true)
            {
                var noMovement = (navAgent.EntityBody.Velocity == float3.zero);
                if (noMovement.x && noMovement.y && noMovement.z)
                    yield break;

                yield return new WaitForSeconds(markerResetPositionDelay);

                mvtComponent.TargetPositionMarker.Toggle(true, mvtComponent.Entity.transform.position);
            }
        }
        #endregion

        public void OnCarrierEnter()
        {
            //Disable navmesh agent transform syncing when unit enters building, otherwise the attacks will be launched from the agent's virtual position instead of the unit's position
            //navAgent.updatePosition = false;
            agentAvoidance.enabled = false;

        }

        public void OnCarrierExit()
        {
            //navAgent.updatePosition = true;
            agentAvoidance.enabled = true;
        }
    }
