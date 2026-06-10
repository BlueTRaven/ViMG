using BrUtility;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using ViMG.IMGUIImpl;
using static ViMG.EntityManagerIO;

namespace Engine.IMGUIImpl
{
    public static class IMGUIClientEntityInspector
    {
        private enum Sort
        {
            None,
            TypeDescending,
            TypeAscending,
            IDDescending,
            IDAscending,
        }

        [ConsoleCommandVar("cl_show_client_entity_inspector", "Show the client entity inspector")]
        public static bool ShowClientEntityInspector = false;
        
        private static string filterStr = "";

        public static void Show()
        {
            var client = GlobalState.GameStateManager.TheIsland.GetClient();
            if (client == null) return;

            if (ShowClientEntityInspector)
            {
                if (ShowClientEntityInspector && ImGui.Begin("Client Entity Inspector"))
                {
                    ImGui.InputText("Filter By Type", ref filterStr, 100, ImGuiInputTextFlags.EnterReturnsTrue);

                    var current = client.Current();
                    for (int i = 0; i < EntityManager.EntMax; i++)
                    {
                        EntityManager.EntityReference reference = current.entities.GetReference(i);
                        if (reference.id != -1 && current.entities.IsActive(ref reference))
                        {
                            var ent = current.entities.GetByRef(ref reference);
                            int typeId = current.entities.GetTypeById(i);
                            if (typeId == 0) 
                                continue;

                            string typeName = GlobalState.Registry.EntityRegistry.Get(typeId).Identifier;

                            if (filterStr != "" && !typeName.ToLower().Contains(filterStr.ToLower()))
                                continue;

                            ImGui.PushID(i);
                            if (ImGui.TreeNode(string.Format("Entity {0} {1}", i, typeName)))
                            {
                                ImGui.Text(string.Format("Position: {0:0.00} {1:0.00} {2:0.00}", ent.position.X, ent.position.Y, ent.position.Z));
                                ImGui.Text(string.Format("Velocity: {0:0.00} {1:0.00} {2:0.00}", ent.velocity.X, ent.velocity.Y, ent.velocity.Z));
                                Vector3 rotationYPR = EngineMathHelper.QuaternionToYawPitchRoll(ent.rotation);
                                ImGui.Text(string.Format("Yaw: {0:0.00} Pitch: {1:0.00} Roll: {2:0.00}", rotationYPR.X, rotationYPR.Y, rotationYPR.Z));
                                ImGui.Text(string.Format("State: {0} Health: {1}", ent.state, ent.health));
                                ImGui.TreePop();
                            }
                            ImGui.PopID();
                        }
                    }
                    //if (ImGui.TreeNode(entityData.id + ": " + entityData.type))
                    //{
                    //}
                    ImGui.End();
                }
            }
        }
    }
}
