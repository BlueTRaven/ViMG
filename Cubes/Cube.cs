using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Items;
using ViMG.UIs;
using static ViMG.Cubes.Cube.CubeVisualInstance;

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
			Platform,
			LiquidWater,
		}

		public string Name = "";
		public string Description = "";

		public ushort Id { get; private set; }
		public string Identifier { get; private set; }
		private readonly CubeFacingLayout layout;
		//private readonly RectangleF[] sourceRectSides = new RectangleF[6];
		private readonly RectangleF sourceRect;
		private readonly Color tintColor;

		public SimpleMesh<VertexCube, int> mesh;

		public int MineProgressToBreak;
		public int MineLevelRequirement;
		public bool Touchable = true;

		//Touchable objects may be collidable, but we assume it's not solid in such a scenario since the player can't interact with it in any way.
		public bool Solid => Collision == CollisionValue.Collidable && Touchable;

		public TransparencyValue Transparency;
		public CollisionValue Collision = CollisionValue.Collidable;

		public Cube(string identifier, RectangleF sourceRect, Color color, int mineProgressToBreak, int mineLevelRequirement = 0)
		{
			this.Identifier = identifier;

			this.sourceRect = sourceRect;
			layout = new CubeFacingLayout(sourceRect);
			//Array.Fill(sourceRectSides, sourceRect);
			this.tintColor = color;
			this.MineProgressToBreak = mineProgressToBreak;
			this.MineLevelRequirement = mineLevelRequirement;
		}

		public Cube(string identifier, CubeFacingLayout layout, Color color, int mineProgressToBreak, int mineLevelRequirement = 0)
		{
			/*if (sourceRectSides.Length != 6)
				throw new Exception("Cubes cannot have more or less than 6 sides.");*/

			this.Identifier = identifier;

			this.sourceRect = layout.Front;
			this.layout = layout;
			//this.sourceRectSides = sourceRectSides;
			this.tintColor = color;
			this.MineProgressToBreak = mineProgressToBreak;
			this.MineLevelRequirement = mineLevelRequirement;
		}

		public void SetId(ushort id)
		{
			this.Id = id;

			Main.Registry.CubeRegistry.noAo[Id] = Transparency == TransparencyValue.Invisible || Transparency == TransparencyValue.Transparent;
		}

		public virtual RectangleF GetSourceRect(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters)
		{
			return sourceRect;
		}

		public virtual RectangleF GetSourceRect(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
		{
			if (cubeFaceLookup[(int)face] != -1)
			{
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
						//return sourceRectSides[cubeFaceLookup[(int)face]];
				}

			}
			
			return RectangleF.Empty;
		}

		public virtual CubeAnimation GetAnimation(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
			return new CubeAnimation();
        }

		public virtual void GetDrops(List<ItemInstance> itemsToDrop)
		{

		}

		protected void DropSelf(List<ItemInstance> itemsToDrop)
		{
			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("item_" + Identifier), 1, 1));
		}

		public virtual bool CanPlace(World world, ChunkManager manager, CubePosition position)
        {
			return true;
        }

		public virtual bool CanMine(CubePosition position)
		{
			return true;
		}

		public virtual void OnMined(Player player, CubePosition position)
        {

        }

		public virtual void OnAdjacentUpdated(World world, ChunkManager manager, CubePosition notified, CubePosition updating, int updatedId)
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

		public virtual SimpleMesh<VertexCube, int> GetHeldMesh(GraphicsDevice device)
		{
			if (mesh == null)
			{
				List<VertexCube> vertices = new List<VertexCube>();
				List<int> indices = new List<int>();

				ChunkMesher.CubeMeshingParameters parameters = new ChunkMesher.CubeMeshingParameters()
				{
					cube = this,
					id = Id,
					faces = MeshHelper.CubeFace.ALL,
					position = new CubePosition(),
					positionWS = new Vector3()
				};

				MakeCubeVerts(RenderPass.Opaque, null, parameters, vertices, indices);

				mesh = new SimpleMesh<VertexCube, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("cubes_textures"));
			}

			return mesh;
		}

		public virtual RectangleF GetHeldSourceRect(World world)
        {
			return new RectangleF(0, 0, 1024, 1024);
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

		public virtual void MakeCubeVerts(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters, List<VertexCube> vertices, List<int> indices)
        {
			if ((parameters.faces & MeshHelper.CubeFace.FRONT) == MeshHelper.CubeFace.FRONT)
				MakeCubeFaceVerts(pass, world, parameters, GetQuadForFace(parameters, MeshHelper.CubeFace.FRONT), MeshHelper.CubeFace.FRONT, vertices, indices);

            if ((parameters.faces & MeshHelper.CubeFace.RIGHT) == MeshHelper.CubeFace.RIGHT)
                MakeCubeFaceVerts(pass, world, parameters, GetQuadForFace(parameters, MeshHelper.CubeFace.RIGHT), MeshHelper.CubeFace.RIGHT, vertices, indices);

            if ((parameters.faces & MeshHelper.CubeFace.BACK) == MeshHelper.CubeFace.BACK)
                MakeCubeFaceVerts(pass, world, parameters, GetQuadForFace(parameters, MeshHelper.CubeFace.BACK), MeshHelper.CubeFace.BACK, vertices, indices);

            if ((parameters.faces & MeshHelper.CubeFace.LEFT) == MeshHelper.CubeFace.LEFT)
                MakeCubeFaceVerts(pass, world, parameters, GetQuadForFace(parameters, MeshHelper.CubeFace.LEFT), MeshHelper.CubeFace.LEFT, vertices, indices);

            if ((parameters.faces & MeshHelper.CubeFace.DOWN) == MeshHelper.CubeFace.DOWN)
                MakeCubeFaceVerts(pass, world, parameters, GetQuadForFace(parameters, MeshHelper.CubeFace.DOWN), MeshHelper.CubeFace.DOWN, vertices, indices);

            if ((parameters.faces & MeshHelper.CubeFace.UP) == MeshHelper.CubeFace.UP)
                MakeCubeFaceVerts(pass, world, parameters, GetQuadForFace(parameters, MeshHelper.CubeFace.UP), MeshHelper.CubeFace.UP, vertices, indices);
        }

		public virtual void MakeCubeFaceVerts(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters, ChunkMesher.CubeMeshingQuad quad, MeshHelper.CubeFace face, List<VertexCube> vertices, List<int> indices)
		{
            int offset = vertices.Count;
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

            RectangleF sourceRect = GetSourceRect(pass, world, parameters, face);

            Vector2 uvNear = new Vector2(sourceRect.x * cubeSideWidth, sourceRect.y * cubeSideHeight);
            Vector2 uvFar = new Vector2((sourceRect.x + sourceRect.width) * cubeSideWidth, (sourceRect.y + sourceRect.height) * cubeSideHeight);

            vertices.Add(new VertexCube(quad.a, GetTintColor(), new Vector2(uvFar.X, uvFar.Y), quad.n));
            vertices.Add(new VertexCube(quad.b, GetTintColor(), new Vector2(uvNear.X, uvFar.Y), quad.n));
            vertices.Add(new VertexCube(quad.c, GetTintColor(), new Vector2(uvNear.X, uvNear.Y), quad.n));
            vertices.Add(new VertexCube(quad.d, GetTintColor(), new Vector2(uvFar.X, uvNear.Y), quad.n));

            var anim = GetAnimation(pass, world, parameters, face);

            if (anim.Valid)
            {
                for (int i = offset; i < 4; i++)
                {
                    var vertex = vertices[i];

                    vertex.AnimFrameTime = anim.FrameTime;
                    vertex.NumAnimFrames = anim.NumFrames;
                    vertex.AnimFrameSize = anim.FrameWidth;

                    vertices[i] = vertex;
                }
            }
        }

		public ChunkMesher.CubeMeshingQuad GetQuadForFace(ChunkMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
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
                case MeshHelper.CubeFace.LEFT:
                    //l_t_f, l_t_n, l_b_n, l_b_f
                    a = new Vector3(min.X, min.Y, max.Z);
                    b = new Vector3(min.X, min.Y, min.Z);
                    c = new Vector3(min.X, max.Y, min.Z);
                    d = new Vector3(min.X, max.Y, max.Z);
                    n = new Vector3(-1, 0, 0);
                    break;
                case MeshHelper.CubeFace.RIGHT:
                    //r_t_n, r_t_f, r_b_f, r_b_n
                    a = new Vector3(max.X, min.Y, min.Z);
                    b = new Vector3(max.X, min.Y, max.Z);
                    c = new Vector3(max.X, max.Y, max.Z);
                    d = new Vector3(max.X, max.Y, min.Z);
                    n = new Vector3(1, 0, 0);
                    break;
                case MeshHelper.CubeFace.UP:
                    //r_b_f, l_b_f, l_b_n, r_b_n
                    a = new Vector3(max.X, max.Y, max.Z);
                    b = new Vector3(min.X, max.Y, max.Z);
                    c = new Vector3(min.X, max.Y, min.Z);
                    d = new Vector3(max.X, max.Y, min.Z);
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

			return new ChunkMesher.CubeMeshingQuad()
			{
				a = a,
				b = b,
				c = c,
				d = d,
				n = n
			};
        }

		public Color GetTintColor()
		{
			return tintColor;
		}

		public void DropSelf(List<ItemInstance> itemsToDrop, int num = 1)
		{
			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get(this.Identifier + "_item"), num, 1));
		}

		public static SimpleMesh<VertexCube, int> MakeCubeWithCorrectedTextureCoordinates(GraphicsDevice device, Color color, Texture2D texture)
		{
			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			Vector3 min = Vector3.Zero;
			Vector3 max = new Vector3(CUBE_SCALE);

			Vector3 l_t_n = new Vector3(min.X, min.Y, min.Z);
			Vector3 r_t_n = new Vector3(max.X, min.Y, min.Z);
			Vector3 r_b_n = new Vector3(max.X, max.Y, min.Z);
			Vector3 l_b_n = new Vector3(min.X, max.Y, min.Z);
			Vector3 l_t_f = new Vector3(min.X, min.Y, max.Z);
			Vector3 r_t_f = new Vector3(max.X, min.Y, max.Z);
			Vector3 r_b_f = new Vector3(max.X, max.Y, max.Z);
			Vector3 l_b_f = new Vector3(min.X, max.Y, max.Z);

			MakeQuad(l_t_n, r_t_n, r_b_n, l_b_n, new Vector3(0, 0, 1), color, vertices, indices, texture);

			MakeQuad(r_t_n, r_t_f, r_b_f, r_b_n, new Vector3(-1, 0, 0), color, vertices, indices, texture);

			MakeQuad(r_t_f, l_t_f, l_b_f, r_b_f, new Vector3(0, 0, -1), color, vertices, indices, texture);

			MakeQuad(l_t_f, l_t_n, l_b_n, l_b_f, new Vector3(1, 0, 0), color, vertices, indices, texture);

			MakeQuad(l_t_f, r_t_f, r_t_n, l_t_n, new Vector3(0, 1, 0), color, vertices, indices, texture);

			MakeQuad(r_b_f, l_b_f, l_b_n, r_b_n, new Vector3(0, -1, 0), color, vertices, indices, texture);

			return new SimpleMesh<VertexCube, int>(device, vertices, indices, texture);
		}

		private static void MakeQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color, List<VertexCube> vertices, List<int> indices, Texture2D texture)
		{
			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			float xmin = 0;
			float xmax = 1f / ((float)texture.Width / 16f);
			float ymin = 0;
			float ymax = 1f / ((float)texture.Height / 16f);

			vertices.Add(new VertexCube(a, color, new Vector2(xmin, ymin), normal));
			vertices.Add(new VertexCube(b, color, new Vector2(xmax, ymin), normal));
			vertices.Add(new VertexCube(c, color, new Vector2(xmax, ymax), normal));
			vertices.Add(new VertexCube(d, color, new Vector2(xmin, ymax), normal));
		}

		public static Item GetItem(Cube cube)
        {
			return Main.Registry.ItemRegistry.Get("item_" + cube.Identifier);
        }
	}
}
