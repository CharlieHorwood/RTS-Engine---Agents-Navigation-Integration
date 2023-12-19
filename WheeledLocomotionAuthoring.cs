using ProjectDawn.Navigation.Hybrid;
using ProjectDawn.Navigation.Sample.Scenarios;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(AgentAuthoring))]
[DisallowMultipleComponent]
public class WheeledLocomotionAuthoring : MonoBehaviour
{
    [SerializeField]
    float Speed = 3.5f;

    [SerializeField]
    float Acceleration = 8;

    [SerializeField]
    float AngularSpeed = 120;

    [SerializeField]
    float StoppingDistance = 0;

    [SerializeField]
    bool AutoBreaking = true;

    Entity m_Entity;

    /// <summary>
    /// Returns default component of <see cref="TankLocomotion"/>.
    /// </summary>
    public WheeledLocomotion DefaultLocomotion => new()
    {
        Speed = Speed,
        Acceleration = Acceleration,
        AngularSpeed = math.radians(AngularSpeed),
        StoppingDistance = StoppingDistance,
        AutoBreaking = AutoBreaking,
    };
    /// Accessing this property is a potentially heavy operation as it will require wait for agent jobs to finish.
    /// </summary>
    public WheeledLocomotion EntityLocomotion
    {
        get => World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<WheeledLocomotion>(m_Entity);
        set => World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(m_Entity, value);
        
    }

    void Awake()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        m_Entity = GetComponent<AgentAuthoring>().GetOrCreateEntity();
        world.EntityManager.AddComponentData(m_Entity, DefaultLocomotion);
    }

    void OnDestroy()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world != null)
            world.EntityManager.RemoveComponent<WheeledLocomotion>(m_Entity);
    }
}

internal class WheeledLocomotionBaker : Baker<WheeledLocomotionAuthoring>
{
    public override void Bake(WheeledLocomotionAuthoring authoring)
    {
        AddComponent(GetEntity(TransformUsageFlags.Dynamic), authoring.DefaultLocomotion);
    }
}