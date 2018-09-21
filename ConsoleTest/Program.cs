using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SharpBroadlink;

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

            Lirc2ProntoTest();
        }

        private static async Task<bool> DiscoverTest()
        {
            var devs = await Broadlink.Discover();
            var a = 1;

            return true;
        }

        private static async Task<bool> AuthTest()
        {
            var devs = await Broadlink.Discover();

            await devs[0].Auth();

            var a = 1;

            return true;
        }

        private static async Task<bool> RmTest()
        {
            var devs = await Broadlink.Discover();

            var rm = (SharpBroadlink.Devices.Rm)devs[0];

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
            await Broadlink.Setup("xxxxxxxxxx", "xxxxxxxxxx", Broadlink.WifiSecurityMode.WPA12);

            var a = 1;

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
    }
}
