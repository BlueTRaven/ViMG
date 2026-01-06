using BrUtility;
using Engine.ChunkStuff;
using Engine.Clients;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Items;
using ViMG.Rendering;
using ViMG.VertexDeclarations;
using static ViMG.Cubes.Cube;
using static ViMG.Cubes.Cube.CubeVisualInstance;
using static ViMG.UIs.UI;

namespace ViMG.Cubes
{
    public class Cube : IRegisterable
	{
		public struct CubeAnimation
        {
			public float FrameTime;
			public int NumFrames;
			public int FrameWidth;

			public readonly bool Valid;

			public CubeAnimation(float frameTime, int numFrames, int frameWidth)
            {
				this.FrameTime = frameTime;
				this.NumFrames = numFrames;
                this.FrameWidth = frameWidth;

                Valid = true;
            }
        }

		public struct CubeInstance
		{
			public Chunk chunk;

			public CubePosition position;

			public int cubeId;
			public int flags;   //TODO

			public bool valid;

			public CubeInstance(Chunk chunk, CubePosition position, int cubeId, int flags = 0)
			{
				this.chunk = chunk;
				this.position = position;
				this.cubeId = cubeId;
				this.flags = flags;

				valid = true;
			}
		}

        public struct CubeVisualInstance
		{
			public enum Face : byte
			{
				NONE = 0,
				LEFT = 1 << 0,
				RIGHT = 1 << 1,
				UP = 1 << 2,
				DOWN = 1 << 3,
				FRONT = 1 << 4,
				BACK = 1 << 5,
				ALL = LEFT | RIGHT | UP | DOWN | FRONT | BACK,
				DIRTY = 1 << 6

			}

			public bool IsDirty { get { return (face & Face.DIRTY) == Face.DIRTY; } set { if (value) face |= Face.DIRTY; else face &= ~Face.DIRTY; } }

			private Face face;

			public CubeVisualInstance(MeshHelper.CubeFace faces, bool dirty)
			{
				face = Face.NONE;
				SetFaces(faces);
				IsDirty = dirty;
			}

			public MeshHelper.CubeFace GetFaces()
			{
				return (MeshHelper.CubeFace)face;
			}

			public void SetFaces(MeshHelper.CubeFace faces)
			{
				face = (Face)faces;
			}

			private static CubeVisualInstance Clean;
			private static CubeVisualInstance Dirty;

			static CubeVisualInstance()
			{
				Clean = new CubeVisualInstance();
				Clean.IsDirty = false;

				Dirty = new CubeVisualInstance();
				Dirty.IsDirty = true;
			}

			public static ref readonly CubeVisualInstance CreateClean()
			{
				return ref Clean;
				/*var cached = new CubeVisualInstance();
				cached.dirty = false;
				return cached;*/
			}

			public static ref readonly CubeVisualInstance CreateDirty()
			{
				return ref Dirty;
				//var cached = new CubeVisualInstance();
				//cached.dirty = true;
				//return cached;
			}
		}

		public const float CUBES_PER_UNIT = 10f;
		public const float CUBE_SCALE = 1f / CUBES_PER_UNIT;
		public const float PIXELS_PER_CUBE = 16;
		public const float PIXEL_SCALE = CUBE_SCALE / PIXELS_PER_CUBE;

		private readonly int[] cubeFaceLookup = new int[(int)MeshHelper.CubeFace.BACK + 1] 
		{
			-1,	//0 - none
			0,	//1 - left
			1,	//2 - right
			-1, //3 - left | right - invalid
			2,  //4 - up
			-1, //5 - left | up
			-1, //6 - right | up
			-1, //7 - left | right | up
			3,  //8 - down
			-1, //9
			-1, //10
			-1, //11
			-1, //12
			-1, //13
			-1, //14
			-1, //15
			4,  //16 - front
			-1, //17
			-1, //18
			-1, //19
			-1, //20
			-1, //21
			-1, //22
			-1, //23
			-1, //24
			-1, //25
			-1, //26
			-1, //27
			-1, //28
			-1, //29
			-1, //30
			-1, //31
			5,  //32 - back
		};

		public enum RenderPass
        {
			Opaque,
			Transparent,
			DepthOnly,
			Fluid,
			Air,
        }

		public enum TransparencyValue
		{
			Opaque,
			Transparent,
			TransparentOccludesSiblings, //occludes "siblings", or cubes of the same type
			Invisible,	//don't mesh at all
			Air,		//Air can be meshed under specific conditions

			InvisibleOnDepth = 1 << 32,
		}

		public enum CollisionValue 
		{
			None,
			Collidable,
			Rope,
			Door,
			Platform,
			LiquidWater,
		}

		public string Name = "";
		public string Description = "";

		public ushort Id { get; private set; }
		public string Identifier { get; private set; }

