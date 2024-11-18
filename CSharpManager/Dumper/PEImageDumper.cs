using AsmResolver.PE.File;

namespace CSharpManager.Dumper;

internal static unsafe class PEImageDumper
{
    /// <summary>
    ///     直接从内存中复制模块，不执行格式转换操作
    /// </summary>
    /// <param name="process"></param>
    /// <param name="address"></param>
    /// <returns></returns>
    public static byte[]? Dump(NativeProcess process, nuint address)
    {
        var pageInfos = process.EnumeratePageInfos((void*)address, (void*)address).ToArray();
        if (pageInfos.Length == 0)
        {
            return null;
        }

        var firstPageInfo = pageInfos[0];
        // 判断内存页是否有效
        if (!firstPageInfo.IsValidPage())
        {
            return null;
        }

        // 如果在内存页头部，说明是内存格式
        if (address == (nuint)firstPageInfo.Address)
        {
            return null;
        }

        var peFile = new byte[(int)((byte*)firstPageInfo.Address + (int)firstPageInfo.Size - (byte*)address)];
        process.ReadBytes((void*)address, peFile);

        // 获取模块在内存中的大小
        var imageSize = GetImageSize(peFile);
        if (imageSize == 0)
        {
            return null;
        }

        var peImage = new byte[imageSize];

        if (!process.TryReadBytes((void*)address, peImage, 0, imageSize))
        {
            return null;
        }

        return peImage;
    }

    /// <summary>
    ///     获取模块大小
    /// </summary>
    /// <param name="peFile"></param>
    /// <returns></returns>
    public static uint GetImageSize(byte[] peFile)
    {
        var peImage = PEFile.FromBytes(peFile);
        return GetImageSize(peImage);
    }

    /// <summary>
    ///     获取模块大小
    /// </summary>
    /// <param name="peFile"></param>
    /// <returns></returns>
    public static uint GetImageSize(PEFile peFile)
    {
        var lastSectionHeader = peFile.Sections[^1];
        var alignment = peFile.OptionalHeader.FileAlignment;
        var imageSize = (uint)lastSectionHeader.Offset + lastSectionHeader.GetPhysicalSize();

        if (imageSize % alignment != 0)
        {
            imageSize = imageSize - imageSize % alignment + alignment;
        }

        return imageSize;
    }
}