using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.UI;

namespace TheBox
{
    public class DotStarStrip
    {
        private const int SpiChipSelectLine = 0;
        private SpiDevice spiDevice;
        public string SpiControllerName { get; private set; }
        public int PixelCount { get; private set; }
        private readonly SpiConnectionSettings settings = new SpiConnectionSettings(SpiChipSelectLine)
        {
            ClockFrequency = 10000000,
            Mode = SpiMode.Mode3,
            DataBitLength = 8
        };

        private byte[] startFrame = { 0, 0, 0, 0 };
        private byte[] endFrame;

        private byte[] spiDataBytes;

        public List<Color> strip;

        public DotStarStrip(int numberOfLeds, string spiPort)
        {
            PixelCount = numberOfLeds;
            SpiControllerName = spiPort;
            int endFrameSize = PixelCount / 2 + 1;
            endFrame = new byte[endFrameSize];
            strip = GetResetPixels();
            spiDataBytes = new byte[startFrame.Length + PixelCount * 4 + endFrame.Length];
            for (int i = 0; i < startFrame.Length; i++)
            {
                spiDataBytes[i] = startFrame[i];
            }
        }

        public async Task Begin()
        {
            spiDevice = await GetSpiDevice();
        }

        public List<Color> GetResetPixels()
        {
            return GetResetPixels(Colors.Black);
        }

        public List<Color> GetResetPixels(Color color)
        {
            List<Color> pixels = new List<Color>();
            for (int i = 0; i < PixelCount; i++)
            {
                pixels.Add(color);
            }
            return pixels;
        }

        public void ResetPixels()
        {
            ResetPixels(Colors.Black);
        }

        public void ResetPixels(Color color)
        {
            for (int i = 0; i < strip.Count; i++)
            {
                strip[i] = color;
            }
        }

        public void SendPixels()
        {
            SendPixels(strip);
        }

        public void SendPixels(List<Color> pixels)
        {
            int i = 4;
            for (int j = 0; j < pixels.Count; j++)
            {
                spiDataBytes[i] = (byte)(0xE0 | (byte)(pixels[j].A >> 3));
                i++;
                spiDataBytes[i] = (byte)(pixels[j].B >> 1);
                i++;
                spiDataBytes[i] = (byte)(pixels[j].G >> 1);
                i++;
                spiDataBytes[i] = (byte)(pixels[j].R >> 1);
                i++;
            }
            for (int j = 0; j < endFrame.Length; j++)
            {
                spiDataBytes[i] = endFrame[j];
                i++;
            }
            if (spiDevice != null)
            {
                spiDevice.Write(spiDataBytes);
            }
        }

        // de optimized version
        //public void SendPixels(List<Color> pixels)
        //{
        //    List<byte> spiDataBytes = new List<byte>();
        //    spiDataBytes.AddRange(startFrame);
        //    foreach (Color pixel in pixels)
        //    {
        //        //spiDataBytes.Add(0xE0 | 0x1F);
        //        spiDataBytes.Add((byte)(0xE0 | (byte)(pixel.A >> 3)));
        //        spiDataBytes.Add((byte)(pixel.B >> 1));
        //        spiDataBytes.Add((byte)(pixel.G >> 1));
        //        spiDataBytes.Add((byte)(pixel.R >> 1));
        //    }
        //    spiDataBytes.AddRange(endFrame);
        //    spiDevice.Write(spiDataBytes.ToArray());
        //}

        public void SendPixels(List<byte> pixels)
        {
            List<byte> spiDataBytes = new List<byte>();
            spiDataBytes.AddRange(startFrame);

            foreach (byte pixel in pixels)
            {
                spiDataBytes.Add(0xE0 | 0x1F);
                spiDataBytes.Add((byte)(pixel >> 1));
                spiDataBytes.Add((byte)(pixel >> 1));
                spiDataBytes.Add((byte)(pixel >> 1));
            }
            spiDataBytes.AddRange(endFrame);
            spiDevice.Write(spiDataBytes.ToArray());
        }

        private async Task<SpiDevice> GetSpiDevice()
        {
            string spiSelector = SpiDevice.GetDeviceSelector(SpiControllerName);
            DeviceInformationCollection devicesInfo = await DeviceInformation.FindAllAsync(spiSelector);
            return await SpiDevice.FromIdAsync(devicesInfo[0].Id, settings);
        }
    }
}
