using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF
{
    public class BundleResources
    {
#if RUNTIME
        private static Dictionary<string, Shader> bundleShaders = new Dictionary<string, Shader>();
        public static void InjectBundleShaders(Shader[] shaders)
        {
            bundleShaders.Clear();
            foreach (var shader in shaders)
            {
                UnityEngine.Debug.Log($"Injected {shader} in UnityGLTF");
                bundleShaders.Add(shader.name, shader);
            }
        }
        public static Shader GetShader(string shaderName)
        {
            if (bundleShaders.ContainsKey(shaderName))
                return bundleShaders[shaderName];

            if (bundleShaders.ContainsKey("Hidden/" + shaderName))
                return bundleShaders["Hidden/" + shaderName];

            if (bundleShaders.ContainsKey("Hidden/Blit/" + shaderName))
                return bundleShaders["Hidden/Blit/" + shaderName];

            UnityEngine.Debug.LogError(shaderName + ": Was not found in BundleResources injected bundle! Did you forget to inject the bundle shaders with your mod??!");
            // fallback
            return Shader.Find(shaderName);
        }
#elif UNITY_EDITOR
		public static Shader GetShader(string shaderName)
        {
            return Resources.Load(shaderName, typeof(Shader)) as Shader;
        }
#endif
    }
}
