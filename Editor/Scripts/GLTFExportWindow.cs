using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGLTF;
using UnityGLTF.Plugins;

namespace UnityGLTF
{
    public class GLTFExportWindow : EditorWindow
    {
        GLTFSettings gltfSettings;

        [MenuItem("Window/GLTF Scene Exporter")]
        public static void ShowWindow()
        {
            GetWindow<GLTFExportWindow>("GLTF Scene Exporter");
        }

        private void OnEnable()
        {
            gltfSettings = GLTFSettings.GetOrCreateSettings();
        }

        void OnGUI()
        {
            gltfSettings.SaveFolderPath = EditorGUILayout.TextField("Export path", "T:/export/gltfscenes");

            gltfSettings.OverwriteTextureSameName = GUILayout.Toggle(gltfSettings.OverwriteTextureSameName, "Overwrite textures with the same name");

            if (GUILayout.Button($"Preprocess all LODs"))
            {
                PreprocessLODs();
            }

            if (GUILayout.Button($"Preprocess & Export All"))
            {
                PreprocessLODs();
                ExportScenes(gltfSettings);
            }
        }

        void PreprocessLODs()
        {
            foreach (var lodGroup in FindObjectsOfType<LODGroup>())
            {
                Undo.RecordObject(lodGroup, $"{lodGroup} - force LOD0");
                lodGroup.ForceLOD(0);

                for (int i = 0; i < lodGroup.GetLODs().Length; i++)
                {
                    foreach (var rend in lodGroup.GetLODs()[i].renderers)
                    {
                        if (rend == null) continue;

                        bool toExport = i == 0 && rend.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

                        if (toExport) // only disable
                            continue;

                        if (rend.gameObject.activeSelf != toExport)
                            Undo.RecordObject(rend.gameObject, $"{rend.gameObject} - SetActive to {toExport}");

                        rend.gameObject.SetActive(toExport);
                    }
                }
            }
        }

        void ExportScenes(GLTFSettings settings)
        {
            settings.ExportDisabledGameObjects = false;
            settings.UseTextureFileTypeHeuristic = false;
            settings.RequireExtensions = true;
            settings.ExportPlugins = new List<GLTFExportPlugin>();
            settings.ExportPlugins.Add(ScriptableObject.CreateInstance(typeof(TarkovMaterialExport)) as TarkovMaterialExport);

            GLTFSceneExporter exporter = new GLTFSceneExporter(GetAllRootTransforms(), new ExportContext(settings));

            string sceneName = "scene";

            exporter.SaveGLTFandBin(settings.SaveFolderPath, sceneName);

            var ext = ".gltf";
            var resultFile = GLTFSceneExporter.GetFileName(settings.SaveFolderPath, sceneName, ext);
            Debug.Log("Exported to " + resultFile);
            EditorUtility.RevealInFinder(resultFile);
        }


        public static Transform[] GetAllRootTransforms()
        {
            List<GameObject> rootObjects = new List<GameObject>();

            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (scene.isLoaded)
                {
                    rootObjects.AddRange(scene.GetRootGameObjects());
                }
            }

            List<Transform> rootTransforms = new List<Transform>();
            foreach (var rootObject in rootObjects)
            {
                rootTransforms.Add(rootObject.transform);
            }

            return rootTransforms.ToArray();
        }
    }
}