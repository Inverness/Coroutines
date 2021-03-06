using System;
using System.Reflection;

namespace Coroutines.Serialization
{
    public static class NameUtility
    {
        /// <summary>
        /// Parse the original name inside angle brackets from a compiler-generated name.
        /// </summary>
        /// <param name="name">A compiler generated name.</param>
        /// <returns>The original name, or the name argument if it was not compiler-generated.</returns>
        public static string ParseOriginalName(string name)
        {
            return TryParseGeneratedName(name, out char _, out string _, out string original) ? original : name;
        }

        /// <summary>
        /// Parse the components of a compiler generated name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="typeChar"></param>
        /// <param name="suffix"></param>
        /// <param name="original"></param>
        /// <returns></returns>
        public static bool TryParseGeneratedName(
            string name,
            out char typeChar,
            out string suffix,
            out string original)
        {
            typeChar = default;
            suffix = null;
            original = null;

            int startBracketIndex = name.IndexOf('<');
            if (startBracketIndex == -1 || !(startBracketIndex == 0 || startBracketIndex == 3 && name.StartsWith("CS$")))
                return false;

            int endBracketIndex = name.IndexOf('>');
            if (endBracketIndex == -1 || endBracketIndex > name.Length - 3)
                return false;

            if (name[endBracketIndex + 2] != '_' || name[endBracketIndex + 3] != '_')
                return false;

            original = endBracketIndex == 1 ? null : name.Substring(1, endBracketIndex - 1);
            typeChar = name[endBracketIndex + 1];

            suffix = name.Substring(endBracketIndex + 4);
            return true;
        }

        /// <summary>
        /// Create an assembly qualified name with only the assembly's name and not any additional information.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetSimpleAssemblyQualifiedName(Type type)
        {
            return type.FullName + ", " + type.GetTypeInfo().Assembly.GetName().Name;
        }
    }
}