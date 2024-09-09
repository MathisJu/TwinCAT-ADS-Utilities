using AdsUtilities;

AdsSystemClient client = new ();
client.Connect("5.30.228.96.1.1");
await client.RebootAsync();
Console.WriteLine("Done");