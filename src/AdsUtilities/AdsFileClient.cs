using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TwinCAT.Ads;

namespace AdsUtilities;

public class AdsFileClient : IDisposable
{
    public string NetId { get { return _netId.ToString(); } }

    private readonly AdsClient _adsClient = new();

    private AmsNetId? _netId;

    private ILogger? _logger;


    public void ConfigureLogger(ILogger logger)
    {
        _logger = logger;
    }

    public AdsFileClient()
    {
        
    }

    public async Task<bool> Connect(string netId, CancellationToken cancel = default)
    {
        _netId = new AmsNetId(netId);
        _adsClient.Connect(_netId, AmsPort.SystemService);
        var readState = await _adsClient.ReadStateAsync(cancel);
        _adsClient.Disconnect();
        return readState.Succeeded;
    }

    public async Task<bool> Connect()
    {
        return await Connect(AmsNetId.Local.ToString());
    }

    private async Task<uint> FileOpenAsync(string path, uint openFlags, CancellationToken cancel = default)
    {
        byte[] wrBfr = Encoding.ASCII.GetBytes(path + '\0');
        
        byte[] rdBfr = new byte[sizeof(UInt32)];
    
        _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
        var rwResult = await _adsClient.ReadWriteAsync(
            Constants.AdsIGrpSysServFOpen,
            openFlags,
            rdBfr,
            wrBfr,
            cancel);
        _adsClient.Disconnect();

        if (rwResult.ErrorCode is AdsErrorCode.DeviceNotFound)
            _logger?.LogError("Could not open file '{filePath}' on {netId} because the file was not found.", path, NetId);

        rwResult.ThrowOnError();

        return BitConverter.ToUInt32(rdBfr);   // Return file handle
    }

    private async Task<uint> FileOpenReadingAsync(string path, bool binaryOpen = true, CancellationToken cancel = default)
    {
        uint tmpOpenMode = Constants.FOpenModeRead | 65536U;
        if (binaryOpen) tmpOpenMode |= Constants.FOpenModeBinary;
        return await FileOpenAsync(path, tmpOpenMode, cancel);
    }

    private async Task<uint> FileOpenWritingAsync(string path, bool binaryOpen = true, CancellationToken cancel = default)
    {
        uint tmpOpenMode = Constants.FOpenModeWrite | 65536U;
        if (binaryOpen) tmpOpenMode |= Constants.FOpenModeBinary;
        return await FileOpenAsync(path, tmpOpenMode, cancel);
    }

    private async Task FileCloseAsync(uint hFile, CancellationToken cancel = default)
    {
        _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
        await _adsClient.ReadWriteAsync(
            Constants.AdsIGrpSysServFClose, 
            hFile, 
            Array.Empty<byte>(), 
            Array.Empty<byte>(), 
            cancel);
        _adsClient.Disconnect();
    }

    private async Task<FileInfoByteMapped> GetFileInfoBytesAsync(string fileName, CancellationToken cancel = default)
    {
        uint hFile = await FileOpenReadingAsync(fileName, false, cancel);
        byte[] wrBfr = Encoding.UTF8.GetBytes(fileName);
        byte[] rdBfr = new byte[Marshal.SizeOf(typeof(FileInfoByteMapped))];      // Allocate memory buffer the size of the byte stream returned by a file info request  

        _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
        await _adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFFind, hFile, rdBfr, wrBfr, cancel);
        _adsClient.Disconnect();

        await FileCloseAsync(hFile, cancel);

