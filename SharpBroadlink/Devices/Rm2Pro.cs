using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharpBroadlink.Devices
{
    public class Rm2Pro : Rm
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host"></param>
        /// <param name="mac"></param>
        /// <param name="devType"></param>
        public Rm2Pro(IPEndPoint host, byte[] mac, int devType) : base(host, mac, devType)
        {
            this.DeviceType = DeviceType.Rm2Pro;
        }

        /// <summary>
        /// Into RF Learning mode
        /// </summary>
        /// <returns></returns>
        public async Task<bool> EnterRfLearning()
        {
            var packet = new byte[16];
            packet[0] = 0x19;

            await this.SendPacket(0x6a, packet);

            return true;
        }

        /// <summary>
        /// Check recieved RF signal-data
        /// </summary>
        /// <returns></returns>
        private async Task<bool> CheckRfStep1Data()
        {
            var packet = new byte[16];
            packet[0] = 0x1a;

            var response = await this.SendPacket(0x6a, packet);
            var err = response[0x22] | (response[0x23] << 8);

            if (err == 0)
            {
                var payload = this.Decrypt(response.Skip(0x38).Take(int.MaxValue).ToArray());

                // 応答データ全体
                var resultData = payload.Skip(4).Take(int.MaxValue).ToArray();

                return (resultData[0] == 0x01);
            }

            // failure
            return false;
        }

        /// <summary>
        /// Check recieved RF signal-data
        /// </summary>
        /// <returns></returns>
        private async Task<byte[]> CheckRfStep2Data()
        {
            var packet = new byte[16];
            packet[0] = 0x1b;

            var response = await this.SendPacket(0x6a, packet);
            var err = response[0x22] | (response[0x23] << 8);

            if (err == 0)
            {
                var payload = this.Decrypt(response.Skip(0x38).Take(int.MaxValue).ToArray());

                // 応答データ全体
                var resultData = payload.Skip(4).Take(int.MaxValue).ToArray();

                return resultData;
            }

            // failure
            return new byte[] { };
        }

        /// <summary>
        /// Cancel RF Learning mode
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Same as RF packet
        /// </remarks>
        public async Task<bool> CancelRfLearning()
        {
            return await this.CancelLearning();
        }

        /// <summary>
        /// Send RF signal-data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendRfData(byte[] data)
        {
            var packet = new List<byte>() { 0x02, 0x00, 0x00, 0x00 };
            packet.AddRange(data);

            await this.SendPacket(0x6a, packet.ToArray());

            return true;
        }
    }
}
