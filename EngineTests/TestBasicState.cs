using Engine.Items;
using Engine.Networking;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace EngineTests
{
    [TestClass]
    public sealed class TestBasicState
    {
        [TestMethod]
        public void TestSerializeDeserialize()
        {
            SyncedEntity orig = new SyncedEntity();
            orig.state = 1256;
            orig.position = new Vector3(32, 64, 100);
            orig.counters[0] = 32;
            orig.timers[3] = 6684;

            var netWriter = new NetDataWriter();
            orig.Serialize(netWriter);

            byte[] bytes = netWriter.CopyData();

            SyncedEntity deser = new SyncedEntity();
            var netReader = new NetDataReader(bytes);
            deser.Deserialize(netReader);

            Assert.AreEqual(orig.state, deser.state, "state not serialized/deserialized correctly");
            Assert.AreEqual(orig.position, deser.position, "position not serialized/deserialized correctly");
            Assert.AreEqual(orig.counters[0], deser.counters[0], "counter 0 not serialized/deserialized correctly");
            Assert.AreEqual(orig.timers[3], deser.timers[3], "timer 3 not serialized/deserialized correctly");

            Assert.AreEqual(orig.counters, deser.counters, "timers not serialized/deserialized correctly");
            Assert.AreEqual(orig.timers, deser.timers, "timers not serialized/deserialized correctly");
        }

        private struct TestExtra
        {
            public int a;
            public int b;
            public float c;
            public Vector3 d;
            public bool e;
        }

        [TestMethod]
        public void TestSerializeDeserializeExtraStruct()
        {
            SyncedEntity orig = new SyncedEntity();
            TestExtra extra = new TestExtra();
            extra.a = 32;
            extra.b = 10341;
            extra.c = 32.566f;
            extra.d = new Vector3(15.3f, 675.5f, 11.2f);
            extra.e = true;
            orig.SetExtra(ref extra);

            var netWriter = new NetDataWriter();
            orig.Serialize(netWriter);

            byte[] bytes = netWriter.CopyData();

            SyncedEntity deser = new SyncedEntity();
            var netReader = new NetDataReader(bytes);
            deser.Deserialize(netReader);

            TestExtra newExtra = deser.GetExtra<TestExtra>();
            Assert.AreEqual(extra.a, newExtra.a);
            Assert.AreEqual(extra.b, newExtra.b);
            Assert.AreEqual(extra.c, newExtra.c);
            Assert.AreEqual(extra.d, newExtra.d);
            Assert.AreEqual(extra.e, newExtra.e);
        }

        [TestMethod]
        public void TestSerializeDeserializeExtraSimple()
        {
            SyncedEntity orig = new SyncedEntity();
            bool extra = true;
            orig.SetExtra(ref extra);

            var netWriter = new NetDataWriter();
            orig.Serialize(netWriter);

            byte[] bytes = netWriter.CopyData();

            SyncedEntity deser = new SyncedEntity();
            var netReader = new NetDataReader(bytes);
            deser.Deserialize(netReader);

            bool newExtra = deser.GetExtra<bool>();
            Assert.AreEqual(extra, newExtra);
        }

        [TestMethod]
        public void TestDeltaSerializeDeserialize()
        {
            SyncedEntity prev = new SyncedEntity();
            prev.state = 1256;
            prev.position = new Vector3(32, 64, 100);
            prev.counters[0] = 32;
            prev.timers[3] = 6684;
            SyncedEntity curr = prev;
            curr.state = 2;
            curr.counters[1] = 13;

            var bits = curr.GetDeltaBits(ref prev);
            var extraBits = curr.GetExtraBytesBits(ref prev);
            var netWriter = new NetDataWriter();
            curr.SerializeDelta(netWriter, bits);
            curr.SerializeDeltaExtraFields(netWriter, extraBits);

            byte[] bytes = netWriter.CopyData();

            SyncedEntity deser = prev;
            var netReader = new NetDataReader(bytes);
            deser.DeserializeDelta(netReader);

            Assert.AreEqual(curr.state, deser.state, "state not serialized/deserialized correctly");
            Assert.AreEqual(curr.position, deser.position, "position not serialized/deserialized correctly");
            Assert.AreEqual(curr.counters[0], deser.counters[0], "counter 0 not serialized/deserialized correctly");
            Assert.AreEqual(curr.timers[3], deser.timers[3], "timer 3 not serialized/deserialized correctly");

            Assert.AreEqual(curr.counters, deser.counters, "timers not serialized/deserialized correctly");
            Assert.AreEqual(curr.timers, deser.timers, "timers not serialized/deserialized correctly");
        }

        [TestMethod]
        public void TestSerializeDeltaDeserializeExtraStruct()
        {
            SyncedEntity prev = new SyncedEntity();
            TestExtra prevExtra = new TestExtra();
            prevExtra.a = 32;
            prevExtra.b = 10341;
            prevExtra.c = 32.566f;
            prevExtra.d = new Vector3(15.3f, 675.5f, 11.2f);
            prevExtra.e = true;
            prev.SetExtra(ref prevExtra);
            SyncedEntity curr = prev;
            var currExtra = curr.GetExtra<TestExtra>();
            currExtra.a = 13;
            currExtra.e = false;
            curr.SetExtra(ref currExtra);

            var bits = curr.GetDeltaBits(ref prev);
            var extrabits = curr.GetExtraBytesBits(ref prev);

            var netWriter = new NetDataWriter();
            curr.SerializeDelta(netWriter, bits);
            curr.SerializeDeltaExtraFields(netWriter, extrabits);

            byte[] bytes = netWriter.CopyData();

            SyncedEntity deser = prev;
            var netReader = new NetDataReader(bytes);
            deser.DeserializeDelta(netReader);

            TestExtra newExtra = deser.GetExtra<TestExtra>();
            Assert.AreEqual(currExtra.a, newExtra.a);
            Assert.AreEqual(currExtra.b, newExtra.b);
            Assert.AreEqual(currExtra.c, newExtra.c);
            Assert.AreEqual(currExtra.d, newExtra.d);
            Assert.AreEqual(currExtra.e, newExtra.e);
        }

        private struct PlayerExtraState
        {
            public InventoryManager.InventoryReference inventory;
            public InventoryManager.InventoryReference heldInventory;
            public InventoryManager.InventoryReference craftInventory;
            public InventoryManager.InventoryReference gearInventory;
            public InventoryManager.InventoryReference accessoryInventory;
            public int highlightIndex;
            public int useAnimType;
            public float useAnimTime;
            public float useAnimTimer;

            public int currency;

            public float deadTime;
            public float damageTime;
            public float inputLockupTimer;

            public int maxHealth;
            public int maxMagic;
            public int magic;

            public float accel;
            public float speed;
            public float runSpeed;
        }


        [TestMethod]
        public void TestSerializeDeltaDeserializePlayerExtraState()
        {
            SyncedEntity prev = new SyncedEntity();
            PlayerExtraState prevExtra = new PlayerExtraState
            {
                useAnimType = 4,
                runSpeed = 0.4f,
            };
            prev.SetExtra(ref prevExtra);

            SyncedEntity curr = prev;
            PlayerExtraState currExtra = new PlayerExtraState
            {
                useAnimType = 3,
            };
            curr.SetExtra(ref currExtra);

            var bits = curr.GetDeltaBits(ref prev);
            var extrabits = curr.GetExtraBytesBits(ref prev);

            var netWriter = new NetDataWriter();
            curr.SerializeDelta(netWriter, bits);
            curr.SerializeDeltaExtraFields(netWriter, extrabits);

            byte[] bytes = netWriter.CopyData();

            SyncedEntity deser = new();
            var netReader = new NetDataReader(bytes);
            deser.DeserializeDelta(netReader);

            PlayerExtraState deserExtra = deser.GetExtra<PlayerExtraState>();
            Assert.AreEqual(deserExtra.useAnimType, currExtra.useAnimType);
            Assert.AreEqual(deserExtra.runSpeed, currExtra.runSpeed);
        }
    }
}
