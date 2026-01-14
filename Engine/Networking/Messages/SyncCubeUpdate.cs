using Engine.Clients;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.IMGUIImpl;

namespace Engine.Networking.Messages
{
    public class SyncCubeUpdate : Message
    {
        public static SyncCubeUpdate Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        private struct QueuedCubeUpdated
        {
            public int player;
            public CubePosition position;
            public ushort oldId, newId;
            public double time;
        }

        private struct CubeToUpdate
        {
            public CubePosition position;
            public double time;
            public sbyte playerId;
            public ushort oldId;
            public ushort newId;
            public ushort newProgress;
        }

        public SyncCubeUpdate()
        {
            Instance = this;
        }

        public void SendCubeUpdate(ChunkManager.CubeUpdated updated)
        {
            Main.gameStateManager.TheIsland.netManagerServer.SendMessageToAll(Instance, Main.gameStateManager.TheIsland.netManagerServer.netManager, new CubeToUpdate
            {
                oldId = updated.oldId,
                newId = updated.newId,
                newProgress = 0,
                position = updated.updated,
                playerId = (sbyte)(updated.player?.playerIndex ?? -1),
                time = updated.timeUpdated,
            });
        }

        public void SendCubeUpdate(CubePosition position, int playerId, ushort progress)
        {
            Main.gameStateManager.TheIsland.netManagerServer.SendMessageToAll(Instance, Main.gameStateManager.TheIsland.netManagerServer.netManager, new CubeToUpdate
            {
                oldId = 0,
                newId = 0,
                newProgress = progress,
                position = position,
                playerId = (sbyte)playerId,
                time = Main.Time,
            });
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            CubeToUpdate cubeUpdated = addData as CubeToUpdate? ?? throw new Exception();
            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;

            netMessage.writer.Put(cubeUpdated.time);
            netMessage.writer.Put(cubeUpdated.playerId);
            netMessage.writer.Put(cubeUpdated.position);
            netMessage.writer.Put(cubeUpdated.oldId);
            netMessage.writer.Put(cubeUpdated.newId);
            netMessage.writer.Put(cubeUpdated.newProgress);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            double time = reader.GetDouble();
            sbyte playerId = reader.GetSByte();
            CubePosition a = reader.Get<CubePosition>();
            CubePosition updatedPos = new CubePosition(a.X, a.Y, a.Z, CubePosition.CoordinateSpace.CubeSpace);
            ushort oldId = reader.GetUShort();
            ushort newId = reader.GetUShort();
            int newProgress = reader.GetUShort();

            DoCubeUpdate(GS.GetClient().ChunkManager, new CubeToUpdate
            {
                oldId = oldId,
                newId = newId,
                position = updatedPos,
                playerId = playerId,
                time = time,
            });
        }

        private void DoCubeUpdate(ClientChunkManager chunkManager, CubeToUpdate update)
        {
            if (update.oldId == update.newId)
            {
                chunkManager.CubeProgressTracker.SetProgress(chunkManager.CubeView, update.position, update.newProgress);
                Console.WriteLine("New progress {0}", update.newProgress);
            }
            else
            {
                chunkManager.CubeView.SetId(update.position, update.newId);
                chunkManager.CopyManager.MarkDirty(ChunkPosition.CubeChunk(update.position));
                for (int i = 0; i < 3 * 3 * 3; i++)
                {
                    Util.OneDToThreeD(i, new ValuePoint3D(3), out var point);
                    ChunkPosition cpos = ChunkPosition.CubeChunk(update.position);
                    cpos += new ChunkPosition(point.x - 1, point.y - 1, point.z - 1);
                    if (chunkManager.IsInWorldBounds(cpos))
                        chunkManager.ChunkMesher.RenderMesher?.MarkDirty(cpos);
                }
            }
        }
    }

    //public class SyncCubeUpdateAuditRequest : Message
    //{
    //    public const int MAX_AUDITS = 32;

    //    public static SyncCubeUpdateAuditRequest Instance { get; private set; }

    //    public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Client;

    //    public const double TIMEOUT = 0.5;

    //    public struct AuditedCubeUpdate
    //    {
    //        public bool active;
    //        public byte index;
    //        public byte player;
    //        public CubePosition position;
    //        public ushort oldId, newId;
    //        public double time;
    //    }

    //    public AuditedCubeUpdate[][] activeAudits = null;

    //    public SyncCubeUpdateAuditRequest()
    //    {
    //        activeAudits = new AuditedCubeUpdate[World.MAX_PLAYERS][];
    //        for (int i = 0; i < World.MAX_PLAYERS; i++)
    //        {
    //            activeAudits[i] = new AuditedCubeUpdate[MAX_AUDITS];
    //        }
    //        Instance = this;
    //    }

    //    public override void SendMessage(NetworkMessage netMessage, object? addData)
    //    {
    //        base.SendMessage(netMessage, addData);
    //        netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;

    //        var action = addData as AuditedCubeUpdate? ?? throw new Exception();

    //        bool found = false;
    //        for (int i = 0; i < activeAudits[GS.GetWorld().localPlayerIndex].Length; i++)
    //        {
    //            if (!activeAudits[GS.GetWorld().localPlayerIndex][i].active)
    //            {
    //                action = action with
    //                {
    //                    active = true,
    //                    index = (byte)i,
    //                    player = (byte)GS.GetWorld().localPlayerIndex,
    //                };

    //                activeAudits[GS.GetWorld().localPlayerIndex][i] = action;

