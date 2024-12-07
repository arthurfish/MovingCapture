using MvCameraControl;
using cszmcaux;
using System.Security.Cryptography.X509Certificates;

namespace MovingCapture
{
    class Hello
    {
        public static void Main()
        {
            var movementPlatformHandle = getMovementPlatform();
            zmcaux.ZAux_Direct_Single_MoveAbs(movementPlatformHandle, 0, 0);
            Thread.Sleep(7000);
            zmcaux.ZAux_Direct_Single_MoveAbs(movementPlatformHandle, 0, 70);
            CameraControl();
        }
        public static nint getMovementPlatform(string ipAddress = "192.168.0.11")
        {
            nint handle = 0;
            var ret = zmcaux.ZAux_OpenEth(ipAddress, out handle);
            if (ret != 0) throw new Exception("Can not connect to PLATFORM.");
            zmcaux.ZAux_Direct_SetAtype(handle, 0, 65);
            zmcaux.ZAux_Direct_SetUnits(handle, 0, 20000);
            zmcaux.ZAux_Direct_SetAccel(handle, 0, 1000);
            zmcaux.ZAux_Direct_SetDecel(handle, 0, 1000);
            zmcaux.ZAux_Direct_SetSpeed(handle, 0, 10);
            return handle;
        }
        public static void CameraControl()
        {
            Console.WriteLine("Scanning Camera...");
            SDKSystem.Initialize();
            List<IDeviceInfo>? deviceInfoList = null;
            var ret = DeviceEnumerator.EnumDevices(DeviceTLayerType.MvGigEDevice, out deviceInfoList);
            if(ret != MvError.MV_OK || deviceInfoList.Count != 1)
                throw new Exception($"ERROR: Can not get the camera.");
            var device = DeviceFactory.CreateDevice(deviceInfoList[0]) as IGigEDevice 
                ?? throw new Exception("Cast to GigE Error.");
            device.Open();
            var packetSize = -1;
            device.GetOptimalPacketSize(out packetSize);
            device.Parameters.SetIntValue("GevSCPSPacketSize", packetSize);
            device.StreamGrabber.SetImageNodeNum(5);
            var movementPlatformHandle = getMovementPlatform();

            EventHandler<FrameGrabbedEventArgs> handler = (sender, e) =>
            {
                var handle = movementPlatformHandle;
                Console.WriteLine($"Frame Got. [{e.FrameOut.Image.Width}x{e.FrameOut.Image.Width}] No.{e.FrameOut.FrameNum}");
                float readPos = 0;
                zmcaux.ZAux_Direct_GetDpos(handle, 0, ref readPos);
                Console.WriteLine($"Axis-0 position: {readPos}");
                e.FrameOut.Image.ToBitmap()?.Save($"{e.FrameOut.FrameNum}-{readPos}.bmp");

            };
            device.StreamGrabber.FrameGrabedEvent += handler;
            device.StreamGrabber.StartGrabbing();
            Console.ReadLine();
            SDKSystem.Finalize();
        }
    }
}