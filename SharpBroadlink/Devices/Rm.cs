using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharpBroadlink.Devices
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// https://github.com/mjg59/python-broadlink/blob/56b2ac36e5a2359272f4af8a49cfaf3e1891733a/broadlink/__init__.py#L524-L559
    /// </remarks>
    public class Rm : Device
    {
        public Rm(IPEndPoint host, byte[] mac, int devType) : base(host, mac, devType)
        {
            this.DeviceType = "RM2";
        }

        public async Task<byte[]> CheckData()
        {
            var packet = new byte[16];
            packet[0] = 4;

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

        public async Task<bool> SendData(byte[] data)
        {
            var packet = new List<byte>() { 0x02, 0x00, 0x00, 0x00 };
            packet.AddRange(data);

            await this.SendPacket(0x6a, packet.ToArray());

            return true;
        }

        public async Task<bool> EnterLearning()
        {
            var packet = new byte[16];
            packet[0] = 3;

            await this.SendPacket(0x6a, packet);

            return true;
        }

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
                    temp = (int.Parse(((char)packet[0x4]).ToString()) * 10
                                + int.Parse(((char)packet[0x4]).ToString()))
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

        //public async Task<bool> SendLirc(int[] data)
        //{
        //    var broadlinkBytes = this.Lirc2Signal(data);
        //    return await this.SendData(broadlinkBytes);
        //}

        /// <summary>
        /// Send Data from Pronto bytes
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendPronto(byte[] data)
        {
            var pulses = this.Pronto2Lirc(data);
            var broadlinkBytes = this.Lirc2Signal(pulses);

            //Xb.Util.Out(BitConverter.ToString(broadlinkBytes));
            return await this.SendData(broadlinkBytes);
        }

        /// <summary>
        /// Send Data from Pronto-Format string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendPronto(string data)
        {
            return await this.SendPronto(this.String2ProntoBytes(data));
        }

        /// <summary>
        /// Convert Pronto-String to Pronto-Bytes
        /// </summary>
        /// <param name="prontoString"></param>
        /// <returns></returns>
        private byte[] String2ProntoBytes(string prontoString)
        {
            var formatted = prontoString.Trim().Replace("0x", "").Replace("-", " ");
            var result = new List<byte>();
            foreach (var twoBytes in formatted.Split(' '))
            {
                var bytes = twoBytes.PadLeft(4, '0');
                result.Add((byte)Convert.ToInt32(bytes.Substring(0, 2), 16));
                result.Add((byte)Convert.ToInt32(bytes.Substring(2, 2), 16));
            }

            return result.ToArray();
        }

        /// <summary>
        /// Convert Pronto-Bytes to Pulse array
        /// </summary>
        /// <param name="pronto"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://community.home-assistant.io/t/configuration-of-broadlink-ir-device-and-getting-the-right-ir-codes/48391
        /// </remarks>
        private int[] Pronto2Lirc(byte[] pronto)
        {
            var codes = new List<int>();
            for (var i = 0; i < pronto.Length; i += 2)
                codes.Add(BitConverter.ToInt32(new byte[] { pronto[i + 1], pronto[i], 0x00, 0x00 }, 0));

            if (codes[0] != 0)
                throw new ArgumentException("Pronto code should start with 0000");
            if (codes.Count != (4 + 2 * (codes[2] + codes[3])))
                throw new ArgumentOutOfRangeException("Number of pulse widths does not match the preamble");

            var frequency = 1 / (codes[1] * 0.241246);

            return codes
                .Skip(4)
                .Select(i => (int)(Math.Round(i / frequency)))
                .ToArray();
        }

        /// <summary>
        /// Convert Pulse array to Broadlink Signal
        /// </summary>
        /// <param name="pulses"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://community.home-assistant.io/t/configuration-of-broadlink-ir-device-and-getting-the-right-ir-codes/48391
        /// </remarks>
        private byte[] Lirc2Signal(int[] pulses)
        {
            var array = new List<byte>();

            foreach (var pulse in pulses)
            {
                var calcedPulse = pulse * 269 / 8192;

                if (calcedPulse < 256)
                {
                    // big endian (1-byte)
                    array.Add((byte)calcedPulse);
                }
                else
                {
                    // indicate next number is 2-bytes
                    array.Add(0x00);
                    // big endian (2-bytes)
                    var bytes = BitConverter.GetBytes((short)calcedPulse);
                    array.Add(bytes[1]);
                    array.Add(bytes[0]);
                }
            }

            var packet = new List<byte>() { 0x26, 0x00 };

            var length = array.Count;
            if (length < 256)
            {
                packet.Add((byte)array.Count);
                packet.Add(0x00);
            }
            else
            {
                var bytes = BitConverter.GetBytes((short)length);
                array.Add(bytes[1]);
                array.Add(bytes[0]);
            }

            packet.AddRange(array);
            packet.Add(0x0d);
            packet.Add(0x05);

            var remainder = (packet.Count + 4) % 16;
            if (remainder > 0)
                packet.AddRange(new byte[remainder]);

            return packet.ToArray();
        }
    }
}
