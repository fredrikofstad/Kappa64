using UnityEngine;
using com.cozyhome.Archetype;
using com.cozyhome.Vectors;
using System;

namespace com.cozyhome.Actors
{
    public static class ActorHeader
    {
        // I've chosen to use a function pointer mapping instead
        // of an ugly switch statement as it just seems more
        // convenient this way. Feel free to change it if you're
        // experiencing excessive slowdowns, I don't really know
        // how C# optimizes these sorts of things, so just give
        // me a break :)
        private delegate void MoveFunc(IActorReceiver _rec, Actor _actor, float fdt);
        private static readonly MoveFunc[] _movefuncs = new MoveFunc[4]
        {
            Actor.Fly, // 0
            Actor.Slide, // 1
            Actor.SlideStep, // 2
            Actor.Noclip // 3
        };

        // im a bit worried about casting to int but premature optimization isn't healthy and should fuck off for the time being
        // ActorHeader.Move() will be the main function that you'll be using to interface with in order to get your character moving around in the scene.
        // In order to efficiently interface with these move calls you'll benefit from are the IActor callbacks automatically attached to your actor.

        // Now that I think about it, I should really write a complementary sub-package that links the ActorHeader to some state machine based integration
        // specifically designed for the Actor system I've designed (just a thought). 
        public static void Move(IActorReceiver _rec, Actor _actor, float fdt) => _movefuncs[(int)_actor.GetMoveType].Invoke(_rec, _actor, fdt);

        public enum SlideSnapType { Never = 0, Toggled = 1, Always = 2 };
        public enum MoveType
        {
            Fly       = 0, /* PM_FlyMove() */
            Slide     = 1, /* PM_SlideMove() */
            SlideStep = 2, /*PM_SlideStepMove() */
            Noclip    = 3 /* PM_NoclipMove() */
        };
        public enum GroundTraceType {
            Default  = 0,
            Assigned = 1
        };

        // A shameless data class that I use to store grounding information. I'm not fucking bothering
        // with getters and setters as they pollute the class and make it more complicated than it 
        // needs to be. If you somehow change the data in these hits, that's on you. I've kept it open-ended
        // so anybody can do what they want with this class when handling the actor's state.
        [System.Serializable]
        public class GroundHit
        {
            public Vector3 actorpoint; // our actor's position at the time of our hit
            public Vector3 point; // our trace point
            public Vector3 normal; // our trace normal
            public Collider collider;
            public float distance; // our trace distance
            public bool stable; // is our trace stable?
            public bool snapped; // is our trace snapping?

            public void Clear()
            {
                actorpoint = Vector3.zero;
                point = Vector3.zero;
                normal = Vector3.zero;
                distance = 0.0F;
                stable = false;
                snapped = false;
                collider = null;
            }
        }

        /* Heavily OOP-designed. Not a fan but it allows for an easier understanding/development
           of future content
        */
        public abstract class Actor : MonoBehaviour
        {
            [Header("Move Type Properties")]
            [Tooltip("The move type the actor will resort to when its Move func is called by the end-user. \nFly = The actor will fly around the scene whilst resolving collision.\nSlide = The actor will slide around the scene whilst resolving collision and simulating ground detection.\nNoclip = The actor will fly around the scene whilst ignoring all collisions")]
            [SerializeField] private MoveType MoveType = ActorHeader.MoveType.Fly;

            [Tooltip("The snap type the actor will abide by when determining its ground state. \nNever = The actor will never snap to the ground. \nToggled = The actor will only snap to the ground if its snapenabled boolean is set to true. \nAlways = The actor will always snap to the ground.")]
            [Header("Snap Type Properties")]
            [SerializeField] private SlideSnapType SnapType = SlideSnapType.Always;
            [Tooltip("The snap type the actor will abide by when determining its ground state. \nNever = The actor will never snap to the ground. \nToggled = The actor will only snap to the ground if its snapenabled boolean is set to true. \nAlways = The actor will always snap to the ground.")]
            [Header("Snap Type Properties")]
            [SerializeField] private GroundTraceType GroundTraceType = GroundTraceType.Default;
            [Tooltip("Whether or not the actor will snap to the ground if its snap type is set to SlideSnapType.Toggled enum.")]
            [SerializeField] private bool SnapEnabled = true;
            [Header("Stepping Properties")]
            [Tooltip("Whether or not the actor will step on perpendicular(ish) surfaces during the traceback loop")]
            [SerializeField] private bool StepEnabled = true;
            [Tooltip("How far up the actor can step upwards when tracing into obstructing planes")]
            [SerializeField] private float StepHeight = 0.5F;
            [Header("Initialization Properties")]
            [Tooltip("Whether this actor will initialize itself using Unity's Start() invokation")]
            [SerializeField] private bool InitializeOnStart = true;

            [Header("Ground Stability Properties")]
            [Tooltip("The maximum angular difference a traced plane must make to the grounding plane in order to be classified as an obstruction.")]
            [SerializeField] private float MaximumStableSlideAngle = 65F;

            [Header("Actor Filter Properties")]
            [Tooltip("A Bitmask to help you filter out specific sets of colliders you want this actor to ignore during its movement.")]
            [SerializeField] protected LayerMask _filter;

            private GroundHit _groundhit;
            private GroundHit _lastgroundhit;

            [System.NonSerialized] protected readonly RaycastHit[] _internalhits = new RaycastHit[ActorHeader.MAX_HITS];

            [System.NonSerialized] protected readonly Collider[] _internalcolliders = new Collider[ActorHeader.MAX_OVERLAPS];

            [System.NonSerialized] protected readonly Vector3[] _internalnormals = new Vector3[ActorHeader.MAX_OVERLAPS];

            /* 
             * I'm keeping these public so you can do whatever you'd like. I'm not necessarily concerned with how you're updating any of these things.
               Whatever you do though, don't update them inside of an IActorReceiver callback. Those callbacks are intended to be for response and are simply
               for information and state handling. If anything, implement a command pattern where you respond to these callbacks inside of your pulse or update
               loop somewhere. This will keep the actor's state information outside of its movement call. (Changing the Actor's velocity mid-trace will cause 
               ambiguous/undefined behaviour!!!)
            */
            [System.NonSerialized] public Vector3 position;
            [System.NonSerialized] public Vector3 velocity;
            [System.NonSerialized] public Quaternion orientation;
            [System.NonSerialized] public Vector3 groundtracedir;

