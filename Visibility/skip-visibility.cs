using System;
using System.Reflection;
using System.Reflection.Emit;

// This forces the AssemblyBuilder to have its backing MonoAssembly have the skipverification bit
// which bypasses access modifiers checks (essentially, it's what "Allow unsafe" would have done if it
// had worked properly in mcs
namespace Mono.CSharp
{
    internal static class SkipVisibilityExt
    {
        static readonly bool IsMono = Type.GetType("Mono.Runtime") != null;
        static readonly bool MonoAssemblyNameHasArch = new AssemblyName("Dummy, ProcessorArchitecture=MSIL").ProcessorArchitecture == ProcessorArchitecture.MSIL;

        static readonly FieldInfo DynAssField =
            typeof(AssemblyBuilder).GetField("dynamic_assembly", BindingFlags.Instance | BindingFlags.NonPublic);

        public static unsafe void MarkSkipVerification(this AssemblyBuilder asm)
        {
            if (!IsMono || DynAssField == null)
                return;

            var asmPtr = (UIntPtr) DynAssField.GetValue(asm);

            if (asmPtr == UIntPtr.Zero)
                return;

            bool isOldMono = Environment.Version.Major <= 3;

            var offs =
                // ref_count (4 + padding)
                IntPtr.Size +
                // basedir
                IntPtr.Size +

                // aname
                // name
                IntPtr.Size +
                // culture
                IntPtr.Size +
                // hash_value
                IntPtr.Size +
                // public_key
                IntPtr.Size +
                // public_key_token (17 + padding)
                20 +
                // hash_alg
                4 +
                // hash_len
                4 +
                // flags
                4 +

                // major, minor, build, revision[, arch] (10 framework / 20 core + padding)
                (
                    !MonoAssemblyNameHasArch
                        ? (
                            typeof(object).Assembly.GetName().Name == "System.Private.CoreLib" ? 16 : 8
                        )
                        : (
                            typeof(object).Assembly.GetName().Name == "System.Private.CoreLib"
                                ? (IntPtr.Size == 4 ? 20 : 24)
                                : (IntPtr.Size == 4 ? 12 : 16)
                        )
                ) +

                // image
                IntPtr.Size +
                // friend_assembly_names
                IntPtr.Size +
                // friend_assembly_names_inited
                1 +
                // in_gac
                1 +
                // dynamic
                1 +
                // corlib_internal
                1 +
                // ref_only
                4 +
                // These are only there on new mono
                (
                    isOldMono ? 0 :
                    // wrap_non_exception_throws
                    1 +
                    // wrap_non_exception_throws_inited
                    1 +
                    // jit_optimizer_disabled
                    1 +
                    // jit_optimizer_disabled_inited
                    1
                ) +
                // First byte of security manager flags (second byte has what we want)
                1;

            var skipVerificationPtr = (byte*) ((long) asmPtr + offs);
            // It is 2 bits and both needs to be 1 (it's both having the value true and that it was lazy initialised)
            *skipVerificationPtr = 3;
        }
    }
}