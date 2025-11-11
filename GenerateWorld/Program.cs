using BrAssetsManager;
using Engine.Mods;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SharpDX.MediaFoundation;
using ViMG;
using ViMG.GameStates;
using ViMG.UIs;

Console.WriteLine("Hello, World!");

SessionInformation ses = new SessionInformation();
ses.ModsFolder = "C:\\Users\\taylo\\Documents\\programming\\CS\\ViMG\\bin\\Debug\\mods\\net8.0-windows";
Main.SessionInformation = ses;

var _services = new GameServiceContainer();
var _content = new ContentManager(_services);
_content.RootDirectory = "Content";

Main.assetsManager = new ViMGAssetsManager(_content);
Main.assetsManager.LoadContent(Directory.GetCurrentDirectory() + "/Content");

ModManager modManager = new ModManager();
modManager.LoadModDlls();
RegistryService registry = new RegistryService(null);
Main.Registry = registry;
registry.Register();

GameStateTheIsland.CreateWorld(null, "thing");
