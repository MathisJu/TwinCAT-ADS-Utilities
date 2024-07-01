<br />
<div align="center">

  <h2 align="center">TwinCAT-ADS-Utilities</h2>

  <p align="center">
    An unofficial toolkit for handling aspects of the daily work with Beckhoff PLCs. Providing access to remote file systems, I/O devices, TwinCAT-route-management, and more – all based on the Beckhoff ADS protocol.
  </p>
</div>



<!-- ABOUT THE PROJECT -->
## About The Project

The Automation Device Specification (ADS) protocol is a crucial component for any PLC running TwinCAT. It serves as a communication layer for many of TwinCAT's basic functionalities outside the real-time environment. This project provides a collection of ADS client functions that allow you to access these functions via an easy-to-use .NET class library. *TwinCAT-ADS-Utilities* uses the .Net API provided by [Beckhoff.TwinCAT.Ads](https://www.nuget.org/packages/Beckhoff.TwinCAT.Ads/). Comprehensive documentation for the ADS API is available on [infosys.beckhoff.com](infosys.beckhoff.com).

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
AdsFileAccess localFileAccess = new(AmsNetId.Local);
byte[] fileContent = localFileAccess.FileRead("C:/temp/someFile.txt");
AdsFileAccess remoteFileAccess = new("192.168.8.188.1.1");
remoteFileAccess.FileWrite("C:/temp/someNewFile.txt", fileContent);
```

#### Perform a broadcast search
```csharp
AdsRoutingAccess localRouting = new(AmsNetId.Local);
List<TargetInfo> devicesFound = await localRouting.AdsBroadcastSearchAsync(secondsTimeout: 5);
foreach (TargetInfo device in devicesFound)
    Console.WriteLine(device.Name);
 ```

#### Add an ADS route to a remote system
```csharp
AdsRoutingAccess localRouting = new(AmsNetId.Local);
localRouting.AddRoute("192.168.1.100.1.1", "192.168.1.100", "CX5130-Office", "Administrator", "1");
```


<!-- CONTRIBUTING -->
## Contributing

Contributions are very much appreachiated. Please fork the repo and create a pull request or simply open an issue with the tag "enhancement".

If you would like to contribute by implementing of a useful ADS function, but don't know how, here is a quick guide:

1. Download the Beckhoff [TF60110 | TwinCAT 3 ADS Monitor](https://www.beckhoff.com/en-us/products/automation/twincat/tfxxxx-twincat-3-functions/tf6xxx-connectivity/tf6010.html) and start an ADS recording.

2. Perform the TwinCAT-action you would like to recreate.

3. Stop recording and search for the ADS frames involved in the action.

4. Implement your method that creates an ADS request according to the recorded pattern.



<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE.txt` for more information.



<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/othneildrew/Best-README-Template.svg?style=for-the-badge
[contributors-url]: https://github.com/othneildrew/Best-README-Template/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/othneildrew/Best-README-Template.svg?style=for-the-badge
[forks-url]: https://github.com/othneildrew/Best-README-Template/network/members
[stars-shield]: https://img.shields.io/github/stars/othneildrew/Best-README-Template.svg?style=for-the-badge
[stars-url]: https://github.com/othneildrew/Best-README-Template/stargazers
[issues-shield]: https://img.shields.io/github/issues/othneildrew/Best-README-Template.svg?style=for-the-badge
[issues-url]: https://github.com/othneildrew/Best-README-Template/issues
[license-shield]: https://img.shields.io/github/license/othneildrew/Best-README-Template.svg?style=for-the-badge
[license-url]: https://github.com/othneildrew/Best-README-Template/blob/master/LICENSE.txt
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://linkedin.com/in/othneildrew
[product-screenshot]: images/screenshot.png
[Next.js]: https://img.shields.io/badge/next.js-000000?style=for-the-badge&logo=nextdotjs&logoColor=white
[Next-url]: https://nextjs.org/
[React.js]: https://img.shields.io/badge/React-20232A?style=for-the-badge&logo=react&logoColor=61DAFB
[React-url]: https://reactjs.org/
[Vue.js]: https://img.shields.io/badge/Vue.js-35495E?style=for-the-badge&logo=vuedotjs&logoColor=4FC08D
[Vue-url]: https://vuejs.org/
[Angular.io]: https://img.shields.io/badge/Angular-DD0031?style=for-the-badge&logo=angular&logoColor=white
[Angular-url]: https://angular.io/
[Svelte.dev]: https://img.shields.io/badge/Svelte-4A4A55?style=for-the-badge&logo=svelte&logoColor=FF3E00
[Svelte-url]: https://svelte.dev/
[Laravel.com]: https://img.shields.io/badge/Laravel-FF2D20?style=for-the-badge&logo=laravel&logoColor=white
[Laravel-url]: https://laravel.com
[Bootstrap.com]: https://img.shields.io/badge/Bootstrap-563D7C?style=for-the-badge&logo=bootstrap&logoColor=white
[Bootstrap-url]: https://getbootstrap.com
[JQuery.com]: https://img.shields.io/badge/jQuery-0769AD?style=for-the-badge&logo=jquery&logoColor=white
[JQuery-url]: https://jquery.com 
