using AsmResolver.PE.File;

namespace CSharpManager.Dumper;

internal static unsafe class PEFileDumper
{
    /// <summary>
    ///     Try to dump the PE file from the specified address.
    /// </summary>
    /// <param name="process"></param>
    /// <param name="address"></param>
    /// <param name="peFileBytes"></param>
    /// <returns></returns>
    internal static bool TryDump(NativeProcess process, nuint address, out byte[] peFileBytes)
    {
        peFileBytes = [];

        var pageInfos = process.EnumeratePageInfos((void*)address, (void*)address).ToArray();
        if (pageInfos.Length == 0)
        {
            return false;
        }

        // Check if the page is valid
        var firstPageInfo = pageInfos[0];
        if (!firstPageInfo.IsValidPage())
        {
            return false;
        }

        // If the address is the start of the page, then it's Memory Layout
        if (address == (nuint)firstPageInfo.Address)
        {
            return false;
        }

        var peFile = new byte[(int)((byte*)firstPageInfo.Address + (int)firstPageInfo.Size - (byte*)address)];
        process.ReadBytes((void*)address, peFile);

        // Get the size of the image
        var peFileSize = GetPEFileSize(peFile);
        if (peFileSize == 0)
        {
            return false;
        }

        peFileBytes = new byte[peFileSize];

        if (!process.TryReadBytes((void*)address, peFileBytes, 0, peFileSize))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Get the size of the PE file
    /// </summary>
    /// <param name="peFileBytes"></param>
    /// <returns></returns>
    private static uint GetPEFileSize(byte[] peFileBytes)
    {
        var peImage = PEFile.FromBytes(peFileBytes);
        return GetPEFileSize(peImage);
    }

    /// <summary>
    ///     Get the size of the PE file
    /// </summary>
    /// <param name="peFile"></param>
    /// <returns></returns>
    private static uint GetPEFileSize(PEFile peFile)
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