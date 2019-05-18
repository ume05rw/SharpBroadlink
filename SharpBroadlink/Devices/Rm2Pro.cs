using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBroadlink.Devices
{
    /// <summary>
    /// Rm2Pro - Programmable Remote Controller for RF
    /// </summary>
    public class Rm2Pro : Rm
    {
        #region Properties

        public static TimeSpan RfFrequencyLearnInterval { get; set; } = TimeSpan.FromMilliseconds(100);
        public static TimeSpan RfCommandLearnInterval { get; set; } = TimeSpan.FromMilliseconds(100);

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host"></param>
        /// <param name="mac"></param>
        /// <param name="devType"></param>
        public Rm2Pro(IPEndPoint host, byte[] mac, int devType) : base(host, mac, devType)
        {
            DeviceType = DeviceType.Rm2Pro;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Switch into RF Learning mode
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SweepFrequencies()
        {
            var packet = new byte[16];
            packet[0] = 0x19;

            await SendPacket(0x6a, packet);

            return true;
        }

        /// <summary>
        /// Enter RF frequency learning
        /// </summary>
        /// <returns>True if entering learning was successful</returns>
        public async Task<bool> LearnRfFrequency()
        {
            var packet = new byte[16];
            packet[0] = 0x1a;

            var response = await SendPacket(0x6a, packet);
            if (response == null || response.Length <= 0x38)
                return false;

            var err = response[0x22] | (response[0x23] << 8);

            if (err == 0)
            {
                var payload = Decrypt(response, 0x38);
                if (payload.Length <= 0x04)
                    return false;

                return payload[0x04] == 1;
            }

            // failure
            return false;
        }

        /// <summary>
        /// Cancel RF Learning mode
        /// </summary>
        /// <returns>True if the succedded, false otherwise</returns>
        public async Task<bool> CancelRfLearning()
        {
            return await CancelLearning();
        }

        /// <summary>
        /// [Pilot Implementation] Send RF signal-data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <remarks>
        /// ** Warning **
        /// This is a pilot implementation.
        /// Learning of RF signal does NOT WORKS.
        /// 
        /// I have not done it.
        /// Because signal data could not be acquired.
        /// </remarks>
        public async Task<bool> SendRfData(byte[] data)
        {
            var packet = new List<byte>() { 0x02, 0x00, 0x00, 0x00 };
            packet.AddRange(data);

            await SendPacket(0x6a, packet.ToArray());

            return true;
        }

        /// <summary>
        /// Async method for learning RF commands
        /// </summary>
        /// <param name="cancellationToken">Used to cancel RF learning</param>
        /// <param name="moveToStep2">Learning RF consists of 2 steps. First user should press and hold remote to learn RF frequency. When this delegate is invoked,
        /// frequency is learned and user should release the remote. After that the method should return and the user should press remote again shortly to learn the actual command.
        /// The actual time of pressing the remote in step 2 influences the command code</param>
        /// <returns>Learned RF command</returns>
        /// <exception cref="InvalidOperationException">In case of failure</exception>
        /// <exception cref="TaskCanceledException">In case learning has benn cancelled</exception>
        public async Task<byte[]> LearnRfCommand(CancellationToken cancellationToken, Action learnProgress = null)
        {            
            return await Task.Run(async () =>
            {
                try
                {
                    if (!await CancelRfLearning())
                        throw new InvalidOperationException("Failed to cancel any previous learning");

                    if (!await SweepFrequencies())
                        throw new InvalidOperationException("Failed to sweep frequencies");

                    //Learn RF frequency - remote should be held all the time
                    while (!await LearnRfFrequency())
                    {
                        learnProgress?.Invoke();
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(RfFrequencyLearnInterval, cancellationToken);
                    }

                    //Move to step 2 - learning the actual command
                    cancellationToken.ThrowIfCancellationRequested();

                    byte[] commandData = null;
                    while(null == (commandData = await CheckData()))
                    {
                        learnProgress?.Invoke();
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(RfCommandLearnInterval, cancellationToken);
                    }
                    return commandData;
                }
                finally
                {
                    await CancelRfLearning();
                }
            }, cancellationToken);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// This doesn't seem to be required for RF learning at all. I'm leaving it because of phyton lib campatibility
        /// </summary>
        /// <returns></returns>
        private async Task<bool> find_rf_packet()
        {
            var packet = new byte[16];
            packet[0] = 0x1b;

            var response = await SendPacket(0x6a, packet);
            if (response == null || response.Length <= 0x38)
                return false;

            var err = response[0x22] | (response[0x23] << 8);
            if (err == 0)
            {
                var payload = Decrypt(response, 0x38);
                return payload[0x04] == 1;
            }

            return false;
        }

        #endregion Private Methods
    }
}
