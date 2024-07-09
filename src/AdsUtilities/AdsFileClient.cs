using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using TwinCAT.Ads;

namespace AdsUtilities
{
    public class AdsFileAccess : IDisposable
    {
        public string NetId { get { return _netId.ToString(); } }

        private readonly AdsClient adsClient = new();

        private readonly AmsNetId _netId;

        private ILogger? _logger;


        public void ConfigureLogger(ILogger logger)
        {
            _logger = logger;
        }        

        public AdsFileAccess(string netId) 
        { 
            _netId = AmsNetId.Parse(netId);
        }

        public AdsFileAccess(AmsNetId netId)
        {
            _netId = netId;
        }

        /// <summary>
        /// Open file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="openFlags">Determines the mode in which the file is opened. Use FileOpenR or FileOpenW if in doubt</param>
        /// <returns>File handle</returns>
        private uint FileOpen(string path, uint openFlags)
        {
            byte[] wrBfr = Encoding.UTF8.GetBytes(path);
            byte[] rdBfr = new byte[4];

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.ReadWrite(Constants.SystemServiceFOpen, openFlags, rdBfr, wrBfr);
            adsClient.Disconnect();

            return BitConverter.ToUInt32(rdBfr);   // Return file handle
        }

        /// <summary>
        /// Open file in reading mode
        /// </summary>
        /// <param name="path"></param>
        /// <param name="binaryOpen"></param>
        /// <returns>File handle</returns>
        private uint FileOpenR(string path, bool binaryOpen = true)
        {
            uint tmpOpenMode = Constants.FOpenModeRead;
            if (binaryOpen) tmpOpenMode += Constants.FOpenModeBinary;
            return FileOpen(path, tmpOpenMode);
        }

        /// <summary>
        /// Open file in writing mode
        /// </summary>
        /// <param name="path"></param>
        /// <param name="binaryOpen"></param>
        /// <returns>File handle</returns>
        private uint FileOpenW(string path, bool binaryOpen = true)
        {
            uint tmpOpenMode = Constants.FOpenModeWrite;
            if (binaryOpen) tmpOpenMode += Constants.FOpenModeBinary;
            return FileOpen(path, tmpOpenMode);
        }

        private void FileClose(uint hFile)
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.ReadWrite(Constants.SystemServiceFClose, hFile, Array.Empty<byte>(), Array.Empty<byte>());
            adsClient.Disconnect();
        }

        private Structs.FileInfoByteMapped FileInfo(string fileName)
        {
            uint hFile = FileOpenR(fileName, false);
            byte[] wrBfr = Encoding.UTF8.GetBytes(fileName);
            byte[] rdBfr = new byte[Marshal.SizeOf(typeof(Structs.FileInfoByteMapped))];      // Allocate memory buffer the size of the byte stream returned by a file info request  

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.ReadWrite(Constants.SystemServiceFFind, hFile, rdBfr, wrBfr);
            adsClient.Disconnect();

            FileClose(hFile);

            return Structs.Converter.MarshalToStructure<Structs.FileInfoByteMapped>(rdBfr);
        }

        public byte[] FileRead(string path, bool binaryOpen = true)
        {
            long fileSize = GetFileInfo(path).fileSize;
            byte[] rdBfr = new byte[fileSize];   // Read file size and allocate memory

            uint hFile = FileOpenR(path, binaryOpen);

            adsClient.Connect((int)Constants.AdsPortSystemService);
            adsClient.ReadWrite(Constants.SystemServiceFRead, hFile, rdBfr, new byte[4]);
            adsClient.Disconnect();
            
            FileClose(hFile);

            return rdBfr.ToArray();
        }

        public void FileWrite(string path, byte[] data, bool binaryOpen = true)
        {
            uint hFile = FileOpenW(path, binaryOpen);

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.ReadWrite(Constants.SystemServiceFWrite, hFile, new byte[4], data);
            adsClient.Disconnect();

            FileClose(hFile);
        }

