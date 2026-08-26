using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ShenshenPet.Core;

public sealed record PetPackInfo(
    string Id,
    string DisplayName,
    string DirectoryPath,
    string ManifestPath,
    string AtlasPath);

public static class PetPackImporter
{
    public const int MaximumEntries = 32;
    public const long MaximumUncompressedBytes = 30L * 1024 * 1024;

    public static string DefaultPacksRoot => Path.Combine(ShenshenDataPaths.DataRoot, "packs");

    public static PetPackInfo Import(string packagePath, string? packsRoot = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException("找不到 Pet Pack 文件。", packagePath);
        }

        var root = Path.GetFullPath(packsRoot ?? DefaultPacksRoot);
        Directory.CreateDirectory(root);
        var stagingDirectory = Path.Combine(root, $".import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingDirectory);

        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            var fileEntries = archive.Entries
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .ToList();
            if (fileEntries.Count is 0 or > MaximumEntries)
            {
                throw new InvalidDataException($"Pet Pack 文件数量必须在 1 到 {MaximumEntries} 之间。");
            }

            var totalLength = fileEntries.Sum(entry => entry.Length);
            if (totalLength > MaximumUncompressedBytes)
            {
                throw new InvalidDataException("Pet Pack 解压后超过 30 MiB 安全上限。");
            }

            foreach (var entry in fileEntries)
            {
                RejectSymbolicLink(entry);
                var destination = ResolveSafeChild(stagingDirectory, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                entry.ExtractToFile(destination, overwrite: false);
            }

            var manifestPath = Path.Combine(stagingDirectory, "pet.manifest.json");
            if (!File.Exists(manifestPath))
            {
                throw new InvalidDataException("Pet Pack 根目录缺少 pet.manifest.json。");
            }

            var manifest = PetManifest.Load(manifestPath);
            ValidatePackId(manifest.Id);
            var atlasPath = ResolveSafeChild(stagingDirectory, manifest.Atlas.Path);
            ValidateAtlas(atlasPath, manifest.Atlas);

            var destinationDirectory = Path.Combine(root, manifest.Id);
            var backupDirectory = Path.Combine(root, $".backup-{manifest.Id}-{Guid.NewGuid():N}");
            var hadExistingPack = Directory.Exists(destinationDirectory);
            if (hadExistingPack)
            {
                Directory.Move(destinationDirectory, backupDirectory);
            }

            try
            {
                Directory.Move(stagingDirectory, destinationDirectory);
                if (hadExistingPack)
                {
                    Directory.Delete(backupDirectory, recursive: true);
                }
            }
            catch
            {
                if (hadExistingPack && !Directory.Exists(destinationDirectory) && Directory.Exists(backupDirectory))
                {
                    Directory.Move(backupDirectory, destinationDirectory);
                }

                throw;
            }

            return new PetPackInfo(
                manifest.Id,
                manifest.DisplayName,
                destinationDirectory,
                Path.Combine(destinationDirectory, "pet.manifest.json"),
                ResolveSafeChild(destinationDirectory, manifest.Atlas.Path));
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }
        }
    }

    public static PetPackInfo? TryResolve(string? id, string? packsRoot = null)
    {
        if (string.IsNullOrWhiteSpace(id) || !PackIdPattern.IsMatch(id))
        {
            return null;
        }

        try
        {
            var root = Path.GetFullPath(packsRoot ?? DefaultPacksRoot);
            var directory = ResolveSafeChild(root, id);
            var manifestPath = Path.Combine(directory, "pet.manifest.json");
            if (!File.Exists(manifestPath))
            {
                return null;
            }

            var manifest = PetManifest.Load(manifestPath);
            if (!string.Equals(manifest.Id, id, StringComparison.Ordinal))
            {
                return null;
            }

            var atlasPath = ResolveSafeChild(directory, manifest.Atlas.Path);
            ValidateAtlas(atlasPath, manifest.Atlas);
            return new PetPackInfo(id, manifest.DisplayName, directory, manifestPath, atlasPath);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static void ValidatePackId(string id)
    {
        if (!PackIdPattern.IsMatch(id))
        {
            throw new InvalidDataException("Pet Pack id 只能包含 1-32 个小写字母、数字和连字符。");
        }
    }

    private static string ResolveSafeChild(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)
            || Path.IsPathFullyQualified(relativePath)
            || relativePath.Contains(':'))
        {
            throw new InvalidDataException("Pet Pack 包含不安全的绝对路径。");
        }

        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var segments = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            throw new InvalidDataException("Pet Pack 包含目录穿越路径。");
        }

        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Pet Pack 文件越过了安装目录。");
        }

        return fullPath;
    }

    private static void RejectSymbolicLink(ZipArchiveEntry entry)
    {
        const int UnixFileTypeMask = 0xF000;
        const int UnixSymbolicLink = 0xA000;
        var unixMode = (entry.ExternalAttributes >> 16) & UnixFileTypeMask;
        if (unixMode == UnixSymbolicLink)
        {
            throw new InvalidDataException("Pet Pack 不允许包含符号链接。");
        }
    }

    private static void ValidateAtlas(string atlasPath, AtlasDefinition atlas)
    {
        if (!File.Exists(atlasPath))
        {
            throw new InvalidDataException("Pet Pack 缺少 manifest 指定的精灵表。");
        }

        Span<byte> header = stackalloc byte[24];
        using (var stream = File.OpenRead(atlasPath))
        {
            stream.ReadExactly(header);
            ReadOnlySpan<byte> pngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
            if (!header[..8].SequenceEqual(pngSignature)
                || !header.Slice(12, 4).SequenceEqual("IHDR"u8))
            {
                throw new InvalidDataException("Pet Pack 精灵表不是有效 PNG。");
            }

            var width = BinaryPrimitives.ReadInt32BigEndian(header.Slice(16, 4));
            var height = BinaryPrimitives.ReadInt32BigEndian(header.Slice(20, 4));
            if (width != atlas.Width || height != atlas.Height)
            {
                throw new InvalidDataException("Pet Pack 精灵表尺寸与 manifest 不一致。");
            }

            stream.Position = 0;
            var actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!string.Equals(actualHash, atlas.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Pet Pack 精灵表 SHA-256 与 manifest 不一致。");
            }
        }
    }

    private static readonly Regex PackIdPattern = new(
        "^[a-z0-9](?:[a-z0-9-]{0,30}[a-z0-9])?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
}