            public RaycastHit[] Hits => _internalhits;
            public Collider[] Colliders => _internalcolliders;
            public bool IsSnapEnabled => SnapEnabled;
            public MoveType GetMoveType => MoveType;
            public SlideSnapType GetSnapType => SnapType;
            public GroundTraceType GetGroundTraceType => GroundTraceType;
            public bool GetSnapEnabled => SnapEnabled;
            public bool GetStepEnabled => StepEnabled;
            public float GetStepHeight => StepHeight;
            public GroundHit Ground => _groundhit;
            public GroundHit LastGround => _lastgroundhit;
            public LayerMask Mask => _filter;

            // Feel free to call these methods directly if you'd like. I don't plan on forcing anyone on a particular path to achieve something
            // as simple as displacing a primitive.
            public static void Fly(IActorReceiver receiver, Actor actor, float fdt) => PM_FlyMove(receiver, actor, fdt);
            public static void Slide(IActorReceiver receiver, Actor actor, float fdt) => PM_SlideMove(receiver, actor, fdt);
            public static void SlideStep(IActorReceiver receiver, Actor actor, float fdt) => PM_SlideStepMove(receiver, actor, fdt);
            public static void Noclip(IActorReceiver receiver, Actor actor, float fdt) => PM_NoclipMove(receiver, actor, fdt);

            public void SetVelocity(Vector3 velocity) => this.velocity = velocity;
            public void SetPosition(Vector3 position) => this.position = position;
            public void SetGroundTraceDir(Vector3 groundtracedir) => this.groundtracedir = groundtracedir;
            public void SetOrientation(Quaternion orientation) => this.orientation = orientation;
            public void SetMoveType(MoveType movetype) => this.MoveType = movetype;
            public void SetGroundTraceType(GroundTraceType gtracetype) => this.GroundTraceType = gtracetype;
            public void SetSnapType(SlideSnapType snaptype) => this.SnapType = snaptype;
            public void SetSnapEnabled(bool snapenabled) => this.SnapEnabled = snapenabled;
            public void SetStepEnabled(bool stepenabled) => this.StepEnabled = stepenabled;

            /* UnityEngine*/
            void Start()
            {
                if (InitializeOnStart)
                    InitializeActor(transform);
            }

            public void InitializeActor(Transform _transform)
            {
                InitializeTransforms(_transform);
                InitializeSpecifics();
            }

            private void InitializeTransforms(Transform _transform) /* a kind of default ctor */
            {
                _groundhit = new GroundHit();
                _lastgroundhit = new GroundHit();

                SetPosition(_transform.position);
                SetOrientation(_transform.rotation);
                SetVelocity(Vector3.zero);
            }

            protected abstract void InitializeSpecifics(); /* exclusively implemented by each actor type */

            public abstract ArchetypeHeader.Archetype GetArchetype();
            public abstract bool DetermineGroundStability(Vector3 velocity, RaycastHit hit, LayerMask gfilter);
            public virtual bool DeterminePlaneStability(Vector3 plane, Collider otherc) => Vector3.Angle(plane, orientation * Vector3.up) <= MaximumStableSlideAngle;

        }

        #region Fly

        /*
         PM_FlyMove() is one of the Move() variants packaged with the Actor sub-package found in the
         decoupling GitHub repository. It's purpose is to allow the player to 'fly' around the physics scene
         whilst also keeping into account the colliders and geometric planes that represent your levels. Use 
         this method primarily if you are dealing with a sort of 'spectating' or 'flying' mechanic for your
         actors.
        */
        public static void PM_FlyMove(IActorReceiver _rec, Actor actor, float _fdt)
        {
            /*
                Steps:

                1. Discrete Overlap Resolution
                2. Continuous Collision Prevention    
            */

            /* actor transform values */
            Vector3 position = actor.position;
            Vector3 velocity = actor.velocity;
            Quaternion orientation = actor.orientation;

            /* archetype buffers & references */
            Vector3 lastplane = Vector3.zero;

            QueryTriggerInteraction querytype = QueryTriggerInteraction.Collide;
            ArchetypeHeader.Archetype archetype = actor.GetArchetype();
            Collider self = archetype.Collider();
            Collider[] colliderbuffer = actor.Colliders;
            LayerMask layermask = actor.Mask;

            RaycastHit[] tracesbuffer = actor.Hits;

            /* tracing values */
            float timefactor = 1F;
            float skin = ArchetypeHeader.GET_SKINEPSILON(archetype.PrimitiveType());

            int numbumps = 0;
            int numpushbacks = 0;
            int geometryclips = 0;
            Vector3 last = Vector3.zero;

            /* attempt an overlap pushback at this current position */
            while (numpushbacks++ < ActorHeader.MAX_PUSHBACKS)
            {
                archetype.Overlap(
                    position,
                    orientation,
                    layermask,
                    /* inflate */ 0F,
                    _interacttype: querytype,
                    colliderbuffer,
                    out int numoverlaps);

                /* filter ourselves out of the collider buffer */
                FilterHeader.ActorOverlapFilter(_rec, ref numoverlaps, self, colliderbuffer);

                if (numoverlaps == 0) // nothing !
                    break;
                else
                {
                    /* pushback against the first valid penetration found in our collider buffer */
                    for (int ci = 0; ci < numoverlaps; ci++)
                    {
                        Collider otherc = colliderbuffer[ci];
                        Transform othert = otherc.GetComponent<Transform>();

                        if (Physics.ComputePenetration(self,
                            position,
                            orientation,
                            otherc,
                            othert.position,
                            othert.rotation,
                            out Vector3 normal,
                            out float mindistance))
                        {
                            /* resolve pushback using closest exit distance */
                            position += normal * (mindistance + MIN_PUSHBACK_DEPTH);

                            /* only consider normals that we are technically penetrating into */
                            if (VectorHeader.Dot(velocity, normal) < 0F)
                                PM_FlyDetermineImmediateGeometry(ref velocity, ref lastplane, normal, ref geometryclips);

                            break;
                        }
                    }
                }
            }

            // We must assume that our position is valid.
            // actor.SetPosition(position);

            while (numbumps++ <= ActorHeader.MAX_BUMPS && timefactor > 0)
            {
                // Begin Trace
                Vector3 _trace = velocity * _fdt;
                float _tracelen = _trace.magnitude;

                // IF unable to trace any further, break and end
                if (_tracelen <= MIN_DISPLACEMENT)
                {
                    timefactor = 0;
                    break;
                }
                else
                {
                    archetype.Trace(
                    position,
                    _trace / _tracelen,
                    _tracelen + skin, /* prevent tunneling by using this skin length */
                    orientation,
                    layermask,
                    0F,
                    _interacttype: querytype,
                    tracesbuffer,
                    out int _tracecount);

                    FilterHeader.ActorTraceFilter(
                        _rec,
                        ref _tracecount,
                        out int _i0,
                        ArchetypeHeader.GET_TRACEBIAS(archetype.PrimitiveType()),
                        self,
                        tracesbuffer);

                    if (_i0 <= -1) /* Nothing was discovered in our trace */
                    {
                        timefactor = 0; // end move
                        position += _trace;
                        break;
                    }
                    else /* Discovered an obstruction along our linear path */
                    {
                        RaycastHit _closest = tracesbuffer[_i0];
                        float _rto = _closest.distance / _tracelen;
                        timefactor -= _rto;

                        float _dis = Mathf.Max(_closest.distance - skin, 0F);
                        position += (_trace / _tracelen) * _dis; /* Move back! */

                        /* determine our topology state */
                        PM_FlyDetermineImmediateGeometry(ref velocity, ref lastplane, _closest.normal, ref geometryclips);
                        continue;
                    }
                }
            }

            /* Safety check to prevent multiple actors phasing through each other... Feel free to disable this for performance if you'd like*/
            archetype.Overlap(
                    position,
                    orientation,
                    layermask,
                    /* inflate */ 0F,
                    _interacttype: querytype,
                    colliderbuffer,
                    out int safetycount);

            /* filter ourselves out of the collider buffer, no need to check for triggers */
            ArchetypeHeader.OverlapFilters.FilterSelf(ref safetycount, self, colliderbuffer);

            if(safetycount == 0)
                actor.SetPosition(position);

            actor.SetVelocity(velocity);
        }

