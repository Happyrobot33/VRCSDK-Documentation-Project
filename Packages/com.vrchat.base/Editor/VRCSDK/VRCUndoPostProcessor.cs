using UnityEditor;
using UnityEngine;

namespace VRC.SDK3.Editor
{
    [InitializeOnLoad]
    internal static class VRCUndoPostProcessor
    {
        private const string ShaderField = "m_Shader";
        private const string FallbackTag = "VRCFallback";

        static VRCUndoPostProcessor()
        {
            Undo.postprocessModifications += HandleUndoPostProcess;
        }

        private static UndoPropertyModification[] HandleUndoPostProcess(UndoPropertyModification[] modifications)
        {
            foreach (UndoPropertyModification modification in modifications)
            {
                PropertyModification propertyMod = modification.currentValue;
                if (propertyMod.propertyPath == ShaderField)
                {
                    Shader oldShader = modification.previousValue.objectReference as Shader;
                    Shader newShader = propertyMod.objectReference as Shader;

                    // Is the shader changing?
                    if (oldShader != newShader)
                    {
                        Material mat = propertyMod.target as Material;
                        if (mat == null)
                        {
                            continue;
                        }

                        // Clear VRCFallback as the shader changes. This attempts to avoid override tags set by community
                        // tooling "bleeding over" into shaders they don't control when the user swaps over directly.
                        mat.SetOverrideTag(FallbackTag, string.Empty);
                    }
                }
            }

            return modifications;
        }
    }
}