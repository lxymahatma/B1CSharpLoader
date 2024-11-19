using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using AsmResolver.PE.File;

namespace CSharpManager.Dumper;

internal sealed unsafe class DotNetDumper(NativeProcess process)
{
    private static readonly ConcurrentDictionary<string, byte[]> DumpedFileCache = new(StringComparer.OrdinalIgnoreCase);

    internal void DumpProcess(string directory) =>
        Parallel.ForEach(process.EnumeratePageInfos(), pageInfo => ProcessPageInfo(pageInfo, directory));

    private void ProcessPageInfo(PageInfo pageInfo, string directory)
    {
        if (!pageInfo.IsValidPage())
        {
            return;
        }

        // 0x40000000 bytes = 1 gigabytes
        var pageSize = Math.Min((int)pageInfo.Size, 0x40000000);
        var page = new byte[pageSize];

        if (!process.TryReadBytes(pageInfo.Address, page))
        {
            return;
        }

        fixed (byte* p = page)
        {
            for (var i = 0; i < pageSize - 0x200; i++)
            {
                if (!MaybePEImage(p + i, pageSize - i))
                {
                    continue;
                }

                var address = (nuint)pageInfo.Address + (uint)i;
                if (!TryDumpDotNetModule(process, address, out var peFileBytes, out var fileName)
                    || ExcludeAssemblyHelper.IsExcludedAssembly(peFileBytes))
                {
                    continue;
                }

                Log.Debug($"Found assembly '{fileName}' at {address.FormatHex()}");

                if (!TryGetValidFilePath(directory, fileName, peFileBytes, out var filePath))
                {
                    continue;
                }

                File.WriteAllBytes(filePath, peFileBytes);
            }
        }
    }

    [HandleProcessCorruptedStateExceptions]
    private static bool MaybePEImage(byte* p, int size)
    {
        try
        {
            var pEnd = p + size;

            // Check DOS Header "MZ"
            if (*(ushort*)p != 0x5A4D)
            {
                return false;
            }

            // Check NT Header Offset
            var ntHeadersOffset = *(ushort*)(p + 0x3C);

            // Check NT Headers Signature "PE\0\0"
            var ntHeaders = p + ntHeadersOffset;
            if (*(uint*)ntHeaders != 0x00004550)
            {
                return false;
            }

            var fileHeader = ntHeaders + 0x04; // Skip PE Signature

            // Check Optional Header SizeOfOptionalHeader
            var sizeOfOptionalHeader = *(ushort*)(fileHeader + 0x10);
            var optionalHeader = fileHeader + 0x14;
            if (sizeOfOptionalHeader == 0 || sizeOfOptionalHeader + optionalHeader > pEnd)
            {
                return false;
            }

            // Check Optional Header Magic
            if (optionalHeader + 2 > pEnd)
            {
                return false;
            }

            var magic = *(ushort*)optionalHeader;
            return magic is 0x010B or 0x020B; // 0x010B: PE32, 0x020B: PE32+
        }
        catch
        {
            return false;
        }
    }


    [HandleProcessCorruptedStateExceptions]
    private static bool TryDumpDotNetModule(NativeProcess process, nuint address, out byte[] peFileBytes, out string fileName)
    {
        fileName = string.Empty;

        if (!PEFileDumper.TryDump(process, address, out peFileBytes))
        {
            return false;
        }

        try
        {
            var peFile = PEFile.FromBytes(peFileBytes);

            // Ensure it's a valid PE file
            if (peFile.OptionalHeader.DataDirectories[14].VirtualAddress == 0)
            {
                return false;
            }

            var module = ModuleDefinition.FromFile(peFile);
            if (string.IsNullOrEmpty(module.Assembly?.Name) && string.IsNullOrEmpty(module.Name))
            {
                return false;
            }

            // Currently it should be always .dll but just in case
            var fileExtension = module.HasNativeEntryPoint ? ".exe" : ".dll";
            fileName = (module.Assembly?.Name ?? module.Name) + fileExtension;
        }
        catch
        {
            return false;
        }

        return true;
    }

    private static bool TryGetValidFilePath(string directory, string fileName, byte[] peFileBytes, out string filePath)
    {
        filePath = string.Empty;
        fileName.RemoveInvalidChars();

        // Check Already Dumped Files
        if (DumpedFileCache.TryGetValue(fileName, out var cachedBytes) && cachedBytes.SequenceEqual(peFileBytes))
        {
            return false;
        }

        // Check Existing Files
        if (File.Exists(Path.Combine(directory, fileName)))
        {
            cachedBytes = File.ReadAllBytes(Path.Combine(directory, fileName));
            if (cachedBytes.SequenceEqual(peFileBytes))
            {
                return false;
            }
        }

        // Add to Dumped File Cache
        DumpedFileCache[fileName] = peFileBytes;

        // Ensure No Repeat File Name
        fileName = GetUniqueFileName(directory, fileName);
        filePath = Path.Combine(directory, fileName);
        return true;
    }

    private static string GetUniqueFileName(string directory, string fileName)
    {
        var count = 1;
        var name = fileName[..^4]; // Remove .dll or .exe
        var extension = fileName[^4..];

        while (File.Exists(Path.Combine(directory, fileName)))
        {
            fileName = $"{name} ({count++}){extension}";
        }

        return fileName;
    }
}