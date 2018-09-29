using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharpBroadlink.Devices
{
    public class Sp2 : Device
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host"></param>
        /// <param name="mac"></param>
        /// <param name="devType"></param>
        public Sp2(IPEndPoint host, byte[] mac, int devType) : base(host, mac, devType)
        {
            this.DeviceType = DeviceType.Sp2;
        }

        /// <summary>
        /// Get power and nightlight state
        /// </summary>
        /// <returns></returns>
        public async Task<(bool Power, bool NightLight)> CheckStatus()
        {
            var packet = new byte[16];
            packet[0] = 1;
            var response = await this.SendPacket(0x6a, packet);
            var err = response[0x22] | (response[0x23] << 8);
            var result = (Power: false, NightLight: false);

            if (err == 0)
            {
                var payload = this.Decrypt(response.Skip(0x38).Take(int.MaxValue).ToArray());
                var charInt = ((char)payload[0x4]).ToString();

                var tmpInt = 0;
                if (int.TryParse(charInt, out tmpInt))
                {
                    // character returned
                    result.Power = (tmpInt == 1 || tmpInt == 3);
                    result.NightLight = (tmpInt == 2 || tmpInt == 3);
                }
                else
                {
                    // int returned
                    var sp2State = (int)payload[0x4];
                    result.Power = (sp2State == 1 || sp2State == 3);
                    result.NightLight = (sp2State == 2 || sp2State == 3);
                }

                return result;
            }

            return result;
        }

        /// <summary>
        /// Get power state
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckPower()
        {
            return (await this.CheckStatus()).Power;
        }

        /// <summary>
        /// Get nightlight state
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckNightLight()
        {
            return (await this.CheckStatus()).NightLight;
        }

        /// <summary>
        /// Set power state
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task<bool> SetPower(bool state)
        {
            var packet = new byte[16];
            packet[0] = 2;

            if ((await this.CheckStatus()).NightLight)
                packet[4] = (byte)(state ? 3 : 2);
            else
                packet[4] = (byte)(state ? 1 : 0);

            var response = await this.SendPacket(0x6a, packet);
            var err = response[0x22] | (response[0x23] << 8);

            return (err == 0);
        }

        /// <summary>
        /// Set nightlight state
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task<bool> SetNightLight(bool state)
        {
            var packet = new byte[16];
            packet[0] = 2;

            if ((await this.CheckStatus()).Power)
                packet[4] = (byte)(state ? 3 : 1);
            else
                packet[4] = (byte)(state ? 2 : 0);

            var response = await this.SendPacket(0x6a, packet);
            var err = response[0x22] | (response[0x23] << 8);

            return (err == 0);
        }

        /// <summary>
        /// Get energy value
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// what's energy?
        /// </remarks>
        public async Task<double> GetEnergy()
        {
            var packet = new byte[] { 8, 0, 254, 1, 5, 1, 0, 0, 0, 45 };
            var response = await this.SendPacket(0x6a, packet);
            var err = response[0x22] | (response[0x23] << 8);

            if (err == 0)
            {
                var payload = this.Decrypt(response.Skip(0x38).Take(int.MaxValue).ToArray());
                var charInt = ((char)payload[0x07]).ToString();

                int p07, p06, p05;
                if (int.TryParse(charInt, out p07))
                {
                    // character returned
                    p06 = int.Parse(((char)payload[0x06]).ToString());
                    p05 = int.Parse(((char)payload[0x05]).ToString());
                }
                else
                {
                    // int returned
                    p07 = (int)payload[0x07];
                    p06 = (int)payload[0x06];
                    p05 = (int)payload[0x05];
                }

                var iHex = ((short)((p07 * 256) + p06)).ToString("x");
                var dHex = ((short)p05).ToString("x");

                var iVal = Convert.ToInt16(iHex, 16);
                var dVal = Convert.ToInt16(dHex, 16);

                var result = iVal + (dVal / 100.0);

                return result;
            }

            return -1;
        }
    }
}
