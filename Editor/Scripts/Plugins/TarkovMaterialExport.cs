using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Extensions;
using static UnityGLTF.GLTFSceneExporter;

namespace UnityGLTF.Plugins
{
	public class TarkovMaterialExport : GLTFExportPlugin
	{
		public override string DisplayName => "Bake to Mesh: TextMeshPro GameObjects";
		public override string Description => "Bakes 3D TextMeshPro objects (not UI/Canvas) into meshes and attempts to faithfully apply their shader settings to generate the font texture.";
		public override GLTFExportPluginContext CreateInstance(ExportContext context)
		{
			return new TarkovMaterialExportContext();
		}
	}
	
	public class TarkovMaterialExportContext : GLTFExportPluginContext
	{
		public override void AfterSceneExport(GLTFSceneExporter _, GLTFRoot __)
		{
			RenderTexture.active = null;
		}

		public override bool BeforeMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode)
        {
            if (material.shader.name.Contains("p0/Reflective/Bumped Specular SMap"))
            {
                GLTF.Math.Color diffuseFactor = KHR_materials_pbrSpecularGlossinessExtension.DIFFUSE_FACTOR_DEFAULT;
                TextureInfo diffuseTexture = KHR_materials_pbrSpecularGlossinessExtension.DIFFUSE_TEXTURE_DEFAULT;
                GLTF.Math.Vector3 specularFactor = KHR_materials_pbrSpecularGlossinessExtension.SPEC_FACTOR_DEFAULT;
                double glossinessFactor = KHR_materials_pbrSpecularGlossinessExtension.GLOSS_FACTOR_DEFAULT;
                TextureInfo specularGlossinessTexture = KHR_materials_pbrSpecularGlossinessExtension.SPECULAR_GLOSSINESS_TEXTURE_DEFAULT;


				diffuseFactor = material.GetColor("_Color").ToNumericsColorGamma();
                float floatDiffuse = material.GetVector("_DefVals").x;
                diffuseFactor.R *= floatDiffuse;
				diffuseFactor.G *= floatDiffuse;
				diffuseFactor.B *= floatDiffuse;


                Texture texAlbedoSpec = material.GetTexture("_MainTex");
                Texture texGlos = material.GetTexture("_SpecMap");
                if (texGlos == null)
                    texGlos = Texture2D.whiteTexture;
                Texture2D texSpecGlos = TextureConverter.ConvertAlbedoSpecGlosToSpecGloss(texAlbedoSpec, texGlos);
				specularGlossinessTexture = exporter.ExportTextureInfo(texSpecGlos, TextureMapType.BaseColor);
                exporter.ExportTextureTransform(specularGlossinessTexture, material, "_MainTex");


                // todo: add tint mask logic
                Material mat = new Material(Shader.Find("Hidden/SetAlpha"));
                mat.SetFloat("_Alpha", 1f);
                diffuseTexture = exporter.ExportTextureInfo(TextureConverter.Convert(texAlbedoSpec, mat), TextureMapType.BaseColor);
                exporter.ExportTextureTransform(diffuseTexture, material, "_MainTex");


                Color colorSpec = material.GetColor("_SpecColor");
                float floatSpec = material.GetFloat("_Glossness");
				floatSpec = Mathf.Clamp01(floatSpec);
                floatSpec *= material.GetVector("_SpecVals").x;
                specularFactor.X = colorSpec.r * floatSpec;
				specularFactor.Y = colorSpec.g * floatSpec;
				specularFactor.Z = colorSpec.b * floatSpec;


				float floatGlos = material.GetFloat("_Specularness");
                glossinessFactor = Mathf.Clamp01(floatGlos);


                exporter.DeclareExtensionUsage(KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME, true);
                if (materialNode.Extensions == null)
                    materialNode.Extensions = new Dictionary<string, IExtension>();
                materialNode.Extensions[KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME] = new KHR_materials_pbrSpecularGlossinessExtension(
                    diffuseFactor,
                    diffuseTexture,
                    specularFactor,
                    glossinessFactor,
                    specularGlossinessTexture
                );


                var normalTex = material.GetTexture("_BumpMap");
                if (normalTex && normalTex is Texture2D)
                {
                    materialNode.NormalTexture = exporter.ExportNormalTextureInfo(normalTex, TextureMapType.Normal, material);
                    exporter.ExportTextureTransform(materialNode.NormalTexture, material, "_BumpMap");
                }
                else
                {
                    Debug.LogWarning($"{material} has an invalid normal map");
                }

                return true;
			}

			return false;
		}
	}
}
