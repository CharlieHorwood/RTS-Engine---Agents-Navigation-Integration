using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;

using ProjectDawn.Navigation.Hybrid;
using RTSEngine.Movement;

public class UnitSetup : MonoBehaviour, IEntityPreInitializable
{
    protected IEntity Entity { get; private set; }
    protected IUnit Unit { get; private set; }

    public UnitType UnitType = UnitType.None;

    public AgentsNavController AgentsNavController { get; set; }
    public AgentAuthoring Agent { set; get; }
    public AgentCylinderShapeAuthoring Shape { set; get; }
    public AgentNavMeshAuthoring NavMesh { set; get; }
    public AgentAvoidAuthoring Avoid { set; get; }
    public AgentSmartStopAuthoring SmartStop { set; get; }

    [HideInInspector]
    public int TabID = 0;
    public bool ConfigCompleted = false;

    public void OnEntityPreInit(IGameManager GameMgr, IEntity Entity)
    {
        this.Entity = Entity;
        this.Unit = Entity as IUnit;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
    }

    public void Disable()
    {
        
    }
}

public enum UnitType
{
    None,
    Infantry,
    Wheeled,
    Tracked,
    Air
}