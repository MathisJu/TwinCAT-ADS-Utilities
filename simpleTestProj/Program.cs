using AdsUtilities;

AdsSystemClient adsSystemClient = new AdsSystemClient();
adsSystemClient.ConnectLocal();
var sysInfo = await adsSystemClient.GetSystemInfoAsync();
Console.WriteLine(sysInfo.ToString());
;