    //                found = true;
    //                break;
    //            }
    //        }

    //        if (!found)
    //        {
    //            RollbackAction(action);
    //        }
    //        else
    //        {
    //            netMessage.writer.Put(action.time);
    //            netMessage.writer.Put(action.index);
    //            netMessage.writer.Put(action.player);
    //            netMessage.writer.Put(action.position);
    //            netMessage.writer.Put(action.oldId);
    //            netMessage.writer.Put(action.newId);

    //            netMessage.Send();
    //        }
    //    }

    //    public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
    //    {
    //        base.ReceiveMessage(reader, peer);

    //        var time = reader.GetDouble();
    //        var index = reader.GetByte();
    //        var player = reader.GetByte();
    //        var position = reader.Get<CubePosition>();
    //        position = new CubePosition(position.X, position.Y, position.Z);
    //        var oldId = reader.GetUShort();
    //        var newId = reader.GetUShort();

    //        IMGUIConsole.Assert(!activeAudits[player][index].active);

    //        activeAudits[player][index] = new AuditedCubeUpdate
    //        {
    //            time = time,
    //            index = index,
    //            player = player,
    //            position = position,
    //            oldId = oldId,
    //            newId = newId,
    //            active = true,
    //        };
    //    }

    //    public void Apply(ChunkManager chunkManager, Player?[] players)
    //    {
    //        for (int i = 0; i < World.MAX_PLAYERS; i++)
    //        {
    //            for (int j = 0; j < MAX_AUDITS; j++)
    //            {
    //                if (activeAudits[i][j].active)
    //                {
    //                    var action = activeAudits[i][j];
    //                    var peer = GS.netManagerServer?.GetPeer(action.player);
    //                    if (peer != null)
    //                    {
    //                        var accepted = true;
    //                        ushort newId = action.newId;
    //                        var player = players[action.player];
    //                        ushort curId = chunkManager.CubeView.GetId(action.position); ;

    //                        if (curId != newId)
    //                        {
    //                            accepted = false;
    //                            newId = curId;
    //                        }

    //                        GS.netManagerServer?.SendMessageToPeer(SyncCubeUpdateAuditResponse.Instance, peer, new AcceptedCubeUpdate()
    //                        {
    //                            accepted = accepted,
    //                            index = (byte)j,
    //                            newId = newId,
    //                        });
    //                    }

    //                    // If peer was no longer alive, then we still need to mark this as inactive
    //                    activeAudits[i][j].active = false;
    //                }
    //                //else
    //                //{
    //                //    if (Main.Time - activeAudits[i][j].time >= TIMEOUT)
    //                //    {
    //                //        RollbackAction(activeAudits[i][j]);
    //                //        activeAudits[i][j].active = false;
    //                //    }
    //                //}
    //            }
    //        }
    //    }

    //    public void RollbackAction(AuditedCubeUpdate action)
    //    {
    //        //Console.WriteLine("did rollback {0}", action.index);
    //        IMGUIConsole.Assert(Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Client);

    //        var player = GS.GetWorld().player[action.player];
    //        if (player != null)
    //            GS.GetWorld().ChunkManager.CubeView.SetCube(action.position, action.oldId, player);
    //        else GS.GetWorld().ChunkManager.CubeView.SetCube(action.position, action.oldId, true);
    //    }

    //    public void DoAction(AuditedCubeUpdate action)
    //    {
    //        //Console.WriteLine("did action {0}", action.index);
    //        IMGUIConsole.Assert(Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Client);

    //        var player = GS.GetWorld().player[action.player];
    //        if (player != null)
    //            GS.GetWorld().ChunkManager.CubeView.SetCube(action.position, action.newId, player);
    //        else GS.GetWorld().ChunkManager.CubeView.SetCube(action.position, action.newId, true);
    //    }
    //}

    //public class SyncCubeUpdateAuditResponse : Message
    //{
    //    public static SyncCubeUpdateAuditResponse Instance { get; private set; }
                
    //    public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

    //    public struct AcceptedCubeUpdate
    //    {
    //        public bool accepted;
    //        public byte index;
    //        public ushort newId;
    //    }

    //    public SyncCubeUpdateAuditResponse()
    //    {
    //        Instance = this;
    //    }

    //    public override void SendMessage(NetworkMessage netMessage, object? addData)
    //    {
    //        base.SendMessage(netMessage, addData);
    //        netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;

    //        var action = addData as AcceptedCubeUpdate? ?? throw new Exception();

    //        netMessage.writer.Put(action.accepted);
    //        netMessage.writer.Put(action.index);
    //        netMessage.writer.Put(action.newId);

    //        netMessage.Send();
    //    }

    //    public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
    //    {
    //        base.ReceiveMessage(reader, peer);

    //        var accepted = reader.GetBool();
    //        var index = reader.GetByte();
    //        var newId = reader.GetUShort();

    //        Console.WriteLine("Received accept {0} AuditedCubeUpdate: {1} newId {2}", index, accepted, newId);

    //        SyncCubeUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index].oldId = newId;
    //        SyncCubeUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index].newId = newId;
    //        if (accepted)
    //        {
    //            SyncCubeUpdateAuditRequest.Instance.DoAction(SyncCubeUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index]);
    //        }
    //        else
    //        {
    //            SyncCubeUpdateAuditRequest.Instance.RollbackAction(SyncCubeUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index]);
    //        }
    //        SyncCubeUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index].active = false;
    //    }
    //}
}
