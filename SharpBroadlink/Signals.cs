using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBroadlink
{
    /// <summary>
    /// Signal converter class
    /// </summary>
    public static class Signals
    {
        /// <summary>
        /// Convert Pronto-string to bytes
        /// </summary>
        /// <param name="prontoString"></param>
        /// <returns></returns>
        public static byte[] String2ProntoBytes(string prontoString)
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
        /// Convert Pronto-btring to string
        /// </summary>
        /// <param name="pronto"></param>
        /// <returns></returns>
        public static string ProntoBytes2String(byte[] pronto)
        {
            var byteStrs = BitConverter.ToString(pronto).Split('-');
            var result = new List<string>();
            for (var i = 0; i < byteStrs.Length; i += 2)
                result.Add(byteStrs[i] + byteStrs[i + 1]);

            return string.Join(" ", result.ToArray());
        }

        /// <summary>
        /// Convert Pronto-bytes to Lirc integer array
        /// </summary>
        /// <param name="pronto"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://community.home-assistant.io/t/configuration-of-broadlink-ir-device-and-getting-the-right-ir-codes/48391
        /// </remarks>
        public static int[] Pronto2Lirc(byte[] pronto)
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
        /// Convert Lirc integer array to Pronto-bytes
        /// </summary>
        /// <param name="raw"></param>
        /// <param name="frequency">unit: kHz, typical: 36.7(AEHA), 38(NEC), 40(Sony)</param>
        /// <returns></returns>
        public static byte[] Lirc2Pronto(int[] raw, double frequency)
        {
            var freqCoff = (ushort)Math.Round((1 / (frequency / 1000)) / 0.241246);
            var freqBytes = BitConverter.GetBytes(freqCoff);

            var bytes = new List<byte>()
            {
                0x00, 0x00,
                freqBytes[1], freqBytes[0],
                0x00, 0x00,
                0x00, 0x00
            };

            var revFreq = 1 / (freqCoff * 0.241246);
            var twoByteSets = raw.Select(i => (ushort)Math.Round(i * revFreq)).ToArray();
            foreach (var elem in twoByteSets)
            {
                var byteSet = BitConverter.GetBytes(elem);
                bytes.Add(byteSet[1]);
                bytes.Add(byteSet[0]);
            }

            var length = ((bytes.Count / 2) - 4) / 2;
            var lengthBytes = BitConverter.GetBytes((ushort)length);
            bytes[4] = lengthBytes[1];
            bytes[5] = lengthBytes[0];

            return bytes.ToArray();
        }

        /// <summary>
        /// Convert Lirc integer array to Broadlink signal
        /// </summary>
        /// <param name="raw"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://community.home-assistant.io/t/configuration-of-broadlink-ir-device-and-getting-the-right-ir-codes/48391
        /// </remarks>
        public static byte[] Lirc2Broadlink(int[] raw)
        {
            var array = new List<byte>();

            foreach (var elem in raw)
            {
                var calcedPulse = (ushort)Math.Round((double)elem * 269 / 8192);

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
                    var bytes = BitConverter.GetBytes((ushort)calcedPulse);
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
                var bytes = BitConverter.GetBytes((ushort)length);
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

        /// <summary>
        /// Convert Broadlink signal to Lirc integer array
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static int[] Broadlink2Lirc(byte[] bytes)
        {
            var length = BitConverter.ToInt16(new byte[] { bytes[2], bytes[3] }, 0);
            var ints = new List<int>();
            var index = 4;

            while(true)
            {
                if ((index - 4) >= length)
                    break;

                if (bytes.Length <= (index + 1))
                    throw new ArgumentException("Invalid Format");

                ushort elem;
                if (bytes[index] == 0x00)
                {
                    if (bytes.Length <= (index + 2))
                        throw new ArgumentException("Invalid Format");

                    elem = BitConverter.ToUInt16(new byte[] { bytes[index + 2], bytes[index + 1] }, 0);
                    index += 3;
                }
                else
                {
                    elem = bytes[index];
                    index += 1;
                }

                var intValue = (int)Math.Round((double)elem / 269 * 8192);
                ints.Add(intValue);
            }

            return ints.ToArray();
        }

        /// <summary>
        /// Convert Broadlink signal to Pronto bytes
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="frequency">unit: kHz, typical: 36.7(AEHA), 38(NEC), 40(Sony)</param>
        /// <returns></returns>
        public static byte[] Broadlink2Pronto(byte[] bytes, double frequency)
        {
            var raw = Signals.Broadlink2Lirc(bytes);
            var result = Signals.Lirc2Pronto(raw, frequency);
            return result;
        }
    }
}