        /* Not used anymore */
        private static bool PM_IsActor(Collider otherc) => otherc.GetComponent<Actor>() != null;

        private static void PM_FlyDetermineImmediateGeometry(
            ref Vector3 velocity,
            ref Vector3 lastplane,
            Vector3 plane,
            ref int geometryclips)
        {
            switch (geometryclips)
            {
                case 0: /* the first penetration plane has been identified in the feedback loop */
                    PM_FlyClipVelocity(ref velocity, plane);
                    geometryclips |= (1 << 0);
                    break;
                case (1 << 0): /* two planes have been discovered, which potentially result in a crease */

                    if (Mathf.Abs(VectorHeader.Dot(lastplane, plane)) < FLY_CREASE_EPSILON)
                    {
                        Vector3 crease = Vector3.Cross(lastplane, plane);
                        crease.Normalize();

                        VectorHeader.ProjectVector(ref velocity, crease);
                        geometryclips |= (1 << 1);
                    }
                    else
                        PM_FlyClipVelocity(ref velocity, plane);

                    break;
                case (1 << 0) | (1 << 1): /* three planes have been detected, our velocity must be cancelled entirely. */
                    velocity = Vector3.zero;
                    geometryclips |= (1 << 2);
                    break;
            }

            lastplane = plane;
        }

        public static void PM_FlyClipVelocity(ref Vector3 velocity, Vector3 plane)
        {
            float len = velocity.magnitude;
            if (len <= 0F) // preventing NaN generation
                return;
            else if (VectorHeader.Dot(velocity / len, plane) < 0F) // only clip if we're piercing into the infinite plane 
                VectorHeader.ClipVector(ref velocity, plane);
        }

        #endregion

        #region Slide

