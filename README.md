<br />
<div align="center">

  <h2 align="center">TwinCAT-ADS-Utilities</h2>

  <p align="center">
    An unofficial toolkit for handling aspects of the daily work with Beckhoff PLCs. Providing access to remote file systems, I/O devices, TwinCAT-route-management, and more – all based on the Beckhoff ADS protocol.
  </p>
</div>



<!-- ABOUT THE PROJECT -->
## About The Project

The Automation Device Specification (ADS) protocol is a crucial component for any PLC running TwinCAT. It serves as a communication layer for many of TwinCAT's basic functionalities outside the real-time environment. This project provides a collection of ADS client wrappers that allow you to access these functions via an easy-to-use .NET class library. *TwinCAT-ADS-Utilities* uses the .Net API provided by [Beckhoff.TwinCAT.Ads](https://www.nuget.org/packages/Beckhoff.TwinCAT.Ads/). Comprehensive documentation for the ADS API is available on [infosys.beckhoff.com](https://infosys.beckhoff.com/).

### Features
* __Remote File System Access:__ Interact with the file system and registry of your PLCs remotely.
* __ADS Route Management:__ Configure and manage ADS routes to and between PLCs.
* __I/O Configuration:__ Read the I/O configuration of remote systems without the need for TwinCAT XAE.
* __System Parameters & Diagnostics:__ Monitor system parameters, utilization, and access diagnostic functions of your PLCs.


### Prerequisites

* Beckhoff TwinCAT-System-Service installed (comes with any TwinCAT installation)
* .NET 6.0 SDK

### Installation

1. Clone the repo
   ```sh
   git clone https://github.com/MathisJu/TwinCAT-ADS-Utilities.git
   ```
2. Open the .csproj from /src and build a dynamic class library



<!-- USAGE EXAMPLES -->
## Examples

#### Copy a file to a remote system
```csharp
using AdsFileClient sourceFileClient = new(AmsNetId.Local);
using AdsFileClient destinationFileClient = new("192.168.1.100.1.1");
await sourceFileClient.FileCopyAsync(@"C:/temp/existingFile.txt", destinationFileClient, @"C:/temp/copiedFile.txt")
```

#### Perform a broadcast search
```csharp
// Option 1:
using AdsRoutingClient localRouting = new(AmsNetId.Local);
List<TargetInfo> devicesFound = await localRouting.AdsBroadcastSearchAsync(secondsTimeout: 5);
foreach (TargetInfo device in devicesFound)
    Console.WriteLine(device.Name);

// Option 2:
using AdsRoutingClient localRouting = new(AmsNetId.Local);
await foreach (TargetInfo device in localRouting.AdsBroadcastSearchAsyncStream(secondsTimeout: 5))
    Console.WriteLine(device.Name);
 ```

#### Add an ADS route to a remote system
```csharp
using AdsRoutingClient localRouting = new(AmsNetId.Local);
await localRouting.AddRouteAsync("192.168.1.100.1.1", "192.168.1.100", "IPC-Office", "Admin", passwordSecStr);
```