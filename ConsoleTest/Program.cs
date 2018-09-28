using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SharpBroadlink;
using SharpBroadlink.Devices;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //Program.DiscoverTest()
            //    .GetAwaiter()
            //    .GetResult();

            //Program.AuthTest()
            //    .GetAwaiter()
            //    .GetResult();

            //Program.RmTest()
            //    .GetAwaiter()
            //    .GetResult();

            //Program.RmTemperatureTest()
            //    .GetAwaiter()
            //    .GetResult();

            //Program.SetupTest()
            //    .GetAwaiter()
            //    .GetResult();

            //Lirc2ProntoTest();

            Program.A1SensorTest()
                .GetAwaiter()
                .GetResult();

            //Program.Sp2SmartPlugTest()
            //    .GetAwaiter()
            //    .GetResult();
        }

        #region "TestOK"

        private static async Task<bool> DiscoverTest()
        {
            var devs = await Broadlink.Discover();

            return true;
        }

        private static async Task<bool> AuthTest()
        {
            var devs = await Broadlink.Discover();

            foreach (var dev in devs)
            {
                // SmarPlug Sp2 cannot Auth
                if (dev.DeviceType == DeviceType.Sp2)
                    continue;

                await dev.Auth();
            }

            return true;
        }

        private static async Task<bool> RmTest()
        {
            var devs = await Broadlink.Discover();

            var rm = (Rm)devs[0];

            if (!await rm.Auth())
                throw new Exception("Auth Failure");

            await rm.EnterLearning();

            var data1 = await rm.CheckData();

            var data2 = await rm.CheckData();

            await rm.SendData(data2);

            return true;
        }

        private static async Task<bool> RmTemperatureTest()
        {
            var devs = await Broadlink.Discover();

            var rm = (SharpBroadlink.Devices.Rm)devs[0];

            if (!await rm.Auth())
                throw new Exception("Auth Failure");

            var temp = await rm.CheckTemperature();

            return true;
        }

        private static async Task<bool> SetupTest()
        {
            await Broadlink.Setup("xxxxx", "xxxxx", Broadlink.WifiSecurityMode.WPA12);

            return true;
        }

        private static void Lirc2ProntoTest()
        {
            int[] raw = new int[] 
            {
                //1000, 2400,  600,  600, 600, 1200,  600, 1200,  600, 1200,
                // 600,  600,  600, 1200, 600,  600,  600,  600,  600, 1200,
                // 600,  600,  600, 1200, 600, 1200,  600, 1200,  600,  600,
                // 600,  600,  600, 1200, 600,  600,  600,  600,  600, 1200,
                // 600,  600,  25350

                9024, 4512,  564,  564,  564,  564,  564,  564,  564,  564,
                 564,  564,  564,  564,  564,  564,  564,  564,  564, 1692,
                 564, 1692,  564, 1692,  564, 1692,  564, 1692,  564, 1692,
                 564,  564,  564, 1692,  564,  564,  564, 1692,  564,  564,
                 564,  564,  564,  564,  564,  564,  564,  564,  564,  564,
                 564, 1692,  564,  564,  564, 1692,  564, 1692,  564, 1692,
                 564, 1692,  564, 1692,  564, 1692,  564,40884
            };
            double frequency = 38.3;

            var result = Signals.Lirc2Pronto(raw, frequency);

            var bStr = Signals.ProntoBytes2String(result);
            Debug.WriteLine(bStr);

            var res2 = Signals.Lirc2Broadlink(raw);
            var reverse = Signals.Broadlink2Lirc(res2);
            var res3 = Signals.Lirc2Broadlink(reverse);

            var a = 1;
        }

        #endregion

        private static async Task<bool> A1SensorTest()
        {
            var devs = await Broadlink.Discover();
            var dev = (SharpBroadlink.Devices.A1)devs.First(d => d.DeviceType == DeviceType.A1);

            var res0 = await dev.CheckSensorsRaw();

            var authRes = await dev.Auth();

            var res1 = await dev.CheckSensorsRaw();
            var res2 = await dev.CheckSensors();

            var a = 1;

            return true;
        }

        private static async Task<bool> Sp2SmartPlugTest()
        {
            var devs = await Broadlink.Discover();
            var dev = (Sp2)devs.First(d => d.DeviceType == DeviceType.Sp2);

            var bytes1 = new byte[]
            {
                0xa8 ,0xde ,0xd0 ,0x62 ,0x92 ,0x77 ,0x7a ,0xd3 ,
                0x71 ,0x00 ,0x0a ,0x46 ,0x24 ,0x78 ,0x21 ,0xc0
            };

            //var bytes2 = new byte[]
            //{
            //    0xA4, 0x39, 0xA5, 0xD8, 0xB3, 0x86, 0xF5, 0xE0, 0x86, 0xF1, 0xE7, 0x2B, 0x49, 0x1C, 0x48, 0xF6
            //};

            var decBytes1 = dev.Decrypt(bytes1);
            //Xb.Util.Out(string.Join(", ", bytes1.Select(b => ((int)b).ToString()).ToArray()));
            var decHex1 = BitConverter.ToString(decBytes1);
            Xb.Util.Out(decHex1);

            var decBytes2 = dev.Encrypt(decBytes1);
            //Xb.Util.Out(string.Join(", ", decBytes1.Select(b => ((int)b).ToString()).ToArray()));
            var decHex2 = BitConverter.ToString(decBytes2);
            Xb.Util.Out(decHex2);

            // result not recieved.
            var powerState = await dev.CheckPower();

            return true;
        }
    }
}
