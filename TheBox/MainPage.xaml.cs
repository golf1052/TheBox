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
        const int desiredNumberOfSamples = 8;

        int[] rainbow = {
            0xFF0000, 0xD52A00, 0xAB5500, 0xAB7F00,
            0xABAB00, 0x56D500, 0x00FF00, 0x00D52A,
            0x00AB55, 0x0056AA, 0x0000FF, 0x2A00D5,
            0x5500AB, 0x7F0081, 0xAB0055, 0xD5002B
        };

        public enum ActiveModes
        {
            Volumes,
            SpeedRainbow,
            SpeedAlternating,
            SpeedSingle,
            SpeedTriRainbow
        }
        ActiveModes currentMode;

        public enum IdleModes
        {
            Rainbow,
            Flair
        }
        IdleModes idleMode;

        DotStarStrip leftStrip;
        DotStarStrip rightStrip;

        AudioGraph audioGraph;
        AudioFrameOutputNode frameOutputNode;

        DeviceInformation audioInput;
        //DeviceInformation audioOutput;
        //DeviceInformation raspiAudioOutput;

        Cube cube;

        const int LowCutoff = 15;
        const int MidCutoff = 100;

        private int brightness;
        public int Brightness
        {
            get
            {
                return brightness;
            }
            set
            {
                brightness = value;
                if (cube != null)
                {
                    cube.Brightness = (byte)brightness;
                    cube.Update();
                }
            }
        }

        Random random;

        public bool AutoCycle { get; set; }

        private DateTime lastChange;
        private TimeSpan changeTime;
        private TimeSpan idleChangeTime;
        private int previousPatternValue = 0;
        private int previousIdleValue = 0;

        private DateTime quietTime;
        public bool Idle { get; set; }
        private bool runningIdleAnimation;
        private int previousRandomEdgeValue = 0;

        private bool reverse;
        public bool Reverse
        {
            get
            {
                return reverse;
            }
            set
            {
                reverse = value;
            }
        }

        public MainPage()
        {
            this.InitializeComponent();
            //leftStrip = new DotStarStrip(78, "SPI0");
            //rightStrip = new DotStarStrip(78, "SPI1");
            //cube = new Cube(leftStrip, rightStrip);
            AutoCycle = true;
            Reverse = false;
            random = new Random();
            currentMode = ActiveModes.SpeedRainbow;
            idleMode = IdleModes.Rainbow;
            lastChange = DateTime.UtcNow;
            quietTime = DateTime.UtcNow;
            changeTime = TimeSpan.FromSeconds(15);
            idleChangeTime = TimeSpan.FromSeconds(30);
            Idle = false;
            runningIdleAnimation = false;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var audioInputDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            foreach (var device in audioInputDevices)
            {
                Debug.WriteLine(device.Name);
                // need to select the device to listen on microphone
                if (device.Name.ToLower().Contains("usb"))
                {
                    audioInput = device;
                    break;
                }
                else if (device.Name.ToLower().Contains("microphone"))
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
            //var audioOutputDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioRender);
            //foreach (var device in audioOutputDevices)
            //{
            //    if (device.Name.ToLower().Contains("usb"))
            //    {
            //        audioOutput = device;
            //    }
            //    else
            //    {
            //        raspiAudioOutput = device;
            //    }
            //}
            //if (audioOutput == null)
            //{
            //    Debug.WriteLine("Could not find USB audio card");
            //    return;
            //}
            
            // Set up LED strips
            //await leftStrip.Begin();
            //await rightStrip.Begin();
            //await AudioTest();
            AudioGraphSettings audioGraphSettings = new AudioGraphSettings(AudioRenderCategory.Media);
            audioGraphSettings.DesiredSamplesPerQuantum = 128;
            audioGraphSettings.DesiredRenderDeviceAudioProcessing = AudioProcessing.Default;
            audioGraphSettings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.ClosestToDesired;
            //audioGraphSettings.PrimaryRenderDevice = raspiAudioOutput;
            CreateAudioGraphResult audioGraphResult = await AudioGraph.CreateAsync(audioGraphSettings);
            if (audioGraphResult.Status != AudioGraphCreationStatus.Success)
            {
                Debug.WriteLine("AudioGraph creation failed! " + audioGraphResult.Status);
                return;
            }
            audioGraph = audioGraphResult.Graph;
            Debug.WriteLine(audioGraph.SamplesPerQuantum);
            CreateAudioDeviceInputNodeResult inputNodeResult = await audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Media, audioGraph.EncodingProperties, audioInput);
            if (inputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Debug.WriteLine("AudioDeviceInputNode creation failed! " + inputNodeResult.Status);
                return;
            }
            AudioDeviceInputNode inputNode = inputNodeResult.DeviceInputNode;
            //CreateAudioDeviceOutputNodeResult outputNodeResult = await audioGraph.CreateDeviceOutputNodeAsync();
            //if (outputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            //{
            //    Debug.WriteLine("AudioDeviceOutputNode creation failed!" + outputNodeResult.Status);
            //}
            //AudioDeviceOutputNode outputNode = outputNodeResult.DeviceOutputNode;
            frameOutputNode = audioGraph.CreateFrameOutputNode();
            inputNode.AddOutgoingConnection(frameOutputNode);
            //inputNode.AddOutgoingConnection(outputNode);
            cube.SetSpeedStripLedColors(LedColorLists.rainbowColors);
            audioGraph.QuantumProcessed += AudioGraph_QuantumProcessed;
            audioGraph.UnrecoverableErrorOccurred += AudioGraph_UnrecoverableErrorOccurred;
            audioGraph.Start();
            //outputNode.Start();
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
                    //cube.SetLedColors();
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

        int rainbowMin = 50;
        int rainbowSpeed = 150;
        int rainbowMax = 250;
        bool rainbowGoingDown = false;

        async Task RainbowTest(float average)
        {
            runningIdleAnimation = true;
            while (Idle)
            {
                for (int j = 0; j < rainbow.Length; j++)
                {
                    for (int i = 0; i < leftStrip.PixelCount; i++)
                    {
                        Color color = HexToRgb(rainbow[(i + j) % rainbow.Length]);
                        color.A = (byte)Brightness;
                        leftStrip.strip[i] = color;
                        rightStrip.strip[i] = color;
                    }
                    leftStrip.SendPixels();
                    rightStrip.SendPixels();
                    await Task.Delay(TimeSpan.FromMilliseconds(rainbowSpeed));
                    if (!rainbowGoingDown)
                    {
                        rainbowSpeed++;
                    }
                    else
                    {
                        rainbowSpeed--;
                    }
                    if (rainbowSpeed <= rainbowMin)
                    {
                        rainbowGoingDown = false;
                        rainbowSpeed = rainbowMin;
                    }
                    else if (rainbowSpeed >= rainbowMax)
                    {
                        rainbowGoingDown = true;
                        rainbowSpeed = rainbowMax;
                    }
                    if (!CheckForIdle(average))
                    {
                        break;
                    }
                    ChangeIdleMode();
                    if (idleMode != IdleModes.Rainbow)
                    {
                        break;
                    }
                }
            }
            runningIdleAnimation = false;
        }

        void Flair(float average)
        {
            runningIdleAnimation = true;
            while (Idle)
            {
                int randomEdge = random.Next(12);
                while (randomEdge == previousRandomEdgeValue)
                {
                    randomEdge = random.Next(12);
                }
                previousRandomEdgeValue = randomEdge;
                ChangeReverse();
                if (randomEdge == 0)
                {
                    RunFlair(cube.bottomFrontEdge);
                }
                else if (randomEdge == 1)
                {
                    RunFlair(cube.bottomRightEdge);
                }
                else if (randomEdge == 2)
                {
                    RunFlair(cube.bottomBackEdge);
                }
                else if (randomEdge == 3)
                {
                    RunFlair(cube.bottomLeftEdge);
                }
                else if (randomEdge == 4)
                {
                    RunFlair(cube.frontLeftEdge);
                }
                else if (randomEdge == 5)
                {
                    RunFlair(cube.rightLeftEdge);
                }
                else if (randomEdge == 6)
                {
                    RunFlair(cube.backLeftEdge);
                }
                else if (randomEdge == 7)
                {
                    RunFlair(cube.leftLeftEdge);
                }
                else if (randomEdge == 8)
                {
                    RunFlair(cube.frontTopEdge);
                }
                else if (randomEdge == 9)
                {
                    RunFlair(cube.rightTopEdge);
                }
                else if (randomEdge == 10)
                {
                    RunFlair(cube.backTopEdge);
                }
                else if (randomEdge == 11)
                {
                    RunFlair(cube.leftTopEdge);
                }
                if (!CheckForIdle(average))
                {
                    break;
                }
                ChangeIdleMode();
                if (idleMode != IdleModes.Flair)
                {
                    break;
                }
            }
            runningIdleAnimation = false;
        }

        private void RunFlair(Edge edge)
        {
            int randomColor = random.Next(3);
            List<Color> ledColors = new List<Color>();
            if (randomColor == 0)
            {
                ledColors = LedColorLists.redFlair;
            }
            else if (randomColor == 1)
            {
                ledColors = LedColorLists.greenFlair;
            }
            else if (randomColor == 2)
            {
                ledColors = LedColorLists.blueFlair;
            }
            do
            {
                edge.StepFlair(ledColors, Reverse);
                Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
            }
            while (edge.flairOffset != 0);
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

        private async void AudioGraph_QuantumProcessed(AudioGraph sender, object args)
        {
            AudioFrame audioFrame = frameOutputNode.GetFrame();
            List<float[]> amplitudeData = ProcessFrameOutput(audioFrame);
            List<float[]> channelData = GetFftData(ConvertToX(amplitudeData, desiredNumberOfSamples));
            for (int i = 0; i < channelData.Count / 2; i++)
            {
                float[] leftChannel = channelData[i];
                float[] rightChannel = channelData[i + 1];
                float average = HelperMethods.Average(leftChannel);
                CheckForIdle(average);
                if (!Idle)
                {
                    if (currentMode == ActiveModes.Volumes)
                    {
                        LowMidHighVolumeBars(leftChannel, rightChannel);
                    }
                    else
                    {
                        LowMidHighSpeedBars(leftChannel, rightChannel);
                    }
                }
                else
                {
                    if (!runningIdleAnimation)
                    {
                        Brightness = 127;
                        UpdateSliderBrightness();
                        cube.Reset();
                        if (idleMode == IdleModes.Rainbow)
                        {
                            await RainbowTest(average);
                        }
                        else if (idleMode == IdleModes.Flair)
                        {
                            Flair(average);
                        }
                    }
                }
            }
            if (AutoCycle && !Idle)
            {
                if (DateTime.UtcNow - lastChange >= changeTime)
                {
                    int randomPattern = random.Next(5);
                    while (randomPattern == previousPatternValue)
                    {
                        randomPattern = random.Next(5);
                    }
                    previousPatternValue = randomPattern;
                    lastChange = DateTime.UtcNow;
                    if (randomPattern == 0)
                    {
                        volumesButton_Click(null, null);
                    }
                    else if (randomPattern == 1)
                    {
                        speedRainbowButton_Click(null, null);
                    }
                    else if (randomPattern == 2)
                    {
                        speedAlternatingButton_Click(null, null);
                    }
                    else if (randomPattern == 3)
                    {
                        speedSingleButton_Click(null, null);
                    }
                    else if (randomPattern == 4)
                    {
                        speedTriRainbowButton_Click(null, null);
                    }
                    ChangeReverse();
                }
            }
        }

        private void ChangeIdleMode()
        {
            if (AutoCycle)
            {
                if (DateTime.UtcNow - lastChange >= idleChangeTime)
                {
                    int randomIdle = random.Next(2);
                    while (randomIdle == previousIdleValue)
                    {
                        randomIdle = random.Next(2);
                    }
                    previousIdleValue = randomIdle;
                    lastChange = DateTime.UtcNow;
                    if (randomIdle == 0)
                    {
                        idleMode = IdleModes.Rainbow;
                    }
                    else if (randomIdle == 1)
                    {
                        idleMode = IdleModes.Flair;
                    }
                    ChangeReverse();
                }
            }
        }

        public void ChangeReverse()
        {
            int randomReverse = random.Next(2);
            if (randomReverse == 0)
            {
                if (Reverse)
                {
                    Reverse = false;
                    cube.Reverse = Reverse;
                    cube.ReverseSpeedStrip = Reverse;
                    DoUIThingSync(() =>
                    {
                        reverseCheckbox.IsChecked = Reverse;
                    });
                }
            }
            else if (randomReverse == 1)
            {
                if (!Reverse)
                {
                    Reverse = true;
                    cube.Reverse = Reverse;
                    cube.ReverseSpeedStrip = Reverse;
                    DoUIThingSync(() =>
                    {
                        reverseCheckbox.IsChecked = Reverse;
                    });
                }
            }
        }

        bool CheckForIdle(float average)
        {
            if (average < 0.0007)
            {
                if (DateTime.UtcNow - quietTime >= TimeSpan.FromSeconds(10))
                {
                    Idle = true;
                    return true;
                }
            }
            else
            {
                quietTime = DateTime.UtcNow;
                if (Idle)
                {
                    Idle = false;
                }
            }
            return false;
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
            cube.Update();
        }

        List<float[]> ConvertToX(List<float[]> channelData, int numberOfSamples)
        {
            List<float[]> newChannelData = new List<float[]>();
            float[] leftChannel = channelData[0];
            float[] rightChannel = channelData[1];
            if (numberOfSamples > leftChannel.Length)
            {
                for (int i = 0; i < leftChannel.Length / audioGraph.SamplesPerQuantum; i++)
                {
                    float[] tmpLeftChannelData = new float[numberOfSamples];
                    float[] tmpRightChannelData = new float[numberOfSamples];

                    // copy the left and right channel data into a new array
                    for (int j = i * audioGraph.SamplesPerQuantum; j < (i + 1) * audioGraph.SamplesPerQuantum; j++)
                    {
                        tmpLeftChannelData[j % audioGraph.SamplesPerQuantum] = leftChannel[j];
                        tmpRightChannelData[j % audioGraph.SamplesPerQuantum] = rightChannel[j];
                    }

                    // then pad the rest with 0s till we get to the desired number of samples
                    for (int j = audioGraph.SamplesPerQuantum; j < numberOfSamples; j++)
                    {
                        tmpLeftChannelData[j] = 0;
                        tmpRightChannelData[j] = 0;
                    }
                    newChannelData.Add(tmpLeftChannelData);
                    newChannelData.Add(tmpRightChannelData);
                }
            }
            else
            {
                float[] tmpLeftChannelData = new float[numberOfSamples];
                float[] tmpRightChannelData = new float[numberOfSamples];
                for (int i = 0; i < leftChannel.Length / numberOfSamples; i++)
                {
                    tmpLeftChannelData[i] = HelperMethods.Average(leftChannel, i * numberOfSamples, (i + 1) * numberOfSamples);
                    tmpRightChannelData[i] = HelperMethods.Average(rightChannel, i * numberOfSamples, (i + 1) * numberOfSamples);
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

        async Task DoUIThing(Action func)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                func.Invoke();
            });
        }

        void DoUIThingSync(Action func)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                func.Invoke();
            }).AsTask().Wait();
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
            ResetLevels();
            currentMode = ActiveModes.Volumes;
            cube.SetColor(Colors.Black);
            cube.Update();
            Brightness = 30;
            UpdateSliderBrightness();
        }

        private void speedRainbowButton_Click(object sender, RoutedEventArgs e)
        {
            cube.SetSpeedStripLedColors(LedColorLists.rainbowColors);
            currentMode = ActiveModes.SpeedRainbow;
            cube.SetColor(Colors.Black);
            cube.Update();
            Brightness = 127;
            UpdateSliderBrightness();
        }

        private void speedAlternatingButton_Click(object sender, RoutedEventArgs e)
        {
            cube.bottomZone.SetSpeedStripLedColors(LedColorLists.redAlternating);
            cube.midZone.SetSpeedStripLedColors(LedColorLists.greenAlternating);
            cube.topZone.SetSpeedStripLedColors(LedColorLists.blueAlternating);
            currentMode = ActiveModes.SpeedAlternating;
            cube.SetColor(Colors.Black);
            cube.Update();
            Brightness = 127;
            UpdateSliderBrightness();
        }

        private void speedSingleButton_Click(object sender, RoutedEventArgs e)
        {
            cube.bottomZone.SetSpeedStripLedColors(LedColorLists.redSingle);
            cube.midZone.SetSpeedStripLedColors(LedColorLists.greenSingle);
            cube.topZone.SetSpeedStripLedColors(LedColorLists.blueSingle);
            currentMode = ActiveModes.SpeedSingle;
            cube.SetColor(Colors.Black);
            cube.Update();
            Brightness = 255;
            UpdateSliderBrightness();
        }

        private void speedTriRainbowButton_Click(object sender, RoutedEventArgs e)
        {
            cube.bottomZone.SetSpeedStripLedColors(LedColorLists.redRainbow);
            cube.midZone.SetSpeedStripLedColors(LedColorLists.greenRainbow);
            cube.topZone.SetSpeedStripLedColors(LedColorLists.blueRainbow);
            currentMode = ActiveModes.SpeedTriRainbow;
            cube.SetColor(Colors.Black);
            cube.Update();
            Brightness = 127;
            UpdateSliderBrightness();
        }

        void UpdateSliderBrightness()
        {
            DoUIThingSync(() =>
            {
                brightnessSlider.Value = Brightness;
            });
        }

        void UpdateAutoCycle()
        {
            DoUIThingSync(() =>
            {
                autoCycleCheckbox.IsChecked = AutoCycle;
            });
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetLevels();
        }

        void ResetLevels()
        {
            cube.ResetMaxes();
        }

        private void autoCycleCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (autoCycleCheckbox.IsChecked.HasValue && autoCycleCheckbox.IsChecked.Value)
            {
                AutoCycle = true;
            }
            else if (autoCycleCheckbox.IsChecked.HasValue && !autoCycleCheckbox.IsChecked.Value)
            {
                AutoCycle = false;
            }
            else
            {
                AutoCycle = true;
            }
        }

        private void reverseCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (reverseCheckbox.IsChecked.HasValue && reverseCheckbox.IsChecked.Value)
            {
                Reverse = true;
                if (cube != null)
                {
                    cube.ReverseSpeedStrip = Reverse;
                }
                
            }
            else
            {
                Reverse = false;
                if (cube != null)
                {
                    cube.ReverseSpeedStrip = Reverse;
                }
            }
        }
    }
}
