using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using static Engine.Networking.Messages.SyncCubeUpdateAuditRequest;
using static Engine.Networking.Messages.SyncCubeUpdateAuditResponse;
using static ViMG.ChunkManager;

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
        private List<QueuedCubeUpdated> queued1 = new();
        private List<QueuedCubeUpdated> queued2 = new();
        private List<QueuedCubeUpdated> queued;

        public SyncCubeUpdate()
        {
            Instance = this;

            queued = queued1;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            ChunkManager.CubeUpdated cubeUpdated = addData as ChunkManager.CubeUpdated? ?? throw new Exception();
            Debug.Assert(cubeUpdated.updated == cubeUpdated.notified);
            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;

            netMessage.writer.Put(cubeUpdated.timeUpdated);
            netMessage.writer.Put(cubeUpdated.player?.playerIndex ?? -1);
            netMessage.writer.Put(cubeUpdated.updated);
            netMessage.writer.Put(cubeUpdated.oldId);
            netMessage.writer.Put(cubeUpdated.newId);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            double time = reader.GetDouble();
            int playerId = reader.GetInt();
            CubePosition a = reader.Get<CubePosition>();
            CubePosition updatedPos = new CubePosition(a.X, a.Y, a.Z, CubePosition.CoordinateSpace.CubeSpace);
            ushort oldId = reader.GetUShort();
            ushort newId = reader.GetUShort();

            queued.Add(new QueuedCubeUpdated
            {
                oldId = oldId,
                newId = newId,
                position = updatedPos,
                player = playerId,
                time = time,
            });
        }

        public void Apply(ChunkManager chunkManager, Player?[] players)
        {
            var otherBuffer = queued == queued1 ? queued2 : queued1;

            foreach (QueuedCubeUpdated qcubeupdated in queued)
            {
                if (Main.Time > qcubeupdated.time)
                {
                    // Invalidate any audits that may be attempting to update this position
                    for (int i = 0; i < MAX_AUDITS; i++)
                    {
                        ref var currAudit = ref SyncCubeUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][i];
                        if (currAudit.active && currAudit.position == qcubeupdated.position && qcubeupdated.time > currAudit.time)
                        {
                            currAudit.active = false;
                        }
                    }
                    var player = qcubeupdated.player == -1 ? null : players[qcubeupdated.player];
                    chunkManager.CubeView.SetCube(qcubeupdated.position, qcubeupdated.newId, false);
                    chunkManager.MarkCubeMeshInfoDirty(player, qcubeupdated.position, qcubeupdated.oldId, qcubeupdated.newId);
                    chunkManager.ChunkMesher?.MarkChunkDirty(ChunkPosition.CubeChunk(qcubeupdated.position));
                }
                else
                {
                    otherBuffer.Add(qcubeupdated);
                }
            }

            queued.Clear();
            queued = otherBuffer;
        }
    }

    public class SyncCubeUpdateAuditRequest : Message
    {
        public const int MAX_AUDITS = 32;

        public static SyncCubeUpdateAuditRequest Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Client;

        public const double TIMEOUT = 0.5;

        public struct AuditedCubeUpdate
        {
            public bool active;
            public byte index;
            public byte player;
            public CubePosition position;
            public ushort oldId, newId;
            public double time;
        }

        public AuditedCubeUpdate[][] activeAudits = null;

        public SyncCubeUpdateAuditRequest()
        {
            activeAudits = new AuditedCubeUpdate[World.MAX_PLAYERS][];
            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                activeAudits[i] = new AuditedCubeUpdate[MAX_AUDITS];
            }
            Instance = this;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);
            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;

            var action = addData as AuditedCubeUpdate? ?? throw new Exception();

            bool found = false;
            for (int i = 0; i < activeAudits[GS.GetWorld().localPlayerIndex].Length; i++)
            {
                if (!activeAudits[GS.GetWorld().localPlayerIndex][i].active)
                {
                    action = action with
                    {
                        active = true,
                        index = (byte)i,
                        player = (byte)GS.GetWorld().localPlayerIndex,
                    };

                    activeAudits[GS.GetWorld().localPlayerIndex][i] = action;

                    found = true;
                    break;
                }
            }

            if (!found)
            {
                RollbackAction(action);
            }
            else
            {
                netMessage.writer.Put(action.time);
                netMessage.writer.Put(action.index);
                netMessage.writer.Put(action.player);
                netMessage.writer.Put(action.position);
                netMessage.writer.Put(action.oldId);
                netMessage.writer.Put(action.newId);

                netMessage.Send();
            }
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            var time = reader.GetDouble();
            var index = reader.GetByte();
            var player = reader.GetByte();
            var position = reader.Get<CubePosition>();
            position = new CubePosition(position.X, position.Y, position.Z);
            var oldId = reader.GetUShort();
            var newId = reader.GetUShort();

            Debug.Assert(!activeAudits[player][index].active);

            activeAudits[player][index] = new AuditedCubeUpdate
            {
                time = time,
                index = index,
                player = player,
                position = position,
                oldId = oldId,
                newId = newId,
                active = true,
            };
        }

        public void Apply(ChunkManager chunkManager, Player?[] players)
        {
            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                for (int j = 0; j < MAX_AUDITS; j++)
                {
                    if (activeAudits[i][j].active)
                    {
                        if (Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Server)
                        {
                            var action = activeAudits[i][j];
                            var peer = GS.netManager.GetPeer(action.player);
                            if (peer != null)
                            {
                                var accepted = true;
                                ushort newId = action.newId;
                                var player = players[action.player];
                                ushort curId = chunkManager.CubeView.GetId(action.position); ;

                                if (curId != newId)
                                {
                                    accepted = false;
                                    newId = curId;
                                }
                                //if (action.oldId != 0 && action.newId == 0)
                                //{
                                //    accepted = GS.GetWorld().TryMineCube(player, action.position, 0, 0, true);
                                //    if (!accepted) newId = chunkManager.CubeView.GetId(action.position);
                                //}
                                //else
                                //{
                                //    chunkManager.CubeView.SetCube(action.position, action.newId, false);
                                //    chunkManager.MarkCubeMeshInfoDirty(player, action.position, action.oldId, action.newId);
                                //    chunkManager.ChunkMesher?.MarkChunkDirty(ChunkPosition.CubeChunk(action.position));
                                //}

                                Main.Registry.MessageRegistry.SendMessageToPeer(SyncCubeUpdateAuditResponse.Instance, peer, new AcceptedCubeUpdate()
                                {
                                    accepted = accepted,
                                    index = (byte)j,
                                    newId = newId,
                                });
                            }

                            // If peer was no longer alive, then we still need to mark this as inactive
                            activeAudits[i][j].active = false;
                        }
                        else
                        {
                            if (Main.Time - activeAudits[i][j].time > TIMEOUT) 
                            {
                                RollbackAction(activeAudits[i][j]);
                                activeAudits[i][j].active = false;
                            }
                        }
                    }
                }
            }
        }

        public void RollbackAction(AuditedCubeUpdate action)
        {
            //Console.WriteLine("did rollback {0}", action.index);
            Debug.Assert(Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Client);

            var player = GS.GetWorld().player[action.player];
            if (player != null)
                GS.GetWorld().ChunkManager.CubeView.SetCube(action.position, action.oldId, player);
            else GS.GetWorld().ChunkManager.CubeView.SetCube(action.position, action.oldId, true);
        }

        public void DoAction(AuditedCubeUpdate action)
        {
            //Console.WriteLine("did action {0}", action.index);
            Debug.Assert(Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Client);

            var player = GS.GetWorld().player[action.player];
            if (player != null)
                GS.GetWorld().ChunkManager.CubeView.SetCube(action.position, action.newId, player);
            else GS.GetWorld().ChunkManager.CubeView.SetCube(action.position, action.newId, true);
        }
    }

    public class SyncCubeUpdateAuditResponse : Message
    {
        public static SyncCubeUpdateAuditResponse Instance { get; private set; }
                
        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public struct AcceptedCubeUpdate
        {
            public bool accepted;
            public byte index;
            public ushort newId;
        }

        public SyncCubeUpdateAuditResponse()
        {
            Instance = this;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);
            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;

            var action = addData as AcceptedCubeUpdate? ?? throw new Exception();

            netMessage.writer.Put(action.accepted);
            netMessage.writer.Put(action.index);
            netMessage.writer.Put(action.newId);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            var accepted = reader.GetBool();
            var index = reader.GetByte();
            var newId = reader.GetUShort();

            Console.WriteLine("Received accept {0} AuditedCubeUpdate: {1} newId {2}", index, accepted, newId);

            SyncCubeUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index].oldId = newId;
            SyncCubeUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index].newId = newId;
            if (accepted)
            {
                SyncCubeUpdateAuditRequest.Instance.DoAction(SyncCubeUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index]);
            }
            else
            {
                SyncCubeUpdateAuditRequest.Instance.RollbackAction(SyncCubeUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index]);
            }
            SyncCubeUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index].active = false;
        }
    }
}
