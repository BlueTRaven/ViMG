using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
	public class CubeEffect
	{
		private Effect effect;

		private EffectParameter textureParam;
		private EffectParameter worldViewProjectionParam;
		private EffectParameter worldParam;
		private EffectParameter worldInverseTransposeParam;
		private EffectParameter eyePositionParam;

		private EffectParameter diffuseColorParam;
		private EffectParameter emissiveColorParam;

		private EffectParameter fogColorParam;
		private EffectParameter fogVectorParam;

		private EffectParameter specularColorParam;
		private EffectParameter specularPowerParam;

		public Texture2D Texture;

		private Matrix projection = Matrix.Identity;
		public Matrix Projection
		{
			get
			{
				return projection;
			}
			set
			{
				projection = value;
				Matrix worldView = world * view;
				SetWorldViewProjection(ref worldView);
			}
		}
		private Matrix view = Matrix.Identity;
		public Matrix View
		{
			get
			{
				return view;
			}
			set
			{
				view = value;
				Matrix worldView = world * view;
				SetWorldViewProjection(ref worldView);
			}
		}
		private Matrix world = Matrix.Identity;
		public Matrix World
		{
			get
			{
				return world;
			}
			set
			{
				world = value;
				Matrix worldView = world * view;
				SetWorldViewProjection(ref worldView);
			}
		}

		public Vector3 DiffuseColor = Vector3.One;
		public Vector3 EmissiveColor = Vector3.Zero;
		public Vector3 AmbientLightColor = Vector3.Zero;

		public Vector3 SpecularColor = Vector3.One;
		public float SpecularPower = 16;

		public float Alpha = 1;

		public float FogStart = 0.01f;
		public float FogEnd = 10000;
		public Vector3 fogColor = Vector3.One;

		private Microsoft.Xna.Framework.Graphics.DirectionalLight light0;
		private Microsoft.Xna.Framework.Graphics.DirectionalLight light1;
		private Microsoft.Xna.Framework.Graphics.DirectionalLight light2;

		public CubeEffect(GraphicsDevice graphicsDevice)
		{
			
		}

		public void CacheValues()
		{
			effect = Main.assetsManager.GetAsset<Effect>("CubeEffect");

			textureParam = effect.Parameters["Texture"];

			worldViewProjectionParam = effect.Parameters["WorldViewProj"];
			worldParam = effect.Parameters["World"];
			worldInverseTransposeParam = effect.Parameters["WorldInverseTranspose"];
			eyePositionParam = effect.Parameters["EyePosition"];

			fogColorParam = effect.Parameters["FogColor"];
			fogVectorParam = effect.Parameters["FogVector"];

			diffuseColorParam = effect.Parameters["DiffuseColor"];
			emissiveColorParam = effect.Parameters["EmissiveColor"];

			specularColorParam = effect.Parameters["SpecularColor"];
			specularPowerParam = effect.Parameters["SpecularPower"];

			light0 = new Microsoft.Xna.Framework.Graphics.DirectionalLight(effect.Parameters["DirLight0Direction"],
				effect.Parameters["DirLight0DiffuseColor"],
				effect.Parameters["DirLight0SpecularColor"], null);
			light1 = new Microsoft.Xna.Framework.Graphics.DirectionalLight(effect.Parameters["DirLight1Direction"],
				effect.Parameters["DirLight1DiffuseColor"],
				effect.Parameters["DirLight1SpecularColor"], null);
			light2 = new Microsoft.Xna.Framework.Graphics.DirectionalLight(effect.Parameters["DirLight2Direction"],
				effect.Parameters["DirLight2DiffuseColor"],
				effect.Parameters["DirLight2SpecularColor"], null);

			light0.Direction = new Vector3(-0.5265408f, -0.5735765f, -0.6275069f);
			light0.DiffuseColor = new Vector3(1, 0.9607844f, 0.8078432f);
			light0.SpecularColor = new Vector3(1, 0.9607844f, 0.8078432f);
			light0.Enabled = true;

			// Fill light.
			light1.Direction = new Vector3(0.7198464f, 0.3420201f, 0.6040227f);
			light1.DiffuseColor = new Vector3(0.9647059f, 0.7607844f, 0.4078432f);
			light1.SpecularColor = Vector3.Zero;
			light1.Enabled = true;

			// Back light.
			light2.Direction = new Vector3(0.4545195f, -0.7660444f, 0.4545195f);
			light2.DiffuseColor = new Vector3(0.3231373f, 0.3607844f, 0.3937255f);
			light2.SpecularColor = new Vector3(0.3231373f, 0.3607844f, 0.3937255f);
			light2.Enabled = true;

			AmbientLightColor = new Vector3(0.05333332f, 0.09882354f, 0.1819608f);
		}

		public void Apply()
		{
			textureParam.SetValue(Texture);

			specularColorParam.SetValue(SpecularColor);
			specularPowerParam.SetValue(SpecularPower);

			Matrix.Multiply(ref world, ref view, out Matrix worldView);

			SetWorldViewProjection(ref worldView);
			SetFogVector(ref worldView);
			fogColorParam.SetValue(fogColor);
			SetMaterialColor();
			SetLightingMatrices();
		}

		private void SetWorldViewProjection(ref Matrix worldView)
		{
			Matrix.Multiply(ref worldView, ref projection, out Matrix worldViewProj);

			worldParam.SetValue(world);
			worldViewProjectionParam.SetValue(worldViewProj);
		}

		private void SetFogVector(ref Matrix worldView)
		{
			// We want to transform vertex positions into view space, take the resulting
			// Z value, then scale and offset according to the fog start/end distances.
			// Because we only care about the Z component, the shader can do all this
			// with a single dot product, using only the Z row of the world+view matrix.

			float scale = 1f / (FogStart - FogEnd);

			Vector4 fogVector = new Vector4();

			fogVector.X = worldView.M13 * scale;
			fogVector.Y = worldView.M23 * scale;
			fogVector.Z = worldView.M33 * scale;
			fogVector.W = (worldView.M43 + FogStart) * scale;

			fogVectorParam.SetValue(fogVector);
		}

		private void SetMaterialColor()
		{
			// Desired lighting model:
			//
			//     ((AmbientLightColor + sum(diffuse directional light)) * DiffuseColor) + EmissiveColor
			//
			// When lighting is disabled, ambient and directional lights are ignored, leaving:
			//
			//     DiffuseColor + EmissiveColor
			//
			// For the lighting disabled case, we can save one shader instruction by precomputing
			// diffuse+emissive on the CPU, after which the shader can use DiffuseColor directly,
			// ignoring its emissive parameter.
			//
			// When lighting is enabled, we can merge the ambient and emissive settings. If we
			// set our emissive parameter to emissive+(ambient*diffuse), the shader no longer
			// needs to bother adding the ambient contribution, simplifying its computation to:
			//
			//     (sum(diffuse directional light) * DiffuseColor) + EmissiveColor
			//
			// For futher optimization goodness, we merge material alpha with the diffuse
			// color parameter, and premultiply all color values by this alpha.
			
			Vector4 diffuse = new Vector4();
			Vector3 emissive = new Vector3();

			diffuse.X = DiffuseColor.X * Alpha;
			diffuse.Y = DiffuseColor.Y * Alpha;
			diffuse.Z = DiffuseColor.Z * Alpha;
			diffuse.W = Alpha;

			emissive.X = (EmissiveColor.X + AmbientLightColor.X * DiffuseColor.X) * Alpha;
			emissive.Y = (EmissiveColor.Y + AmbientLightColor.Y * DiffuseColor.Y) * Alpha;
			emissive.Z = (EmissiveColor.Z + AmbientLightColor.Z * DiffuseColor.Z) * Alpha;

			diffuseColorParam.SetValue(diffuse);
			emissiveColorParam.SetValue(emissive);
		}

		private void SetLightingMatrices()
		{
			Matrix.Invert(ref world, out Matrix worldTranspose);
			Matrix.Transpose(ref worldTranspose, out Matrix worldInverseTranspose);

			worldParam.SetValue(world);
			worldInverseTransposeParam.SetValue(worldInverseTranspose);

			Matrix.Invert(ref view, out Matrix viewInverse);
			eyePositionParam.SetValue(viewInverse.Translation);
		}

		public Effect GetEffect()
		{
			return effect;
		}
	}
}
