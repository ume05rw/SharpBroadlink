using System;
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

            Program.AuthTest()
                .GetAwaiter()
                .GetResult();

            //Program.RmTest()
            //    .GetAwaiter()
            //    .GetResult();

            //Program.RmTemperatureTest()
            //    .GetAwaiter()
            //    .GetResult();

            //Program.SetupTest()
            //    .GetAwaiter()
            //    .GetResult();
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
    }
}
