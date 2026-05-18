using System;

namespace IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem
{
    public static class DialogueSystemIntegrationAvailability
    {
        private static readonly string[] ProbedTypes =
        {
            "PixelCrushers.DialogueSystem.DialogueManager",
            "PixelCrushers.DialogueSystem.DialogueSystemController",
            "PixelCrushers.DialogueSystem.DialogueDatabase",
        };

        public static bool IsAvailable()
        {
            for (int i = 0; i < ProbedTypes.Length; i++)
            {
                if (TypeExists(ProbedTypes[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TypeExists(string fullTypeName)
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType(fullTypeName, false) != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}