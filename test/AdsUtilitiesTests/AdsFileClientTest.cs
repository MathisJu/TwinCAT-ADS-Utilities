namespace AdsUtilitiesTests;

public class AdsFileClientTests : IDisposable
{
    private readonly AdsFileClient _client;
    private readonly string _testDirectory;

    public AdsFileClientTests()
    {
        _client = new AdsFileClient(AmsNetId.Local);

        // Create a temporary directory for the tests
        _testDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        // Clean up the temporary directory and its contents
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task FileReadFullAsync_ShouldReturnByteArray_WhenFileExists()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "testfile.txt");
        var expectedData = new byte[] { 1, 2, 3 };
        await File.WriteAllBytesAsync(filePath, expectedData);

        // Act
        var result = await _client.FileReadFullAsync(filePath);

        // Assert
        Assert.Equal(expectedData, result);
    }

    [Fact]
    public async Task FileCopyAsync_ShouldCopyFile_WhenCalled()
    {
        // Arrange
        var sourcePath = Path.Combine(_testDirectory, "sourcefile.txt");
        var destinationPath = Path.Combine(_testDirectory, "destinationfile.txt");
        var data = new byte[] { 1, 2, 3 };
        await File.WriteAllBytesAsync(sourcePath, data);

        // Act
        await _client.FileCopyAsync(sourcePath, _client, destinationPath);

        // Assert
        Assert.True(File.Exists(destinationPath));
        Assert.Equal(data, await File.ReadAllBytesAsync(destinationPath));
    }

    [Fact]
    public async Task FileWriteFullAsync_ShouldWriteData_WhenCalled()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "writefile.txt");
        var data = new byte[] { 1, 2, 3 };

        // Act
        await _client.FileWriteFullAsync(filePath, data);

        // Assert
        Assert.True(File.Exists(filePath));
        Assert.Equal(data, await File.ReadAllBytesAsync(filePath));

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldDeleteFile_WhenCalled()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "deletefile.txt");
        var data = new byte[] { 1, 2, 3 };
        await File.WriteAllBytesAsync(filePath, data);

        // Act
        await _client.DeleteFileAsync(filePath);

        // Assert
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task RenameFileAsync_ShouldRenameFile_WhenCalled()
    {
        // Arrange
        var oldPath = Path.Combine(_testDirectory, "oldfile.txt");
        var newPath = Path.Combine(_testDirectory, "newfile.txt");
        var data = new byte[] { 1, 2, 3 };
        await File.WriteAllBytesAsync(oldPath, data);

        // Act
        await _client.RenameFileAsync(oldPath, newPath);

        // Assert
        Assert.False(File.Exists(oldPath));
        Assert.True(File.Exists(newPath));
    }

    [Fact]
    public async Task GetFileInfoAsync_ShouldReturnFileInfo_WhenFileExists()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "fileinfo.txt");
        var data = new byte[] { 1, 2, 3 };
        await File.WriteAllBytesAsync(filePath, data);

        // Act
        var result = await _client.GetFileInfoAsync(filePath);

        // Assert
        Assert.NotNull(result.fileName);
        Assert.Equal(new FileInfo(filePath).Length, result.fileSize);
    }

    [Fact]
    public async Task CreateDirectoryAsync_ShouldCreateDirectory_WhenCalled()
    {
        // Arrange
        var dirPath = Path.Combine(_testDirectory, "testdir");

        // Act
        await _client.CreateDirectoryAsync(dirPath);

        // Assert
        Assert.True(Directory.Exists(dirPath));

        // Cleanup
        Directory.Delete(dirPath, true);
    }

    [Fact]
    public async Task GetFolderContentStreamAsync_ShouldReturnFileInfoDetails_WhenDirectoryHasFiles()
    {
        // Arrange
        var dirPath = Path.Combine(_testDirectory, "folderContentTest");
        Directory.CreateDirectory(dirPath);
        var filePath = Path.Combine(dirPath, "file.txt");
        var data = new byte[] { 1, 2, 3 };
        await File.WriteAllBytesAsync(filePath, data);

        // Act
        var fileInfos = new List<AdsUtilities.Structs.FileInfoDetails>();
        await foreach (var info in _client.GetFolderContentStreamAsync(dirPath))
        {
            fileInfos.Add(info);
        }

        // Assert
        Assert.Single(fileInfos);
        Assert.Equal("file.txt", fileInfos.First().fileName);

        // Cleanup
        File.Delete(filePath);
        Directory.Delete(dirPath);
    }
}
