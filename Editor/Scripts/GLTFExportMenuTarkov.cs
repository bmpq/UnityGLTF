using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityGLTF.Plugins;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	public static class GLTFExportMenuTarkov
    {
        private const string MenuPrefix = "Assets/UnityGLTF/";
        private const string MenuPrefixGameObject = "GameObject/UnityGLTF/";

		private const string ExportGltf = "Export selected Tarkov object as glTF";

	    private static bool TryGetExportNameAndRootTransformsFromSelection(out string sceneName, out Transform[] rootTransforms, out Object[] rootResources)
	    {
		    if (Selection.transforms.Length > 1)
		    {
			    sceneName = SceneManager.GetActiveScene().name;
			    rootTransforms = Selection.transforms;
			    rootResources = null;
			    return true;
		    }
		    if (Selection.transforms.Length == 1)
		    {
			    sceneName = Selection.activeGameObject.name;
			    rootTransforms = Selection.transforms;
			    rootResources = null;
			    return true;
		    }
		    if (Selection.objects.Any() && Selection.objects.All(x => x is GameObject))
		    {
			    sceneName = Selection.objects.First().name;
			    rootTransforms = Selection.objects.Select(x => (x as GameObject).transform).ToArray();
			    rootResources = null;
			    return true;
		    }

		    if (Selection.objects.Any() && Selection.objects.All(x => x is Material))
		    {
			    sceneName = "Material Library";
			    rootTransforms = null;
			    rootResources = Selection.objects;
			    return true;
		    }

		    sceneName = null;
		    rootTransforms = null;
		    rootResources = null;
		    return false;
	    }

	    [MenuItem(MenuPrefix + ExportGltf, true)]
	    [MenuItem(MenuPrefixGameObject + ExportGltf, true)]
	    private static bool ExportSelectedValidate()
	    {
		    return TryGetExportNameAndRootTransformsFromSelection(out _, out _, out _);
	    }

	    [MenuItem(MenuPrefix + ExportGltf)]
	    [MenuItem(MenuPrefixGameObject + ExportGltf, false, 33)]
	    private static void ExportSelected(MenuCommand command)
		{
			// The exporter handles multi-selection. We don't want to call export multiple times here.
			if (Selection.gameObjects.Length > 1 && command.context != Selection.gameObjects[0])
				return;
			
			if (!TryGetExportNameAndRootTransformsFromSelection(out var sceneName, out var rootTransforms, out var rootResources))
			{
				Debug.LogError("Can't export: selection is empty");
				return;
			}

			Export(rootTransforms, rootResources, false, sceneName);
		}

		private static void Export(Transform[] transforms, Object[] resources, bool binary, string sceneName)
		{
			var settings = GLTFSettings.GetOrCreateSettings();

            settings.ExportDisabledGameObjects = false;
            settings.UseTextureFileTypeHeuristic = false;
            settings.RequireExtensions = true;
            settings.OverwriteTextureSameName = true;
            settings.ExportPlugins = new List<GLTFExportPlugin>
            {
                ScriptableObject.CreateInstance(typeof(TarkovMaterialExport)) as TarkovMaterialExport,
                ScriptableObject.CreateInstance(typeof(LightsPunctualExport)) as LightsPunctualExport
            };

            var exportOptions = new ExportContext(settings) {};
			var exporter = new GLTFSceneExporter(transforms, exportOptions);

			if (resources != null)
			{
				exportOptions.AfterSceneExport += (sceneExporter, _) =>
				{
					foreach (var resource in resources)
					{
						if (resource is Material material)
							sceneExporter.ExportMaterial(material);
						if (resource is Texture2D texture)
							sceneExporter.ExportTexture(texture, "unknown");
						if (resource is Mesh mesh)
							sceneExporter.ExportMesh(mesh);
					}
				};
			}

			var invokedByShortcut = Event.current?.type == EventType.KeyDown;
			var path = settings.SaveFolderPath;
			if (!invokedByShortcut || !Directory.Exists(path))
				path = EditorUtility.SaveFolderPanel("glTF Export Path", settings.SaveFolderPath, "");

			if (!string.IsNullOrEmpty(path))
			{
				var ext = binary ? ".glb" : ".gltf";
				var resultFile = GLTFSceneExporter.GetFileName(path, sceneName, ext);
				settings.SaveFolderPath = path;
				
				if (binary)
					exporter.SaveGLB(path, sceneName);
				else
					exporter.SaveGLTFandBin(path, sceneName);

				Debug.Log("Exported to " + resultFile);
				EditorUtility.RevealInFinder(resultFile);
			}
		}
	}
}
