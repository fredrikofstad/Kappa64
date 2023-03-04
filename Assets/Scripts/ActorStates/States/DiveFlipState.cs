using System.Collections;
using System.Collections.Generic;
using com.cozyhome.Actors;
using com.cozyhome.Vectors;
using UnityEngine;

public class DiveFlipState : ActorState
{
    [SerializeField] private AnimationCurve FallCurve;
    [SerializeField] private float FlipHeight = 1.0F;
    [SerializeField] private float HorizontalVelocity = 1.2F;

    private float InitYVelocity;

    public override void Enter(ActorState prev)
    {
        Animator Animator = Machine.GetAnimator;
        ActorHeader.Actor Actor = Machine.GetActor;

        Animator.SetInteger("Step", 1);
        Animator.SetFloat("Speed", 0F);
        Animator.SetFloat("Time", 0F);
        Animator.SetFloat("Tilt", 0F);

        Vector3 Velocity = Actor.velocity;

        Velocity[1] = 0F;
        Velocity[1] = Velocity.magnitude;

        if(Velocity[1] != 0F)
        {
            for (int i = 0; i < 3; i += 2)
                Velocity[i] *= (HorizontalVelocity / Velocity[1]);
        }

        InitYVelocity = Velocity[1] = Mathf.Sqrt(2F * FlipHeight * PlayerVars.GRAVITY);
        Actor.SetVelocity(Velocity);
        Actor.SetSnapEnabled(false);
    }

    public override void Exit(ActorState next)
    {
        ActorHeader.Actor Actor = Machine.GetActor;
        Animator Animator = Machine.GetAnimator;

        Actor.SetSnapEnabled(true);
        Animator.SetFloat("Time", 0F);
    }

    public override void OnGroundHit(ActorHeader.GroundHit ground, ActorHeader.GroundHit lastground, LayerMask layermask)
    { }

    public override void OnTraceHit(ActorHeader.TraceHitType tracetype, RaycastHit trace, Vector3 position, Vector3 velocity)
    { }

    public override void OnTriggerHit(ActorHeader.TriggerHitType triggertype, Collider trigger)
    { }

    public override void Tick(float fdt)
    {
        Transform ModelView = Machine.GetModelView;
        LedgeRegistry LedgeRegistry = Machine.GetLedgeRegistry;
        Animator Animator = Machine.GetAnimator;
        ActorHeader.Actor Actor = Machine.GetActor;
        Vector3 Velocity = Actor.velocity;

        float percent = Velocity[1] / InitYVelocity;
        if (percent < 0F)
            percent = 0F;

        float amount = FallCurve.Evaluate(percent);

        // Animator.SetFloat("Time", amount);
        if(ActorStateHeader.Transitions.CheckGeneralLedgeTransition(
            Actor.position,
            ModelView.forward,
            Actor.orientation,
            LedgeRegistry,
            Machine))
            return;
        else if (Actor.Ground.stable && Mathf.Abs(VectorHeader.Dot(Velocity, Actor.Ground.normal)) <= 0.1F)
        { 
            /* only land when our velocity is penetrating into the ground plane */
            Machine.GetFSM.SwitchState("Ground");
            return;
        }
        else
        {
            ActorStateHeader.AccumulateConstantGravity(ref Velocity, fdt, PlayerVars.GRAVITY);
            Actor.SetVelocity(Velocity);
            return;
        }
    }

    protected override void OnStateInitialize() { }
}
