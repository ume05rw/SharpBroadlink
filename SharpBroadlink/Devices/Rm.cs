using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBroadlink.Devices
{
    /// <summary>
    /// Rm - Programmable Remote Controller for IR
    /// </summary>
    /// <remarks>
    /// https://github.com/mjg59/python-broadlink/blob/56b2ac36e5a2359272f4af8a49cfaf3e1891733a/broadlink/__init__.py#L524-L559
    /// </remarks>
    public class Rm : Device
    {
        #region Properties

        public static TimeSpan IRLearnIterval { get; set; } = TimeSpan.FromMilliseconds(1000);

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host"></param>
        /// <param name="mac"></param>
        /// <param name="devType"></param>
        public Rm(IPEndPoint host, byte[] mac, int devType) : base(host, mac, devType)
        {
            DeviceType = DeviceType.Rm;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Cancel IR Learning mode
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CancelLearning()
        {
            var packet = new byte[16];
            packet[0] = 0x1e;

            await SendPacket(0x6a, packet);

            return true;
        }

        /// <summary>
        /// Send IR signal-data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendData(byte[] data)
        {
            var packet = new List<byte>() { 0x02, 0x00, 0x00, 0x00 };
            packet.AddRange(data);

            await SendPacket(0x6a, packet.ToArray());

            return true;
        }

        /// <summary>
        /// Get temperature
        /// </summary>
        /// <returns></returns>
        public async Task<double> CheckTemperature()
        {
            double temp = double.MinValue;
            var packet = new byte[16];
            packet[0] = 1;

            var response = await SendPacket(0x6a, packet);
            var err = response[0x22] | (response[0x23] << 8);

            if (err == 0)
            {
                var payload = Decrypt(response, 0x38);
                var charInt = ((char)packet[0x4]).ToString();
                var tmpInt = 0;
                if (int.TryParse(charInt, out tmpInt))
                {
                    // character returned
                    temp = (
                                int.Parse(((char)packet[0x4]).ToString()) * 10
                                + int.Parse(((char)packet[0x5]).ToString())
                            )
                            / 10.0;
                }
                else
                {
                    // int returned
                    temp = (payload[0x4] * 10 + payload[0x5]) / 10.0;
                }

                return temp;
            }

            // failure
            return double.MinValue;
        }

        /// <summary>
        /// Send Data from Pronto bytes
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendPronto(byte[] data)
        {
            var raw = Signals.Pronto2Lirc(data);
            var bytes = Signals.Lirc2Broadlink(raw);

            //Xb.Util.Out(BitConverter.ToString(broadlinkBytes));
            return await SendData(bytes);
        }

        /// <summary>
        /// Send Data from Pronto-Format string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendPronto(string data)
        {
            return await SendPronto(Signals.String2ProntoBytes(data));
        }

        /// <summary>
        /// Learn IR command
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token for IR learning</param>
        /// <returns>IR command</returns>
        /// <exception cref="InvalidOperationException">In case of failure</exception>
        /// <exception cref="TaskCanceledException">In case learning has benn cancelled</exception>
        public Task<byte[]> LearnIRCommnad(Action<LearnInstructions> learnInstructions, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    if (!await CancelLearning())
                        throw new InvalidOperationException("Failed to cancel any previous learning");

                    if (!await EnterIrLearning())
                        throw new InvalidOperationException("Failed to enter IR learning");

                    cancellationToken.ThrowIfCancellationRequested();
                    learnInstructions?.Invoke(LearnInstructions.PressRemoteButtonShortly);
                    cancellationToken.ThrowIfCancellationRequested();

                    byte[] command = null;
                    while (null == (command = await CheckData()))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(IRLearnIterval);
                    }

                    return command;
                }
                finally
                {
                    await CancelLearning();
                }
            }, cancellationToken);
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Check recieved IR signal-data
        /// </summary>
        /// <returns></returns>
        protected async Task<byte[]> CheckData()
        {
            var packet = new byte[16];
            packet[0] = 0x04;

            var response = await SendPacket(0x6a, packet);
            if (response == null || response.Length <= 0x38)
                return null;

            var err = response[0x22] | (response[0x23] << 8);
            if (err == 0)
            {
                var payload = Decrypt(response, 0x38);
                if (payload.Length <= 0x04)
                    return null;

                return payload.Skip(0x04).Take(int.MaxValue).ToArray();
            }

            // failure
            return null;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Into IR Learning mode
        /// </summary>
        /// <returns></returns>
        private async Task<bool> EnterIrLearning()
        {
            var packet = new byte[16];
            packet[0] = 0x03;

            await SendPacket(0x6a, packet);

            return true;
        }

        #endregion Private Methods
    }
}