        return StructConverter.MarshalToStructure<FileInfoByteMapped>(rdBfr);
    }

    public async Task<byte[]> FileReadFullAsync(string path, bool binaryOpen = true, CancellationToken cancel = default)
    {
        long fileSize = (await GetFileInfoAsync(path, cancel)).fileSize;
        byte[] rdBfr = new byte[fileSize];   // Read file size and allocate memory for the whole file --> use with caution

        uint hFile = await FileOpenReadingAsync(path, binaryOpen, cancel);

        _adsClient.Connect((int)Constants.AdsPortSystemService);
        await _adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFRead, hFile, rdBfr, new byte[4], cancel);
        _adsClient.Disconnect();

        await FileCloseAsync(hFile, cancel);

        return rdBfr.ToArray();
    }

    public async Task FileCopyAsync(string pathSource, AdsFileClient destinationFileClient, string pathDestination, bool binaryOpen = true, IProgress<double>? progress = null, uint chunkSizeKB = 100, CancellationToken cancel = default)
    {
        var fileInfo = await GetFileInfoAsync(pathSource, cancel);
        long fileSize = fileInfo.fileSize;
        uint chunkSizeBytes = chunkSizeKB * 1000;
        long bytesCopied = 0;

        uint hFileRead = await FileOpenReadingAsync(pathSource, binaryOpen, cancel);
        uint hFileWrite = await destinationFileClient.FileOpenWritingAsync(pathDestination, binaryOpen, cancel);
        
        while (true)
        {
            cancel.ThrowIfCancellationRequested();

            byte[] fileContentBuffer = await FileReadChunkAsync(hFileRead, chunkSizeBytes, cancel);
            await destinationFileClient.FileWriteChunkAsync(hFileWrite, fileContentBuffer, cancel);

            if (progress is not null)
            {
                bytesCopied += fileContentBuffer.Length;
                double progressPercentage = 100 * (double)bytesCopied / fileSize;
                progress.Report(progressPercentage);
            }

            if (fileContentBuffer.Length == chunkSizeBytes)  // not finished reading content from file
                continue;
            break;
        }
        await FileCloseAsync(hFileRead, cancel);
        await destinationFileClient.FileCloseAsync(hFileWrite, cancel);
        
        
    }

    public async Task FileCopyAsync(string pathSource, string pathDestination, bool binaryOpen = true, IProgress<double>? progress = null, uint chunkSizeBytes = 10_000, CancellationToken cancel = default) 
        => await FileCopyAsync(pathSource, this, pathDestination, binaryOpen, progress, chunkSizeBytes, cancel);

    private async Task FileWriteChunkAsync(uint hFile, byte[] chunk, CancellationToken cancel = default)
    {
        _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
        await _adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFWrite, hFile, new byte[4], chunk, cancel);
        _adsClient.Disconnect();
    }

    private async Task<byte[]> FileReadChunkAsync(uint hFile, uint chunkSize, CancellationToken cancel = default)
    {
        byte[] rdBfr = new byte[chunkSize];

        _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
        var readWriteResult = await _adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFRead, hFile, rdBfr, new byte[4], cancel);
        _adsClient.Disconnect();

        if(readWriteResult.ReadBytes < chunkSize)
            return rdBfr.Take(readWriteResult.ReadBytes).ToArray();
        return rdBfr.ToArray();
    }

    public async Task FileWriteFullAsync(string path, byte[] data, bool binaryOpen = true, CancellationToken cancel = default)
    {
        uint hFile = await FileOpenWritingAsync(path, binaryOpen, cancel);

        _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
        await _adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFWrite, hFile, new byte[4], data, cancel);
        _adsClient.Disconnect();

        await FileCloseAsync(hFile, cancel);
    }

    public async Task DeleteFileAsync(string path, CancellationToken cancel = default)
    {
        _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
        await _adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFDelete, Constants.PathGeneric << 16, Array.Empty<byte>(), Encoding.UTF8.GetBytes(path), cancel);
        _adsClient.Disconnect();
    }

    public async Task RenameFileAsync(string filePathCurrent, string filePathNew, CancellationToken cancel = default)
    {
        WriteRequestHelper renameRequest = new WriteRequestHelper()
            .AddStringUTF8(filePathCurrent)
            .AddStringUTF8(filePathNew);

        _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
        await _adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFRename, 1 << 16, Array.Empty<byte>(), renameRequest.GetBytes(), cancel);
        _adsClient.Disconnect();
    }

    public async Task<FileInfoDetails> GetFileInfoAsync(string path, CancellationToken cancel = default)
    {
        FileInfoByteMapped fileEntry = await GetFileInfoBytesAsync(path, cancel);
        return (FileInfoDetails)fileEntry;
    }

    public async Task CreateDirectoryAsync(string path, CancellationToken cancel = default)
    {
        _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
        await _adsClient.ReadWriteAsync(Constants.AdsIGrpSysServMkDir, Constants.PathGeneric, Array.Empty<byte>(), Encoding.UTF8.GetBytes(path), cancel);
        _adsClient.Disconnect();
    }

    public async Task DeleteDirectoryAsync(string path, CancellationToken cancel = default)
    {
        _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
        await _adsClient.ReadWriteAsync(Constants.AdsIGrpSysServRmDir, Constants.PathGeneric, Array.Empty<byte>(), Encoding.UTF8.GetBytes(path), cancel);
        _adsClient.Disconnect();
    }

    public async IAsyncEnumerable<FileInfoDetails> GetFolderContentStreamAsync(string path, [EnumeratorCancellation] CancellationToken cancel = default)
    {
        if (path.EndsWith("/") || path.EndsWith("\\"))
            path += "*";    // Add wild-card character 
        else if (!path.EndsWith("*"))
            path += "/*";    // Add wild-card character

        uint idxOffs = Constants.PathGeneric;   // for first file

        byte[] nextFileBuffer = new WriteRequestHelper().AddStringUTF8(path).GetBytes();               // for first file

        byte[] fileInfoBuffer = new byte[Marshal.SizeOf(typeof(FileInfoByteMapped))];      // Allocate memory buffer the size of the byte stream returned by a file info request

        _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);

        while (true)
        {
            cancel.ThrowIfCancellationRequested();

            Array.Clear(fileInfoBuffer, 0, fileInfoBuffer.Length);    // Clear buffer for next file

            var rwResult = await _adsClient.ReadWriteAsync(
                Constants.AdsIGrpSysServFFind,
                idxOffs,
                fileInfoBuffer,
                nextFileBuffer,
                cancel
            );

            if (rwResult.ErrorCode == AdsErrorCode.DeviceNotFound)
                yield break;    // Reached end of folder content
            else if (rwResult.ErrorCode != AdsErrorCode.NoError)
            {
                _logger?.LogError("Unexpected exception '{eMessage}' while getting folder content of '{path}' from '{netId}'. Results may be incomplete.", rwResult.ErrorCode.ToString(), path, _netId);
                yield break;
            }

            FileInfoByteMapped latestFile = StructConverter.MarshalToStructure<FileInfoByteMapped>(fileInfoBuffer);
            FileInfoDetails latestFileDetails = (FileInfoDetails)latestFile;
            idxOffs = latestFile.hFile;
            nextFileBuffer = Array.Empty<byte>();

            if (latestFileDetails.fileName is "." or "..")  // ignore current dir and parent dir
                continue;

            yield return latestFileDetails;
        }
    }

    public async Task<List<FileInfoDetails>> GetFolderContentListAsync(string path, CancellationToken cancel = default)
    {
        List<FileInfoDetails> folderContent = new();
        await foreach (var item in GetFolderContentStreamAsync(path, cancel))
        {
            folderContent.Add(item);
        }
        return folderContent;
    }

    public IEnumerable<FileInfoDetails> GetFolderContentStream(string path)
    {
        IAsyncEnumerator<FileInfoDetails> enumerator = GetFolderContentStreamAsync(path).GetAsyncEnumerator();
        while (true)
        {
            var moveNextTask = Task.Run(() => enumerator.MoveNextAsync().AsTask());
            if (!moveNextTask.Result)
                yield break;
            yield return enumerator.Current;
        }
    }

    public async Task StartProcessAsync(string applicationPath, string workingDirectory, string commandLineParameters, CancellationToken cancel = default)
    {
        WriteRequestHelper startProcessRequest = new WriteRequestHelper()
            .AddInt(applicationPath.Length)
            .AddInt(workingDirectory.Length)
            .AddInt(commandLineParameters.Length)
            .AddStringAscii(applicationPath)
            .AddStringAscii(workingDirectory)
            .AddStringAscii(commandLineParameters);

        _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
        var res = await _adsClient.WriteAsync(Constants.AdsIGrpSysServStartProcess, 0, startProcessRequest.GetBytes(), cancel);
        _adsClient.Disconnect();
        res.ThrowOnError();   
    }

    public void StartProcess(string applicationPath, string workingDirectory, string commandLineParameters)
    {
        StartProcessAsync(applicationPath, workingDirectory, commandLineParameters).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (_adsClient.IsConnected)
            _adsClient.Disconnect();
        if (!_adsClient.IsDisposed)
        {
            _adsClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}