        /*
         PM_SlideMove() is one of the several variant Move() funcs available standard with the
         Actor package provided. It's entire purpose is to 'slide' and 'snap' the Actor on 'stable'
         surfaces whilst also dealing with the conventional issue of movement into and along blocking
         planes in the physics scene. Use this method primarily if you plan on keeping your actor level
         with the floor.
        */
        public static void PM_SlideMove(
            IActorReceiver receiver,
            Actor actor,
            float fdt)
        {
            /* BASE CASES IN WHICH WE SHOULDN'T MOVE AT ALL */
            if (actor == null || receiver == null)
                return;

            /* Steps:    
                1. Continuous Ground Resolution
                2. Discrete Overlap Resolution
                3. Continuous Trace Prevention
            */

            /* actor transform values */
            Vector3 position = actor.position;
            Vector3 velocity = actor.velocity;
            Quaternion orientation = actor.orientation;

            /* archetype buffers & references */
            QueryTriggerInteraction querytype = QueryTriggerInteraction.Collide;
            ArchetypeHeader.Archetype archetype = actor.GetArchetype();
            Collider self = archetype.Collider();
            SlideSnapType snaptype = actor.GetSnapType;
            GroundTraceType gtracetype = actor.GetGroundTraceType;
            Collider[] colliderbuffer = actor.Colliders;
            LayerMask layermask = actor.Mask;

            RaycastHit[] tracebuffer = actor.Hits;

            /* ground trace values */
            Vector3 gposition = position;
            Vector3 groundtracedir = gtracetype == GroundTraceType.Default ? orientation * new Vector3(0, -1, 0) : actor.groundtracedir;

            /* trace values */
            Vector3 lastplane = Vector3.zero;
            Vector3 updir = orientation * new Vector3(0, 1, 0);

            float timefactor = 1F;
            float skin = ArchetypeHeader.GET_SKINEPSILON(archetype.PrimitiveType());
            float bias = ArchetypeHeader.GET_TRACEBIAS(archetype.PrimitiveType());

            int numbumps = 0;
            int numgroundbumps = 0;
            int numpushbacks = 0;
            int geometryclips = 0;

            GroundHit ground = actor.Ground;
            GroundHit lastground = actor.LastGround;

            /* preserve last frame ground data into another struct */
            lastground.actorpoint = ground.actorpoint;
            lastground.normal = ground.normal;
            lastground.point = ground.point;
            lastground.stable = ground.stable;
            lastground.snapped = ground.snapped;
            lastground.distance = ground.distance;
            lastground.collider = ground.collider;

            ground.Clear();

            /* 
               I personally wish I could figure out a "stateless" way to implement ground snapping, but
               for the time being this seems to work best.
            */

            /* feel free to change these values, I think they're pretty decent atm */
            float gtracelen = (lastground.stable && lastground.snapped) ? MAX_GROUNDQUERY : MIN_GROUNDQUERY;

            while (numgroundbumps++ < MAX_GROUNDBUMPS &&
                gtracelen > 0F)
            {
                /*
                    -- trace along dir --

                    if detected 
                        if stable : 
                            end trace and determine whether a snap is to occur
                        else :
                            clip along floor
                            continue
                    else : 
                    break out of loop as no floor was detected
                */

                /* 
                    IMPORTANT! 
                        Our ground snapping position needs to be offset
                        upward as our trace may end up "tunneling" as a result of 
                        being too close to the floor. 

                        We compensate this offset by increasing our trace length to traverse
                        the offset distance in addition to our additional length

                */
                archetype.Trace(gposition + (updir * skin),
                    groundtracedir,
                    gtracelen + skin,
                    orientation,
                    layermask,
                    /* inflate */ 0F,
                    QueryTriggerInteraction.Ignore,
                    tracebuffer,
                    out int numgroundtraces);

                /* filter out our archetype and find the closest valid interception */
                ArchetypeHeader.TraceFilters.FindClosestFilterInvalids(
                    ref numgroundtraces,
                    out int i0,
                    bias,
                    self,
                    tracebuffer);

                if (i0 >= 0) /* an intersection has occured, but we aren't sure its ground yet */
                {
                    RaycastHit _closest = tracebuffer[i0];

                    ground.distance = _closest.distance;
                    ground.point = _closest.point;
                    ground.normal = _closest.normal;
                    ground.actorpoint = gposition;
                    ground.stable = actor.DetermineGroundStability(velocity, _closest, layermask);
                    ground.snapped = false;
                    ground.collider = _closest.collider;

                    gposition += groundtracedir * (_closest.distance);
                    /*
                     warp regardless of stablility. We'll only be setting our trace position
                     to our ground trace position if a stable floor has been determined, and snapping is enabled. 
                    */

                    if (ground.stable)
                    {
                        /* 
                         Assume we can snap automatically if our snap type
                         is set to always 
                        */
                        bool cansnap = (snaptype == SlideSnapType.Always);

                        /*
                         Handling the other snap types:
                        */
                        switch (snaptype)
                        {
                            case SlideSnapType.Never:
                                cansnap = false;
                                break;
                            case SlideSnapType.Toggled:
                                /* Snap type is determined by a separate bool */
                                cansnap = actor.IsSnapEnabled;
                                break;
                        }

                        ground.snapped = cansnap;

                        /* trace upwards to see if we can fit inside a snap position and a ceiling
                            that may exist above it... */
                        archetype.Trace(
                            gposition,
                            updir,
                            skin + 0.1F,
                            orientation,
                            layermask,
                            0F,
                            QueryTriggerInteraction.Ignore,
                            tracebuffer,
                            out int numupwardbumps);

                        ArchetypeHeader.TraceFilters.FindClosestFilterInvalids(ref numupwardbumps,
                            out int gi,
                            bias,
                            self,
                            tracebuffer);

                        /* this part may be a bit confusing to understand */

                        /* if a ceiling is discovered: */
                        if (gi >= 0)
                        {
                            /* 
                             generate a potential crease vector given 
                             the ground normal and the ceiling normal
                            */

                            RaycastHit snaptrace = tracebuffer[gi];

                            Vector3 crease = Vector3.Cross(snaptrace.normal, ground.normal);
                            crease.Normalize();

                            Vector3 forw = Vector3.Cross(updir, crease);
                            forw.Normalize();

                            if (VectorHeader.Dot(velocity, forw) <= 0F)
                            {
                                if (VectorHeader.Dot(velocity, snaptrace.normal) < 0F)
                                    receiver.OnTraceHit(TraceHitType.Ceiling, snaptrace, gposition, velocity);

                                geometryclips |= (1 << 1);
                                VectorHeader.ProjectVector(ref velocity, crease);
                            }

                            /*
                             if a ceiling is found, only move upward by the difference
                             between our ceiling height and our skin.

                             in the scenario where our ceiling distance is greater than 
                             our skin length, only move the minimum amount (skin)

                             in the scenario where our ceiling distance is less than our skin,
                             we've potentially encountered a tunneling position. In this scenario,
                             don't move upward at all.
                            */

                            gposition += updir * Mathf.Max(Mathf.Min(snaptrace.distance - skin, skin), 0F);
                        }
                        else /* if no ceiling is found, move the require distance upward */
                            gposition += updir * (skin);

                        /* 
                         if a snap was valid and allowed, 
                         1. set our position to the snap point 
                         
                         2. set our last plane discovered to the ground normal
                         
                         3. notify our geometry clipping algorithm that we've hit our first blocking plane
                         
                         4. clip our velocity along the ground normal as we are effectively skipping the 
                            first iteration of our geometry clipping algorithm

                        */

                        if (ground.snapped)
                        {
                            position = gposition;

                            lastplane = ground.normal;
                            geometryclips |= (1 << 0);

                            VectorHeader.ClipVector(ref velocity, ground.normal);
                        }

                        /* 
                         1. send a callback to our receiver to notify them we've hit ground somewhere 
                         2. set gtracelen to zero to exit out of ground snap loop                        
                        */

                        receiver.OnGroundHit(ground, lastground, layermask);
                        gtracelen = 0F;
                    }
                    else
                    {
                        /* 
                        1. clip our ground tracing direction along the normal we've discovered 
                            -this is effectively creating a normal that "rides" along it tangentially
                        
                        2. normalize it for next iteration
                        
                        3. subtract our trace length for next iteration
                        */

                        VectorHeader.ClipVector(ref groundtracedir, _closest.normal);
                        groundtracedir.Normalize();
                        gtracelen -= _closest.distance;
                    }
                }
                else /* nothing discovered, end out of our ground loop */
                    gtracelen = 0F;
            }

            while (numpushbacks++ < ActorHeader.MAX_PUSHBACKS)
            {
                archetype.Overlap(
                    position,
                    orientation,
                    layermask,
                    /* inflate */ 0F,
                    _interacttype: querytype,
                    colliderbuffer,
                    out int numoverlaps);

                FilterHeader.ActorOverlapFilter(
                    receiver,
                    ref numoverlaps,
                    self,
                    colliderbuffer);

                if (numoverlaps == 0) // nothing !
                    break;
                else
                {
                    for (int _colliderindex = 0; _colliderindex < numoverlaps; _colliderindex++)
                    {
                        Collider otherc = colliderbuffer[_colliderindex];
                        Transform othert = otherc.GetComponent<Transform>();

                        if (Physics.ComputePenetration(self,
                            position,
                            orientation,
                            otherc,
                            othert.position,
                            othert.rotation,
                            out Vector3 _normal,
                            out float _distance))
                        {
                            position += _normal * (_distance + skin);

                            PM_SlideDetermineImmediateGeometry(ref velocity,
                                ref lastplane,
                                actor.DeterminePlaneStability(_normal, otherc),
                                _normal,
                                ground.normal,
                                ground.stable && ground.snapped,
                                updir,
                                ref geometryclips
                            );
                            break;
                        }
                    }
                }
            }
            
            while (numbumps++ < ActorHeader.MAX_BUMPS
                  && timefactor > 0)
            {
                // Begin Trace
                Vector3 _trace = velocity * fdt;
                float _tracelen = _trace.magnitude;

                // IF unable to trace any further, break and end
                if (_tracelen <= MIN_DISPLACEMENT)
                    timefactor = 0;
                else
                {
                    archetype.Trace(position,
                        _trace / _tracelen,
                        _tracelen + skin,
                        orientation,
                        layermask,
                        0F,
                        _interacttype: querytype,
                        tracebuffer,
                        out int _tracecount);

                    FilterHeader.ActorTraceFilter(
                        receiver,
                        ref _tracecount,
                        out int _i0,
                        bias,
                        self,
                        tracebuffer);

                    if (_i0 <= -1) // nothing discovered :::
                    {
                        timefactor = 0; // end move
                        position += _trace;
                        break;
                    }
                    else // discovered an obstruction:::
                    {
                        RaycastHit _closest = tracebuffer[_i0];
                        Vector3 _normal = _closest.normal;

                        float _rto = _closest.distance / (_tracelen + skin);
                        timefactor -= _rto;

                        float _dis = _closest.distance - skin;
                        position += (_trace / _tracelen) * _dis; // move back along the trace line!

                        receiver.OnTraceHit(TraceHitType.Trace, _closest, position, velocity);

                        PM_SlideDetermineImmediateGeometry(ref velocity,
                                ref lastplane,
                                actor.DeterminePlaneStability(_normal, _closest.collider),
                                _normal,
                                ground.normal,
                                ground.stable && ground.snapped,
                                updir,
                                ref geometryclips);

                        continue;
                    }
                }
            }

            actor.SetPosition(position);
            actor.SetVelocity(velocity);
        }