        public void DeleteFile(string path)
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.ReadWrite(Constants.SystemServiceFDelete, Constants.PathGeneric << 16, Array.Empty<byte>(), Encoding.UTF8.GetBytes(path));
            adsClient.Disconnect();
        }

        /// <summary>
        /// This function tries to read the content of a file as a string and determines if the result is valid
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileContent"></param>
        /// <returns>returns true if the interpreted file content is assumed to be a valid string</returns>
        public bool TryReadFileAsText(string path, out string fileContent)
        {
            byte[] data = FileRead(path, false);            
            try
            {
                string dataAsString = Encoding.UTF8.GetString(data);
                bool eof = false;
                foreach (char c in dataAsString)
                {
                    if (c == '\0')
                        eof = true;
                    if (eof && c != '\0')
                    {
                        fileContent = dataAsString;
                        return false;   // content continued after e o f character - content probably is not text
                    }
                    if (char.IsControl(c) && c != '\0' && c != '\r' && c != '\n' && c != '\t')
                    {
                        fileContent = dataAsString;
                        return false;   // found control char that is not a white space
                    }
                }
                fileContent = dataAsString;              
                return true;    // all characters appear valid - content probably is text
            }
            catch (DecoderFallbackException)
            {
                fileContent = string.Empty;
                return false;   // exception during decoding - content probably is not text
            }
        }

        public void RenameFile(string filePathCurrent, string filePathNew)
        {
            IWriteRequest renameRequest = RequestFactory.CreateWriteRequest(filePathCurrent.Length + filePathNew.Length + 2);
            renameRequest.AddStringUTF8(filePathCurrent);
            renameRequest.AddStringUTF8(filePathNew);

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.ReadWrite(Constants.SystemServiceFRename, 1 << 16, Array.Empty<byte>(), renameRequest.GetBytes());
            adsClient.Disconnect();
        }
      
        public Structs.FileInfoDetails GetFileInfo(string path)
        {
            Structs.FileInfoByteMapped fileEntry = FileInfo(path);
            return (Structs.FileInfoDetails)fileEntry;
        }

        public void CreateDirectory(string path)
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.ReadWrite(Constants.SystemServiceMkDir, Constants.PathGeneric, Array.Empty<byte>(), Encoding.UTF8.GetBytes(path));
            adsClient.Disconnect();
        }

        public void DeleteDirectory(string path)
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.ReadWrite(Constants.SystemServiceRmDir, Constants.PathGeneric, Array.Empty<byte>(), Encoding.UTF8.GetBytes(path));
            adsClient.Disconnect();
        }

        public List<Structs.FileInfoDetails> GetFolderContent(string path)
        {
            if (path.EndsWith("/") || path.EndsWith("\\"))
            {
                path += "*";    // Add wild-card character 
            }
            else if (!path.EndsWith("*"))
            {
                path += "/*";    // Add wild-card character
            }

            const int EndOfEnumerationError = 0x70C;   // Exiting the content enumeration loop is error driven. This is by design of the system service and no bad practice on my part

            List<Structs.FileInfoDetails> folderContent = new();

            uint idxOffs = Constants.PathGeneric;   // for first file

            IWriteRequest fileInfoRequest = RequestFactory.CreateWriteRequest(path.Length + 1);
            fileInfoRequest.AddStringUTF8(path);    // for first file
            byte[] wrBfr = fileInfoRequest.GetBytes();

            byte[] rdBfr = new byte[Marshal.SizeOf(typeof(Structs.FileInfoByteMapped))];      // Allocate memory buffer the size of the byte stream returned by a file info request

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);

            bool reachedEndOfContent = false;
            while (!reachedEndOfContent)
            {
                Array.Clear(rdBfr, 0, rdBfr.Length);    // Clear buffer for next file
                AdsErrorCode rwError = adsClient.TryReadWrite(Constants.SystemServiceFFind, idxOffs, rdBfr, wrBfr, out _);
                if (rwError == AdsErrorCode.NoError)
                {
                    Structs.FileInfoByteMapped latestFile = Structs.Converter.MarshalToStructure<Structs.FileInfoByteMapped>(rdBfr);
                    
                    idxOffs = latestFile.hFile;
                    wrBfr = Array.Empty<byte>();
                    folderContent.Add((Structs.FileInfoDetails)latestFile);
                }
                else if (((int)rwError) == EndOfEnumerationError)
                {
                    reachedEndOfContent = true;
                }
                else
                {
                    _logger?.LogError("Unexpected exception '{eMessage}' while getting folder content of '{path}' from '{netId}'. Returning empty file list.", rwError.ToString(), path, _netId);
                    break;
                }
            }

            adsClient.Disconnect();

            return folderContent;
        }

        // ToDo: Add a getNextFileInFolder method to make this easier to understand

        public void StartProcess(string applicationPath, string workingDirectory, string commandLineParameters)
        {
            IWriteRequest startProcessRequest = RequestFactory.CreateWriteRequest(15 + applicationPath.Length + workingDirectory.Length + commandLineParameters.Length); // 15 = 3* sizeof(uint) + 3
            startProcessRequest.AddInt(applicationPath.Length);
            startProcessRequest.AddInt(workingDirectory.Length);
            startProcessRequest.AddInt(commandLineParameters.Length);
            startProcessRequest.AddStringAscii(applicationPath);
            startProcessRequest.AddStringAscii(workingDirectory);
            startProcessRequest.AddStringAscii(commandLineParameters);

            adsClient.Connect((int)Constants.AdsPortSystemService);
            adsClient.Write(Constants.SystemServiceStartProcess, 0, startProcessRequest);
            adsClient.Disconnect();
        }

        public async Task StartProcessAsync(string applicationPath, string workingDirectory, string commandLineParameters, CancellationToken cancel)
        {
            IWriteRequest startProcessRequest = RequestFactory.CreateWriteRequest(15 + applicationPath.Length + workingDirectory.Length + commandLineParameters.Length); // 15 = 3* sizeof(uint) + 3
            startProcessRequest.AddInt(applicationPath.Length);
            startProcessRequest.AddInt(workingDirectory.Length);
            startProcessRequest.AddInt(commandLineParameters.Length);
            startProcessRequest.AddStringAscii(applicationPath);
            startProcessRequest.AddStringAscii(workingDirectory);
            startProcessRequest.AddStringAscii(commandLineParameters);

            adsClient.Connect(new AmsAddress(_netId, AmsPort.SystemService));
            var res = await adsClient.WriteAsync(Constants.SystemServiceStartProcess, 0, startProcessRequest.GetBytes(), cancel);
            adsClient.Disconnect();
            res.ThrowOnError();
            
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