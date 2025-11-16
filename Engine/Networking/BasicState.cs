using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Networking
{
    public struct BasicState : INetSerializable
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 velocity;
        public int health;
        public int state;

        public void Deserialize(NetDataReader reader)
        {
            position.X = reader.GetFloat();
            position.Y = reader.GetFloat();
            position.Z = reader.GetFloat();
            velocity.X = reader.GetFloat();
            velocity.Y = reader.GetFloat();
            velocity.Z = reader.GetFloat();
            rotation.X = reader.GetFloat();
            rotation.Y = reader.GetFloat();
            rotation.Z = reader.GetFloat();
            health = reader.GetInt();
            state = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(position.X);
            writer.Put(position.Y);
            writer.Put(position.Z);
            writer.Put(velocity.X);
            writer.Put(velocity.Y);
            writer.Put(velocity.Z);
            writer.Put(rotation.X);
            writer.Put(rotation.Y);
            writer.Put(rotation.Z);
            writer.Put(health);
            writer.Put(state);
        }
    }
}
