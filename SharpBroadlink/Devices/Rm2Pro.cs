using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharpBroadlink.Devices
{
    /// <summary>
    /// Rm2Pro - Programmable Remote Controller for RF
    /// </summary>
    /// <remarks>
    /// https://github.com/kemalincekara/Broadlink.NET/blob/master/Broadlink.NET/PacketGenerator.cs
    /// 
    /// ** Warning **
    /// This is a pilot implementation.
    /// Learning of RF signal does NOT WORKS.
    /// 
    /// </remarks>
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
        /// [Pilot Implementation] Into RF Learning mode
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// ** Warning **
        /// This is a pilot implementation.
        /// Learning of RF signal does NOT WORKS.
        /// 
        /// This method is probably correct.
        /// </remarks>
        public async Task<bool> EnterRfLearning()
        {
            var packet = new byte[16];
            packet[0] = 0x19;

            await this.SendPacket(0x6a, packet);

            return true;
        }

        /// <summary>
        /// [Pilot Implementation] Check recieved RF signal-data
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// ** Warning **
        /// This is a pilot implementation.
        /// Learning of RF signal does NOT WORKS.
        /// 
        /// It seems that it is detecting the RF signal,
        /// I don't know if the result judgment is correct or not.
        /// </remarks>
        public async Task<bool> CheckRfStep1Data()
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
        /// [Pilot Implementation] Check recieved RF signal-data
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// ** Warning **
        /// This is a pilot implementation.
        /// Learning of RF signal does NOT WORKS.
        /// 
        /// The result is always zero x 12bytes
        /// </remarks>
        public async Task<byte[]> CheckRfStep2Data()
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
        /// [Pilot Implementation] Cancel RF Learning mode
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Same as RF packet
        /// 
        /// ** Warning **
        /// This is a pilot implementation.
        /// Learning of RF signal does NOT WORKS.
        /// 
        /// This method is probably correct.
        /// </remarks>
        public async Task<bool> CancelRfLearning()
        {
            return await this.CancelLearning();
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

            await this.SendPacket(0x6a, packet.ToArray());

            return true;
        }
    }
}
