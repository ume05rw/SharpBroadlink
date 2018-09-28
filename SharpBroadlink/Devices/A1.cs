using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharpBroadlink.Devices
{
    /// <summary>
    /// A1 - Temperature, Humidity, Light, Noise, Air-Quality Sensor
    /// </summary>
    /// <remarks>
    /// https://github.com/mjg59/python-broadlink/blob/56b2ac36e5a2359272f4af8a49cfaf3e1891733a/broadlink/__init__.py#L446-L521
    /// </remarks>
    public class A1 : Device
    {
        public enum LightLevel
        {
            Dark = 0,
            Dim = 1,
            Normal = 2,
            Bright = 3,
            Unknown = 99
        }

        public enum NoiseLevel
        {
            Quiet = 0,
            Normal = 1,
            Noisy = 2,
            Unknown = 99
        }

        public enum AirQualityLevel
        {
            Excellent = 0,
            Good = 1,
            Normal = 2,
            Bad = 3,
            Unknown = 99
        }


        /// <summary>
        /// Sensor Values
        /// </summary>
        public class Result
        {
            public double Temperature { get; internal set; }

            public double Humidity { get; internal set; }

            public LightLevel Light { get; internal set; }

            public NoiseLevel Noise { get; internal set; }

            public AirQualityLevel AirQuality { get; internal set; }
        }

        /// <summary>
        /// Sensor Raw-Value
        /// </summary>
        public class RawResult
        {
            public double Temperature { get; internal set; }

            public double Humidity { get; internal set; }

            public int Light { get; internal set; }

            public int Noise { get; internal set; }

            public int AirQuality { get; internal set; }
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host"></param>
        /// <param name="mac"></param>
        /// <param name="devType"></param>
        public A1(IPEndPoint host, byte[] mac, int devType) : base(host, mac, devType)
        {
            this.DeviceType = DeviceType.A1;
        }

        /// <summary>
        /// Get sensor raw-values
        /// </summary>
        /// <returns></returns>
        public async Task<RawResult> CheckSensorsRaw()
        {
            var packet = new byte[16];
            packet[0] = 1;

            var response = await this.SendPacket(0x6a, packet);

            // Authentication is required before obtaining the value.
            var err = response[0x22] | (response[0x23] << 8);

            if (err == 0)
            {
                var result = new RawResult();
                var payload = this.Decrypt(response.Skip(0x38).Take(int.MaxValue).ToArray());
                var charInt = ((char)payload[0x4]).ToString();

                var tmpInt = 0;
                if (int.TryParse(charInt, out tmpInt))
                {
                    // character returned
                    result.Temperature 
                        = (
                            (
                                int.Parse(((char)payload[0x4]).ToString()) * 10
                                + int.Parse(((char)payload[0x5]).ToString())
                            )
                            / 10.0
                        );
                    result.Humidity
                        = (
                            (
                                int.Parse(((char)payload[0x6]).ToString()) * 10
                                + int.Parse(((char)payload[0x7]).ToString())
                            )
                            / 10.0
                        );
                    result.Light = int.Parse(((char)payload[0x8]).ToString());
                    result.AirQuality = int.Parse(((char)payload[0x0a]).ToString());
                    result.Noise = int.Parse(((char)payload[0xc]).ToString());
                }
                else
                {
                    // int returned
                    result.Temperature = ((payload[0x4] * 10) + payload[0x5]) / 10.0;
                    result.Humidity = ((payload[0x6] * 10) + payload[0x7]) / 10.0;
                    result.Light = (int)payload[0x8];
                    result.AirQuality = (int)payload[0x0a];
                    result.Noise = (int)payload[0xc];
                }

                return result;
            }

            return null;
        }

        /// <summary>
        /// Get sensor values
        /// </summary>
        /// <returns></returns>
        public async Task<Result> CheckSensors()
        {
            var rawValues = await this.CheckSensorsRaw();

            if (rawValues == null)
                return null;

            var result = new Result();
            result.Temperature = rawValues.Temperature;
            result.Humidity = rawValues.Humidity;

            if (rawValues.Light == 0)
                result.Light = LightLevel.Dark;
            else if (rawValues.Light == 1)
                result.Light = LightLevel.Dim;
            else if (rawValues.Light == 2)
                result.Light = LightLevel.Normal;
            else if (rawValues.Light == 3)
                result.Light = LightLevel.Bright;
            else
                result.Light = LightLevel.Unknown;

            if (rawValues.AirQuality == 0)
                result.AirQuality = AirQualityLevel.Excellent;
            else if (rawValues.AirQuality == 1)
                result.AirQuality = AirQualityLevel.Good;
            else if (rawValues.AirQuality == 2)
                result.AirQuality = AirQualityLevel.Normal;
            else if (rawValues.AirQuality == 3)
                result.AirQuality = AirQualityLevel.Bad;
            else
                result.AirQuality = AirQualityLevel.Unknown;

            if (rawValues.Noise == 0)
                result.Noise = NoiseLevel.Quiet;
            else if (rawValues.Noise == 1)
                result.Noise = NoiseLevel.Normal;
            else if (rawValues.Noise == 2)
                result.Noise = NoiseLevel.Noisy;
            else
                result.Noise = NoiseLevel.Unknown;

            return result;
        }
    }
}
