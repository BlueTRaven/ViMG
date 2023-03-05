using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Physics
{
    public class ContactChecker
    {
        public bool WasOnGround;
        public bool OnGround;
        public Vector3 GroundNormal;

        public void Update(World world, BodyHandle handle)
        {
            WasOnGround = OnGround;
            OnGround = false;
            GroundNormal = Vector3.Zero;
            int groundCount = 0;

            var sensorBody = world.PhysicsInfo.Simulation.Bodies[handle];
            var extractor = new SolverContactDataExtractor(world.PhysicsInfo.GlobalBufferPool, sensorBody.Constraints.Count);
            //The basic idea behind the contact extractor is to submit it to a narrow phase contact accessor that is able to understand the solver's layout,
            //which will then call the contact extractor's relevant callbacks for the type of constraint encountered.
            //Here, we'll enumerate over all the constraints currently affecting the sensor body, attempting to extract contact data from each one.
            //If there are constraints that aren't contact constraints, they'll just get skipped.
            for (int i = 0; i < sensorBody.Constraints.Count; ++i)
            {
                world.PhysicsInfo.Simulation.NarrowPhase.TryExtractSolverContactData(sensorBody.Constraints[i].ConnectingConstraintHandle, ref extractor);
            }
            //We now have extracted contact data. Let's analyze it!
            //For the purposes of the demo, we'll draw debug shapes at the contacts to represent the different properties.
            for (int manifoldIndex = 0; manifoldIndex < extractor.Constraints.Count; ++manifoldIndex)
            {
                ref var constraintContacts = ref extractor.Constraints[manifoldIndex];

                //If we're colliding with a static, then it doesn't have a body, and therefore the handle will be -1.
                //We really only want to care about statics for the moment.
                //And maybe kinematics later.
                if (constraintContacts.BodyB.Value == -1)
                {
                    for (int contactIndex = 0; contactIndex < constraintContacts.Contacts.Count; ++contactIndex)
                    {
                        //var other = world.PhysicsInfo.Simulation.Bodies[constraintContacts.BodyB].
                        ref var contact = ref constraintContacts.Contacts[contactIndex];

                        float dot = Vector3.Dot(contact.Normal, Vector3.Up);
                        if (dot > 0.5f)
                        {
                            OnGround = true;
                            GroundNormal += contact.Normal;
                            groundCount++;
                        }
                    }
                }
            }
            extractor.Dispose();

            GroundNormal /= groundCount;
            GroundNormal.Normalize();
        }
    }
}
