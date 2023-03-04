using com.cozyhome.Actors;
using com.cozyhome.Timers;
using UnityEngine;

public class DiveLiftState : ActorState
{
    [SerializeField] private TimerHeader.DeltaTimer LiftTimer;

    public override void Enter(ActorState prev)
    {
        Animator Animator = Machine.GetAnimator;
        Animator.SetInteger("Step", 2); // notify we are going to dive lift
        Animator.SetFloat("Speed", 0F); // notify we are going to dive lift
        Animator.SetFloat("Tilt", 0F); // notify we are going to dive lift

        LiftTimer.Reset();
    }

    public override void Exit(ActorState next) { }

    public override void Tick(float fdt)
    {
        LiftTimer.Accumulate(fdt);

        if (LiftTimer.Check())
            Machine.GetFSM.SwitchState("Ground");
    }

    protected override void OnStateInitialize() { }
    public override void OnGroundHit(ActorHeader.GroundHit ground, ActorHeader.GroundHit lastground, LayerMask layermask) { }
    public override void OnTraceHit(ActorHeader.TraceHitType tracetype, RaycastHit trace, Vector3 position, Vector3 velocity)
    { }

    public override void OnTriggerHit(ActorHeader.TriggerHitType triggertype, Collider trigger)
    { }
}
