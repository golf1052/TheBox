﻿using System;
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

        Cube cube;

        List<Rectangle> rectangles;

        public MainPage()
        {
            this.InitializeComponent();
            leftStrip = new DotStarStrip(78, "SPI0");
            rightStrip = new DotStarStrip(78, "SPI1");
            cube = new Cube(leftStrip, rightStrip);
            rectangles = new List<Rectangle>();
            for (int i = 0; i < 220; i++)
            {
                Rectangle rect = new Rectangle();
                if (i < 15)
                {
                    rect.Fill = new SolidColorBrush(Colors.Red);
                }
                else if (i < 100)
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
            }
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
            //if (!doneProcessing)
            //{
            //    return;
            //}
            //doneProcessing = false;
            //await PrintToLog("processed");
            AudioFrame audioFrame = frameOutputNode.GetFrame();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            List<float[]> amplitudeData = ProcessFrameOutput(audioFrame);
            List<float[]> channelData = GetFftData(ConvertTo512(amplitudeData));
            stopwatch.Stop();
            //await PrintToLog(stopwatch.ElapsedMilliseconds.ToString());
            if (channelData.Count == 0)
            {
                doneProcessing = true;
                return;
            }
            for (int i = 0; i < channelData.Count / 2; i++)
            {
                float[] leftChannel = channelData[i];
                float[] rightChannel = channelData[i + 1];
                //for (int j = 0; j < leftStrip.PixelCount; j++)
                //{
                //    Color leftColor;
                //    Color rightColor;
                //    if (j < 13)
                //    {
                //        leftColor = Colors.Red;
                //        rightColor = Colors.Red;
                //    }
                //    else if (j < 39)
                //    {
                //        leftColor = Colors.Green;
                //        leftColor = Colors.Green;
                //    }
                //    else if (j < 78)
                //    {
                //        leftColor = Colors.Blue;
                //        rightColor = Colors.Blue;
                //    }

                //    float leftAverage = 0;
                //    for (int k = j * 2; k < (j + 1) * 2; k++)
                //    {
                //        leftAverage += Math.Abs(leftChannel[k]);
                //    }
                //    leftAverage /= 2.0f;
                //    leftColor.A = (byte)(HelperMethods.Clamp(leftAverage * 1280, 0, 255));
                //    leftStrip.strip[j] = leftColor;

                //    float rightAverage = 0;
                //    for (int k = j * 2; k < (j + 1) * 2; k++)
                //    {
                //        rightAverage += Math.Abs(rightChannel[k]);
                //    }
                //    rightAverage /= 2;
                //    rightColor.A = (byte)(HelperMethods.Clamp(rightAverage * 255, 0, 255));
                //    rightStrip.strip[j] = rightColor;

                //    leftStrip.SendPixels();
                //    rightStrip.SendPixels();
                //}
                Task t = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        for (int j = 0; j < rectangles.Count; j++)
                        {
                            //rectangles[j].Height = GammaCorrection(Math.Abs(leftChannel[j]), maxInput: 1, maxOutput: 1080);
                            //var height = Math.Abs(Math.Pow(Math.Abs(leftChannel[j]), 10));
                            var height = Math.Abs(leftChannel[j]);
                            if (j < 15)
                            {
                                double max = double.Parse(lowBlock.Text);
                                if (height > max)
                                {
                                    lowBlock.Text = height.ToString();
                                }
                            }
                            else if (j < 100)
                            {
                                double max = double.Parse(midBlock.Text);
                                if (height > max)
                                {
                                    midBlock.Text = height.ToString();
                                }
                            }
                            else
                            {
                                double max = double.Parse(highBlock.Text);
                                if (height > max)
                                {
                                    highBlock.Text = height.ToString();
                                }
                            }
                            rectangles[j].Height = height * 1080;
                        }
                    }).AsTask();
                t.Wait();
            }
            //await DoUIThing(() =>
            //{
            //    debugBlock.Text = stopwatch.ElapsedMilliseconds.ToString();
            //});
            //await PrintToLog("finished sending pixels");
            //float[] leftChannel = channelData[0];
            //Complex[] leftFft = new Complex[256];
            //for (int i = 0; i < leftFft.Length; i++)
            //{
            //    Complex c = new Complex();
            //    c.X = leftChannel[i] * (float)Fft.HannWindow(i, leftFft.Length);
            //    leftFft[i] = c;
            //}
            //Fft.Calculate(leftFft);
            //float[] leftFftResult = new float[leftFft.Length];
            //for (int i = 0; i < leftFft.Length; i++)
            //{
            //    leftFftResult[i] = (float)Math.Sqrt(Math.Pow(leftFft[i].X, 2) + Math.Pow(leftFft[i].Y, 2));
            //}
            //float[] rightChannel = channelData[1];
            //float leftAverage = 0;
            //float rightAverage = 0;
            //float leftPositiveAverage = 0;
            //int leftPositiveAdded = 0;
            //float leftNegativeAverage = 0;
            //int leftNegativeAdded = 0;
            //float rightPositiveAverage = 0;
            //int rightPositiveAdded = 0;
            //float rightNegativeAverage = 0;
            //int rightNegativeAdded = 0;
            //for (int i = 0; i < leftChannel.Length; i++)
            //{
            //    float leftValue = leftChannel[i];
            //    leftAverage += Math.Abs(leftValue);
            //    if (leftValue < 0)
            //    {
            //        leftNegativeAverage += leftValue;
            //        leftNegativeAdded++;
            //    }
            //    else
            //    {
            //        leftPositiveAverage += leftValue;
            //        leftPositiveAdded++;
            //    }

            //    float rightValue = rightChannel[i];
            //    rightAverage += Math.Abs(rightValue);
            //    if (rightValue < 0)
            //    {
            //        rightNegativeAverage += rightValue;
            //        rightNegativeAdded++;
            //    }
            //    else
            //    {
            //        rightPositiveAverage += rightValue;
            //        rightPositiveAdded++;
            //    }
            //}
            //if (leftPositiveAdded > 0)
            //{
            //    leftPositiveAverage /= leftPositiveAdded;
            //}
            //if (leftNegativeAdded > 0)
            //{
            //    leftNegativeAverage /= leftNegativeAdded;
            //}
            //if (rightPositiveAdded > 0)
            //{
            //    rightPositiveAverage /= rightPositiveAdded;
            //}
            //if (rightNegativeAdded > 0)
            //{
            //    rightNegativeAverage /= rightNegativeAdded;
            //}
            //if (leftChannel.Length > 0)
            //{
            //    leftAverage /= leftChannel.Length;
            //}
            //if (rightChannel.Length > 0)
            //{
            //    rightAverage /= rightChannel.Length;
            //}
            //await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
            //    () =>
            //    {
            //        leftPositiveRect.Height = leftPositiveAverage * 100;
            //        leftNegativeRect.Height = Math.Abs(leftNegativeAverage) * 100;
            //        rightPositiveRect.Height = rightPositiveAverage * 100;
            //        rightNegativeRect.Height = Math.Abs(rightNegativeAverage) * 100;
            //    });
            //if (leftChannel.Length == 0)
            //{
            //    //leftStrip.ResetPixels();
            //    //rightStrip.ResetPixels();
            //    //doneProcessing = true;
            //    return;
            //}
            //float leftAverage = 0;
            //float rightAverage = 0;
            //for (int i = 0; i < leftChannel.Length; i++)
            //{
            //    float leftValue = leftChannel[i];
            //    leftAverage += Math.Abs(leftValue);
            //    float rightValue = rightChannel[i];
            //    rightAverage += Math.Abs(rightValue);
            //}
            //if (leftAverage == 0 || rightAverage == 0)
            //{
            //    //leftStrip.ResetPixels();
            //    //rightStrip.ResetPixels();
            //    doneProcessing = true;
            //    return;
            //}
            //if (leftChannel.Length > 0)
            //{
            //    //Debug.WriteLine(leftChannel.Length);
            //    leftAverage /= leftChannel.Length;
            //}
            //if (rightChannel.Length > 0)
            //{
            //    //Debug.WriteLine(rightChannel.Length);
            //    rightAverage /= rightChannel.Length;
            //}
            //List<Color> leftStripColors = new List<Color>();
            //List<Color> rightStripColors = new List<Color>();
            //float currentAverage = (leftAverage + rightAverage) / 2;
            //bool beat = false;
            //if (currentAverage > beatValue - 0.1)
            //{
            //    lastBeat = DateTime.Now;
            //    beatValue = currentAverage;
            //    beat = true;
            //    if (lastBeat == DateTime.MinValue)
            //    {
            //        lastBeat = DateTime.Now;
            //        beatValue = currentAverage;
            //    }
            //    else
            //    {
            //        DateTime now = DateTime.Now;
            //        TimeSpan difference = ((now - lastBeat) - timeBetweenBeats).Duration();
            //        if (timeBetweenBeats == TimeSpan.Zero)
            //        {
            //            timeBetweenBeats = now - lastBeat;
            //            lastBeat = now;
            //            beatValue = currentAverage;
            //        }
            //        else if (difference.Ticks >= timeBetweenBeats.Ticks - timeBetweenBeats.Ticks / 4 &&
            //            difference.Ticks <= timeBetweenBeats.Ticks - timeBetweenBeats.Ticks / 4)
            //        {
            //            timeBetweenBeats = now - lastBeat;
            //            lastBeat = now;
            //            beatValue = currentAverage;
            //            beat = true;
            //        }
            //    }                
            //}
            //await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
            //    () =>
            //    {
            //        currentAverageBlock.Text = currentAverage.ToString();
            //        lastBeatBlock.Text = lastBeat.ToString();
            //        timeBetweenBeatsBlock.Text = timeBetweenBeats.ToString();
            //        beatValueBlock.Text = beatValue.ToString();
            //    });
            //if (beat)
            //{
            //    leftStripColors = leftStrip.GetResetPixels(Colors.OrangeRed);
            //    //rightStripColors = rightStrip.GetResetPixels(Colors.OrangeRed);
            //}
            //else
            //{
            //    leftStripColors = leftStrip.GetResetPixels();
            //    //rightStripColors = rightStrip.GetResetPixels();
            //}
            //for (int i = leftStrip.PixelCount / 2 - 1; i < Math.Ceiling(leftPositiveAverage * leftStrip.PixelCount); i++)
            //{
            //    leftStripColors[i] = Colors.White;
            //}
            //for (int i = leftStrip.PixelCount / 2 - 2; i > Math.Ceiling((1 - Math.Abs(leftNegativeAverage)) * (leftStrip.PixelCount / 2)); i--)
            //{
            //    leftStripColors[i] = Colors.White;
            //}
            //for (int i = rightStrip.PixelCount / 2 - 1; i < Math.Ceiling(rightPositiveAverage * rightStrip.PixelCount); i++)
            //{
            //    rightStripColors[i] = Colors.White;
            //}
            //for (int i = rightStrip.PixelCount / 2 - 2; i > Math.Ceiling((1 - Math.Abs(rightNegativeAverage)) * (rightStrip.PixelCount / 2)); i--)
            //{
            //    rightStripColors[i] = Colors.White;
            //}
            //for (int i = 0; i < Math.Ceiling(leftAverage * leftStrip.PixelCount); i++)
            //{
            //    leftStripColors[i] = Colors.CornflowerBlue;
            //}
            //for (int i = 0; i < Math.Ceiling(rightAverage * rightStrip.PixelCount); i++)
            //{
            //    rightStripColors[i] = Colors.CornflowerBlue;
            //}
            //leftStrip.SendPixels(leftStripColors);
            //rightStrip.SendPixels(rightStripColors);
            //doneProcessing = true;
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
            //Fft.Calculate(fftData);
            float[] fftResult = new float[audioGraph.SamplesPerQuantum / 2];
            for (int j = 0; j < fftResult.Length; j++)
            {
                fftResult[j] = (float)Math.Sqrt(Math.Pow(fftData[j].Re, 2) + Math.Pow(fftData[j].Im, 2));
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
    }
}
