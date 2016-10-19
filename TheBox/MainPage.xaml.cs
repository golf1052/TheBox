using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Render;
using Windows.UI.Xaml.Shapes;
using System.Text;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TheBox
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        int[] rainbow = {
            0xFF0000, 0xD52A00, 0xAB5500, 0xAB7F00,
            0xABAB00, 0x56D500, 0x00FF00, 0x00D52A,
            0x00AB55, 0x0056AA, 0x0000FF, 0x2A00D5,
            0x5500AB, 0x7F0081, 0xAB0055, 0xD5002B
        };

        public enum Modes
        {
            Volumes,
            SpeedRainbow,
            SpeedAlternating
        }
        Modes currentMode;

        DotStarStrip leftStrip;
        DotStarStrip rightStrip;

        AudioGraph audioGraph;
        AudioFrameOutputNode frameOutputNode;
        static bool doneProcessing = true;

        DeviceInformation audioInput;
        DeviceInformation audioOutput;
        DeviceInformation raspiAudioOutput;

        DateTime lastBeat = DateTime.MinValue;
        TimeSpan timeBetweenBeats = TimeSpan.Zero;
        float beatValue = 0;
        BeatDetector beatDetector;

        Cube cube;

        List<Rectangle> rectangles;
        List<AdjustableMax> maxes;

        const int LowCutoff = 15;
        const int MidCutoff = 100;

        public MainPage()
        {
            this.InitializeComponent();
            leftStrip = new DotStarStrip(78, "SPI0");
            rightStrip = new DotStarStrip(78, "SPI1");
            cube = new Cube(leftStrip, rightStrip);
            rectangles = new List<Rectangle>();
            maxes = new List<AdjustableMax>();
            beatDetector = new BeatDetector(50);
            for (int i = 0; i < 220; i++)
            {
                Rectangle rect = new Rectangle();
                if (i < LowCutoff)
                {
                    rect.Fill = new SolidColorBrush(Colors.Red);
                }
                else if (i < MidCutoff)
                {
                    rect.Fill = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    rect.Fill = new SolidColorBrush(Colors.CornflowerBlue);
                }
                rect.HorizontalAlignment = HorizontalAlignment.Left;
                rect.VerticalAlignment = VerticalAlignment.Bottom;
                rect.Width = 8;
                rect.Height = 0;
                rect.Margin = new Thickness(i * 8, 0, 0, 0);
                rectangleGrid.Children.Add(rect);
                rectangles.Add(rect);
                maxes.Add(new AdjustableMax());
            }
            currentMode = Modes.SpeedRainbow;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var audioInputDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            foreach (var device in audioInputDevices)
            {
                if (device.Name.ToLower().Contains("usb"))
                {
                    audioInput = device;
                    break;
                }
            }
            if (audioInput == null)
            {
                Debug.WriteLine("Could not find USB audio card");
                return;
            }
            var audioOutputDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioRender);
            foreach (var device in audioOutputDevices)
            {
                if (device.Name.ToLower().Contains("usb"))
                {
                    audioOutput = device;
                }
                else
                {
                    raspiAudioOutput = device;
                }
            }
            if (audioOutput == null)
            {
                Debug.WriteLine("Could not find USB audio card");
                return;
            }
            
            // Set up LED strips
            await leftStrip.Begin();
            await rightStrip.Begin();
            //await AudioTest();
            AudioGraphSettings audioGraphSettings = new AudioGraphSettings(AudioRenderCategory.Media);
            audioGraphSettings.DesiredSamplesPerQuantum = 440;
            audioGraphSettings.DesiredRenderDeviceAudioProcessing = AudioProcessing.Default;
            audioGraphSettings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.ClosestToDesired;
            audioGraphSettings.PrimaryRenderDevice = raspiAudioOutput;
            CreateAudioGraphResult audioGraphResult = await AudioGraph.CreateAsync(audioGraphSettings);
            if (audioGraphResult.Status != AudioGraphCreationStatus.Success)
            {
                Debug.WriteLine("AudioGraph creation failed! " + audioGraphResult.Status);
                return;
            }
            audioGraph = audioGraphResult.Graph;
            //Debug.WriteLine(audioGraph.SamplesPerQuantum);
            CreateAudioDeviceInputNodeResult inputNodeResult = await audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Media, audioGraph.EncodingProperties, audioInput);
            if (inputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Debug.WriteLine("AudioDeviceInputNode creation failed! " + inputNodeResult.Status);
                return;
            }
            AudioDeviceInputNode inputNode = inputNodeResult.DeviceInputNode;
            CreateAudioDeviceOutputNodeResult outputNodeResult = await audioGraph.CreateDeviceOutputNodeAsync();
            if (outputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Debug.WriteLine("AudioDeviceOutputNode creation failed!" + outputNodeResult.Status);
            }
            AudioDeviceOutputNode outputNode = outputNodeResult.DeviceOutputNode;
            frameOutputNode = audioGraph.CreateFrameOutputNode();
            inputNode.AddOutgoingConnection(frameOutputNode);
            inputNode.AddOutgoingConnection(outputNode);
            cube.SetSpeedStripLedColors(LedColorLists.rainbowColors);
            audioGraph.QuantumProcessed += AudioGraph_QuantumProcessed;
            // z = sin(sqrt(x2+y2)) from 0 to 2p1
            audioGraph.UnrecoverableErrorOccurred += AudioGraph_UnrecoverableErrorOccurred;
            audioGraph.Start();
            outputNode.Start();
            inputNode.Start();
            frameOutputNode.Start();
            cube.Reset();
            cube.Update();
            //await MathFunc();
            //cube.ApplyColorFunction((x, y, z) =>
            //{
            //    Color c = Color.FromArgb(255,
            //        (byte)((x / 14.0) * 255.0),
            //        (byte)((y / 14.0) * 255.0),
            //        (byte)((z / 14.0) * 255.0));
            //    return c;
            //});
            //cube.SetLedColors();
            //cube.Update();
            //cube.bottomFrontEdge.SetColor(Colors.Red);
            //cube.bottomRightEdge.SetColor(Colors.OrangeRed);
            //cube.bottomBackEdge.SetColor(Colors.Yellow);
            //cube.bottomLeftEdge.SetColor(Colors.Green);
            //cube.frontLeftEdge.SetColor(Colors.Blue);
            //cube.frontTopEdge.SetColor(Colors.Purple);
            //cube.rightLeftEdge.Brightness = 10;
            //cube.rightLeftEdge.SetColor(Colors.Red);
            //cube.rightTopEdge.Brightness = 10;
            //cube.rightTopEdge.SetColor(Colors.OrangeRed);
            //cube.backLeftEdge.Brightness = 10;
            //cube.backLeftEdge.SetColor(Colors.Yellow);
            //cube.backTopEdge.Brightness = 10;
            //cube.backTopEdge.SetColor(Colors.Green);
            //cube.leftLeftEdge.Brightness = 10;
            //cube.leftLeftEdge.SetColor(Colors.Blue);
            //cube.leftTopEdge.Brightness = 10;
            //cube.leftTopEdge.SetColor(Colors.Purple);
            //cube.Update();
            //await RainbowTest();
            //cube.Brightness = 30;
            //await FlashTest();
            //SetAll();
            //await FadeTest();
            //cube.Reset();
            //cube.Update();
            //await cube.rightLeftEdge.DoLine();
            //ZackTest();
        }

        async Task MathFunc()
        {
            // z = sin(sqrt(x^2+y^2)) from 0 to 2p1
            while (true)
            {
                for (int i = 1; i < 32; i++)
                {
                    var j = Math.Abs(i - 16.0);
                    cube.ApplyColorFunction((x, y, z) =>
                    {
                        var xp = x / 14.0 * 2 * Math.PI; // 0, 2PI
                        var yp = y / 14.0 * 2 * Math.PI; // 0, 2PI
                        var zc = Math.Abs(Math.Sin(Math.Sqrt(Math.Pow(xp, 2) + Math.Pow(yp, 2))));
                        var phase = j * 16;
                        if (phase == 256)
                        {
                            phase = 255;
                        }
                        Color c = Color.FromArgb(255,
                            (byte)((((Math.Abs(x - 7.0) / 7.0) * 255.0) + phase) % 255.0),
                            (byte)((((Math.Abs(y - 7.0) / 7.0) * 255.0) + phase) % 255.0),
                            (byte)(((zc * 255.0) + phase) % 255.0));
                        return c;
                    });
                    cube.SetLedColors();
                    cube.Update();
                    await Task.Delay(TimeSpan.FromMilliseconds(16));
                }
            }
            
        }

        void ZackTest()
        {
            while (true)
            {
                cube.SetColor(Colors.Red);
                cube.Update();
                cube.SetColor(Colors.Green);
                cube.Update();
                cube.SetColor(Colors.Blue);
                cube.Update();
            }
        }

        async Task FadeTest()
        {
            bool ascending = false;
            cube.SetColor(Colors.Red);
            while (true)
            {
                if (ascending)
                {
                    cube.Brightness++;
                    if (cube.Brightness >= 255)
                    {
                        ascending = false;
                        cube.Brightness = 255;
                    }
                }
                else
                {
                    cube.Brightness--;
                    if (cube.Brightness <= 0)
                    {
                        ascending = true;
                        cube.Brightness = 0;
                    }
                }
                cube.Update();
                await Task.Delay(TimeSpan.FromMilliseconds(1/120));
            }
        }

        void SetAll()
        {
            cube.SetColor(Colors.Red);
            cube.Update();
        }

        async Task FlashTest()
        {
            Random random = new Random();

            while (true)
            {
                cube.top.Reset();
                cube.bottom.SetColor(RandomColor());
                cube.Update();
                await Task.Delay(TimeSpan.FromMilliseconds(500));

                cube.bottom.Reset();
                cube.front.SetColor(RandomColor());
                cube.Update();
                await Task.Delay(TimeSpan.FromMilliseconds(500));

                cube.front.Reset();
                cube.right.SetColor(RandomColor());
                cube.Update();
                await Task.Delay(TimeSpan.FromMilliseconds(500));

                cube.right.Reset();
                cube.back.SetColor(RandomColor());
                cube.Update();
                await Task.Delay(TimeSpan.FromMilliseconds(500));

                cube.back.Reset();
                cube.left.SetColor(RandomColor());
                cube.Update();
                await Task.Delay(TimeSpan.FromMilliseconds(500));

                cube.left.Reset();
                cube.top.SetColor(RandomColor());
                cube.Update();
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }

        Color RandomColor()
        {
            Random random = new Random();
            return Color.FromArgb(255, (byte)random.Next(0, 256), (byte)random.Next(0, 256), (byte)random.Next(256));
        }

        async Task RainbowTest()
        {
            while (true)
            {
                for (int j = 0; j < rainbow.Length; j++)
                {
                    for (int i = 0; i < leftStrip.PixelCount; i++)
                    {
                        Color color = HexToRgb(rainbow[(i + j) % rainbow.Length]);
                        color.A = 255;
                        leftStrip.strip[i] = color;
                        rightStrip.strip[i] = color;
                    }
                    leftStrip.SendPixels();
                    rightStrip.SendPixels();
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
            }
        }

        async Task AudioTest()
        {
            var initSettings = new MediaCaptureInitializationSettings();
            var mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();
            var storageFile = await KnownFolders.VideosLibrary.CreateFileAsync("audioOut.mp3", CreationCollisionOption.GenerateUniqueName);
            var profile = MediaEncodingProfile.CreateM4a(AudioEncodingQuality.Auto);
            await mediaCapture.StartRecordToStorageFileAsync(profile, storageFile);
            await Task.Delay(TimeSpan.FromSeconds(5));
            await mediaCapture.StopRecordAsync();
            var stream = await storageFile.OpenAsync(FileAccessMode.Read);
            if (stream != null)
            {
                mediaElement.SetSource(stream, storageFile.ContentType);
                mediaElement.Play();
            }
        }

        private void AudioGraph_UnrecoverableErrorOccurred(AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
        {
            Debug.WriteLine("UNRECOVERABLE ERRORRRRRR");
        }

        private void AudioGraph_QuantumProcessed(AudioGraph sender, object args)
        {
            AudioFrame audioFrame = frameOutputNode.GetFrame();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            List<float[]> amplitudeData = ProcessFrameOutput(audioFrame);
            List<float[]> channelData = GetFftData(ConvertTo512(amplitudeData));
            stopwatch.Stop();
            if (channelData.Count == 0)
            {
                doneProcessing = true;
                return;
            }
            for (int i = 0; i < channelData.Count / 2; i++)
            {
                float[] leftChannel = channelData[i];
                float[] rightChannel = channelData[i + 1];
                if (currentMode == Modes.Volumes)
                {
                    LowMidHighVolumeBars(leftChannel, rightChannel);
                }
                else
                {
                    LowMidHighSpeedBars(leftChannel, rightChannel);
                }
            }
        }

        void LowMidHighSpeedBars(float[] leftChannel, float[] rightChannel)
        {
            float leftLowAverage = HelperMethods.Average(leftChannel, 0, LowCutoff);
            float rightLowAverage = HelperMethods.Average(rightChannel, 0, LowCutoff);
            float leftMidAverage = HelperMethods.Average(leftChannel, LowCutoff, MidCutoff);
            float rightMidAverage = HelperMethods.Average(rightChannel, LowCutoff, MidCutoff);
            float leftHighAverage = HelperMethods.Average(leftChannel, MidCutoff);
            float rightHighAverage = HelperMethods.Average(rightChannel, MidCutoff);
            cube.bottomFrontEdge.speedStripBeginning.UpdateSpeed(leftLowAverage);
            cube.bottomFrontEdge.speedStripEnd.UpdateSpeed(rightLowAverage);
            cube.bottomRightEdge.speedStripBeginning.UpdateSpeed(leftLowAverage);
            cube.bottomRightEdge.speedStripEnd.UpdateSpeed(rightLowAverage);
            cube.bottomBackEdge.speedStripBeginning.UpdateSpeed(leftLowAverage);
            cube.bottomBackEdge.speedStripEnd.UpdateSpeed(rightLowAverage);
            cube.bottomLeftEdge.speedStripBeginning.UpdateSpeed(leftLowAverage);
            cube.bottomLeftEdge.speedStripEnd.UpdateSpeed(rightLowAverage);
            cube.frontLeftEdge.speedStripBeginning.UpdateSpeed(leftMidAverage);
            cube.frontLeftEdge.speedStripEnd.UpdateSpeed(rightMidAverage);
            cube.rightLeftEdge.speedStripBeginning.UpdateSpeed(leftMidAverage);
            cube.rightLeftEdge.speedStripEnd.UpdateSpeed(rightMidAverage);
            cube.backLeftEdge.speedStripBeginning.UpdateSpeed(leftMidAverage);
            cube.backLeftEdge.speedStripEnd.UpdateSpeed(rightMidAverage);
            cube.leftLeftEdge.speedStripBeginning.UpdateSpeed(leftMidAverage);
            cube.leftLeftEdge.speedStripEnd.UpdateSpeed(rightMidAverage);
            cube.frontTopEdge.speedStripBeginning.UpdateSpeed(leftHighAverage);
            cube.frontTopEdge.speedStripEnd.UpdateSpeed(rightHighAverage);
            cube.rightTopEdge.speedStripBeginning.UpdateSpeed(leftHighAverage);
            cube.rightTopEdge.speedStripEnd.UpdateSpeed(rightHighAverage);
            cube.backTopEdge.speedStripBeginning.UpdateSpeed(leftHighAverage);
            cube.backTopEdge.speedStripEnd.UpdateSpeed(rightHighAverage);
            cube.leftTopEdge.speedStripBeginning.UpdateSpeed(leftHighAverage);
            cube.leftTopEdge.speedStripEnd.UpdateSpeed(rightHighAverage);
        }

        void LowMidHighVolumeBars(float[] leftChannel, float[] rightChannel)
        {
            float leftLowAverage = HelperMethods.Average(leftChannel, 0, LowCutoff);
            float rightLowAverage = HelperMethods.Average(rightChannel, 0, LowCutoff);
            float leftMidAverage = HelperMethods.Average(leftChannel, LowCutoff, MidCutoff);
            float rightMidAverage = HelperMethods.Average(rightChannel, LowCutoff, MidCutoff);
            float leftHighAverage = HelperMethods.Average(leftChannel, MidCutoff);
            float rightHighAverage = HelperMethods.Average(rightChannel, MidCutoff);

            // set strip sides to repeating patterns, increase pattern loop speed based upon loudness of level
            beatDetector.UpdateBeat((leftLowAverage + rightLowAverage) / 2);
            if (beatDetector.Beat)
            {
                cube.SetColor(Colors.White);
            }
            else
            {
                cube.Reset();
                cube.bottomFrontEdge.UpdateLeft(leftLowAverage, Colors.Red);
                cube.bottomFrontEdge.UpdateRight(rightLowAverage, Colors.Red);
                cube.bottomRightEdge.UpdateLeft(leftLowAverage, Colors.Red);
                cube.bottomRightEdge.UpdateRight(rightLowAverage, Colors.Red);
                cube.bottomBackEdge.UpdateLeft(leftLowAverage, Colors.Red);
                cube.bottomBackEdge.UpdateRight(rightLowAverage, Colors.Red);
                cube.bottomLeftEdge.UpdateLeft(leftLowAverage, Colors.Red);
                cube.bottomLeftEdge.UpdateRight(rightLowAverage, Colors.Red);
                cube.frontLeftEdge.UpdateLeft(leftMidAverage, Colors.Green);
                cube.frontLeftEdge.UpdateRight(rightMidAverage, Colors.Green);
                cube.rightLeftEdge.UpdateLeft(leftMidAverage, Colors.Green);
                cube.rightLeftEdge.UpdateRight(rightMidAverage, Colors.Green);
                cube.backLeftEdge.UpdateLeft(leftMidAverage, Colors.Green);
                cube.backLeftEdge.UpdateRight(rightMidAverage, Colors.Green);
                cube.leftLeftEdge.UpdateLeft(leftMidAverage, Colors.Green);
                cube.leftLeftEdge.UpdateRight(rightMidAverage, Colors.Green);
                cube.frontTopEdge.UpdateLeft(leftHighAverage, Colors.Blue);
                cube.frontTopEdge.UpdateRight(rightHighAverage, Colors.Blue);
                cube.rightTopEdge.UpdateLeft(leftHighAverage, Colors.Blue);
                cube.rightTopEdge.UpdateRight(rightHighAverage, Colors.Blue);
                cube.backTopEdge.UpdateLeft(leftHighAverage, Colors.Blue);
                cube.backTopEdge.UpdateRight(rightHighAverage, Colors.Blue);
                cube.leftTopEdge.UpdateLeft(leftHighAverage, Colors.Blue);
                cube.leftTopEdge.UpdateRight(rightHighAverage, Colors.Blue);
            }
            cube.Update();
        }

        async Task PrintToLog(string str)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        listView.Items.Add(str);
                        listView.ScrollIntoView(listView.Items.Last());
                    });
        }

        async Task DoUIThing(Action func)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                func.Invoke();
            });
        }

        List<float[]> ConvertTo512(List<float[]> channelData)
        {
            List<float[]> newChannelData = new List<float[]>();
            float[] leftChannel = channelData[0];
            float[] rightChannel = channelData[1];
            for (int i = 0; i < leftChannel.Length / audioGraph.SamplesPerQuantum; i++)
            {
                float[] tmpLeftChannelData = new float[512];
                float[] tmpRightChannelData = new float[512];

                // copy the left and right channel data into a new array
                for (int j = i * audioGraph.SamplesPerQuantum; j < (i + 1) * audioGraph.SamplesPerQuantum; j++)
                {
                    tmpLeftChannelData[j % audioGraph.SamplesPerQuantum] = leftChannel[j];
                    tmpRightChannelData[j % audioGraph.SamplesPerQuantum] = rightChannel[j];
                }

                // then pad the rest with 0s till we get to 512
                for (int j = audioGraph.SamplesPerQuantum; j < 512; j++)
                {
                    tmpLeftChannelData[j] = 0;
                    tmpRightChannelData[j] = 0;
                }
                newChannelData.Add(tmpLeftChannelData);
                newChannelData.Add(tmpRightChannelData);
            }
            return newChannelData;
        }

        List<float[]> GetFftData(List<float[]> channelData)
        {
            List<float[]> fftData = new List<float[]>();
            for (int i = 0; i < channelData.Count / 2; i++)
            {
                float[] leftChannel = GetFftChannelData(channelData[i]);
                float[] rightChannel = GetFftChannelData(channelData[i + 1]);
                fftData.Add(leftChannel);
                fftData.Add(rightChannel);
            }
            return fftData;
        }

        float[] GetFftChannelData(float[] channelData)
        {
            Complex[] fftData = new Complex[512];
            for (int j = 0; j < fftData.Length; j++)
            {
                Complex c = new Complex();
                c.Re = channelData[j] * (float)Fft.HannWindow(j, fftData.Length);
                fftData[j] = c;
            }
            Fft.FFT(fftData, Fft.Direction.Forward);
            float[] fftResult = new float[audioGraph.SamplesPerQuantum / 2];
            for (int j = 0; j < fftResult.Length; j++)
            {
                fftResult[j] = Math.Abs((float)Math.Sqrt(Math.Pow(fftData[j].Re, 2) + Math.Pow(fftData[j].Im, 2)));
            }
            return fftResult;
        }

        int GammaCorrection(float val, float gamma = 2.8f, int maxInput = 255, int maxOutput = 255)
        {
            return (int)(Math.Pow(val / (float)maxInput, gamma) * (float)maxOutput + 0.5f);
        }

        void PrintArray(float[] array)
        {
            StringBuilder str = new StringBuilder();
            str.Append("[");
            for (int i = 0; i < array.Length; i++)
            {
                if (i != array.Length - 1)
                {
                    str.Append(array[i]).Append(", ");
                }
                else
                {
                    str.Append(array[i]);
                }
            }
            str.Append("]");
            Debug.WriteLine(str.ToString());
        }

        Color HexToRgb(int hex)
        {
            byte r = (byte)((hex & 0xFF0000) >> (2 * 8));
            byte g = (byte)((hex & 0x00FF00) >> (1 * 8));
            byte b = (byte)(hex & 0x0000FF);
            return Color.FromArgb(255, r, g, b);
        }

        unsafe private List<float[]> ProcessFrameOutput(AudioFrame frame)
        {
            using (AudioBuffer audioBuffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = audioBuffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;
                float* dataInFloat;

                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);
                dataInFloat = (float*)dataInBytes;
                uint dataInFloatLength = audioBuffer.Length / sizeof(float);
                List<float[]> channelData = new List<float[]>();
                float[] leftChannel = new float[dataInFloatLength / 2];
                float[] rightChannel = new float[dataInFloatLength / 2];
                channelData.Add(leftChannel);
                channelData.Add(rightChannel);
                if (dataInFloatLength > 0)
                {
                    int channelCount = 0;
                    for (int i = 0; i < dataInFloatLength; i++)
                    {
                        float datum = dataInFloat[i];
                        if (i % 2 == 0)
                        {
                            leftChannel[channelCount] = datum;
                        }
                        else
                        {
                            rightChannel[channelCount] = datum;
                            channelCount++;
                        }
                    }
                }
                return channelData;
            }
        }

        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        private void brightnessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (cube != null)
            {
                cube.Brightness = (byte)e.NewValue;
                cube.Update();
            }
        }

        private void volumesButton_Click(object sender, RoutedEventArgs e)
        {
            currentMode = Modes.Volumes;
            cube.SetColor(Colors.Black);
            cube.Update();
        }

        private void speedRainbowButton_Click(object sender, RoutedEventArgs e)
        {
            cube.SetSpeedStripLedColors(LedColorLists.rainbowColors);
            currentMode = Modes.SpeedRainbow;
            cube.SetColor(Colors.Black);
            cube.Update();
        }

        private void speedAlternatingButton_Click(object sender, RoutedEventArgs e)
        {
            cube.bottomFrontEdge.SetSpeedStripLedColors(LedColorLists.redTest);
            cube.bottomRightEdge.SetSpeedStripLedColors(LedColorLists.redTest);
            cube.bottomBackEdge.SetSpeedStripLedColors(LedColorLists.redTest);
            cube.bottomLeftEdge.SetSpeedStripLedColors(LedColorLists.redTest);
            cube.frontLeftEdge.SetSpeedStripLedColors(LedColorLists.greenTest);
            cube.rightLeftEdge.SetSpeedStripLedColors(LedColorLists.greenTest);
            cube.backLeftEdge.SetSpeedStripLedColors(LedColorLists.greenTest);
            cube.leftLeftEdge.SetSpeedStripLedColors(LedColorLists.greenTest);
            cube.frontTopEdge.SetSpeedStripLedColors(LedColorLists.blueTest);
            cube.rightTopEdge.SetSpeedStripLedColors(LedColorLists.blueTest);
            cube.backTopEdge.SetSpeedStripLedColors(LedColorLists.blueTest);
            cube.leftTopEdge.SetSpeedStripLedColors(LedColorLists.blueTest);
            currentMode = Modes.SpeedAlternating;
            cube.SetColor(Colors.Black);
            cube.Update();
        }
    }
}