		public VerySimpleMesh mesh;

		public int MineProgressToBreak;
		public int MineLevelRequirement;
		public bool Touchable = true;

		//Touchable objects may be collidable, but we assume it's not solid in such a scenario since the player can't interact with it in any way.
		public bool Solid => Collision == CollisionValue.Collidable && Touchable;

		public TransparencyValue Transparency;
		public CollisionValue Collision = CollisionValue.Collidable;

		public ClientCube Client { get; protected set; }

		public Cube(string identifier, int mineProgressToBreak, int mineLevelRequirement = 0)
		{
			this.Identifier = identifier;
			this.MineProgressToBreak = mineProgressToBreak;
			this.MineLevelRequirement = mineLevelRequirement;
		}

		public void SetId(ushort id)
		{
			this.Id = id;
		}

		public virtual void GetDrops(List<ItemInstance> itemsToDrop)
		{

		}

		protected void DropSelf(List<ItemInstance> itemsToDrop)
		{
			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("item_" + Identifier), 1, 1));
		}

		public virtual bool CanRightClick(World world, CubePosition position)
		{
			//cube trackers and multi cube trackers can be right clicked under any situation
			//return this is ICubeTracker || this is IMultiCubeTracker; // ??? this is a Cube, not an entity...
			return false;
		}

		public virtual void OnLeftClick(World world, CubePosition position)
		{

		}

		public virtual void OnRightClick(World world, CubePosition position)
		{

		}

		public virtual bool CanPlace(World world, ChunkManager manager, CubePosition position)
        {
			return true;
        }

		public virtual bool CanMine(CubePosition position)
		{
			return true;
		}

		public virtual void OnMined(Player? player, CubePosition position)
        {

        }

		public virtual void OnAdjacentUpdated(World world, ChunkManager manager, CubePosition notified, CubePosition updating, int updatedId, double updatedTime)
		{

		}

		public virtual void OnLoaded(World world, CubePosition position)
        {

        }

		public virtual void OnPlayerPlaced(Player player, CubePosition position)
		{

		}

		public virtual void PostChunkGen(WorldPrototype world, CubePosition position)
		{

		}

		public virtual void OnRandomUpdate(World world, ChunkManager manager, CubePosition position)
        {

        }

		public virtual bool ShouldMeshPass(RenderPass pass)
		{
            if (Transparency == TransparencyValue.Invisible)
                return false;

            if (pass == RenderPass.DepthOnly && (Transparency & TransparencyValue.InvisibleOnDepth) > 0)
                return false;

            //Opaque cubes only generate a mesh in the opaque pass.
            if (Transparency == TransparencyValue.Opaque && pass == RenderPass.Transparent)
                return false;
            //Transparent cubes may generate a mesh in both the opaque and the transparent pass.
            //however, by default, assume we only want to generate in the opaque pass. The overwhelming majority of cubes will use
            //boolean alpha.
            //Transparent cubes with both opaque and transparent components may override MakeVerts to generate.
            if ((Transparency == TransparencyValue.Transparent || Transparency == TransparencyValue.TransparentOccludesSiblings) && pass == RenderPass.Transparent)
                return false;

            if (Transparency == TransparencyValue.Air && pass != RenderPass.Air)
                return false;

			return true;
        }

		public virtual void MakeCubeVerts(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, FastList<VertexCube> vertices, List<int> indices, int vertexOffset = 0)
		{
            //using var zone = TracyImpl.Tracy.BeginZone();

            if ((parameters.faces & MeshHelper.CubeFace.FRONT) == MeshHelper.CubeFace.FRONT)
				MakeCubeFaceVerts(pass, data, parameters, GetQuadForFace(parameters, MeshHelper.CubeFace.FRONT), MeshHelper.CubeFace.FRONT, vertices, indices, vertexOffset);

            if ((parameters.faces & MeshHelper.CubeFace.RIGHT) == MeshHelper.CubeFace.RIGHT)
                MakeCubeFaceVerts(pass, data, parameters, GetQuadForFace(parameters, MeshHelper.CubeFace.RIGHT), MeshHelper.CubeFace.RIGHT, vertices, indices, vertexOffset);

            if ((parameters.faces & MeshHelper.CubeFace.BACK) == MeshHelper.CubeFace.BACK)
                MakeCubeFaceVerts(pass, data, parameters, GetQuadForFace(parameters, MeshHelper.CubeFace.BACK), MeshHelper.CubeFace.BACK, vertices, indices, vertexOffset);

            if ((parameters.faces & MeshHelper.CubeFace.LEFT) == MeshHelper.CubeFace.LEFT)
                MakeCubeFaceVerts(pass, data, parameters, GetQuadForFace(parameters, MeshHelper.CubeFace.LEFT), MeshHelper.CubeFace.LEFT, vertices, indices, vertexOffset);

            if ((parameters.faces & MeshHelper.CubeFace.DOWN) == MeshHelper.CubeFace.DOWN)
                MakeCubeFaceVerts(pass, data, parameters, GetQuadForFace(parameters, MeshHelper.CubeFace.DOWN), MeshHelper.CubeFace.DOWN, vertices, indices, vertexOffset);

            if ((parameters.faces & MeshHelper.CubeFace.UP) == MeshHelper.CubeFace.UP)
                MakeCubeFaceVerts(pass, data, parameters, GetQuadForFace(parameters, MeshHelper.CubeFace.UP), MeshHelper.CubeFace.UP, vertices, indices, vertexOffset);
        }

		public virtual void MakeCubeFaceVerts(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, ChunkRenderMesher.CubeMeshingQuad quad, MeshHelper.CubeFace face, FastList<VertexCube> vertices, List<int> indices, int vertexOffset)
		{
            //using var zone = TracyImpl.Tracy.BeginZone();

            int offset = vertices.Length + vertexOffset;
            indices.Add(offset + 0);
            indices.Add(offset + 1);
            indices.Add(offset + 3);
            indices.Add(offset + 1);
            indices.Add(offset + 2);
            indices.Add(offset + 3);

            const int textureWidth = 1024;
            const int textureHeight = 1024;

            const float cubeSideWidth = 1f / textureWidth;
            const float cubeSideHeight = 1f / textureHeight;

			RectangleF sourceRect = Client.GetSourceRect(pass, data, parameters, face);

            Vector2 uvNear = new Vector2(sourceRect.x * cubeSideWidth, sourceRect.y * cubeSideHeight);
            Vector2 uvFar = new Vector2((sourceRect.x + sourceRect.width) * cubeSideWidth, (sourceRect.y + sourceRect.height) * cubeSideHeight);

            vertices.Add(new VertexCube(quad.a, Client?.GetTintColor() ?? Color.White, new Vector2(uvFar.X, uvFar.Y), quad.n));
            vertices.Add(new VertexCube(quad.b, Client?.GetTintColor() ?? Color.White, new Vector2(uvNear.X, uvFar.Y), quad.n));
            vertices.Add(new VertexCube(quad.c, Client?.GetTintColor() ?? Color.White, new Vector2(uvNear.X, uvNear.Y), quad.n));
            vertices.Add(new VertexCube(quad.d, Client?.GetTintColor() ?? Color.White, new Vector2(uvFar.X, uvNear.Y), quad.n));

            var anim = Client.GetAnimation(pass, data, parameters, face);

            if (anim.Valid)
            {
                for (int i = offset; i < 4; i++)
                {
                    var vertex = vertices[i];

                    vertex.AnimFrameTime = anim.FrameTime;
                    vertex.NumAnimFrames = anim.NumFrames;
                    vertex.AnimFrameSize = anim.FrameWidth;

                    vertices.Buffer[i] = vertex;
                }
            }
        }

		public ChunkRenderMesher.CubeMeshingQuad GetQuadForFace(ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
		{
            Vector3 min = parameters.positionWS;
            Vector3 max = parameters.positionWS + new Vector3(Cube.CUBE_SCALE);

            Vector3 a;
            Vector3 b;
            Vector3 c;
            Vector3 d;
            Vector3 n;

            switch (face)
            {
                case MeshHelper.CubeFace.RIGHT:
                    //l_t_f, l_t_n, l_b_n, l_b_f
                    a = new Vector3(min.X, min.Y, max.Z);
                    b = new Vector3(min.X, min.Y, min.Z);
                    c = new Vector3(min.X, max.Y, min.Z);
                    d = new Vector3(min.X, max.Y, max.Z);
                    n = new Vector3(-1, 0, 0);
                    break;
                case MeshHelper.CubeFace.LEFT:
                    //r_t_n, r_t_f, r_b_f, r_b_n
                    a = new Vector3(max.X, min.Y, min.Z);
                    b = new Vector3(max.X, min.Y, max.Z);
                    c = new Vector3(max.X, max.Y, max.Z);
                    d = new Vector3(max.X, max.Y, min.Z);
                    n = new Vector3(1, 0, 0);
                    break;
                case MeshHelper.CubeFace.UP:
                    //r_b_f, l_b_f, l_b_n, r_b_n
                    a = new Vector3(min.X, max.Y, min.Z);
                    b = new Vector3(max.X, max.Y, min.Z);
                    c = new Vector3(max.X, max.Y, max.Z);
                    d = new Vector3(min.X, max.Y, max.Z);
                    n = new Vector3(0, 1, 0);
                    break;
                case MeshHelper.CubeFace.DOWN:
                    //l_t_f, r_t_f, r_t_n, l_t_n
                    a = new Vector3(min.X, min.Y, max.Z);
                    b = new Vector3(max.X, min.Y, max.Z);
                    c = new Vector3(max.X, min.Y, min.Z);
                    d = new Vector3(min.X, min.Y, min.Z);
                    n = new Vector3(0, -1, 0);
                    break;
                case MeshHelper.CubeFace.FRONT:
                    //l_t_n, r_t_n, r_b_n, l_b_n
                    a = new Vector3(min.X, min.Y, min.Z);
                    b = new Vector3(max.X, min.Y, min.Z);
                    c = new Vector3(max.X, max.Y, min.Z);
                    d = new Vector3(min.X, max.Y, min.Z);
                    n = new Vector3(0, 0, -1);
                    break;
                case MeshHelper.CubeFace.BACK:
                    //r_t_f, l_t_f, l_b_f, r_b_f
                    a = new Vector3(max.X, min.Y, max.Z);
                    b = new Vector3(min.X, min.Y, max.Z);
                    c = new Vector3(min.X, max.Y, max.Z);
                    d = new Vector3(max.X, max.Y, max.Z);
                    n = new Vector3(0, 0, 1);
                    break;
                default:
                    a = Vector3.Zero;
                    b = Vector3.Zero;
                    c = Vector3.Zero;
                    d = Vector3.Zero;
                    n = Vector3.Zero;
                    break;
            }

			return new ChunkRenderMesher.CubeMeshingQuad()
			{
				a = a,
				b = b,
				c = c,
				d = d,
				n = n
			};
        }

		public void DropSelf(List<ItemInstance> itemsToDrop, int num = 1)
		{
			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get(this.Identifier + "_item"), num, 1));
		}

		public static Item GetItem(Cube cube)
        {
			return Main.Registry.ItemRegistry.Get("item_" + cube.Identifier);
        }
	}

	public class ClientCube
	{
		private static VerySimpleMesh heldMesh = default;

		protected Cube cube;
		private RectangleF sourceRect;
		public readonly Color tint;
		private CubeFacingLayout? layout = null;

		public ClientCube(Cube cube, RectangleF sourceRect, Color tint)
		{
			this.cube = cube;
			this.sourceRect = sourceRect;
            this.tint = tint;
        }
		public ClientCube(Cube cube, CubeFacingLayout layout, Color tint) : this(cube, layout.Front, tint)
		{
			this.layout = layout;
		}

        public virtual RectangleF GetSourceRect(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters)
        {
            return sourceRect;
        }

        public virtual RectangleF GetSourceRect(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            if (layout == null)
                return GetSourceRect(pass, data, parameters);

            switch (face)
            {
                case MeshHelper.CubeFace.NONE:
                    return RectangleF.Empty;
                case MeshHelper.CubeFace.LEFT:
                    return layout.Left;
                case MeshHelper.CubeFace.RIGHT:
                    return layout.Right;
                case MeshHelper.CubeFace.UP:
                    return layout.Top;
                case MeshHelper.CubeFace.DOWN:
                    return layout.Bottom;
                case MeshHelper.CubeFace.FRONT:
                    return layout.Front;
                case MeshHelper.CubeFace.BACK:
                    return layout.Back;
                case MeshHelper.CubeFace.ALL:
                    return RectangleF.Empty;
            }

            return RectangleF.Empty;
        }

        public virtual RectangleF GetHeldSourceRect()
        {
            if (layout == null)
                return sourceRect;
            else return layout.Front;
        }

        public Color GetTintColor()
        {
            return tint;
        }

        public virtual CubeAnimation GetAnimation(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            return new CubeAnimation();
        }

        public virtual VerySimpleMesh GetHeldMesh(GraphicsDevice device)
        {
            if (heldMesh.IBO == null)
            {
                FastList<VertexCube> vertices = new FastList<VertexCube>();
                List<int> indices = new List<int>();

                ChunkRenderMesher.CubeMeshingParameters parameters = new ChunkRenderMesher.CubeMeshingParameters()
                {
                    cube = cube,
                    id = cube.Id,
                    faces = MeshHelper.CubeFace.ALL,
                    position = new CubePosition(),
                    positionWS = new Vector3()
                };

                cube.MakeCubeVerts(RenderPass.Opaque, default, parameters, vertices, indices);

                if (vertices.Length > 0)
                    heldMesh = VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices));
            }

            return heldMesh;
        }

        public virtual RectangleF GetHeldSourceRect(ClientStates client)
        {
            return new RectangleF(0, 0, 1024, 1024);
        }

        public virtual void OnLeftClick(ClientStates client, int playerId, CubePosition position)
        {

        }

        public virtual void OnRightClick(ClientStates client, int playerId, CubePosition position)
        {

        }
    }
}
