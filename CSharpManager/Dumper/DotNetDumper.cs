using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using AsmResolver.PE.File;

namespace CSharpManager.Dumper;

public sealed unsafe class DotNetDumper(NativeProcess process)
{
    private static readonly ConcurrentDictionary<string, byte[]> DumpedFileCache = new(StringComparer.OrdinalIgnoreCase);

    public void DumpProcess(string directory) =>
        Parallel.ForEach(process.EnumeratePageInfos(), pageInfo => ProcessPageInfo(pageInfo, directory));

    private void ProcessPageInfo(PageInfo pageInfo, string directory)
    {
        if (!pageInfo.IsValidPage())
        {
            return;
        }

        // 0x40000000 bytes = 1 gigabytes
        var page = new byte[Math.Min((int)pageInfo.Size, 0x40000000)];

        if (!process.TryReadBytes(pageInfo.Address, page))
        {
            return;
        }

        for (var i = 0; i < page.Length - 0x200; i++)
        {
            fixed (byte* p = page)
            {
                if (!MaybePEImage(p + i, page.Length - i))
                {
                    continue;
                }
            }

            var address = (nuint)pageInfo.Address + (uint)i;
            var peImage = DumpDotNetModule(process, address, out var fileName);
            if (peImage is null || ExcludeAssemblyHelper.IsExcludedAssembly(peImage))
            {
                continue;
            }

            Log.Debug($"Found assembly '{fileName}' at {address.FormatHex()}");

            fileName = fileName.RemoveInvalidChars();
            if (IsSameFile(directory, fileName, peImage))
            {
                continue;
            }

            fileName = EnsureNoRepeatFileName(directory, fileName);
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllBytes(filePath, peImage);
        }
    }

    [HandleProcessCorruptedStateExceptions]
    private static bool MaybePEImage(byte* p, int size)
    {
        try
        {
            var pEnd = p + size;

            if (*(ushort*)p != 0x5A4D)
            {
                return false;
            }

            var ntHeadersOffset = *(ushort*)(p + 0x3C);
            p += ntHeadersOffset;
            if (p > pEnd - 4)
            {
                return false;
            }

            if (*(uint*)p != 0x00004550)
            {
                return false;
            }

            p += 0x04;
            // NT headers Signature

            if (p + 0x10 > pEnd - 2)
            {
                return false;
            }

            if (*(ushort*)(p + 0x10) == 0)
            {
                return false;
            }

            p += 0x14;
            // File header SizeOfOptionalHeader

            if (p > pEnd - 2)
            {
                return false;
            }

            if (*(ushort*)p != 0x010B && *(ushort*)p != 0x020B)
            {
                return false;
            }
            // Optional header Magic

            return true;
        }
        catch
        {
            return false;
        }
    }

    [HandleProcessCorruptedStateExceptions]
    private static byte[]? DumpDotNetModule(NativeProcess process, nuint address, out string fileName)
    {
        fileName = string.Empty;

        var data = PEImageDumper.Dump(process, address);
        if (data is null)
        {
            return null;
        }

        try
        {
            var peFile = PEFile.FromBytes(data);

            // 确保为有效PE文件
            if (peFile.OptionalHeader.DataDirectories[14].VirtualAddress == 0)
            {
                return null;
            }

            var module = ModuleDefinition.FromFile(peFile);
            if (module.Assembly is not null ? module.Assembly.Name!.Length == 0 : module.Name!.Length == 0)
            {
                return null;
            }

            fileName = module.Assembly is not null
                ? module.Assembly.Name + (module.HasNativeEntryPoint ? ".exe" : ".dll")
                : module.Name!;
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            fileName = address.FormatHex();
        }

        return data;
    }

    private static bool IsSameFile(string directory, string fileName, byte[] data)
    {
        var filePath = Path.Combine(directory, fileName);
        if (!File.Exists(filePath))
        {
            DumpedFileCache[fileName] = data;
            return false;
        }

        if (!DumpedFileCache.TryGetValue(fileName, out var originalData))
        {
            originalData = File.ReadAllBytes(filePath);
            DumpedFileCache[fileName] = originalData;
        }

        return data.Length == originalData.Length && data.SequenceEqual(originalData);
    }

    private static string EnsureNoRepeatFileName(string directory, string fileName)
    {
        var count = 1;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        while (File.Exists(Path.Combine(directory, fileName)))
        {
            count++;
            fileName = $"{fileNameWithoutExtension} ({count}){extension}";
        }

        return fileName;
    }
}