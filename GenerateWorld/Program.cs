using BrAssetsManager;
using Engine;
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
// TODO this is hardcoded...
ses.ModsFolder = "C:\\Users\\taylo\\Documents\\programming\\CS\\ViMG2\\bin\\Debug\\mods\\net8.0-windows";
GlobalState.SessionInformation = ses;
Main.IsHeadless = true;
Main.gameStateManager = new GameStateManager();
Main.gameStateManager.Initialize();
//Main.camera = new Engine.Common.CameraPerspective(new Vector3(0, 0, 0), new Vector3(0, 180, 0), new Vector3(1), Main.FOV_DEGREES, Main.NEAR, Main.FAR);

var _services = new GameServiceContainer();
var _content = new ContentManager(_services);
_content.RootDirectory = "Content";

GlobalState.assetsManager = new ViMGAssetsManager(_content);
GlobalState.assetsManager.LoadContent(Directory.GetCurrentDirectory() + "/Content");

ModManager modManager = new ModManager();
modManager.LoadModDlls();
RegistryService registry = new RegistryService(null);
GlobalState.Registry = registry;
registry.Register();

GameStateTheIsland.CreateWorld(null, "thing");
