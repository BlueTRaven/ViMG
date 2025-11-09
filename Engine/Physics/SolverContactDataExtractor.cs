using BepuPhysics;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Physics
{
    /// <summary>
    /// Stores the easy to read data associated with a single contact extracted from solver contact constraints.
    /// </summary>
    public struct ExtractedContact
    {
        public Vector3 OffsetA;
        public float Depth;
        //For the purposes of the demo, we'll store a normal for every single contact, even though the original manifold might have been convex (and so shared one normal across all its contacts).
        public Vector3 Normal;
        public float PenetrationImpulse;
        //We'll also derive friction impulse from different sources depending on convexity- convex manifolds have a single twist/tangent friction constraint, while nonconvex manifolds have a per-contact tangent friction.
        public float FrictionImpulseMagnitude;
    }

    /// <summary>
    /// Stores the connected bodies and contacts for a contact manifold constraint extracted from the solver.
    /// </summary>
    public struct ExtractedManifold
    {
        public QuickList<ExtractedContact> Contacts;
        public BodyHandle BodyA;
        //For one body constraints, this will be -1.
        public BodyHandle BodyB;

        public ExtractedManifold(BufferPool pool, BodyHandle a, BodyHandle b)
        {
            //Nonconvex manifolds will never have less than the convex count, so we'll preallocate enough space for a nonconvex manifold.
            Contacts = new QuickList<ExtractedContact>(NonconvexContactManifold.MaximumContactCount, pool);
            BodyA = a;
            BodyB = b;
        }

        public ExtractedManifold(BufferPool pool, BodyHandle a) : this(pool, a, new BodyHandle(-1)) { }

        public void Dispose(BufferPool pool)
        {
            Contacts.Dispose(pool);
        }
    }

    /// <summary>
    /// Example implementation of a <see cref="ISolverContactDataExtractor"/> that pulls contact data into an easier to read format.
    /// </summary>
    public struct SolverContactDataExtractor : ISolverContactDataExtractor
    {
        //We'll pull the solver data into a different form- just a list of contacts with basic data about each one.
        //What you store depends on your application's needs- there's nothing saying you have to use this layout.
        //The ISolverContactDataExtractor can be thought of as the foundation on which to build other (more convenient) abstractions.
        public QuickList<ExtractedManifold> Constraints;
        public BufferPool Pool;

        public SolverContactDataExtractor(BufferPool pool, int initialCapacity)
        {
            Pool = pool;
            Constraints = new QuickList<ExtractedManifold>(initialCapacity, pool);
        }

        //The callbacks distinguish between convex and nonconvex because the underlying data is different.
        //Nonconvex manifolds can have different normals at each contact, while convex manifolds share one normal across all contacts.
        //This also affects the accumulated impulses- nonconvex impulses need to include per-contact tangent friction impulses due to the potentially differing normals.

        //In all of the following, you might notice an unusual pattern- things like prestep.GetNormal(ref prestep...). 
        //This is a bit of a hack to work around the fact that 
        //1) struct member functions cannot return a reference to the 'this' instance, and 
        //2) there is no way in the current version of C# for an interface to require a static function. (This will change with static abstracts!)
        //It's another weird little tradeoff for minimal overhead.

        void ExtractConvexData<TPrestep, TAccumulatedImpulses>(ref ExtractedManifold constraintContacts, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, IConvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, IConvexContactAccumulatedImpulses<TAccumulatedImpulses>
        {
            //Note that we narrow from the raw vectorized reference into a more application-convenient AOS representation.
            Vector3Wide.ReadFirst(prestep.GetNormal(ref prestep), out var normal);

            //We'll approximate the per-contact friction by allocating the shared friction impulses to contacts weighted by their penetration impulse.
            float totalPenetrationImpulse = 0;
            for (int i = 0; i < prestep.ContactCount; ++i)
            {
                ref var sourceContact = ref prestep.GetContact(ref prestep, i);
                ref var targetContact = ref constraintContacts.Contacts.AllocateUnsafely();
                Vector3Wide.ReadFirst(sourceContact.OffsetA, out targetContact.OffsetA);
                //We can use [0] to access the slot because the prestep bundle memory reference was already offset for us.
                targetContact.Depth = sourceContact.Depth[0];
                targetContact.Normal = normal;
                targetContact.PenetrationImpulse = impulses.GetPenetrationImpulseForContact(ref impulses, i)[0];
                totalPenetrationImpulse += targetContact.PenetrationImpulse;
            }
            Vector2Wide.ReadFirst(impulses.GetTangentFriction(ref impulses), out var tangentFriction);
            var twistFriction = impulses.GetTwistFriction(ref impulses)[0];
            //This isn't a 'correct' allocation of impulses, we just want a rough sense.
            var frictionMagnitudeApproximation = MathF.Sqrt(tangentFriction.LengthSquared() + twistFriction * twistFriction);
            var impulseScale = totalPenetrationImpulse > 0 ? frictionMagnitudeApproximation / totalPenetrationImpulse : 0;
            for (int i = 0; i < prestep.ContactCount; ++i)
            {
                ref var contact = ref constraintContacts.Contacts[i];
                contact.FrictionImpulseMagnitude = contact.PenetrationImpulse * impulseScale;
            }
        }

        public void ConvexOneBody<TPrestep, TAccumulatedImpulses>(BodyHandle bodyHandle, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, IConvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, IConvexContactAccumulatedImpulses<TAccumulatedImpulses>
        {
            ref var constraintContacts = ref Constraints.Allocate(Pool);
            constraintContacts = new ExtractedManifold(Pool, bodyHandle);
            ExtractConvexData(ref constraintContacts, ref prestep, ref impulses);
        }

        public void ConvexTwoBody<TPrestep, TAccumulatedImpulses>(BodyHandle bodyHandleA, BodyHandle bodyHandleB, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, ITwoBodyConvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, IConvexContactAccumulatedImpulses<TAccumulatedImpulses>
        {
            ref var constraintContacts = ref Constraints.Allocate(Pool);
            constraintContacts = new ExtractedManifold(Pool, bodyHandleA, bodyHandleB);
            ExtractConvexData(ref constraintContacts, ref prestep, ref impulses);
        }

        void ExtractNonconvexData<TPrestep, TAccumulatedImpulses>(ref ExtractedManifold constraintContacts, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, INonconvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, INonconvexContactAccumulatedImpulses<TAccumulatedImpulses>
        {
            //Nonconvex types require no approximation of friction; we can pull it directly from the solved results.
            for (int i = 0; i < prestep.ContactCount; ++i)
            {
                ref var sourceContact = ref prestep.GetContact(ref prestep, i);
                ref var targetContact = ref constraintContacts.Contacts.AllocateUnsafely();
                Vector3Wide.ReadFirst(sourceContact.Offset, out targetContact.OffsetA);
                targetContact.Depth = sourceContact.Depth[0];
                Vector3Wide.ReadFirst(sourceContact.Normal, out targetContact.Normal);

                ref var contactImpulses = ref impulses.GetImpulsesForContact(ref impulses, i);
                targetContact.PenetrationImpulse = contactImpulses.Penetration[0];
                Vector2Wide.ReadFirst(contactImpulses.Tangent, out var tangentImpulses);
                targetContact.FrictionImpulseMagnitude = tangentImpulses.Length();
            }
        }

        public void NonconvexOneBody<TPrestep, TAccumulatedImpulses>(BodyHandle bodyHandle, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, INonconvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, INonconvexContactAccumulatedImpulses<TAccumulatedImpulses>
        {
            ref var constraintContacts = ref Constraints.Allocate(Pool);
            constraintContacts = new ExtractedManifold(Pool, bodyHandle);
            ExtractNonconvexData(ref constraintContacts, ref prestep, ref impulses);
        }

        public void NonconvexTwoBody<TPrestep, TAccumulatedImpulses>(BodyHandle bodyHandleA, BodyHandle bodyHandleB, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, ITwoBodyNonconvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, INonconvexContactAccumulatedImpulses<TAccumulatedImpulses>
        {
            ref var constraintContacts = ref Constraints.Allocate(Pool);
            constraintContacts = new ExtractedManifold(Pool, bodyHandleA, bodyHandleB);
            ExtractNonconvexData(ref constraintContacts, ref prestep, ref impulses);
        }

        public void Reset()
        {
            for (int i = 0; i < Constraints.Count; ++i)
            {
                Constraints[i].Dispose(Pool);
            }
            Constraints.Count = 0;
        }

        public void Dispose()
        {
            Reset();
            Constraints.Dispose(Pool);
        }
    }
}
