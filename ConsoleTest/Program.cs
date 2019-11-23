using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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

            try
            {
                //Program.SetupTest()
                //    .GetAwaiter()
                //    .GetResult();

                //Program.DiscoverTest()
                //    .GetAwaiter()
                //    .GetResult();

                //Program.AuthTest()
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

                //TestIrLearning().Wait();
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

            while (Console.KeyAvailable)
                Console.ReadKey();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        #region "TestOK"

        private static async Task TestIrLearning()
        {
            Console.WriteLine("Searching for Rm2Pro devices...");
            var devs = await Broadlink.Discover(1);
            var rm = (Rm2Pro)devs.FirstOrDefault(d => d.DeviceType == DeviceType.Rm2Pro);

            if (rm == null)
                throw new Exception("Rm2Pro Not Found");
            else Console.WriteLine($"Rm2Pro found at {rm.Host}");

            if (!await rm.Auth())
                throw new Exception("Auth Failure");

            Console.WriteLine("Learning command, press remote now");
            var command = await rm.LearnIRCommnad(CancellationToken.None);
            if (command == null || command.Length == 0)
                throw new Exception("Failed to learn command");

            Console.WriteLine("Command learned, press any key to send the command now...");
            Console.ReadKey();

            Console.WriteLine("Sending IR command");
            await rm.SendData(command);
            Console.WriteLine("IR command sent");
        }

        private static async Task<bool> RmRfSignalTest()
        {
            Console.WriteLine("Searching for Rm2Pro devices...");
            var devs = await Broadlink.Discover(1);
            var rm = (Rm2Pro)devs.FirstOrDefault(d => d.DeviceType == DeviceType.Rm2Pro);

            if (rm == null)
                throw new Exception("Rm2Pro Not Found");
            else Console.WriteLine($"Rm2Pro found at {rm.Host}");

            if (!await rm.Auth())
                throw new Exception("Auth Failure");
            
            Console.WriteLine("Starting RF frequency learning - press any button to continue, then, press and hold the remote button...");
            while (!Console.KeyAvailable)
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            while (Console.KeyAvailable)
                Console.ReadKey(true);

            var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            byte[] command = null;
            try
            {
                Action<Rm2Pro.RfLearningSteps> learnInstructionHandler = (instructions) =>
                {
                    //Get description from the enum - any other text will do
                    var msg = instructions.GetType().GetField(instructions.ToString())
                        ?.GetCustomAttributes(typeof(DescriptionAttribute), true)
                        .OfType<DescriptionAttribute>()
                        .FirstOrDefault()
                        ?.Description
                        ?? instructions.ToString();
                        
                    Console.WriteLine(msg);
                    Console.ReadKey(true);
                };

                command = await rm.LearnRfCommand(learnInstructionHandler, cancellationSource.Token, () =>
                {
                    Console.Write('.');
                });

                if (command == null || command.Length == 0)
                    throw new InvalidOperationException("Failed to learn RF command");
            }
            catch(TaskCanceledException)
            {
                Console.WriteLine("Command learning cancelled");
                return false;
            }

            while (Console.KeyAvailable)
                Console.ReadKey(true);
            Console.WriteLine();
            Console.WriteLine($"RF Command learned [{command.Length}], press any key to trigger the command. Press ESC to exit...");
            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
                Console.Write("Sending RF command... ");
                await rm.SendData(command);
                Console.WriteLine("RF command sent");
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
