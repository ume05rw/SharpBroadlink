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

            //Program.SetupTest()
            //    .GetAwaiter()
            //    .GetResult();

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

            //Lirc2ProntoTest();

            //Program.A1SensorTest()
            //    .GetAwaiter()
            //    .GetResult();

            //Program.Sp2SmartPlugTest()
            //    .GetAwaiter()
            //    .GetResult();

            Program.RmRfSignalTest()
                .GetAwaiter()
                .GetResult();

            Console.ReadKey();
        }

        #region "TestOK"

        private static async Task<bool> RmRfSignalTest()
        {
            var devs = await Broadlink.Discover(3);
            //var targetIp = new byte[] { 192, 168, 254, 142 };
            //var rm = (Rm2Pro)devs.FirstOrDefault(d => d.Host.Address.GetAddressBytes().SequenceEqual(targetIp));
            var rm = (Rm2Pro)devs.FirstOrDefault(d => d.DeviceType == DeviceType.Rm2Pro);

            if (rm == null)
                throw new Exception("Rm2Pro Not Found");

            if (!await rm.Auth())
                throw new Exception("Auth Failure");

            if (!await rm.SweepFrequencies())
                throw new Exception("Sweeping Frequencies failed");

            Console.WriteLine("Sweepint Frequencies success, LED should be on");

            byte[] packetData = null;
            try
            {
                while(!await rm.CheckFrequency())
                {
                    Console.WriteLine("Check frequency failed");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
                        return false;
                }
                Console.WriteLine("Check frequency success");

                if (!await rm.find_rf_packet())
                    Console.WriteLine("Find rf packet failed");

                Console.WriteLine("Press any key to continue learning");
                Console.ReadKey();

                while (true)
                {
                    if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
                        return false;

                    packetData = await rm.CheckData();
                    if(packetData == null || packetData.Length == 0)
                    {
                        Console.WriteLine("Check data failed");
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        Console.WriteLine("command learned!");
                        break;
                    }
                }
            }
            finally
            {
                if (!await rm.CancelRfLearning())
                    Console.WriteLine("Failed to cancel learning");
            }

            while (true)
            {
                Console.WriteLine("Press any key to trigger");
                if (Console.ReadKey().Key == ConsoleKey.Escape)
                    break;

                if (!await rm.SendRfData(packetData))
                    Console.WriteLine("Failed to send packet data");
                else Console.WriteLine("Command sent");
            }
            return true;
        }

        private static async Task<bool> DiscoverTest()
        {
            var devs = await Broadlink.Discover(5);

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
            await Broadlink.Setup("XXXX", "XXXX", Broadlink.WifiSecurityMode.WPA12);

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

        private static async Task<bool> A1SensorTest()
        {
            var devs = await Broadlink.Discover(1);
            var dev = (SharpBroadlink.Devices.A1)devs.First(d => d.DeviceType == DeviceType.A1);

            // before Auth, cannot get values.
            var res0 = await dev.CheckSensorsRaw();

            var authRes = await dev.Auth();

            var res1 = await dev.CheckSensorsRaw();
            var res2 = await dev.CheckSensors();

            return true;
        }

        private static async Task<bool> Sp2SmartPlugTest()
        {
            var devs = await Broadlink.Discover(3);
            var dev = (Sp2)devs.First(d => d.DeviceType == DeviceType.Sp2);

            // before Auth, cannot get values.
            var res1 = await dev.Auth();

            // result not recieved.
            var powerState = await dev.CheckPower();
            var nightLightState = await dev.CheckNightLight();
            var status = await dev.CheckStatus();

            var res2 = await dev.SetPower(true);
            var res3 = await dev.SetNightLight(true);

            await dev.SetPower(false);
            await dev.SetNightLight(false);

            var energy = await dev.GetEnergy();

            return true;
        }

        #endregion

    }
}
