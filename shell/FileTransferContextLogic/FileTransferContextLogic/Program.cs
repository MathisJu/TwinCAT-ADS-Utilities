using AdsUtilities;
using System.Xml.Linq;

namespace FileTransferContextLogic
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No file specified.");
                return;
            }

            Console.ReadLine();

            string filePath = args[0];
            filePath = filePath.Substring(1, filePath.Length-2);

            string xmlPath = @"C:\TwinCAT\3.1\Target\StaticRoutes.xml";

            DeviceConfig config = new DeviceConfig(xmlPath);
            var devices = config.GetDevices();

            Console.WriteLine("Select the target device:");
            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {devices[i].DeviceName}");
            }

            int selectedDeviceIndex;
            while (!int.TryParse(Console.ReadLine(), out selectedDeviceIndex) || selectedDeviceIndex < 1 || selectedDeviceIndex > devices.Count)
            {
                Console.WriteLine("Invalid selection. Please try again.");
            }

            var selectedDevice = devices[selectedDeviceIndex - 1];
            TransferFile(selectedDevice.AmsNetId, filePath);
        }

        static void TransferFile(string amsNetId, string filePath)
        {
            var data = File.ReadAllBytes(filePath);
            FileWrite(amsNetId, "C:/TwinCAT/FileTransfer/" + Path.GetFileName(filePath), data);
        }

        public static void FileWrite(string amsNetId, string path, byte[] data)
        {
            AdsFileAccess fileWriter = new(amsNetId);
            fileWriter.FileWrite(path, data);
        }
    }

    public class Device
    {
        public string AmsNetId { get; set; }
        public string DeviceName { get; set; }
        public string IpAddress { get; set; }
    }

    public class DeviceConfig
    {
        private List<Device> devices = new List<Device>();

        public DeviceConfig(string xmlPath)
        {
            XDocument doc = XDocument.Load(xmlPath);
            foreach (var route in doc.Descendants("Route"))
            {
                devices.Add(new Device
                {
                    AmsNetId = route.Element("NetId").Value,
                    DeviceName = route.Element("Name").Value,
                    IpAddress = route.Element("Address").Value
                });
            }
        }

        public List<Device> GetDevices()
        {
            return devices;
        }
    }
}
