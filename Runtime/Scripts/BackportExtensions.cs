using UnityEngine;

public static class BackportExtensions
{
    public static bool HasFloat(this Material mat, string parameter) => mat.HasProperty(parameter);
}
