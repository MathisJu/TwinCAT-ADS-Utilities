using AdsUtilities;

AdsFileClient fileClient = new();

fileClient.ConnectLocal();

var fileList = await fileClient.GetFolderContentListAsync("C:");
foreach(var file in fileList)
{
    Console.WriteLine(file.fileName);
}