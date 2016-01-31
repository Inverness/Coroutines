using System;
using System.Reflection;

namespace Coroutines.Serialization
{
    internal class NameUtility
    {
        public static string ParseOriginalName(string name)
        {
            char typeChar;
            string suffix;
            string original;
            return TryParseGeneratedName(name, out typeChar, out suffix, out original) ? original : name;
        }

        public static bool TryParseGeneratedName(
            string name,
            out char typeChar,
            out string suffix,
            out string original)
        {
            typeChar = default(char);
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

        public static string GetSimpleAssemblyQualifiedName(Type type)
        {
            return type.FullName + ", " + type.GetTypeInfo().Assembly.GetName().Name;
        }
    }
}