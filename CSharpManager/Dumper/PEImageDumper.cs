using dnlib.PE;

namespace CSharpManager.Dumper;

internal static unsafe class PEImageDumper
{
    /// <summary>
    ///     直接从内存中复制模块，不执行格式转换操作
    /// </summary>
    /// <param name="process"></param>
    /// <param name="address"></param>
    /// <param name="imageLayout"></param>
    /// <returns></returns>
    public static byte[]? Dump(NativeProcess process, nuint address, ref ImageLayout imageLayout)
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

        // 如果不在内存页头部，只可能是文件布局
        if (address != (nuint)firstPageInfo.Address)
        {
            imageLayout = ImageLayout.File;
        }

        var peHeader = new byte[(int)((byte*)firstPageInfo.Address + (int)firstPageInfo.Size - (byte*)address)];
        process.ReadBytes((void*)address, peHeader);

        // 获取模块在内存中的大小
        var imageSize = GetImageSize(peHeader, imageLayout);
        var peImage = new byte[imageSize];

        // 转储
        switch (imageLayout)
        {
            case ImageLayout.File:
                if (!process.TryReadBytes((void*)address, peImage, 0, imageSize))
                {
                    return null;
                }

                break;

            case ImageLayout.Memory:
                pageInfos = process.EnumeratePageInfos((void*)address, (byte*)address + imageSize).Where(t => t.IsValidPage()).ToArray();
                if (pageInfos.Length == 0)
                {
                    return null;
                }

                foreach (var pageInfo in pageInfos)
                {
                    var offset = (ulong)pageInfo.Address - address;
                    if (!process.TryReadBytes(pageInfo.Address, peImage, (uint)offset, (uint)pageInfo.Size))
                    {
                        return null;
                    }
                }

                break;
            default:
                throw new NotSupportedException();
        }

        return peImage;
    }

    /// <summary>
    ///     转换模块布局
    /// </summary>
    /// <param name="peImage"></param>
    /// <param name="fromImageLayout"></param>
    /// <param name="toImageLayout"></param>
    /// <returns></returns>
    public static byte[] ConvertImageLayout(byte[] peImage, ImageLayout fromImageLayout, ImageLayout toImageLayout)
    {
        if (fromImageLayout == toImageLayout)
        {
            return peImage;
        }

        var newPEImageData = new byte[GetImageSize(peImage, toImageLayout)];
        using var peHeader = new PEImage(peImage, false);
        Buffer.BlockCopy(peImage, 0, newPEImageData, 0, (int)peHeader.ImageSectionHeaders[^1].EndOffset);
        // 复制PE头
        foreach (var sectionHeader in peHeader.ImageSectionHeaders)
        {
            switch (toImageLayout)
            {
                case ImageLayout.File:
                    // ImageLayout.Memory -> ImageLayout.File
                    Buffer.BlockCopy(peImage, (int)sectionHeader.VirtualAddress, newPEImageData, (int)sectionHeader.PointerToRawData,
                        (int)sectionHeader.SizeOfRawData);
                    break;
                case ImageLayout.Memory:
                    // ImageLayout.File -> ImageLayout.Memory
                    Buffer.BlockCopy(peImage, (int)sectionHeader.PointerToRawData, newPEImageData, (int)sectionHeader.VirtualAddress,
                        (int)sectionHeader.SizeOfRawData);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        return newPEImageData;
    }

    /// <summary>
    ///     获取模块大小
    /// </summary>
    /// <param name="peHeader"></param>
    /// <param name="imageLayout"></param>
    /// <returns></returns>
    public static uint GetImageSize(byte[] peHeader, ImageLayout imageLayout)
    {
        // PEImage构造器中的imageLayout参数无关紧要，因为只需要解析PEHeader
        using var peImage = new PEImage(peHeader, false);
        return GetImageSize(peImage, imageLayout);
    }

    /// <summary>
    ///     获取模块大小
    /// </summary>
    /// <param name="peHeader"></param>
    /// <param name="imageLayout"></param>
    /// <returns></returns>
    public static uint GetImageSize(PEImage peHeader, ImageLayout imageLayout)
    {
        var lastSectionHeader = peHeader.ImageSectionHeaders[^1];
        uint alignment;
        uint imageSize;
        switch (imageLayout)
        {
            case ImageLayout.File:
                alignment = peHeader.ImageNTHeaders.OptionalHeader.FileAlignment;
                imageSize = lastSectionHeader.PointerToRawData + lastSectionHeader.SizeOfRawData;
                break;
            case ImageLayout.Memory:
                alignment = peHeader.ImageNTHeaders.OptionalHeader.SectionAlignment;
                imageSize = (uint)lastSectionHeader.VirtualAddress + lastSectionHeader.VirtualSize;
                break;
            default:
                throw new NotSupportedException();
        }

        if (imageSize % alignment != 0)
        {
            imageSize = imageSize - imageSize % alignment + alignment;
        }

        return imageSize;
    }
}