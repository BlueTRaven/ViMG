using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class Cube : IRegisterable
	{
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
			public bool dirty;

			public MeshHelper.CubeFace clearSides;  //sides that are clear of other cubes

			public AdjacentCubes adjacents;

			[Flags]
			public enum AdjacentCubes
			{
				None = 1,
				TopLeftBack		= 1 << 0 + 0 + 0,
				TopBack			= 1 << 1 + 0 + 0,
				TopRightBack	= 1 << 2 + 0 + 0,
				MidLeftBack		= 1 << 3,
				MidBack			= 1 << 4,
				MidRightBack	= 1 << 5,
				BotLeftBack		= 1 << 6,
				BotBack			= 1 << 7,
				BotRightBack	= 1 << 8,
				TopLeftMid		= 1 << 9,
				TopMid			= 1 << 10,
				TopRightMid		= 1 << 11,
				MidLeftMid		= 1 << 12,
				// our block
				MidRightMid		= 1 << 13,
				BotLeftMid		= 1 << 15,
				BotMid			= 1 << 16,
				BotRightMid		= 1 << 17,
				TopLeftFront	= 1 << 18,
				TopFront		= 1 << 19,
				TopRightFront	= 1 << 20,
				MidLeftFront	= 1 << 21,
				MidFront		= 1 << 22,
				MidRightFront	= 1 << 23,
				BotLeftFront	= 1 << 24,
				BotFront		= 1 << 25,
				BotRightFront	= 1 << 26
			}

			private static CubeVisualInstance Clean;
			private static CubeVisualInstance Dirty;

			static CubeVisualInstance()
			{
				Clean = new CubeVisualInstance();
				Clean.dirty = false;

				Dirty = new CubeVisualInstance();
				Dirty.dirty = true;
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

		public const float CUBE_SCALE = 20;

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

		public enum TransparencyValue
		{
			Opaque,
			Transparent,
			TransparentOccludesSiblings, //occludes "siblings", or cubes of the same type
			Invisible,	//don't mesh at all
		}

		public int Id { get; private set; }
		public string Identifier { get; private set; }
		private readonly RectangleF[] sourceRectSides = new RectangleF[6];
		private readonly RectangleF sourceRect;
		private readonly Color tintColor;

		public SimpleMesh<VertexPositionColorTextureNormal, int> mesh;

		public int MineProgressRequirement;
		public bool Solid = true;

		public TransparencyValue Transparency;

		public Cube(string identifier, RectangleF sourceRect, Color color, int mineProgressRequirement)
		{
			this.Identifier = identifier;

			this.sourceRect = sourceRect;
			Array.Fill(sourceRectSides, sourceRect);
			this.tintColor = color;
			this.MineProgressRequirement = mineProgressRequirement;
		}

		public Cube(string identifier, RectangleF[] sourceRectSides, Color color, int mineProgressRequirement)
		{
			if (sourceRectSides.Length != 6)
				throw new Exception("Cubes cannot have more or less than 6 sides.");

			this.Identifier = identifier;

			this.sourceRect = sourceRectSides[0];
			this.sourceRectSides = sourceRectSides;
			this.tintColor = color;
			this.MineProgressRequirement = mineProgressRequirement;
		}

		public void SetId(int id)
		{
			this.Id = id;
		}

		public RectangleF GetSourceRect()
		{
			return sourceRect;
		}

		public RectangleF GetSourceRect(MeshHelper.CubeFace face)
		{
			if (cubeFaceLookup[(int)face] != -1)
			{
				return sourceRectSides[cubeFaceLookup[(int)face]];
			}

			return RectangleF.Empty;
		}

		public virtual void GetDrops(List<ItemInstance> itemsToDrop)
		{

		}

		public virtual bool CanMine(CubePosition position)
		{
			return true;
		}

		public virtual void OnAdjacentUpdated(ChunkData parent, CubePosition position, ChunkData updatingParent, CubePosition updating, int updatedId)
		{

		}

		public virtual void PostChunkInit(ChunkData chunkData, CubePosition position)
		{

		}

		public SimpleMesh<VertexPositionColorTextureNormal, int> GetMesh(GraphicsDevice device)
		{
			if (mesh == null)
			{
				List<VertexPositionColorTextureNormal> vertices = new List<VertexPositionColorTextureNormal>();
				List<int> indices = new List<int>();

				ChunkMesher.MakeCubeVerts(Vector3.Zero, new Vector3(Cube.CUBE_SCALE), new CubeVisualInstance() { clearSides = MeshHelper.CubeFace.ALL, dirty = false }, this, vertices, indices);

				mesh = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("cubes_textures"));
			}

			return mesh;
		}

		public Color GetTintColor()
		{
			return tintColor;
		}

		public static SimpleMesh<VertexPositionColorTextureNormal, int> MakeCubeWithCorrectedTextureCoordinates(GraphicsDevice device, Color color, Texture2D texture)
		{
			List<VertexPositionColorTextureNormal> vertices = new List<VertexPositionColorTextureNormal>();
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

			return new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, texture);
		}

		private static void MakeQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color, List<VertexPositionColorTextureNormal> vertices, List<int> indices, Texture2D texture)
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

			vertices.Add(new VertexPositionColorTextureNormal(a, color, new Vector2(xmin, ymin), normal));
			vertices.Add(new VertexPositionColorTextureNormal(b, color, new Vector2(xmax, ymin), normal));
			vertices.Add(new VertexPositionColorTextureNormal(c, color, new Vector2(xmax, ymax), normal));
			vertices.Add(new VertexPositionColorTextureNormal(d, color, new Vector2(xmin, ymax), normal));
		}
	}
}
