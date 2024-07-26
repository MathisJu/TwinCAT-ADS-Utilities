using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TwinCAT.Ads;

namespace AdsUtilities
{
    public class AdsFileClient : IDisposable
    {
        public string NetId { get { return _netId.ToString(); } }

        private readonly AdsClient adsClient = new();

        private readonly AmsNetId _netId;

        private ILogger? _logger;


        public void ConfigureLogger(ILogger logger)
        {
            _logger = logger;
        }

        public AdsFileClient(string netId)
        {
            _netId = AmsNetId.Parse(netId);
        }

        public AdsFileClient(AmsNetId netId)
        {
            _netId = netId;
        }

        private async Task<uint> FileOpenAsync(string path, uint openFlags, CancellationToken cancel = default)
        {
            byte[] wrBfr = Encoding.UTF8.GetBytes(path);
            byte[] rdBfr = new byte[4];

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFOpen, openFlags, rdBfr, wrBfr, cancel);
            adsClient.Disconnect();

            return BitConverter.ToUInt32(rdBfr);   // Return file handle
        }

        private async Task<uint> FileOpenReadingAsync(string path, bool binaryOpen = true, CancellationToken cancel = default)
        {
            uint tmpOpenMode = Constants.FOpenModeRead;
            if (binaryOpen) tmpOpenMode += Constants.FOpenModeBinary;
            return await FileOpenAsync(path, tmpOpenMode, cancel);
        }

        private async Task<uint> FileOpenWritingAsync(string path, bool binaryOpen = true, CancellationToken cancel = default)
        {
            uint tmpOpenMode = Constants.FOpenModeWrite;
            if (binaryOpen) tmpOpenMode += Constants.FOpenModeBinary;
            return await FileOpenAsync(path, tmpOpenMode, cancel);
        }

