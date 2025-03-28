using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGLTF;

namespace UnityGLTF
{
    public class GLTFExportWindow : EditorWindow
    {
        [MenuItem("Window/GLTF Scene Exporter")]
        public static void ShowWindow()
        {
            GetWindow<GLTFExportWindow>("GLTF Scene Exporter");
        }

        void OnGUI()
        {
            var settings = GLTFSettings.GetOrCreateSettings();

            settings.SaveFolderPath = EditorGUILayout.TextField("Export path", "T:/export/gltfscenes");

            settings.OverwriteTextureSameName = GUILayout.Toggle(true, "Overwrite textures with the same name");

            if (GUILayout.Button($"Preprocess all LODs"))
            {
                PreprocessLODs();
            }

            if (GUILayout.Button($"Preprocess all Materials"))
            {
                PreprocessMaterials();
            }

            if (GUILayout.Button($"Preprocess & Export All"))
            {
                PreprocessLODs();
                PreprocessMaterials();
                ExportScenes(settings);
            }
        }

        void PreprocessMaterials()
        {
            foreach (var rend in FindObjectsOfType<Renderer>())
            {
                if (!rend.enabled || !rend.gameObject.activeSelf)
                    continue;

                throw new System.NotImplementedException();
            }
        }

        void PreprocessLODs()
        {
            foreach (var lodGroup in FindObjectsOfType<LODGroup>())
            {
                for (int i = 0; i < lodGroup.GetLODs().Length; i++)
                {
                    foreach (var rend in lodGroup.GetLODs()[i].renderers)
                    {
                        if (rend == null) continue;

                        bool toExport = i == 0 && rend.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

                        if (rend.gameObject.activeSelf != toExport)
                            Undo.RecordObject(rend.gameObject, $"{rend.gameObject} - SetActive to {toExport}");

                        rend.gameObject.SetActive(toExport);
                    }
                }
            }
        }

        void ExportScenes(GLTFSettings settings)
        {
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