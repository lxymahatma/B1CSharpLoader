using dnlib.DotNet;

namespace CSharpManager.Dumper;

internal static class ExcludeAssemblyHelper
{
    private static HashSet<string> ExcludedAssemblyFullNames { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accessibility, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        // "b1.Managed, Version=0.0.0.0, Culture=neutral, PublicKeyToken=",
        // "b1.Native, Version=0.0.0.0, Culture=neutral, PublicKeyToken=",
        // "b1.NativePlugins, Version=0.0.0.0, Culture=neutral, PublicKeyToken=",
        // "B1UI_GSE.Script, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        // "BtlSvr.Main, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        "CSharpManager, Version=0.0.6.0, Culture=neutral, PublicKeyToken=",
        "CSharpModBase, Version=0.0.6.0, Culture=neutral, PublicKeyToken=",
        // "Diana.Client.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        // "Diana.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        // "Diana.Server.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        "dnlib, Version=4.4.0.0, Culture=neutral, PublicKeyToken=50e96378b6e77999",
        // "Google.Protobuf, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        // "GSE.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        // "GSE.GSNet, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        // "GSE.GSSdk, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        // "GSE.OnlineBase, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        // "GSE.ProtobufDB, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        // "GUR.Runtime, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        // "ICSharpCode.SharpZipLib, Version=1.2.0.246, Culture=neutral, PublicKeyToken=1b03e6acf1164f73",
        // "ILRuntime, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        // "ILRuntime.Mono.Cecil, Version=0.11.3.0, Culture=neutral, PublicKeyToken=",
        // "ILRuntime.Mono.Cecil.Mdb, Version=0.11.3.0, Culture=neutral, PublicKeyToken=",
        // "ILRuntime.Mono.Cecil.Pdb, Version=0.11.3.0, Culture=neutral, PublicKeyToken=",
        // "LiteNetLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        // "LitJson, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        // "Microsoft.Bcl.AsyncInterfaces, Version=1.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
        "Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        // "Mono.Data.Sqlite, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756",
        // "Mono.Posix, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756",
        // "Mono.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756",
        // "Mono.WebBrowser, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756",
        "netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
        "NativeSharp, Version=3.0.0.1, Culture=neutral, PublicKeyToken=",
        // "Novell.Directory.Ldap, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756",
        // "Protobuf.RunTime, Version=1.0.0.0, Culture=neutral, PublicKeyToken=",
        // "Sentry, Version=3.41.3.0, Culture=neutral, PublicKeyToken=fba2ec45388e2af0",
        "SharpDX.XInput, Version=4.2.0.0, Culture=neutral, PublicKeyToken=b4dcf0f35e5521f1",
        "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        // "System.Buffers, Version=4.0.3.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
        // "System.Collections.Immutable, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        "System.ComponentModel.Composition, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        "System.ComponentModel.DataAnnotations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
        "System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        "System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        "System.Data.DataSetExtensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        "System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        "System.DirectoryServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        "System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        "System.EnterpriseServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        "System.IO.Compression, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        "System.IO.Compression.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        // "System.IO.FileSystem, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        // "System.Memory, Version=4.0.1.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
        "System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        "System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        // "System.Numerics.Vectors, Version=4.1.4.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        // "System.Reflection.Metadata, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        // "System.Runtime.CompilerServices.Unsafe, Version=4.0.4.1, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        "System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        "System.Runtime.Serialization.Formatters.Soap, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        "System.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        // "System.ServiceModel.Internals, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        // "System.Text.Encodings.Web, Version=4.0.4.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
        // "System.Text.Json, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
        // "System.Threading.Tasks.Extensions, Version=4.2.0.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
        "System.Transactions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        "System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        "System.Web.ApplicationServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
        "System.Web.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        "System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        "System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        // "UnrealEngine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=",
        // "UnrealEngine.Runtime, Version=1.0.0.0, Culture=neutral, PublicKeyToken="
    };

    public static bool IsExcludedAssembly(byte[] data)
    {
        using var module = ModuleDefMD.Load(data);
        return IsExcludedAssembly(module.Assembly);
    }

    public static bool IsExcludedAssembly(IAssembly? assembly)
    {
        if (assembly is null)
        {
            return false;
        }

        var fullName = $"{assembly.Name}, Version={assembly.Version}, Culture=neutral, PublicKeyToken={assembly.PublicKeyOrToken.Token}";
        return ExcludedAssemblyFullNames.Contains(fullName);
    }
}