        // This func is vital to preventing undesirable behaviour throughout the lifetime of the
        // PM_SlideMove() execution loop. This function is responsible for identifying the geometry around
        // an actor's position throughout the duration of the move. 
        // It is responsible for:
        //      Handling generic velocity clipping
        //      Handling generic crease projecting
        //      Preventing tunneling at corners/creases at any point in our movement.
        private static void PM_SlideDetermineImmediateGeometry(
            ref Vector3 velocity,
            ref Vector3 lastplane,
            bool stability,
            Vector3 plane,
            Vector3 groundplane,
            bool groundstability,
            Vector3 up,
            ref int geometryclips)
        {
            switch (geometryclips)
            {
                case 0: // plane detected
                    PM_SlideClipVelocity(ref velocity, stability, plane, groundstability, groundplane, up);
                    geometryclips |= (1 << 0);
                    break;
                case (1 << 0): // potential crease detected

                    float _od = Mathf.Abs(VectorHeader.Dot(lastplane, plane));
                    if (!stability && _od < FLY_CREASE_EPSILON)
                    {
                        Vector3 _c2 = Vector3.Cross(lastplane, plane);
                        _c2.Normalize();
                        VectorHeader.ProjectVector(ref velocity, _c2);
                        geometryclips |= (1 << 1);
                    }
                    else {
                        PM_SlideClipVelocity(ref velocity, stability, plane, groundstability, groundplane, up);
                    }
                    break;
                case (1 << 0) | (1 << 1): // multiple creases detected
                    velocity = Vector3.zero;
                    geometryclips |= (1 << 2);
                    break;
            }
            
            lastplane = plane;
        }


        // The velocity 'clipping' algorithm that is ran any time a plane is detected throughout
        // the PM_SlideMove() func execution.
        // It is responsible for:
        //      Handling velocity orientation along stable planes
        //      Handling velocity clipping along unstable 'wall' planes
        public static void PM_SlideClipVelocity(
            ref Vector3 velocity,
            bool stability,
            Vector3 plane,
            bool groundstability,
            Vector3 groundplane,
            Vector3 up)
        {
            float len = velocity.magnitude;
            if (len <= 0F) // preventing NaN generation
                return;
            else
            {
                if (VectorHeader.Dot(velocity / len, plane) < 0F) // only clip if we're piercing into the infinite plane 
                {
                    if (stability) // if stable, just orient and maintain magnitude
                    {
                        // anyways just clip along the newly discovered stable plane
                        // preserve magnitude
                        VectorHeader.ClipVector(ref velocity, plane);

                        // im indifferent to whether I should clip or orient at this stage, but for now
                        // we'll stick to clipping
                    }
                    else
                    {
                        if (groundstability) // clip along the surface of the ground
                        {
                            // clip normally
                            VectorHeader.ClipVector(ref velocity, plane);
                            // orient velocity to ground plane
                            VectorHeader.CrossProjection(ref velocity, up, groundplane);
                        }
                        else // wall clip
                            VectorHeader.ClipVector(ref velocity, plane);
                    }
                }
                else
                    return;
            }
        }