        private async Task FileCloseAsync(uint hFile, CancellationToken cancel = default)
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFClose, hFile, Array.Empty<byte>(), Array.Empty<byte>(), cancel);
            adsClient.Disconnect();
        }

        private async Task<Structs.FileInfoByteMapped> GetFileInfoBytesAsync(string fileName, CancellationToken cancel = default)
        {
            uint hFile = await FileOpenReadingAsync(fileName, false, cancel);
            byte[] wrBfr = Encoding.UTF8.GetBytes(fileName);
            byte[] rdBfr = new byte[Marshal.SizeOf(typeof(Structs.FileInfoByteMapped))];      // Allocate memory buffer the size of the byte stream returned by a file info request  

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFFind, hFile, rdBfr, wrBfr, cancel);
            adsClient.Disconnect();

            await FileCloseAsync(hFile, cancel);

            return Structs.Converter.MarshalToStructure<Structs.FileInfoByteMapped>(rdBfr);
        }

        internal async Task<byte[]> FileReadFullAsync(string path, bool binaryOpen = true, CancellationToken cancel = default)
        {
            long fileSize = (await GetFileInfoAsync(path, cancel)).fileSize;
            byte[] rdBfr = new byte[fileSize];   // Read file size and allocate memory

            uint hFile = await FileOpenReadingAsync(path, binaryOpen, cancel);

            adsClient.Connect((int)Constants.AdsPortSystemService);
            await adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFRead, hFile, rdBfr, new byte[4], cancel);
            adsClient.Disconnect();

            await FileCloseAsync(hFile, cancel);

            return rdBfr.ToArray();
        }

        public async Task FileCopyAsync(string pathLocal, AdsFileClient destinationFileClient, string pathTarget, bool binaryOpen = true, uint chunkSize = 10000, CancellationToken cancel = default)
        {
            uint hFileRead = await FileOpenReadingAsync(pathLocal, binaryOpen, cancel);

            uint hFileWrite = await destinationFileClient.FileOpenWritingAsync(pathTarget, binaryOpen, cancel);
            
            while (true)
            {
                byte[] fileContentBuffer = await FileReadChunkAsync(hFileRead, chunkSize, binaryOpen, cancel);
                await destinationFileClient.FileWriteChunkAsync(hFileWrite, fileContentBuffer, binaryOpen, cancel);

                if (fileContentBuffer.Length == chunkSize)  // not finished reading content from file
                    continue;
                break;
            }
            await FileCloseAsync(hFileRead, cancel);
            await destinationFileClient.FileCloseAsync(hFileWrite, cancel);
        }

        private async Task FileWriteChunkAsync(uint hFile, byte[] chunk, bool binaryOpen = true, CancellationToken cancel = default)
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFWrite, hFile, new byte[4], chunk, cancel);
            adsClient.Disconnect();
        }

        internal async Task<byte[]> FileReadChunkAsync(uint hFile, uint chunkSize, bool binaryOpen = true, CancellationToken cancel = default)
        {
            byte[] rdBfr = new byte[chunkSize];

            adsClient.Connect((int)Constants.AdsPortSystemService);
            var readWriteResult = await adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFRead, hFile, rdBfr, new byte[4], cancel);
            adsClient.Disconnect();

            if(readWriteResult.ReadBytes < chunkSize)
                return rdBfr.Take(readWriteResult.ReadBytes).ToArray();
            return rdBfr.ToArray();
        }

        internal async Task FileWriteFullAsync(string path, byte[] data, bool binaryOpen = true, CancellationToken cancel = default)
        {
            uint hFile = await FileOpenWritingAsync(path, binaryOpen, cancel);

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFWrite, hFile, new byte[4], data, cancel);
            adsClient.Disconnect();

            await FileCloseAsync(hFile, cancel);
        }

        public async Task DeleteFileAsync(string path, CancellationToken cancel = default)
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFDelete, Constants.PathGeneric << 16, Array.Empty<byte>(), Encoding.UTF8.GetBytes(path), cancel);
            adsClient.Disconnect();
        }

        public async Task RenameFileAsync(string filePathCurrent, string filePathNew, CancellationToken cancel = default)
        {
            WriteRequestHelper renameRequest = new WriteRequestHelper()
                .AddStringUTF8(filePathCurrent)
                .AddStringUTF8(filePathNew);

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await adsClient.ReadWriteAsync(Constants.AdsIGrpSysServFRename, 1 << 16, Array.Empty<byte>(), renameRequest.GetBytes(), cancel);
            adsClient.Disconnect();
        }

        public async Task<Structs.FileInfoDetails> GetFileInfoAsync(string path, CancellationToken cancel = default)
        {
            Structs.FileInfoByteMapped fileEntry = await GetFileInfoBytesAsync(path, cancel);
            return (Structs.FileInfoDetails)fileEntry;
        }

        public async Task CreateDirectoryAsync(string path, CancellationToken cancel = default)
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await adsClient.ReadWriteAsync(Constants.AdsIGrpSysServMkDir, Constants.PathGeneric, Array.Empty<byte>(), Encoding.UTF8.GetBytes(path), cancel);
            adsClient.Disconnect();
        }

        public async Task DeleteDirectoryAsync(string path, CancellationToken cancel = default)
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await adsClient.ReadWriteAsync(Constants.AdsIGrpSysServRmDir, Constants.PathGeneric, Array.Empty<byte>(), Encoding.UTF8.GetBytes(path), cancel);
            adsClient.Disconnect();
        }

        public async IAsyncEnumerable<Structs.FileInfoDetails> GetFolderContentStreamAsync(string path, [EnumeratorCancellation] CancellationToken cancel = default)
        {
            if (path.EndsWith("/") || path.EndsWith("\\"))
                path += "*";    // Add wild-card character 
            else if (!path.EndsWith("*"))
                path += "/*";    // Add wild-card character

            uint idxOffs = Constants.PathGeneric;   // for first file

            byte[] nextFileBuffer = new WriteRequestHelper().AddStringUTF8(path).GetBytes();               // for first file

            byte[] fileInfoBuffer = new byte[Marshal.SizeOf(typeof(Structs.FileInfoByteMapped))];      // Allocate memory buffer the size of the byte stream returned by a file info request

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);

            while (true)
            {
                if (cancel.IsCancellationRequested)
                {
                    _logger?.LogWarning("Getting folder content of of '{path}' from '{netId}' was cancelled. Results may be incomplete.", path, _netId);
                    yield break;
                }

                Array.Clear(fileInfoBuffer, 0, fileInfoBuffer.Length);    // Clear buffer for next file

                AdsErrorCode rwError = await adsClient.TryReadWriteAsync(
                    Constants.AdsIGrpSysServFFind,
                    idxOffs,
                    fileInfoBuffer,
                    nextFileBuffer,
                    cancel
                );

                if (rwError == AdsErrorCode.DeviceNotFound)
                    yield break;    // Reached end of folder content
                else if (rwError != AdsErrorCode.NoError)
                {
                    _logger?.LogError("Unexpected exception '{eMessage}' while getting folder content of '{path}' from '{netId}'. Results may be incomplete.", rwError.ToString(), path, _netId);
                    yield break;
                }

                Structs.FileInfoByteMapped latestFile = Structs.Converter.MarshalToStructure<Structs.FileInfoByteMapped>(fileInfoBuffer);
                idxOffs = latestFile.hFile;
                nextFileBuffer = Array.Empty<byte>();
                yield return (Structs.FileInfoDetails)latestFile;
            }
        }

        public async Task<List<Structs.FileInfoDetails>> GetFolderContentListAsync(string path, CancellationToken cancel = default)
        {
            List<Structs.FileInfoDetails> folderContent = new();
            await foreach (var item in GetFolderContentStreamAsync(path, cancel))
            {
                folderContent.Add(item);
            }
            return folderContent;
        }

        public IEnumerable<Structs.FileInfoDetails> GetFolderContentStream(string path)
        {
            IAsyncEnumerator<Structs.FileInfoDetails> enumerator = GetFolderContentStreamAsync(path).GetAsyncEnumerator();
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

            adsClient.Connect(new AmsAddress(_netId, AmsPort.SystemService));
            var res = await adsClient.WriteAsync(Constants.AdsIGrpSysServStartProcess, 0, startProcessRequest.GetBytes(), cancel);
            adsClient.Disconnect();
            res.ThrowOnError();   
        }

        public void StartProcess(string applicationPath, string workingDirectory, string commandLineParameters)
        {
            StartProcessAsync(applicationPath, workingDirectory, commandLineParameters).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            if (adsClient.IsConnected)
                adsClient.Disconnect();
            if (!adsClient.IsDisposed)
            {
                adsClient.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}