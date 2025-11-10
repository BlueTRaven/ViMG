using Engine.Mods;
using Microsoft.Win32;
using SharpDX.MediaFoundation;
using ViMG;
using ViMG.GameStates;
using ViMG.UIs;

Console.WriteLine("Hello, World!");

SessionInformation ses = new SessionInformation();
ses.ModsFolder = "C:\\Users\\taylo\\Documents\\programming\\CS\\ViMG\\bin\\Debug\\mods\\net8.0-windows";
Main.SessionInformation = ses;

ModManager modManager = new ModManager();
modManager.LoadModDlls();
RegistryService registry = new RegistryService(null);
registry.Register();

GameStateTheIsland.CreateWorld(null, "thing");
