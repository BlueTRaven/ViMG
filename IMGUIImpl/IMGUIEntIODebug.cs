using ImGuiNET;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.GameStates;

namespace ViMG.IMGUIImpl
{
    public static class IMGUIEntIODebug
    {
        private enum Sort
        {
            None,
            TypeDescending,
            TypeAscending,
            IDDescending,
            IDAscending,
        }

        private static string selectedFolder = null;
        private static int selectedLayer = 0;
        private static WorldIO.LoadError? error = null;
        private static string[] folders;
        private static EntityManagerIO.EntityDatas data;

        private static string filterStr = "";
        private static List<EntityManagerIO.EntityData> filterCache;

        private static Sort currentSort = Sort.None;

        private static ulong? debugDraw = null;

        public static void Show()
        {
            if (IMGUISettings.ShowEntIODebug && ImGui.Begin("Entity IO Debug Info", ref IMGUISettings.ShowEntIODebug))
            {
                if (folders == null)
                {
                    if (Directory.Exists(WorldIO.SAVE_FOLDER))
                        folders = Directory.GetDirectories(WorldIO.SAVE_FOLDER);
                    else folders = null;

                    for (int i = 0; i < folders.Length; i++)
                    {
                        int ind = folders[i].LastIndexOf('/');
                        folders[i] = folders[i].Substring(ind + 1);
                    }

                    //folders = Directory.EnumerateDirectories(WorldIO.SAVE_FOLDER).ToArray();
                }

                ImGui.Text("Select a save file");
                if (ImGui.BeginCombo("Save files", selectedFolder))
                {
                    foreach (string folder in folders)
                    {
                        if (ImGui.Selectable(folder, folder == selectedFolder))
                        {
                            selectedFolder = folder;
                        }
                    }

                    ImGui.EndCombo();
                }

                if (selectedFolder != null)
                {
                    ImGui.InputInt("Layer", ref selectedLayer);
                    
                    if (error != null)
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), error.ToString());
                    }

                    if (data == null)
                    {
                        if (ImGui.Button("Load"))
                        {
                            data = new EntityManagerIO.EntityDatas();
                            var error = data.Load(selectedFolder, selectedLayer);

                            if (error != WorldIO.LoadError.Success)
                            {
                                IMGUIEntIODebug.error = error;
                                data = null;
                            }
                        }
                    }
                    else
                    {
                        if (ImGui.CollapsingHeader("Entities: " + data.numEntities.ToString()))
                        {
                            bool rebuildFilter = filterCache == null;

                            if (ImGui.InputText("Filter By Type", ref filterStr, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                            {
                                rebuildFilter = true;
                            }

                            if (ImGui.BeginCombo("Sort", currentSort.ToString()))
                            {
                                foreach (string name in typeof(Sort).GetEnumNames())
                                {
                                    if (ImGui.Selectable(name, name == currentSort.ToString()))
                                    {
                                        currentSort = Enum.Parse<Sort>(name);
                                        rebuildFilter = true;
                                    }
                                }
                                ImGui.EndCombo();
                            }

                            bool scrollToDebug = false;
                            bool lookAt = false;

                            if (debugDraw.HasValue)
                            {
                                ImGui.Text("Currently Drawing Id " + debugDraw.Value);
                                ImGui.SameLine();
                                if (ImGui.Button("Look at")) lookAt = true;
                                ImGui.SameLine();
                                if (ImGui.Button("Reset")) debugDraw = null;
                                ImGui.SameLine();
                                if (ImGui.Button("Scroll To")) scrollToDebug = true;   
                            }

                            if (rebuildFilter)
                            {
                                filterCache = new List<EntityManagerIO.EntityData>();

                                var lists = data.entityDatas.Values;

                                foreach (var list in lists)
                                {
                                    foreach (var entityData in list)
                                    {
                                        if (filterStr == "" || entityData.type.Contains(filterStr))
                                            filterCache.Add(entityData);
                                    }
                                }

                                switch (currentSort)
                                {
                                    case Sort.TypeDescending:
                                        filterCache = filterCache.OrderByDescending(x => x.type).ToList();
                                        break;
                                    case Sort.TypeAscending:
                                        filterCache = filterCache.OrderBy(x => x.type).ToList();
                                        break;
                                    case Sort.IDDescending:
                                        filterCache = filterCache.OrderByDescending(x => x.id).ToList();
                                        break;
                                    case Sort.IDAscending:
                                        filterCache = filterCache.OrderBy(x => x.id).ToList();
                                        break;
                                    case Sort.None:
                                    default:
                                        break;
                                }
                            }

                            foreach (var entityData in filterCache)
                            {
                                ImGui.PushID((int)entityData.id);
                                
                                if (scrollToDebug)
                                    ImGui.SetScrollHereY();
                                if (ImGui.TreeNode(entityData.id + ": " + entityData.type))
                                {
                                    ImGui.Text("Id:");
                                    ImGui.SameLine();
                                    ImGui.Text(entityData.id.ToString());
                                    ImGui.Text("Type:");
                                    ImGui.SameLine();
                                    ImGui.Text(entityData.type);
                                    ImGui.Text("Size:");
                                    ImGui.SameLine();
                                    ImGui.Text(entityData.size.ToString());

                                    ImGui.Text("Position:");
                                    ImGui.SameLine();
                                    ImGui.Text("X: " + entityData.position.X + " Y: " + entityData.position.Y + " Z: " + entityData.position.Z);

                                    if (Main.gameStateManager.GetCurrentGameState() is GameStateTheIsland gsIsland) 
                                    {
                                        if (lookAt)
                                        {
                                            // TODO
                                            // Too lazy to figure the math rn
                                            //(Main.gameStateManager.GetCurrentGameState() as GameStateTheIsland).GetWorld().player.rot
                                        }
                                        if (ImGui.Button("Draw in world"))
                                        {
                                            debugDraw = entityData.id;
                                        }

                                        if (debugDraw.HasValue && debugDraw.Value == entityData.id)
                                        {
                                            Main.Renderer.DEBUGMarkersRect.Add(new Rendering.RendererDeferred.DEBUGDraw
                                            {
                                                Color = Color.Purple,
                                                Position = entityData.position.InWorldSpace(),
                                                Scale = new Vector3(Cubes.Cube.CUBE_SCALE * Chunk.CHUNK_SIZE),
                                            });
                                        }
                                    }
                                    ImGui.TreePop();
                                }
                                ImGui.PopID();
                            }
                        }
                    }
                }
            }
            ImGui.End();
        }
    }
}
