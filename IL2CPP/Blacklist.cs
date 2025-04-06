using System.Collections.Generic;
using System.Reflection;

namespace Mono.CSharp.IL2CPP
{
    public static class Blacklist
    {
        public static HashSet<string> SignatureBlacklist = new();

        public static bool IsBlacklisted(TypeSpec declaringType, MemberInfo member)
        {
            if (declaringType.GetMetaInfo() is System.Type metaInfo && !string.IsNullOrEmpty(metaInfo.Namespace))
            {
                var sig = $"{metaInfo.Namespace}.{declaringType.Name}.{member.Name}";
                if (SignatureBlacklist.Contains(sig))
                    return true;
            }
            return false;
        }
    }
}