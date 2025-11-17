using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking
{
    public struct BasicState : INetSerializable
    {
        public Vector3 position;
        public Quaternion rotation;
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
            rotation.W = reader.GetFloat();
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
            writer.Put(rotation.W);
            writer.Put(health);
            writer.Put(state);
        }

        public void OnSave(List<byte> saveBytes)
        {
            SaveHelper.SaveVector3(saveBytes, position);
            SaveHelper.SaveVector3(saveBytes, velocity);
            SaveHelper.SaveVector4(saveBytes, rotation.ToVector4());
            SaveHelper.SaveInt32(saveBytes, health);
            SaveHelper.SaveInt32(saveBytes, state);
        }

        public void OnLoad(byte[] loadBytes, ref int index)
        {
            position = SaveHelper.LoadVector3(loadBytes, ref index);
            velocity = SaveHelper.LoadVector3(loadBytes, ref index);
            rotation = new Quaternion(SaveHelper.LoadVector4(loadBytes, ref index));
            health = SaveHelper.LoadInt32(loadBytes, ref index);
            state = SaveHelper.LoadInt32(loadBytes, ref index);
        }
    }
}
