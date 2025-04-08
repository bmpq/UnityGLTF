using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGLTF;
using UnityGLTF.Plugins;
using System.Reflection;

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
            if (gltfSettings == null)
                gltfSettings = GLTFSettings.GetOrCreateSettings();

            gltfSettings.SaveFolderPath = EditorGUILayout.TextField("Export path", gltfSettings.SaveFolderPath);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);
            gltfSettings.UseWebp = EditorGUILayout.Toggle("WebP format", gltfSettings.UseWebp);
            if (gltfSettings.UseWebp)
                gltfSettings.DefaultJpegQuality = EditorGUILayout.IntSlider("Quality", gltfSettings.DefaultJpegQuality, 0, 100);

            gltfSettings.OverwriteTextureSameName = GUILayout.Toggle(gltfSettings.OverwriteTextureSameName, "Overwrite files with the same name");
            EditorGUILayout.EndVertical();



            if (GUILayout.Button($"Preprocess all GameObjects"))
            {
                PreprocessGameObjects();
            }

            if (GUILayout.Button($"Preprocess all LODs"))
            {
                PreprocessLODs();
            }

            if (GUILayout.Button($"Preprocess & Export All"))
            {
                PreprocessGameObjects();
                PreprocessLODs();
                ExportScenes(gltfSettings);
            }
        }

        void PreprocessGameObjects()
        {
            string[] typesToDisable = new string[] { "GripPose" };

            for (int i = 0; i < typesToDisable.Length; i++)
            {
                Type targetType = FindTypeByName(typesToDisable[i]);
                UnityEngine.Object[] foundComponents = UnityEngine.Object.FindObjectsOfType(targetType, false);
                foreach (UnityEngine.Object foundComponent in foundComponents)
                {
                    if (foundComponent is Component comp)
                        comp.gameObject.SetActive(false);
                }
            }
        }

        public static Type FindTypeByName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            Type type = Type.GetType(typeName);
            if (type != null) return type;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName, false);
                    if (type != null) return type;

                    // Try finding by simple name (more expensive)
                    if (!typeName.Contains('.'))
                    {
                        type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
                        if (type != null) return type;
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Debug.LogWarning($"Could not load types from assembly {assembly.FullName}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error searching assembly {assembly.FullName} for type '{typeName}': {ex.Message}");
                }
            }

            return null;
        }

        void PreprocessLODs()
        {
            Dictionary<GameObject, bool> origActiveState = new Dictionary<GameObject, bool>();

            LODGroup[] lodGroups = FindObjectsOfType<LODGroup>(true);

            foreach (var lodGroup in lodGroups)
            {
                lodGroup.enabled = true;
                for (int i = 0; i < lodGroup.GetLODs().Length; i++)
                {
                    foreach (var rend in lodGroup.GetLODs()[i].renderers)
                    {
                        if (rend == null) continue;
                        rend.enabled = true;

                        bool shadow = rend.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

                        bool toExport = i == 0 && !shadow;

                        if (toExport)
                            continue; // only disable

                        if (i > 0 && lodGroup.GetLODs()[0].renderers.Contains(rend))
                            continue; // don't disable if the mesh is also in LOD0

                        if (rend.gameObject.activeSelf != toExport)
                            Undo.RecordObject(rend.gameObject, $"{rend.gameObject} - SetActive to {toExport}");

                        rend.gameObject.SetActive(toExport);
                    }
                }

                Undo.RecordObject(lodGroup, $"{lodGroup} - disable");
                lodGroup.enabled = false;
            }
        }

        void ExportScenes(GLTFSettings settings)
        {
            settings.ExportDisabledGameObjects = false;
            settings.UseTextureFileTypeHeuristic = false;
            settings.RequireExtensions = true;
            settings.ExportPlugins = new List<GLTFExportPlugin>
            {
                ScriptableObject.CreateInstance(typeof(TarkovMaterialExport)) as TarkovMaterialExport,
                ScriptableObject.CreateInstance(typeof(LightsPunctualExport)) as LightsPunctualExport
            };

            GLTFSceneExporter exporter = new GLTFSceneExporter(GetAllRootTransforms(), new ExportContext(settings));

            string sceneName = SceneManager.GetActiveScene().name;

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