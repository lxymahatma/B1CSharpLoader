using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Text;
using CSharpModBase;
using dnlib.DotNet;
using dnlib.PE;
using NativeSharp;
using static CSharpManager.Dumper.ExcludeAssemblyHelper;

namespace CSharpManager.Dumper;

public sealed unsafe class Dumper(NativeProcess process)
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    public int DumpProcess(string directoryPath)
    {
        var count = 0;
        var originalFileCache = new ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        Parallel.ForEach(process.EnumeratePageInfos(), pageInfo =>
        {
            if (!IsValidPage(pageInfo))
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

                var imageLayout = i == 0 ? GetProbableImageLayout(page) : ImageLayout.File;
                var address = (nuint)pageInfo.Address + (uint)i;
                var peImage = DumpDotNetModule(process, address, imageLayout, out var fileName);
                if (peImage is null && i == 0)
                {
                    // 也许判断有误，尝试一下另一种格式。如果不是页面起始位置，必须是文件布局。
                    imageLayout = imageLayout == ImageLayout.File ? ImageLayout.Memory : ImageLayout.File;
                    peImage = DumpDotNetModule(process, address, ImageLayout.File, out fileName);
                }

                if (peImage is null || IsExcludedAssembly(peImage))
                {
                    continue;
                }

                Console.WriteLine($"Found assembly '{fileName}' at {address.FormatHex()} and image layout is {imageLayout}");

                fileName = EnsureValidFileName(fileName);
                if (IsSameFile(directoryPath, fileName, peImage, originalFileCache))
                {
                    continue;
                }

                fileName = EnsureNoRepeatFileName(directoryPath, fileName);
                var filePath = Path.Combine(directoryPath, fileName);
                File.WriteAllBytes(filePath, peImage);
                count++;
            }
        });
        return count;
    }

    private static bool IsValidPage(PageInfo pageInfo) =>
        pageInfo.Protection != 0 && (pageInfo.Protection & MemoryProtection.NoAccess) == 0 && (ulong)pageInfo.Size <= int.MaxValue;

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
    private static ImageLayout GetProbableImageLayout(byte[] firstPage)
    {
        try
        {
            var imageSize = PEImageDumper.GetImageSize(firstPage, ImageLayout.File);
            // 获取文件格式大小
            var imageLayout = imageSize >= (uint)firstPage.Length ? ImageLayout.Memory : ImageLayout.File;
            // 如果文件格式大小大于页面大小，说明在内存中是内存格式的，反之为文件格式
            // 这种判断不准确，如果文件文件大小小于最小页面大小，判断会出错
            Log.Debug(imageLayout.ToString());
            return imageLayout;
        }
        catch
        {
            return ImageLayout.Memory;
        }
    }

    [HandleProcessCorruptedStateExceptions]
    private static byte[]? DumpDotNetModule(NativeProcess process, nuint address, ImageLayout imageLayout, out string fileName)
    {
        fileName = string.Empty;
        try
        {
            var data = PEImageDumper.Dump(process, address, ref imageLayout);
            if (data is null)
            {
                return null;
            }

            data = PEImageDumper.ConvertImageLayout(data, imageLayout, ImageLayout.File);
            using var peImage = new PEImage(data, true);
            // 确保为有效PE文件
            if (peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14].VirtualAddress == 0)
            {
                return null;
            }

            try
            {
                using var moduleDef = ModuleDefMD.Load(peImage);
                // 再次验证是否为.NET程序集
                if (moduleDef is null)
                {
                    return null;
                }

                if (moduleDef.Assembly is not null ? moduleDef.Assembly.Name.Length == 0 : moduleDef.Name.Length == 0)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = moduleDef.Assembly is not null ? moduleDef.Assembly.Name + (moduleDef.EntryPoint is null ? ".dll" : ".exe") : moduleDef.Name;
                }
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
        catch
        {
            return null;
        }
    }

    private static string EnsureValidFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return string.Empty;
        }

        var newFileName = new StringBuilder(fileName.Length);
        foreach (var chr in fileName)
        {
            if (!InvalidFileNameChars.Contains(chr))
            {
                newFileName.Append(chr);
            }
        }

        return newFileName.ToString();
    }

    private static bool IsSameFile(string directoryPath, string fileName, byte[] data, ConcurrentDictionary<string, byte[]> originalFileCache)
    {
        var filePath = Path.Combine(directoryPath, fileName);
        if (!File.Exists(filePath))
        {
            originalFileCache[fileName] = data;
            return false;
        }

        if (!originalFileCache.TryGetValue(fileName, out var originalData))
        {
            originalData = File.ReadAllBytes(filePath);
            originalFileCache.TryAdd(fileName, originalData);
        }

        if (data.Length != originalData.Length)
        {
            return false;
        }

        for (var i = 0; i < data.Length; i++)
        {
            if (data[i] != originalData[i])
            {
                return false;
            }
        }

        return true;
    }

    private static string EnsureNoRepeatFileName(string directoryPath, string fileName)
    {
        var count = 1;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        while (File.Exists(Path.Combine(directoryPath, fileName)))
        {
            count++;
            fileName = $"{fileNameWithoutExtension} ({count}){extension}";
        }

        return fileName;
    }
}