        #endregion

        #region Slide/Step

        /*
         PM_SlideMove() is one of the several variant Move() funcs available standard with the
         Actor package provided. It's entire purpose is to 'slide' and 'snap' the Actor on 'stable'
         surfaces whilst also dealing with the conventional issue of movement into and along blocking
         planes in the physics scene. Use this method primarily if you plan on keeping your actor level
         with the floor.
        */
        public static void PM_SlideStepMove(
            IActorReceiver receiver,
            Actor actor,
            float fdt)
        {
            /* BASE CASES IN WHICH WE SHOULDN'T MOVE AT ALL */
            if (actor == null || receiver == null)
                return;

            /* Steps:    
                1. Continuous Ground Resolution
                2. Discrete Overlap Resolution
                3. Continuous Trace Prevention
            */

            /* actor transform values */
            Vector3 position = actor.position;
            Vector3 velocity = actor.velocity;
            Quaternion orientation = actor.orientation;

            /* archetype buffers & references */
            QueryTriggerInteraction querytype = QueryTriggerInteraction.Collide;
            ArchetypeHeader.Archetype archetype = actor.GetArchetype();
            Collider self = archetype.Collider();
            SlideSnapType snaptype = actor.GetSnapType;
            GroundTraceType gtracetype = actor.GetGroundTraceType;
            Collider[] overlapbuffer = actor.Colliders;
            LayerMask layermask = actor.Mask;

            RaycastHit[] tracebuffer = actor.Hits;

            /* ground trace values */
            Vector3 gposition = position;            
            Vector3 groundtracedir = gtracetype == GroundTraceType.Default ? orientation * new Vector3(0, -1, 0) : actor.groundtracedir;

            /* trace values */
            Vector3 lastplane = Vector3.zero;
            Vector3 updir = orientation * new Vector3(0, 1, 0);

            float timefactor = velocity.magnitude;
            float skin = ArchetypeHeader.GET_SKINEPSILON(archetype.PrimitiveType());
            float bias = ArchetypeHeader.GET_TRACEBIAS(archetype.PrimitiveType());
            int numbumps = 0;
            int numgroundbumps = 0;
            int numpushbacks = 0;
            int geometryclips = 0;

            /* stepping values */
            bool canstep = actor.GetStepEnabled;
            float stepheight = actor.GetStepHeight;

            /* end of references */

            GroundHit ground = actor.Ground;
            GroundHit lastground = actor.LastGround;

            /* preserve last frame ground data into another struct */
            lastground.actorpoint = ground.actorpoint;
            lastground.normal = ground.normal;
            lastground.point = ground.point;
            lastground.stable = ground.stable;
            lastground.snapped = ground.snapped;
            lastground.distance = ground.distance;
            lastground.collider = ground.collider;

            ground.Clear();

            /* 
               I personally wish I could figure out a "stateless" way to implement ground snapping, but
               for the time being this seems to work best.
            */

            // if we're grounded, we'd like to stay that way! If we're not grounded, search a smaller distance to prevent
            // getting snapped to the floor in a visibly noticeable way by the camera. If you're interpolating player movement then
            // this may not be visible.
            float gtracelen = MIN_GROUNDQUERY;// (lastground.stable/* && lastground.snapped*/) ? MAX_GROUNDQUERY : MIN_GROUNDQUERY;

            if(lastground.snapped && lastground.stable)
                gtracelen = MAX_GROUNDQUERY > stepheight ? MAX_GROUNDQUERY : stepheight; // downward stepping

            while (numgroundbumps++ < MAX_GROUNDBUMPS &&
                gtracelen > 0F)
            {
                /*
                    -- trace along dir --

                    if detected 
                        if stable : 
                            end trace and determine whether a snap is to occur
                        else :
                            clip along floor
                            continue
                    else : 
                    break out of loop as no floor was detected
                */

                /* 
                    IMPORTANT! 
                        Our ground snapping position needs to be offset
                        upward as our trace may end up "tunneling" as a result of 
                        being too close to the floor. 

                        We compensate this offset by increasing our trace length to traverse
                        the offset distance in addition to our additional length
                */
                archetype.Trace(gposition + (updir * skin),
                    groundtracedir,
                    gtracelen + 2F * skin,
                    orientation,
                    layermask,
                    /* inflate */ 0F, //0F,
                    QueryTriggerInteraction.Ignore,
                    tracebuffer,
                    out int numgroundtraces);

                //DC
                /* filter out our archetype and find the closest valid interception */
                ArchetypeHeader.TraceFilters.FindClosestFilterInvalids(
                    ref numgroundtraces,
                    out int i0,
                    bias,
                    self,
                    tracebuffer);

                if (i0 >= 0) /* an intersection has occured, but we aren't sure its ground yet */
                {
                    RaycastHit _closest = tracebuffer[i0];

                    ground.distance = _closest.distance;
                    ground.point = _closest.point;
                    ground.normal = _closest.normal;
                    ground.actorpoint = gposition;
                    ground.stable = actor.DetermineGroundStability(velocity, _closest, layermask);
                    ground.snapped = false;
                    ground.collider = _closest.collider;

                    /*
                     warp regardless of stablility. We'll only be setting our trace position
                     to our ground trace position if a stable floor has been determined, and snapping is enabled. 
                    */
                    gposition += groundtracedir * (_closest.distance);

                    if (ground.stable)
                    {
                        /* 
                         Assume we can snap automatically if our snap type
                         is set to always 
                        */
                        bool cansnap = (snaptype == SlideSnapType.Always);

                        /*
                         Handling the other snap types:
                        */
                        switch (snaptype)
                        {
                            case SlideSnapType.Never:
                                cansnap = false;
                                break;
                            case SlideSnapType.Toggled:
                                /* Snap type is determined by a separate bool */
                                cansnap = actor.IsSnapEnabled;
                                break;
                        }

                        ground.snapped = cansnap;

                        /* trace upwards to see if we can fit inside a snap position and a ceiling
                            that may exist above it... */
                        archetype.Trace(
                            gposition,
                            updir,
                            skin + 0.1F,
                            orientation,
                            layermask,
                            0F,
                            QueryTriggerInteraction.Ignore,
                            tracebuffer,
                            out int numupwardbumps);

                        ArchetypeHeader.TraceFilters.FindClosestFilterInvalids(
                            ref numupwardbumps,
                            out int gi,
                            bias,
                            self,
                            tracebuffer);

                        /* this part may be a bit confusing to understand */
                        /* if a ceiling is discovered: */
                        if (gi >= 0)
                        {
                            /* 
                             generate a potential crease vector given 
                             the ground normal and the ceiling normal
                            */

                            RaycastHit ceiltrace = tracebuffer[gi];

                            Vector3 crease = Vector3.Cross(ceiltrace.normal, ground.normal);
                            crease.Normalize();

                            Vector3 forw = Vector3.Cross(updir, crease);
                            forw.Normalize();

                            if (VectorHeader.Dot(velocity, forw) <= 0F)
                            {
                                if (VectorHeader.Dot(velocity, ceiltrace.normal) < 0F)
                                    receiver.OnTraceHit(TraceHitType.Ceiling, ceiltrace, gposition, velocity);

                                geometryclips |= (1 << 1);
                                VectorHeader.ProjectVector(ref velocity, crease);
                            }

                            /*
                             if a ceiling is found, only move upward by the difference
                             between our ceiling height and our skin.

                             in the scenario where our ceiling distance is greater than 
                             our skin length, only move the minimum amount (skin)

                             in the scenario where our ceiling distance is less than our skin,
                             we've potentially encountered a tunneling position. In this scenario,
                             don't move upward at all.
                            */

                            gposition += updir * Mathf.Max(
                                Mathf.Min(ceiltrace.distance - skin, skin), 0F);

                        }
                        else /* if no ceiling is found, move the require distance upward */
                            gposition += updir * (skin + MIN_HOVER_DISTANCE);

                        /* 
                         if a snap was valid and allowed, 
                         1. set our position to the snap point 
                         2. set our last plane discovered to the ground normal                      
                         3. notify our geometry clipping algorithm that we've hit our first blocking plane
                         4. clip our velocity along the ground normal as we are effectively skipping the 
                            first iteration of our geometry clipping algorithm
                        */

                        if (ground.snapped)
                        {
                            position = gposition;

                            lastplane = ground.normal;
                            geometryclips |= (1 << 0);

                            VectorHeader.ClipVector(ref velocity, ground.normal);
                        }

                        /* 
                         1. send a callback to our receiver to notify them we've hit ground somewhere 
                         2. set gtracelen to zero to exit out of ground snap loop                        
                        */

                        receiver.OnGroundHit(ground, lastground, layermask);
                        gtracelen = 0F;
                    }
                    else
                    {
                        /* 
                        1. clip our ground tracing direction along the normal we've discovered 
                            -this is effectively creating a normal that "rides" along it tangentially
                        
                        2. normalize it for next iteration
                        
                        3. subtract our trace length for next iteration
                        */

                        VectorHeader.ClipVector(ref groundtracedir, _closest.normal);
                        groundtracedir.Normalize();
                        
                        gtracelen -= _closest.distance;

                        gtracelen = Mathf.Min(Mathf.Max(gtracelen, 0.0F), 0.01F ); // prevents sliding across asymptotically level slopes
                        // usually the first iteration will deal with most scenarios involving somewhat perpendicular slopes. I now fully
                        // understand why the KCC did this initially. -DC @ January 1st, 2021
                    }
                }
                else /* nothing discovered, end out of our ground loop */
                    gtracelen = 0F;
            }

            while (numpushbacks++ < ActorHeader.MAX_PUSHBACKS)
            {
                archetype.Overlap(
                    position,
                    orientation,
                    layermask,
                    /* inflate */ 0F,
                    _interacttype: querytype,
                    overlapbuffer,
                    out int numoverlaps);

                //DC
                FilterHeader.ActorOverlapFilter(
                    receiver,
                    ref numoverlaps,
                    self,
                    overlapbuffer);

                if (numoverlaps == 0) // nothing !
                    break;
                else
                {
                    for (int _colliderindex = 0; _colliderindex < numoverlaps; _colliderindex++)
                    {
                        Collider otherc = overlapbuffer[_colliderindex];
                        Transform othert = otherc.GetComponent<Transform>();

                        // another bug: Actor transform data must be set at the same time, not step by step.
                        // By doing this, we avoid stuttering between other actors

                        // we need an actor system that handles these types of movements
                        // this would also be a good time to decouple our movement from Unity's magic methods

                        if (Physics.ComputePenetration(self, 
                            position, orientation,
                            otherc, 
                            othert.position, 
                            othert.rotation, 
                            out Vector3 _normal, 
                            out float _distance))
                        {
                            position += _normal * (_distance + MIN_PUSHBACK_DEPTH);
                            PM_SlideDetermineImmediateGeometry(ref velocity,
                                ref lastplane,
                                actor.DeterminePlaneStability(_normal, otherc),
                                _normal,
                                ground.normal,
                                (ground.stable && ground.snapped),
                                updir,
                                ref geometryclips);
                            break;
                        }
                    }
                }
            }
            
            while (numbumps++ < ActorHeader.MAX_BUMPS
                  && timefactor > 0)
            {
                // Begin Trace
                Vector3 _trace = velocity * fdt;
                float _tracelen = _trace.magnitude;

                // IF unable to trace any further, break and end
                if (_tracelen <= skin)
                    timefactor = 0;
                else
                {
                    Vector3 trace_dir = _trace / _tracelen;

                    archetype.Trace(position,
                        trace_dir,
                        _tracelen + skin,
                        orientation,
                        layermask,
                        0F,
                        _interacttype: querytype,
                        tracebuffer,
                        out int _tracecount);

                    //DC
                    FilterHeader.ActorTraceFilter(
                        receiver,
                        ref _tracecount,
                        out int _i0,
                        bias,
                        self,
                        tracebuffer
                    );

                    // bad coder

                    bool success = _i0 >= 0;
                    if (!success) // nothing discovered :::
                    {
                        timefactor = 0; // end move
                        position += _trace;
                        break;
                    }
                    else // discovered an obstruction:::
                    {
                        RaycastHit _closest = tracebuffer[_i0]; /* struct buffer so no need to worry about overriding data */
                        Vector3 normal = _closest.normal;
                        Vector3 tracepoint = _closest.point;
                        float _dis = _closest.distance;
                        // float _rto = _closest.distance;
                        // timefactor -= _rto;

                        Vector3 change = trace_dir * _dis + normal * skin;
                        position += change;

                        timefactor -= change.magnitude;
                        /* 
                         *  only step if the following is true:
                         *  1. we are grounded
                         *  2. our step check is valid
                        */

                        // could potentially optimize this with branching
                        // by skipping the second AND= and just only doing so 
                        // if we're grounded
                        canstep &= ground.stable;
                        canstep &= PM_SlideStepValidation(position,
                            orientation,
                            normal,
                            tracepoint,
                            tracebuffer,
                            overlapbuffer,
                            archetype,
                            layermask,
                            stepheight,
                            out Vector3 step_position);

                        if (!canstep)
                        {
                            receiver.OnTraceHit(TraceHitType.Trace, _closest, position, velocity);

                            PM_SlideDetermineImmediateGeometry(ref velocity,
                                        ref lastplane,
                                        actor.DeterminePlaneStability(normal, _closest.collider),
                                        normal,
                                        ground.normal,
                                        ground.stable && ground.snapped,
                                        updir,
                                        ref geometryclips);
                        }
                        else
                        {
                            position = step_position;
                            velocity -= VectorHeader.ProjectVector(position - step_position, normal);
                        }
                    }
                }
            }

            actor.SetPosition(position);
            actor.SetVelocity(velocity);
        }

