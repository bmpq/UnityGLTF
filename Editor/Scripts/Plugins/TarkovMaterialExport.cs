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
            if (material.shader.name.Contains("SMap") && material.shader.name.Contains("Reflective"))
            {
                bool TransparentCutoff = material.shader.name.Contains("Transparent Cutoff");
                
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
                if (TransparentCutoff)
                    texAlbedoSpec = material.GetTexture("_SpecMap"); // asinine thing, idk why this is
                Texture texGlos = material.GetTexture("_SpecMap");
                if (TransparentCutoff)
                    texGlos = material.GetTexture("_MainTex"); // asinine thing, idk why this is
                if (texGlos == null)
                    texGlos = Texture2D.whiteTexture; 
                if (texAlbedoSpec == null)
                    texAlbedoSpec = Texture2D.whiteTexture;
                Texture2D texSpecGlos = TextureConverter.ConvertAlbedoSpecGlosToSpecGloss(texAlbedoSpec, texGlos);
				specularGlossinessTexture = exporter.ExportTextureInfo(texSpecGlos, TextureMapType.Linear);
                exporter.ExportTextureTransform(specularGlossinessTexture, material, "_MainTex");


                if (TransparentCutoff)
                    materialNode.AlphaMode = AlphaMode.MASK;


                Material setAlpha = new Material(Shader.Find("Hidden/SetAlphaFromTexture"));
                setAlpha.SetTexture("_AlphaTex", texGlos);
                diffuseTexture = exporter.ExportTextureInfo(TextureConverter.Convert(texAlbedoSpec, setAlpha), TextureMapType.BaseColor);
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

                if (material.HasFloat("_HasTint") && material.GetFloat("_HasTint") > 0.5f)
                {
                    // export the tint mask as occlusion tex. just for the blender convenience.
                    materialNode.OcclusionTexture = new OcclusionTextureInfo();
                    materialNode.OcclusionTexture.Index = exporter.ExportTextureInfo(material.GetTexture("_TintMask"), TextureMapType.Linear).Index;
                    exporter.ExportTextureTransform(materialNode.OcclusionTexture, material, "_TintMask");
                }
                else if (material.shader.name.Contains("Emissive") && material.HasColor("_EmissiveColor"))
                {
                    materialNode.EmissiveTexture = exporter.ExportTextureInfo(material.GetTexture("_EmissionMap"), TextureMapType.BaseColor);
                    exporter.ExportTextureTransform(materialNode.EmissiveTexture, material, "_EmissionMap");

                    materialNode.EmissiveFactor = material.GetColor("_EmissiveColor").ToNumericsColorGamma();


                    KHR_materials_emissive_strength emissive = new KHR_materials_emissive_strength();
                    emissive.emissiveStrength = material.GetFloat("_EmissionPower") * material.GetFloat("_EmissionVisibility");

                    exporter.DeclareExtensionUsage(KHR_materials_emissive_strength_Factory.EXTENSION_NAME, true);
                    materialNode.Extensions[KHR_materials_emissive_strength_Factory.EXTENSION_NAME] = emissive;
                }

                return true;
			}
            else if (material.shader.name == "p0/Reflective/Bumped Specular")
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
                if (texAlbedoSpec == null)
                    texAlbedoSpec = Texture2D.whiteTexture;
                Texture texGlos = Texture2D.whiteTexture;
                Texture2D texSpecGlos = TextureConverter.ConvertAlbedoSpecGlosToSpecGloss(texAlbedoSpec, texGlos);
                specularGlossinessTexture = exporter.ExportTextureInfo(texSpecGlos, TextureMapType.Linear);
                exporter.ExportTextureTransform(specularGlossinessTexture, material, "_MainTex");


                Material mat = new Material(Shader.Find("Hidden/SetAlpha"));
                mat.SetFloat("_Alpha", 1f);
                diffuseTexture = exporter.ExportTextureInfo(TextureConverter.Convert(texAlbedoSpec, mat), TextureMapType.BaseColor);
                exporter.ExportTextureTransform(diffuseTexture, material, "_MainTex");


                Color colorSpec = material.GetColor("_SpecColor");
                float floatSpec = material.GetFloat("_SpecPower");
                floatSpec = Mathf.Clamp01(floatSpec);
                specularFactor.X = colorSpec.r * floatSpec;
                specularFactor.Y = colorSpec.g * floatSpec;
                specularFactor.Z = colorSpec.b * floatSpec;


                float floatGlos = material.GetFloat("_SpecPower") * material.GetFloat("_Shininess");
                glossinessFactor = floatGlos;


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
                    // exporter.ExportTextureTransform(materialNode.NormalTexture, material, "_BumpMap");
                    // the tex tiling isn't used in-game, but some materials have random values, so we omit exporting tex transform
                }

                return true;
            }
            else if (material.shader.name == "p0/Cutout/Bumped Diffuse")
            {
                material.EnableKeyword("_BUMPMAP");
            }
            else if (material.shader.name == "Global Fog/Transparent Reflective Specular")
            {
                var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1.0f };
                pbr.BaseColorTexture = exporter.ExportTextureInfo(material.mainTexture, TextureMapType.BaseColor);
                exporter.ExportTextureTransform(pbr.BaseColorTexture, material, "_MainTex");
                pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();
                pbr.BaseColorFactor.A = 1f;

                pbr.RoughnessFactor = 0f;
                pbr.MetallicFactor = 0f;

                KHR_materials_transmission transmission = new KHR_materials_transmission();
                transmission.transmissionFactor = 1f;

                exporter.DeclareExtensionUsage(KHR_materials_transmission_Factory.EXTENSION_NAME, true);
                if (materialNode.Extensions == null)
                    materialNode.Extensions = new Dictionary<string, IExtension>();
                materialNode.Extensions[KHR_materials_transmission_Factory.EXTENSION_NAME] = transmission;

                materialNode.PbrMetallicRoughness = pbr;
                materialNode.AlphaMode = AlphaMode.MASK;

                return true;
            }
            else if (material.shader.name == "Cloth/ClothShader")
            {
                var pbr = new PbrMetallicRoughness();

                pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();

                Material setAlpha = new Material(Shader.Find("Hidden/SetAlphaFromTexture"));
                if (material.HasTexture("_CutoutMask"))
                    setAlpha.SetTexture("_AlphaTex", material.GetTexture("_CutoutMask"));
                else
                    setAlpha.SetTexture("_AlphaTex", Texture2D.whiteTexture);
                Texture mainTex = TextureConverter.Convert(material.GetTexture("_MainTex"), setAlpha);
                pbr.BaseColorTexture = exporter.ExportTextureInfo(mainTex, TextureMapType.BaseColor);
                exporter.ExportTextureTransform(pbr.BaseColorTexture, material, "_MainTex");
                materialNode.AlphaMode = AlphaMode.MASK;

                Material channelMixer = new Material(Shader.Find("Hidden/ChannelMixer"));
                Texture2D texRoughness = TextureConverter.Invert(material.GetTexture("_GlossMap"));
                channelMixer.SetTexture("_TexFirst", texRoughness);
                channelMixer.SetTexture("_TexSecond", Texture2D.whiteTexture);
                channelMixer.SetFloat("_SourceR", (int)ChannelSource.TexSecond_Red);
                channelMixer.SetFloat("_SourceG", (int)ChannelSource.TexFirst_Red);
                channelMixer.SetFloat("_SourceB", (int)ChannelSource.TexSecond_Red);
                channelMixer.SetFloat("_SourceA", (int)ChannelSource.TexSecond_Red);
                Texture2D texMetallicRoughness = TextureConverter.Convert(material.GetTexture("_GlossMap"), channelMixer, "MR");

                pbr.MetallicRoughnessTexture = exporter.ExportTextureInfo(texMetallicRoughness, TextureMapType.Linear);
                exporter.ExportTextureTransform(pbr.MetallicRoughnessTexture, material, "_GlossMap");

                pbr.RoughnessFactor = 1f - material.GetFloat("_Glossiness");
                pbr.MetallicFactor = (material.GetFloat("_Metallic") + 1f) / 2f;

                var normalTex = material.GetTexture("_NormalMap1");
                if (normalTex && normalTex is Texture2D)
                {
                    materialNode.NormalTexture = exporter.ExportNormalTextureInfo(normalTex, TextureMapType.Normal, material);
                    exporter.ExportTextureTransform(materialNode.NormalTexture, material, "_NormalMap1");
                }

                materialNode.PbrMetallicRoughness = pbr;

                return true;
            }
            else if (material.shader.name == "Custom/Vert Paint Shader Solid")
            {
                // unexportable
                // the shader logic must be remade manually in blender

                // skip exporting completely
                return true;
            }

            return false;
		}
	}
}
