using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SharpBroadlink.Devices
{
    /// <summary>
    /// Base class of devices.
    /// </summary>
    /// <remarks>
    /// https://github.com/mjg59/python-broadlink/blob/56b2ac36e5a2359272f4af8a49cfaf3e1891733a/broadlink/__init__.py#L142-L291
    /// </remarks>
    public class Device : IDevice
    {
        private class LockObject
        {
            public bool IsLocked { get; set; }
        }

        private static readonly byte[] KeyTemplate
            = new byte[] {
                0x09, 0x76, 0x28, 0x34, 0x3f, 0xe9, 0x9e, 0x23, 0x76, 0x5c,
                0x15, 0x13, 0xac, 0xcf, 0x8b, 0x02
            };

        private static readonly byte[] IvTemplate
            = new byte[] {
                0x56, 0x2e, 0x17, 0x99, 0x6d, 0x09, 0x3d, 0x28, 0xdd, 0xb3,
                0xba, 0x69, 0x5a, 0x2e, 0x6f, 0x58
            };


        public IPEndPoint Host { get; private set; }

        public byte[] Mac { get; private set; }

        public int DevType { get; private set; }

        public int Timeout { get; private set; }

        public byte[] Id { get; private set; }
            = new byte[] { 0, 0, 0, 0 };

        public DeviceType DeviceType { get; protected set; }

        private byte[] Key;

        private byte[] Iv { get; }

        private Xb.Net.Udp Cs { get; }

        private LockObject Lock { get; } = new LockObject();

        private int Count;

        public Device(IPEndPoint host, byte[] mac, int devType, int timeout = 10)
        {
            this.Host = host;
            this.Mac = mac;
            this.DevType = devType;
            this.Timeout = timeout;

            this.Key = Device.KeyTemplate;
            this.Iv = Device.IvTemplate;
            this.Cs = new Xb.Net.Udp(0);

            this.Count = (new System.Random(int.Parse(DateTime.Now.ToString("HHmmssfff"))))
                .Next(0xffff);

            this.DeviceType = DeviceType.Unknown; // overwrite on subclass
        }

        protected byte[] Encrypt(byte[] payload)
        {
            using (var aes = new AesManaged())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.IV = this.Iv;
                aes.Key = this.Key;
                //aes.Padding = PaddingMode.PKCS7;
                aes.Padding = PaddingMode.None;

                using (var encryptor = aes.CreateEncryptor())
                using (var toStream = new MemoryStream())
                using (var fromStream = new CryptoStream(toStream, encryptor, CryptoStreamMode.Write))
                {
                    fromStream.Write(payload, 0, payload.Length);

                    Xb.Util.Out($"Encrypt before: {BitConverter.ToString(payload)}");
                    Xb.Util.Out($"Encrypt after : {BitConverter.ToString(toStream.ToArray())}");

                    return toStream.ToArray();
                }
            }
        }

        protected byte[] Decrypt(byte[] payload)
        {
            using (var aes = new AesManaged())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.IV = this.Iv;
                aes.Key = this.Key;
                //aes.Padding = PaddingMode.PKCS7;
                aes.Padding = PaddingMode.None;

                using (var decryptor = aes.CreateDecryptor())
                using (var fromStream = new MemoryStream(payload))
                using (var toStream = new CryptoStream(fromStream, decryptor, CryptoStreamMode.Read))
                {
                    var resultStream = new MemoryStream();
                    toStream.CopyTo(resultStream);

                    Xb.Util.Out($"Decrypt before: {BitConverter.ToString(payload)}");
                    Xb.Util.Out($"Decrypt after : {BitConverter.ToString(resultStream.ToArray())}");

                    return resultStream.ToArray();
                }
            }
        }

        public async Task<bool> Auth()
        {
            var payload = new List<byte>(new byte[0x50]);

            payload[0x04] = 0x31;
            payload[0x05] = 0x31;
            payload[0x06] = 0x31;
            payload[0x07] = 0x31;
            payload[0x08] = 0x31;
            payload[0x09] = 0x31;
            payload[0x0a] = 0x31;
            payload[0x0b] = 0x31;
            payload[0x0c] = 0x31;
            payload[0x0d] = 0x31;
            payload[0x0e] = 0x31;
            payload[0x0f] = 0x31;
            payload[0x10] = 0x31;
            payload[0x11] = 0x31;
            payload[0x12] = 0x31;
            payload[0x1e] = 0x01;
            payload[0x2d] = 0x01;
            payload[0x30] = (int)'T';
            payload[0x31] = (int)'e';
            payload[0x32] = (int)'s';
            payload[0x33] = (int)'t';
            payload[0x34] = (int)' ';
            payload[0x35] = (int)' ';
            payload[0x36] = (int)'1';

            var response = await this.SendPacket(0x65, payload.ToArray());

            if (response == null)
                return false;

            var result = this.Decrypt(response.Skip(0x38).Take(int.MaxValue).ToArray());

            if (result.Length <= 0)
                return false;

            var key = result.Skip(0x04).Take(16).ToArray();
            if (key.Length % 16 != 0)
                return false;

            this.Id = result.Take(0x04).ToArray();
            this.Key = key;

            return true;
        }

        public DeviceType GetDeviceType()
        {
            return this.DeviceType;
        }

        public async Task<byte[]> SendPacket(int command, byte[] payload)
        {
            this.Count = (this.Count + 1) & 0xffff;
            var packet = new List<byte>(new byte[0x38]);
            packet[0x00] = 0x5a;
            packet[0x01] = 0xa5;
            packet[0x02] = 0xaa;
            packet[0x03] = 0x55;
            packet[0x04] = 0x5a;
            packet[0x05] = 0xa5;
            packet[0x06] = 0xaa;
            packet[0x07] = 0x55;
            packet[0x24] = 0x2a;
            packet[0x25] = 0x27;
            packet[0x26] = (byte)command; //UDP-0x5a
            packet[0x28] = (byte)(this.Count & 0xff);
            packet[0x29] = (byte)(this.Count >> 8);
            packet[0x2a] = this.Mac[0];
            packet[0x2b] = this.Mac[1];
            packet[0x2c] = this.Mac[2];
            packet[0x2d] = this.Mac[3];
            packet[0x2e] = this.Mac[4];
            packet[0x2f] = this.Mac[5];
            packet[0x30] = this.Id[0];
            packet[0x31] = this.Id[1];
            packet[0x32] = this.Id[2];
            packet[0x33] = this.Id[3];

            if (payload.Length > 0)
            {
                var numPad = ((int)(payload.Length / 16) + 1) * 16;
                var basePayload = payload;
                payload = Enumerable.Repeat((byte)0x00, numPad).ToArray();
                Array.Copy(basePayload, payload, basePayload.Length);
            }

            var checksum = 0xbeaf;
            foreach (var b in payload)
            {
                checksum += b;
                checksum = checksum & 0xffff;
            }
            packet[0x34] = (byte)(checksum & 0xff);
            packet[0x35] = (byte)(checksum >> 8);

            payload = this.Encrypt(payload);

            // append 0x38- (UDP-0x62)
            packet.AddRange(payload);

            checksum = 0xbeaf;
            foreach (var b in packet)
            {
                checksum += b;
                checksum = checksum & 0xffff;
            }
            packet[0x20] = (byte)(checksum & 0xff);
            packet[0x21] = (byte)(checksum >> 8);

            byte[] result = null;
            await Task.Run(() => 
            {
                lock (this.Lock)
                {
                    this.Lock.IsLocked = true;

                    result = this.Cs
                        .SendAndRecieveAsync(packet.ToArray(), this.Host, this.Timeout)
                        .GetAwaiter()
                        .GetResult();

                    this.Lock.IsLocked = false;
                }
            });

            return result;
        }
    }
}