        private static bool PM_SlideStepValidation(
            Vector3 position,
            Quaternion orientation,
            Vector3 normal,
            Vector3 tracepoint,
            RaycastHit[] tracebuffer,
            Collider[] overlapbuffer,
            ArchetypeHeader.Archetype archetype,
            LayerMask layermask,
            float max_step,
            out Vector3 step_position)
        {
            step_position = position;
            return false;
        }

        /* 
            if we've discovered an obstruction, we'll want to see if it is steppable:

            at the moment, I want to make sure our stepping algorithm is fairly efficient, considering
            how theoretically costly this algo can get

            simply raycast inward from the clipping plane and check below using the up dir

            we can simplify our query search for the following reasons:
            (1) steps are usually perpendicular to the primitive being cast, so if the angular difference/correspondance between
            the up vector and the surface normal are in completeley opposite dirs (n dot v) is approx -1, we have a flat surface.

            (2) since the surface is flat, we don't have to worry about our safety overlap colliding with the ground plane, so it's a
            simple case of detecting anything at a specified point above the plane

            capsules will usually work best for this type of stepping, but if your geometry is fairly simple, boxes are great too.

            I'm going to attempt to write a resolver that doesn't rely on any virtual calls/indirection to potentially aid
            performance.    
        */

        #endregion

