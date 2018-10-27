using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SharpBroadlink
{
    public class Broadlink
    {
        public enum WifiSecurityMode
        {
            None = 0,
            Wep = 1,
            WPA1 = 2,
            WPA2 = 3,
            WPA12 = 4
        }

        /// <summary>
        /// Find devices on the LAN.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="localIpAddress"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://github.com/mjg59/python-broadlink/blob/56b2ac36e5a2359272f4af8a49cfaf3e1891733a/broadlink/__init__.py#L61-L138
        /// </remarks>
        public static async Task<Devices.IDevice[]> Discover(int timeout = 0, IPAddress localIpAddress = null)
        {
            if (localIpAddress == null)
                localIpAddress = IPAddress.Any;

            var address = localIpAddress.GetAddressBytes();
            if (address.Length != 4)
                throw new NotSupportedException("Not Supported IPv6");

            // for address endian validation
            var localPrimaryIpAddress = Xb.Net.Util
                .GetLocalPrimaryAddress()
                .GetAddressBytes();

            using (var cs = new Xb.Net.Udp())
            {
                var port = cs.LocalPort;
                var startTime = DateTime.Now;
                var devices = new List<Devices.IDevice>();
                var timezone = (int)((System.TimeZoneInfo.Local).BaseUtcOffset.TotalSeconds / -3600);
                var year = startTime.Year;
                var subYear = year % 100;

                // 1=mon, 2=tue,... 7=sun
                var isoWeekday = (startTime.DayOfWeek == DayOfWeek.Sunday)
                    ? 7
                    : (int)startTime.DayOfWeek;

                var packet = new byte[0x30];
                if (timezone < 0)
                {
                    packet[0x08] = (byte)(0xff + timezone - 1);
                    packet[0x09] = 0xff;
                    packet[0x0a] = 0xff;
                    packet[0x0b] = 0xff;
                }
                else
                {
                    packet[0x08] = (byte)timezone;
                    packet[0x09] = 0;
                    packet[0x0a] = 0;
                    packet[0x0b] = 0;
                }
                packet[0x0c] = (byte)(year & 0xff);
                packet[0x0d] = (byte)(year >> 8);
                packet[0x0e] = (byte)startTime.Minute;
                packet[0x0f] = (byte)startTime.Hour;
                packet[0x10] = (byte)subYear;
                packet[0x11] = (byte)isoWeekday;
                packet[0x12] = (byte)startTime.Day;
                packet[0x13] = (byte)startTime.Month;
                packet[0x18] = address[0];
                packet[0x19] = address[1];
                packet[0x1a] = address[2];
                packet[0x1b] = address[3];
                packet[0x1c] = (byte)(port & 0xff);
                packet[0x1d] = (byte)(port >> 8);
                packet[0x26] = 6;

                var checksum = 0xbeaf;
                foreach (var b in packet)
                    checksum += (int)b;

                checksum = checksum & 0xffff;

                packet[0x20] = (byte)(checksum & 0xff);
                packet[0x21] = (byte)(checksum >> 8);

                var isRecievedOnce = false;
                cs.OnRecieved += (object sender, Xb.Net.RemoteData rdata) =>
                {
                    // Get mac
                    // 0x3a-0x3f, Little Endian
                    var mac = new byte[6];
                    Array.Copy(rdata.Bytes, 0x3a, mac, 0, 6);

                    // Get IP address
                    byte[] addr;
                    if (rdata.RemoteEndPoint.AddressFamily 
                        == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        var tmpAddr = rdata.RemoteEndPoint.Address.GetAddressBytes();
                        addr = tmpAddr.Skip(tmpAddr.Length - 4).Take(4).ToArray();
                    } else if (rdata.RemoteEndPoint.AddressFamily 
                               == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        // Get the IPv4 address in Broadlink-Device response.
                        // 0x36-0x39, Mostly Little Endian
                        addr = new byte[4];
                        Array.Copy(rdata.Bytes, 0x36, addr, 0, 4);
                        if (addr[0] == localPrimaryIpAddress[0] && addr[1] == localPrimaryIpAddress[1])
                        {
                            // Recieve IP address is Big Endian
                            // Do nothing.
                        }
                        else
                        {
                            // Recieve IP address is Little Endian
                            // Change to Big Endian.
                            Array.Reverse(addr);
                        }
                    }
                    else
                    {
                        Xb.Util.Out("Unexpected Address: " + BitConverter.ToString(rdata.RemoteEndPoint.Address.GetAddressBytes()));
                        return;
                    }
                    




                    var host = new IPEndPoint(new IPAddress(addr), 80);

                    var devType = (rdata.Bytes[0x34] | rdata.Bytes[0x35] << 8);
                    devices.Add(Devices.Factory.GenDevice(devType, host, mac));

                    isRecievedOnce = true;
                };

                await cs.SendToAsync(packet, IPAddress.Broadcast, 80);

                await Task.Run(() =>
                {
                    while (true)
                    {
                        if ((timeout <= 0 && isRecievedOnce)
                            || (timeout > 0
                                && (DateTime.Now - startTime).TotalSeconds > timeout)
                            )
                            break;

                        Task.Delay(100)
                            .ConfigureAwait(false)
                            .GetAwaiter()
                            .GetResult();
                    }
                });

                return devices.ToArray();
            }
        }

        /// <summary>
        /// Get IDevice object
        /// </summary>
        /// <param name="deviceType"></param>
        /// <param name="mac"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static Devices.IDevice Create(int deviceType, byte[] mac, IPEndPoint endPoint)
        {
            return Devices.Factory.GenDevice(deviceType, endPoint, mac);
        }

        /// <summary>
        /// Set the Wi-Fi setting to devices.
        /// </summary>
        /// <param name="ssid"></param>
        /// <param name="password"></param>
        /// <param name="securityMode"></param>
        /// <remarks>
        /// https://github.com/mjg59/python-broadlink/blob/56b2ac36e5a2359272f4af8a49cfaf3e1891733a/broadlink/__init__.py#L848-L883
        /// </remarks>
        public static async Task<bool> Setup(string ssid, string password, WifiSecurityMode securityMode)
        {
            var payload = new List<byte>();
            payload.AddRange(new byte[0x88]);

            payload[0x26] = 0x14;
            var ssidStart = 68;
            var ssidLength = 0;
            foreach (var schr in ssid)
            {
                payload[ssidStart + ssidLength] = (byte)schr;
                ssidLength++;
            }

            var passStart = 100;
            var passLength = 0;
            foreach (var pchar in password)
            {
                payload[passStart + passLength] = (byte)pchar;
                passLength++;
            }

            payload[0x84] = (byte)ssidLength;
            payload[0x85] = (byte)passLength;
            payload[0x86] = (byte)securityMode;

            var checksum = 0xbeaf;
            foreach (var b in payload)
            {
                checksum += (int)b;
                checksum = checksum & 0xffff;
            }

            payload[0x20] = (byte)(checksum & 0xff);
            payload[0x21] = (byte)(checksum >> 8);

            await Xb.Net.Udp.SendOnceAsync(payload.ToArray(), IPAddress.Broadcast, 80);

            return true;
        }
    }
}
