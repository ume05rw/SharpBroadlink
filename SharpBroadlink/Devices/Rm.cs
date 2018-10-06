using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharpBroadlink.Devices
{
    /// <summary>
    /// Rm - Programmable Remote Controller
    /// </summary>
    /// <remarks>
    /// https://github.com/mjg59/python-broadlink/blob/56b2ac36e5a2359272f4af8a49cfaf3e1891733a/broadlink/__init__.py#L524-L559
    /// </remarks>
    public class Rm : Device
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host"></param>
        /// <param name="mac"></param>
        /// <param name="devType"></param>
        public Rm(IPEndPoint host, byte[] mac, int devType) : base(host, mac, devType)
        {
            this.DeviceType = DeviceType.Rm;
        }

        /// <summary>
        /// Into IR Learning mode
        /// </summary>
        /// <returns></returns>
        public async Task<bool> EnterLearning()
        {
            var packet = new byte[16];
            packet[0] = 0x03;

            await this.SendPacket(0x6a, packet);

            return true;
        }

        /// <summary>
        /// Check recieved IR signal-data
        /// </summary>
        /// <returns></returns>
        public async Task<byte[]> CheckData()
        {
            var packet = new byte[16];
            packet[0] = 0x04;

            var response = await this.SendPacket(0x6a, packet);
            var err = response[0x22] | (response[0x23] << 8);

            if (err == 0)
            {
                var payload = this.Decrypt(response.Skip(0x38).Take(int.MaxValue).ToArray());
                return payload.Skip(0x04).Take(int.MaxValue).ToArray();
            }

            // failure
            return new byte[] { };
        }

        /// <summary>
        /// Cancel IR Learning mode
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CancelLearning()
        {
            var packet = new byte[16];
            packet[0] = 0x1e;

            await this.SendPacket(0x6a, packet);

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

            await this.SendPacket(0x6a, packet.ToArray());

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

            var response = await this.SendPacket(0x6a, packet);
            var err = response[0x22] | (response[0x23] << 8);

            if (err == 0)
            {
                var payload = this.Decrypt(response.Skip(0x38).Take(int.MaxValue).ToArray());
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
            return await this.SendData(bytes);
        }

        /// <summary>
        /// Send Data from Pronto-Format string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendPronto(string data)
        {
            return await this.SendPronto(Signals.String2ProntoBytes(data));
        }
    }
}