        #region Noclip

        // PM_NoclipMove() is the last variant in the Move() subset provided. It is mostly used for debugging
        // purposes but I've chosen to include it as it may come of use for you when giving players the ability
        // to change MoveFunc states.
        public static void PM_NoclipMove(
            IActorReceiver receiver,
            Actor actor,
            float fdt)
        {

            /* STEPS:
                RUN:
                DISPLACE
            */

            actor.Ground.Clear();
            actor.LastGround.Clear();
            actor.SetPosition(actor.position + actor.velocity * fdt);
        }


        #endregion

        public enum TraceHitType
        {
            Ground = 0, /* Whenever a trace is discovered during our groundward trace */
            Ceiling = 1, // During our ground trace, we check for ceilings to prevent sliding into creases protruding into the floor
            Trace = 2, /* Whenever a trace is discovered during our linear trace phase */
            Step = 3 /* only in PM_SlideStepMove */
        };

        public enum TriggerHitType
        {
            Traced = 0,
            Overlapped = 1
        };

        // In an effort to remove Actor Object & Callback Object coupling, you'll be required to pass reference to an IActorReceiver interface
        // whenever calling your move funcs as this will allow you to directly respond to information received during any of the Move() executions
        // during tracing/grounding/overlapping.. etc.
        public interface IActorReceiver
        {
            /* 
             * Called strictly in our slide implementations to inform end-user when ground is detected, but 
             * not necessarily if we should be grounded. That's up to you to work out. */
            void OnGroundHit(GroundHit ground, GroundHit lastground, LayerMask layermask);
            /* Called a plethora of times in various trace subroutines to inform end-users time of impact information */
            void OnTraceHit(TraceHitType tracetype, RaycastHit trace, Vector3 position, Vector3 velocity);
            /* If you'd like to compute information about the closest normal, see Physics.ComputePenetration() inside of my pushback code for easy copy/paste */
            void OnTriggerHit(TriggerHitType triggertype, Collider trigger);
        }

        public const float MIN_GROUNDQUERY = .1F; // distance queried in our ground traces if we weren't grounded the previous simulated step
        public const float MAX_GROUNDQUERY = .5F; // distnace queried in our ground traces if we were grounded in the previous simulation step

        public const int MAX_GROUNDBUMPS = 2; // # of ground snaps/iterations in a SlideMove() 
        public const int MAX_PUSHBACKS = 8; // # of iterations in our Pushback() funcs
        public const int MAX_BUMPS = 6; // # of iterations in our Move() funcs
        public const int MAX_HITS = 12; // # of RaycastHit[] structs allocated to
                                       // a hit buffer.
        public const int MAX_OVERLAPS = 8; // # of Collider classes allocated to a
                                           // overlap buffer.
        public const float MIN_DISPLACEMENT = 0.001F; // min squared length of a displacement vector required for a Move() to proceed.
        public const float FLY_CREASE_EPSILON = 1F; // minimum distance angle during a crease check to disregard any normals being queried.
        public const float INWARD_STEP_DISTANCE = 0.01F; // minimum displacement into a stepping plane
        public const float MIN_HOVER_DISTANCE = 0.025F;
        public const float MIN_PUSHBACK_DEPTH = 0.005F;
    }
}