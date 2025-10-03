#if IOS
using Foundation;
using System.IO;
#endif





//using Android.Media;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
//using Java.IO;

//using Javax.Annotation.Meta;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Graphics;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
//using Plugin.BLE.Android;
using QRCoder;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
//using static Java.Util.Concurrent.Flow;

// ALTO HEADER                 PUB, 1000, 4, 100, 500, 1.1, 3.30
// [DOTNET][BLE] Header:       PUB, 4833, 6, 100, 2,   6.0, 4.3400
// ALTO HEADER ACK             4, 1000, 01/01/2017 01:01:01, 025
// [DOTNET][Header Ack Sent]   4, 4833, 09/30/2025 09:09:00, 005
// ALTO DATA PACKET            DATA,   10-23-2017 23:59.55,   359.9,    359.9,    359.9,    3.30,     3.30,     2000,     2000,     3.525,    10-23-2017 23:59.59
// VUSE DATA PACKET            DATA,   09/29/2025 11:37:17,   000.00,   000.00,   000.00,   1.5680,   3.9200,   0.0320,   0.0320,   2.9600,   09/29/2025 11:37:20
//  { "PuffId":2,"dataString":"DATA","Start":"2025-09-29T11:37:23","indexPlaceholderIndex2":0,"indexPlaceholderIndex3":0,"indexPlaceholderIndex4":0,"VAve":3.8317,"VHigh":3.89,"Current7":0.036,"Current8":0.036,"Duration":2.93,"End":"2025-09-29T11:37:26"}
// VUSE DATA PACKET            DATA,          09/29/2025 11:37:17,                          000.00,                    000.00,                    000.00,  1.5680,        3.9200,         0.0320,          0.0320,          2.9600,     09/29/2025 11:37:20


//VUSE
//Custom Service Characteristic (CSC) Profile
//Primary Service UUID (128bit) fd5abba0-3935-11e5-85a6-0002a5d5c51b
//Characteristic UUID (bit)     fd5abba0-3935-11e5-85a6-0002a5d5c51b
//[DOTNET] Characteristic:      fd5abba0 - 3935 - 11e5 - 85a6 - 0002a5d5c51b
//[DOTNET]     CanRead: True
//[DOTNET]     CanWrite: True
//[DOTNET]     CanUpdate: True

//ALTO
//Custom Service Characteristic (CSC) Profile
//Primary Service UUID (128bit)        fd5abba0-3935-11e5-85a6-0002a5d5c51b
//Characteristic Service UUID (128bit) fd5abba1-3935-11e5-85a6-0002a5d5c51b
//[DOTNET] Characteristic:             fd5abba1 - 3935 - 11e5 - 85a6 - 0002a5d5c51b
//[DOTNET]     CanRead: False
//[DOTNET]     CanWrite: False
//[DOTNET]     CanUpdate: True



namespace PUBTransfer
{
    public static class EventHubUploader
    {
        //prod
        //private static string connectionString = "Endpoint=sb://EH-CME-PUBDelivery-CENTRAL.servicebus.windows.net/;SharedAccessKeyName=pubstream-policy-central;SharedAccessKey=D5FY6WNY3o4akIha1gQ7qelwicMX8L6nFT1BKpjWxe4=";
        //dev
        private static string connectionString = "Endpoint=sb://eh-cme-pubdelivery-central-dev.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=meH+L8ec18ALUA8Cyq7LysSuXrb4Ho9tpQGpsQpyH38=";
        private static string eventHubName = "pubstream-rd1-central-dev";

        public static async Task<bool> SendPuffsAsync(List<PuffData> puffs)
        {
            try
            {
                await using var producer = new EventHubProducerClient(connectionString, eventHubName);
                // Convert PuffData to JSON strings
                var events = new List<EventData>();
                foreach (var puff in puffs)
                {
                    string json = JsonSerializer.Serialize(puff);
                    events.Add(new EventData(json));
                }
                // Send the batch
                using EventDataBatch eventBatch = await producer.CreateBatchAsync();
                foreach (var e in events)
                {
                    if (!eventBatch.TryAdd(e))
                    {
                        Console.WriteLine("[EventHub] Event too large for batch, skipping.");
                        continue;
                    }
                }
                await producer.SendAsync(eventBatch);
                Console.WriteLine($"[EventHub] Sent {events.Count} puff events.");
                return true; // indicate success
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EventHub] Failed to send events: {ex.Message}");
                return false; // indicate failure
            }
        }
    }
    public class PuffData
    {
        public int PuffId { get; set; }
        public string dataString { get; set; }
        public DateTime Start { get; set; }
        public double indexPlaceholderIndex2 { get; set; }
        public double indexPlaceholderIndex3 { get; set; }
        public double indexPlaceholderIndex4 { get; set; }
        public double VAve { get; set; }
        public double VHigh { get; set; }
        public double Current7 { get; set; }
        public double Current8 { get; set; }
        public double Duration { get; set; }
        public DateTime End { get; set; }
        public override string ToString()
        {
            return $"Puff {PuffId} | DATA={dataString} | " +
                   $"Start={Start:MM/dd/yyyy HH:mm:ss} | " +
                   $"Index2={indexPlaceholderIndex2:F2} | " +
                   $"Index3={indexPlaceholderIndex3:F2} | " +
                   $"Index4={indexPlaceholderIndex4:F2} | " +
                   $"VAve={VAve:F4} | " +
                   $"VHigh={VHigh:F4} | " +
                   $"Current7={Current7:F4} | " +
                   $"Current8={Current8:F4} | " +
                   $"Duration={Duration:F4} | " +
                   $"End={End:MM/dd/yyyy HH:mm:ss}";
        }
    }
    public static class Globals
    {
        public static BLEDeviceDetails CurrentDevice;
        //should this be private, should it even be in globals
        //VUSE
        //public static readonly Guid HeaderCharacteristicId = Guid.Parse("fd5abba0-3935-11e5-85a6-0002a5d5c51b");
        //ALTO
        //subscribe to updates for this characteristic
        public static readonly Guid HeaderCharacteristicId = Guid.Parse("fd5abba1-3935-11e5-85a6-0002a5d5c51b");
    }
    public enum EnvironmentType
    {
        DEV,
        QA,
        PROD,
        Nothing
    }
    public class BLEDeviceDetails
    {
        public IDevice Device { get; set; }
        public string SerialNumber { get; set; }
        public string Status { get; set; }
        public DateTime TransferTime { get; set; }
        public List<PuffData> Puffs { get; set; } = new List<PuffData>();
    }
    public enum datatable
    {
        RawData1,
        RawData2,
        Events,
        Nothing
    }
    public enum environment
    {
        DEV,
        QA,
        PROD,
        Nothing
    }
    public abstract class TransferManager
    {
        public string type;
        public datatable currentTable;
        public string currentEventHubNamespace;
        public string currentPolicyName;
        public string currentPolicyKey;
        public string currentEventHub;
        public string currentStreamName;
        public string currentAccessKey;
        public string currentSecretKey;
        public string currentEnvironment;
        public string EventHubNamespace = "EH-CME-PUBDelivery-CENTRAL.servicebus.windows.net";
        public string PolicyName = "pubstream-policy-central";
        public string EventHubRD1 = "pubstream-rd1-central";
        public string PolicyKeyRD1 = "D5FY6WNY3o4akIha1gQ7qelwicMX8L6nFT1BKpjWxe4=";
        public string EventHubRD2 = "pubstream-rd2-central";
        public string PolicyKeyRD2 = "ZqKYOaPH9V6xSh5zIbdMEtMuBr2WqG0ySni2l1omPoo=";
        public string EventHubEvents = "pubstream-events-central";
        public string PolicyKeyEvents = "AU6hfNsXwmBy3WHiFUlMQIvuSogYSlZ8ivUwc3luY1Y=";
        public string status;
        public string streamStatus;
        internal EventHandler newStatus;
        internal EventHandler updateCurrentStatus;
        public abstract void configureManager(environment DBName, datatable table);
        public abstract Task<bool> openStream();
        public abstract Task<bool> runExampleV1(string DB);
        public abstract Task<bool> runMultiExampleV1(string DB);
        public abstract Task<bool> runExampleV2(string DB);
        public abstract Task<bool> runMultiExampleV2(string DB);
        public abstract Task<bool> runExampleEvents();
        public abstract Task<bool> runMultiExampleEvents();
        public abstract Task<bool> writeRecord(string rawData, string typeOfData, string DB);
        public abstract Task<bool> writeMultiRecords(string[] rawData, string typeOfData);
        public abstract void destroyManager();
    }
    public partial class MainPage : ContentPage
    {
        private EnvironmentType currentEnvironment = EnvironmentType.DEV;
        private readonly IAdapter _bluetoothAdapter;
        private readonly IBluetoothLE _bluetoothLE;
        private BLEDeviceDetails _currentDevice;
        private ICharacteristic _writeCharacteristic;
        private bool _isCollectingData = false;
        private StringBuilder _logData = new StringBuilder();
        public ObservableCollection<IDevice> Devices { get; set; } = new();
        public MainPage()
        {
            InitializeComponent();
            DisplayQRCode();
#if ANDROID
    Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("WebGLSettings", (handler, view) =>
    {
        if (handler.PlatformView is Android.Webkit.WebView webView)
        {
            var settings = webView.Settings;
            settings.JavaScriptEnabled = true;
            settings.DomStorageEnabled = true;
            settings.SetSupportZoom(false);
            
            // Critical for WebGL:
            settings.AllowFileAccess = true;
            settings.AllowContentAccess = true;
            settings.AllowFileAccessFromFileURLs = true;
            settings.AllowUniversalAccessFromFileURLs = true;
            
            // Enable WebGL
            settings.SetRenderPriority(Android.Webkit.WebSettings.RenderPriority.High);
            webView.SetLayerType(Android.Views.LayerType.Hardware, null);
        }
    });
#endif


#if IOS

var htmlFile = "modelviewer.html";
var htmlPath = Path.Combine(NSBundle.MainBundle.BundlePath, htmlFile);
var htmlContent = File.ReadAllText(htmlPath);

ModelViewer.Source = new HtmlWebViewSource
{
    Html = htmlContent,
    BaseUrl = NSBundle.MainBundle.BundlePath // This tells the WebView where to resolve relative paths
};
#endif






            ModelViewer.Source = new UrlWebViewSource
            {
                Url = "file:///android_asset/modelviewer.html"
            };
            ModelViewer.HorizontalOptions = LayoutOptions.FillAndExpand;
            ModelViewer.VerticalOptions = LayoutOptions.FillAndExpand;
            _bluetoothLE = CrossBluetoothLE.Current;
            _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;
            DevicesListView.ItemsSource = Devices;
        }
        private async void OnDeviceSelected(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is IDevice selectedDevice && !_isCollectingData)
            {
                _isCollectingData = true;
                try
                {
                    //await DisplayAlert("Connecting", $"Connecting to {selectedDevice.Name}...", "OK");
                    // 1. Connect to the device
                    await _bluetoothAdapter.ConnectToDeviceAsync(selectedDevice);
                    // Store current device details
                    _currentDevice = new BLEDeviceDetails
                    {
                        Device = selectedDevice,
                        Status = "Connected",
                        SerialNumber = selectedDevice.Name.Length > 3 ? selectedDevice.Name.Substring(3) : "",
                        TransferTime = DateTime.UtcNow
                    };
                    Globals.CurrentDevice = _currentDevice;
                    //var headerChar = await GetHeaderCharacteristicAsync(selectedDevice);



                    //var altoHeaderChar = await GetHeaderCharacteristicAltoAsync(selectedDevice);
                    var (notifyChar, writeChar) = await GetHeaderCharacteristicAltoAsync(selectedDevice, "1192");

                    if (notifyChar != null && writeChar != null)
                    {
                        Console.WriteLine("Header handshake sent, notifications subscribed.");

                        // Later, after receiving the header notification, ACK it:
                        string ack = $"4,1192,{DateTime.Now:MM/dd/yyyy HH:mm:ss},005";
                        await writeChar.WriteAsync(Encoding.UTF8.GetBytes(ack));
                        Console.WriteLine($"[BLE] Sent header ACK: {ack}");
                    }




                    //await DisplayAlert("Connected", $"Connected to {selectedDevice.Name}...", "OK");
                    // for ALTO you need to subscribe to updates rather than read
                    // all of this is just for VUSE
                    // STEP 1: Read header

                    //var (headerBytes, resultCode) = await headerChar.ReadAsync();
                    //var header = System.Text.Encoding.UTF8.GetString(headerBytes);
                    //Console.WriteLine($"[BLE] Header: {header}");
                    ////await DisplayAlert("Header Data", header, "OK");
                    //// STEP 2: Ack header
                    //var parts = header.Split(',');
                    //string serial = parts.Length > 1 ? parts[1] : "";
                    //await AcknowledgeHeaderAsync(headerChar, serial);
                    //// STEP 3: Read data
                    //int batchSize = int.Parse(parts[3]);
                    //int puffCount = int.Parse(parts[4]);
                    //Console.WriteLine($"batchSize {batchSize}");
                    //Console.WriteLine($"puffCount {puffCount}");
                    //var dataPoints = await ReadDataBatchAsync(headerChar, batchSize, puffCount, serial, this);
                    ////STEP 3: Put data into puffdata objects
                    //ParsePuffData(dataPoints);

                    //STEP 4: Confirm upload
                    //if (dataPoints.Count > 0)
                    //{
                    //    await ConfirmUploadAsync(headerChar, puffCount);
                    //}
                    //STEP 5: Push parsed data to backend
                    if (_currentDevice?.Puffs?.Count > 0)
                    {
                        bool success = await EventHubUploader.SendPuffsAsync(_currentDevice.Puffs);
                        if (success)
                        {
#if ANDROID
                            var buzz = new PUBTransfer.Platforms.Android.BuzzAndDing(Android.App.Application.Context);
                            buzz.Ding();

                            //var buzz = new PUBTransfer.Platforms.Android.BuzzAndDing(Android.App.Application.Context);
                            //buzz.ShowNotification("Upload Complete", "Puff data sent to Event Hub!");
#elif IOS
                                    //var buzz = new PUBTransfer.Platforms.iOS.BuzzAndDing();
                                    //buzz.Ding();

                                    var notifier = new PUBTransfer.Platforms.iOS.NotificationHelper();
                                    await notifier.RequestPermissionsAsync(); // Only needed once; you could move this to app startup
                                    notifier.ShowNotification("Upload Complete", "Puff data sent to Event Hub!");
                                    notifier.Release();
#endif
                            //await DisplayAlert("Success", "Puff data sent to Event Hub!", "OK");
                        }
                        else
                        {
                            await DisplayAlert("Error", "Failed to send data to Event Hub.", "OK");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to connect or read data: {ex.Message}", "OK");
                }
            }
        }

        //        private async void OnDeviceSelected(object sender, ItemTappedEventArgs e)
        //        {
        //            if (!(e.Item is IDevice selectedDevice) || _isCollectingData)
        //                return;

        //            _isCollectingData = true;

        //            try
        //            {
        //                // 1️⃣ Connect to the device
        //                await _bluetoothAdapter.ConnectToDeviceAsync(selectedDevice);
        //                _currentDevice = new BLEDeviceDetails
        //                {
        //                    Device = selectedDevice,
        //                    Status = "Connected",
        //                    SerialNumber = selectedDevice.Name.Length > 3 ? selectedDevice.Name.Substring(3) : "",
        //                    TransferTime = DateTime.UtcNow
        //                };
        //                Globals.CurrentDevice = _currentDevice;

        //                // 2️⃣ Get the header characteristic
        //                var headerChar = await GetHeaderCharacteristicAsync(selectedDevice);
        //                if (headerChar == null)
        //                {
        //                    await DisplayAlert("Error", "Header characteristic not found.", "OK");
        //                    return;
        //                }

        //                // 3️⃣ Wait for the header notification
        //                var headerTcs = new TaskCompletionSource<string>();

        //                void LocalHeaderHandler(object sender2, CharacteristicUpdatedEventArgs e2)
        //                {
        //                    var data = e2.Characteristic.Value;
        //                    if (data != null && data.Length > 0)
        //                    {
        //                        var header = Encoding.UTF8.GetString(data);
        //                        headerTcs.TrySetResult(header);
        //                    }
        //                }

        //                headerChar.ValueUpdated += LocalHeaderHandler;
        //                await headerChar.StartUpdatesAsync();
        //                Console.WriteLine("[BLE] Waiting for header notification...");

        //                string header = await headerTcs.Task; // Wait until header is received
        //                Console.WriteLine($"[BLE] Header received: {header}");

        //                // Remove temporary handler
        //                headerChar.ValueUpdated -= LocalHeaderHandler;

        //                // 4️⃣ Acknowledge the header
        //                var parts = header.Split(',');
        //                string serial = parts.Length > 1 ? parts[1] : "";
        //                await AcknowledgeHeaderAsync(headerChar, serial);

        //                // 5️⃣ Parse puff data info
        //                int batchSize = parts.Length > 3 ? int.Parse(parts[3]) : 0;
        //                int puffCount = parts.Length > 4 ? int.Parse(parts[4]) : 0;
        //                Console.WriteLine($"batchSize: {batchSize}");
        //                Console.WriteLine($"puffCount: {puffCount}");

        //                // 6️⃣ Read puff data
        //                var dataPoints = await ReadDataBatchAsync(headerChar, batchSize, puffCount, serial, this);
        //                ParsePuffData(dataPoints);

        //                // 7️⃣ Upload data to backend if available
        //                if (_currentDevice?.Puffs?.Count > 0)
        //                {
        //                    bool success = await EventHubUploader.SendPuffsAsync(_currentDevice.Puffs);
        //                    if (success)
        //                    {
        //#if ANDROID
        //                        var buzz = new PUBTransfer.Platforms.Android.BuzzAndDing(Android.App.Application.Context);
        //                        buzz.Ding();
        //#elif IOS
        //                var notifier = new PUBTransfer.Platforms.iOS.NotificationHelper();
        //                await notifier.RequestPermissionsAsync();
        //                notifier.ShowNotification("Upload Complete", "Puff data sent to Event Hub!");
        //                notifier.Release();
        //#endif
        //                    }
        //                    else
        //                    {
        //                        await DisplayAlert("Error", "Failed to send data to Event Hub.", "OK");
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                await DisplayAlert("Error", $"Failed to connect or read data: {ex.Message}", "OK");
        //                Console.WriteLine($"[BLE] Exception: {ex}");
        //            }
        //            finally
        //            {
        //                _isCollectingData = false;
        //            }
        //        }





        private async Task AcknowledgeHeaderAsync(ICharacteristic characteristic, string serialNumber)
        {
            string timeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            string responseString = $"4,{serialNumber},{timeStamp},005";
            byte[] payload = System.Text.Encoding.UTF8.GetBytes(responseString);
            await characteristic.WriteAsync(payload);
            Console.WriteLine($"[Header Ack Sent] {responseString}");
            //await DisplayAlert("Sending Header Response Data", responseString, "OK");
        }
        private async Task<List<string>> ReadDataBatchAsync(ICharacteristic headerChar, int batchSize, int puffCount, string serialNumber, Page page)
        {
            var dataPoints = new List<string>();
            try
            {
                // Read puff data repeatedly from same characteristic
                for (int i = 0; i < puffCount; i++)
                {
                    var (dataBytes, resultCode) = await headerChar.ReadAsync();
                    var dataLine = System.Text.Encoding.UTF8.GetString(dataBytes);
                    if (string.IsNullOrWhiteSpace(dataLine) || !dataLine.StartsWith("DATA"))
                    {
                        Console.WriteLine($"[BLE] Invalid or empty puff data at index {i}: {dataLine}");
                        continue;
                    }
                    Console.WriteLine($"[BLE] Puff {i + 1}/{puffCount}: {dataLine}");
                    dataPoints.Add(dataLine);
                }
                // Show all puff data in one alert at the end
                if (dataPoints.Count > 0)
                {
                    var allData = string.Join(System.Environment.NewLine, dataPoints);
                    //await page.DisplayAlert("Puff Data", allData, "OK");
                }
                else
                {
                    await page.DisplayAlert("Puff Data", "No valid puff data received.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BLE] Error in ReadDataBatchAsync: {ex.Message}");
                await page.DisplayAlert("Error", $"Error reading puff data: {ex.Message}", "OK");
            }
            return dataPoints;
        }












        //private async Task<ICharacteristic?> GetHeaderCharacteristicAsync(IDevice device)
        //{
        //    try
        //    {
        //        var service = await device.GetServiceAsync(Guid.Parse("fd5abba0-3935-11e5-85a6-0002a5d5c51b"));
        //        if (service == null)
        //        {
        //            Console.WriteLine("[BLE] Service not found!");
        //            return null;
        //        }
        //        // ALTO
        //        //var characteristic = await service.GetCharacteristicAsync(Guid.Parse("fd5abba1-3935-11e5-85a6-0002a5d5c51b"));
        //        // VUSE
        //        var characteristic = await service.GetCharacteristicAsync(Guid.Parse("fd5abba0-3935-11e5-85a6-0002a5d5c51b"));
        //        if (characteristic == null)
        //        {
        //            Console.WriteLine("[BLE] Header characteristic not found!");
        //            return null;
        //        }
        //        Console.WriteLine($"[BLE] Found Header Characteristic: {characteristic.Id}");
        //        Console.WriteLine($"  CanRead={characteristic.CanRead}, CanWrite={characteristic.CanWrite}, CanUpdate={characteristic.CanUpdate}");
        //        // If notifications are supported, subscribe
        //        if (characteristic.CanUpdate)
        //        {
        //            characteristic.ValueUpdated += OnHeaderValueUpdated;
        //            await characteristic.StartUpdatesAsync();
        //            Console.WriteLine("[BLE] Subscribed to notifications.");
        //        }
        //        else
        //        {
        //            Console.WriteLine("[BLE] Characteristic does not support notifications.");
        //        }
        //        return characteristic;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[BLE] Error in GetHeaderCharacteristicAsync: {ex.Message}");
        //        return null;
        //    }
        //}

        // Event handler for notifications
        //check if this is expecting notifications/updates in the form of new puffs as the bluetooth is connected and waiting for new puffs or if
        //it is expecting notifications/updates in regards to transmission type
        private void OnHeaderValueUpdated(object sender, CharacteristicUpdatedEventArgs e)
        {
            var data = e.Characteristic.Value;
            if (data == null || data.Length == 0)
            {
                Console.WriteLine("[BLE] Empty notification received.");
                return;
            }
            // Convert bytes to string
            var header = System.Text.Encoding.UTF8.GetString(data);
            // Display the full header in the console
            Console.WriteLine($"[BLE] Header received: {header}");
            // Optional: show an alert in the UI if you want
            // await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("Header Data", header, "OK"));
            // Extract parts for further processing
            var parts = header.Split(',');
            string serial = parts.Length > 1 ? parts[1] : "";
            int batchSize = parts.Length > 3 ? int.Parse(parts[3]) : 0;
            int puffCount = parts.Length > 4 ? int.Parse(parts[4]) : 0;
            Console.WriteLine($"batchSize: {batchSize}");
            Console.WriteLine($"puffCount: {puffCount}");
            // Start reading puff data based on the header
            _ = ReadDataBatchAsync((ICharacteristic)sender, batchSize, puffCount, serial, this);
        }




        //private async Task<ICharacteristic?> GetHeaderCharacteristicAsync(IDevice device)
        //{
        //    try
        //    {
        //        // get the known service
        //        var service = await device.GetServiceAsync(Guid.Parse("fd5abba0-3935-11e5-85a6-0002a5d5c51b"));
        //        if (service == null)
        //        {
        //            Console.WriteLine("[BLE] Service not found!");
        //            return null;
        //        }
        //        // get the known characteristic
        //        //var characteristic = await service.GetCharacteristicAsync(Guid.Parse("fd5abba1-3935-11e5-85a6-0002a5d5c51b"));
        //        var characteristic = await service.GetCharacteristicAsync(Guid.Parse("fd5abba1-3935-11e5-85a6-0002a5d5c51b"));
        //        if (characteristic == null)
        //        {
        //            Console.WriteLine("[BLE] Header characteristic not found!");
        //            return null;
        //        }
        //        Console.WriteLine($"[BLE] Found Header Characteristic: {characteristic.Id}");
        //        return characteristic;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[BLE] Error in GetHeaderCharacteristicAsync: {ex.Message}");
        //        return null;
        //    }
        //}



        //ALTO / SOLO / VIBE(fd5abba1 characteristic)
        //They don’t just start streaming when you subscribe.
        //The PUB expects the app(phone) to write a “start” or “ready” message to the characteristic.
        //After that, the PUB will send you the header packet, and then you fall into the “pump” flow:
        //App subscribes.
        //App sends “ready/start” command.
        //PUB sends PUB Phone Header.
        //App ACKs header.
        //PUB sends PUB Phone Data.
        //App ACKs data.
        //Repeat until batch is done, then PUB disconnects.

        private async Task<(ICharacteristic? NotifyChar, ICharacteristic? WriteChar)> GetHeaderCharacteristicAltoAsync(IDevice device, string serial)
        {
            try
            {
                var services = await device.GetServicesAsync();

                ICharacteristic? notifyChar = null;
                ICharacteristic? writeChar = null;

                foreach (var service in services)
                {
                    var characteristics = await service.GetCharacteristicsAsync();

                    foreach (var c in characteristics)
                    {
                        Console.WriteLine($"[BLE] Service: {service.Id} | Characteristic: {c.Id} | Read={c.CanRead} Write={c.CanWrite} Update={c.CanUpdate}");

                        // Notify characteristic
                        if (c.Id == Guid.Parse("fd5abba1-3935-11e5-85a6-0002a5d5c51b") && c.CanUpdate)
                        {
                            notifyChar = c;

                            // Subscribe to notifications
                            c.ValueUpdated += (s, e) =>
                            {
                                var data = e.Characteristic.Value;
                                Console.WriteLine($"[BLE] Notification received: {BitConverter.ToString(data)}");
                            };

                            await c.StartUpdatesAsync();
                            Console.WriteLine("[BLE] Subscribed to header characteristic updates.");
                        }

                        // Writable characteristic (for handshake)
                        if (c.CanWrite && writeChar == null)
                        {
                            writeChar = c;
                        }
                    }
                }

                if (notifyChar == null)
                {
                    Console.WriteLine("[BLE] Notify characteristic not found!");
                    return (null, null);
                }

                if (writeChar == null)
                {
                    Console.WriteLine("[BLE] Writable characteristic not found!");
                    return (notifyChar, null);
                }

                // Send initial header request handshake
                string timeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                string handshake = $"2,{serial},{timeStamp},005";
                await writeChar.WriteAsync(Encoding.UTF8.GetBytes(handshake));
                Console.WriteLine($"[BLE] Sent header request handshake: {handshake}");

                return (notifyChar, writeChar);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BLE] Error in GetHeaderCharacteristicAltoAsync: {ex.Message}");
                return (null, null);
            }
        }



        //private async Task<ICharacteristic?> GetHeaderCharacteristicAltoAsync(IDevice device)
        //{
        //    try
        //    {
        //        //var services = await device.GetServicesAsync();

        //        //foreach (var service in services)
        //        //{
        //        //    Console.WriteLine($"Service: {service.Id}");
        //        //    var characteristics = await service.GetCharacteristicsAsync();
        //        //    foreach (var characteristic in characteristics)
        //        //    {
        //        //        Console.WriteLine($"  Characteristic: {characteristic.Id}");

        //        //        // Check if this is your notify characteristic (fd5abba1…)
        //        //        if (characteristic.Id.ToString().StartsWith("fd5abba1", StringComparison.OrdinalIgnoreCase)
        //        //            && characteristic.CanUpdate)
        //        //        {
        //        //            // Subscribe to notifications
        //        //            characteristic.ValueUpdated += (s, e) =>
        //        //            {
        //        //                var data = e.Characteristic.Value;
        //        //                Console.WriteLine($"Notification received: {BitConverter.ToString(data)}");
        //        //            };

        //        //            //break here hits, do i need to print these values to the console after this function call
        //        //            await characteristic.StartUpdatesAsync(); // Keeps it active


        //        //            await Task.Delay(-1); // keeps app alive indefinitely

        //        //        }
        //        //    }
        //        //}










        //        //write to a descriptor with uuid 2902


        //        var servicess = await device.GetServicesAsync();
        //        foreach (var service in servicess)
        //        {
        //            Console.WriteLine($"[BLE] Service: {service.Id}");
        //            var characteristics = await service.GetCharacteristicsAsync();
        //            foreach (var ch in characteristics)
        //            {
        //                Console.WriteLine($"  [BLE] Characteristic: {ch.Id} | Read={ch.CanRead} Write={ch.CanWrite} Update={ch.CanUpdate}");
        //            }
        //        }




        //        //var servicesx = await device.GetServicesAsync();
        //        //foreach (var service in servicesx)
        //        //{
        //        //    if (service.Id == Guid.Parse("fd5abba0-3935-11e5-85a6-0002a5d5c51b"))
        //        //    {
        //        //        var characteristics = await service.GetCharacteristicsAsync();
        //        //        foreach (var ch in characteristics)
        //        //        {
        //        //            if (ch.Id == Guid.Parse("fd5abba1-3935-11e5-85a6-0002a5d5c51b"))
        //        //            {
        //        //                // Subscribe to notifications
        //        //                ch.ValueUpdated += (s, e) =>
        //        //                {
        //        //                    var data = e.Characteristic.Value; // raw byte[]
        //        //                    Console.WriteLine($"[BLE] Notification: {BitConverter.ToString(data)}");

        //        //                    // TODO: Parse header/data according to PUB spec
        //        //                };

        //        //                await ch.StartUpdatesAsync();
        //        //            }
        //        //        }
        //        //    }
        //        //}



        //        //var servicesz = await device.GetServicesAsync();
        //        //foreach (var service in servicesz)
        //        //{
        //        //    Console.WriteLine($"Service: {service.Id}");
        //        //    var characteristics = await service.GetCharacteristicsAsync();
        //        //    foreach (var ch in characteristics)
        //        //    {
        //        //        Console.WriteLine($"  Characteristic: {ch.Id} | Read={ch.CanRead} Write={ch.CanWrite} Update={ch.CanUpdate}");
        //        //    }
        //        //}



        //        //var services = await device.GetServicesAsync();
        //        //foreach (var service in services)
        //        //{
        //        //    var characteristics = await service.GetCharacteristicsAsync();
        //        //    foreach (var c in characteristics)
        //        //    {
        //        //        if (c.Id == Globals.HeaderCharacteristicId)
        //        //        {
        //        //            Console.WriteLine($"[BLE] Found Header Characteristic: {c.Id}");
        //        //            //if (c.CanUpdate)
        //        //            //{
        //        //            //    // Subscribe to notifications
        //        //            //    c.ValueUpdated += (s, e) =>
        //        //            //    {
        //        //            //        var data = e.Characteristic.Value; // byte[]
        //        //            //        Console.WriteLine($"[BLE] Update received: {BitConverter.ToString(data)}");
        //        //            //        // TODO: parse ALTO data here
        //        //            //    };
        //        //            //    await c.StartUpdatesAsync();
        //        //            //    Console.WriteLine("[BLE] Subscribed to header characteristic updates.");
        //        //            //}
        //        //            //else
        //        //            //{
        //        //            //    Console.WriteLine("[BLE] Characteristic does not support updates.");
        //        //            //}
        //        //            if (c.CanUpdate)
        //        //            {
        //        //                c.ValueUpdated += async (s, e) =>
        //        //                {
        //        //                    var data = e.Characteristic.Value; // raw header packet
        //        //                    var header = System.Text.Encoding.UTF8.GetString(data);
        //        //                    Console.WriteLine($"[BLE] Header received: {header}");
        //        //                    // Parse header if you want, or just ACK immediately
        //        //                    string serialNumber = "1192"; // TODO: parse or store device serial
        //        //                    string now = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        //        //                    string transferDelay = "025"; // ms, depends on spec
        //        //                    string ackPacket = $"4,{serialNumber},{now},{transferDelay}";
        //        //                    byte[] ackBytes = System.Text.Encoding.UTF8.GetBytes(ackPacket);
        //        //                    await c.WriteAsync(ackBytes);
        //        //                    Console.WriteLine($"[BLE] Sent Header ACK: {ackPacket}");
        //        //                };

        //        //                //i need to send a "header request" here to get the header, then i need to subscribe to updates
        //        //                //var altoHeaderChar = await GetHeaderCharacteristicAltoAsync(selectedDevice);
        //        //                //string serial = "1192";
        //        //                //await AcknowledgeAltoHeaderAsync(altoHeaderChar, serial);
        //        //                string serial = "1192";
        //        //                //how can i write to some characteristic to acknowledge a header / tell the pub to start talking if the characteristic is canWrite = false
        //        //                //i probably can "write" to this through updating it
        //        //                await AcknowledgeAltoHeaderAsync(c, serial);



        //        //                await c.StartUpdatesAsync();
        //        //                Console.WriteLine("[BLE] Subscribed to header characteristic updates.");
        //        //            }
        //        //            return c;
        //        //        }
        //        //    }
        //        //}
        //        //Console.WriteLine("[BLE] Header characteristic not found!");
        //        //return null;





        //        var services = await device.GetServicesAsync();

        //        foreach (var service in services)
        //        {
        //            Console.WriteLine($"Service: {service.Id}");
        //            var characteristics = await service.GetCharacteristicsAsync();

        //            foreach (var characteristic in characteristics)
        //            {
        //                Console.WriteLine($"  Characteristic: {characteristic.Id}");

        //                // Check if this is your notify characteristic
        //                if (characteristic.Id == Guid.Parse("fd5abba1-3935-11e5-85a6-0002a5d5c51b")
        //                    && characteristic.CanUpdate)
        //                {
        //                    // Subscribe to notifications
        //                    characteristic.ValueUpdated += (s, e) =>
        //                    {
        //                        var data = e.Characteristic.Value;
        //                        Console.WriteLine($"[BLE] Notification received: {BitConverter.ToString(data)}");
        //                    };

        //                    await characteristic.StartUpdatesAsync();
        //                    Console.WriteLine("[BLE] Subscribed to header characteristic updates.");

        //                    // return it to caller so you can use it later
        //                    return characteristic;
        //                }
        //            }
        //        }

        //        Console.WriteLine("[BLE] Header characteristic not found!");
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[BLE] Error in GetHeaderCharacteristicAltoAsync: {ex.Message}");
        //        return null;
        //    }
        //}
        private async Task AcknowledgeAltoHeaderAsync(ICharacteristic characteristic, string serialNumber)
        {
            string timeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            string responseString = $"4,{serialNumber},{timeStamp},005";
            byte[] payload = System.Text.Encoding.UTF8.GetBytes(responseString);
            await characteristic.WriteAsync(payload);
            Console.WriteLine($"[Header Ack Sent] {responseString}");
            //await DisplayAlert("Sending Header Response Data", responseString, "OK");
        }


        //private async Task<ICharacteristic?> GetHeaderCharacteristicAltoAsync(IDevice device)
        //{
        //    try
        //    {
        //        //var service = await device.GetServiceAsync(Guid.Parse("fd5abba0-3935-11e5-85a6-0002a5d5c51b"));
        //        //if (service != null)
        //        //{
        //        //    var characteristic = await service.GetCharacteristicAsync(Guid.Parse("fd5abba1-3935-11e5-85a6-0002a5d5c51b"));
        //        //    if (characteristic != null)
        //        //    {
        //        //        Console.WriteLine("Header characteristic found!");
        //        //        // You can now Read, Write, or Subscribe to notifications
        //        //    }
        //        //    else
        //        //    {
        //        //        Console.WriteLine("Characteristic not found!");
        //        //    }
        //        //}


        //        var servicess = await device.GetServicesAsync();
        //        //to see the services and characteristics on the device
        //        foreach (var service in servicess)
        //        {
        //            Console.WriteLine($"Service: {service.Id}");
        //            var characteristics = await service.GetCharacteristicsAsync();
        //            foreach (var characteristic in characteristics)
        //            {
        //                Console.WriteLine($"  Characteristic: {characteristic.Id}");
        //            }
        //        }


        //        var servicesss = await device.GetServicesAsync();
        //        foreach (var service in servicesss)
        //        {
        //            Console.WriteLine($"Service: {service.Id}");
        //            var characteristics = await service.GetCharacteristicsAsync();
        //            foreach (var characteristic in characteristics)
        //            {
        //                Console.WriteLine($"  Characteristic: {characteristic.Id}");
        //                Console.WriteLine($"    CanRead   : {characteristic.CanRead}");
        //                Console.WriteLine($"    CanWrite  : {characteristic.CanWrite}");
        //                Console.WriteLine($"    CanUpdate : {characteristic.CanUpdate}");
        //            }
        //        }

        //        //                         fd5abba1 - 3935 - 11e5 - 85a6 - 0002a5d5c51b
        //        //[DOTNET] Characteristic: fd5abba1 - 3935 - 11e5 - 85a6 - 0002a5d5c51b
        //        //[DOTNET]     CanRead: False
        //        //[DOTNET]     CanWrite: False
        //        //[DOTNET]     CanUpdate: True
        //        //this is why you cant just read, you must do it by subscribing to updates on an ALTO






        //        var services = await device.GetServicesAsync();
        //        foreach (var service in services)
        //        {
        //            var characteristics = await service.GetCharacteristicsAsync();
        //            //Console.WriteLine($"Characteristic: {characteristics}");
        //            foreach (var c in characteristics)
        //            {
        //                //if (c.Id == HeaderCharacteristicId)
        //                if (c.Id == Globals.HeaderCharacteristicId)
        //                {
        //                    Console.WriteLine($"[BLE] Found Header Characteristic: {c.Id}");
        //                    return c;
        //                }
        //            }
        //        }
        //        Console.WriteLine("[BLE] Header characteristic not found!");
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[BLE] Error in GetHeaderCharacteristicAsync: {ex.Message}");
        //        return null;
        //    }
        //}
        private async Task<ICharacteristic?> GetHeaderCharacteristicAsync(IDevice device)
        {
            try
            {
                //var service = await device.GetServiceAsync(Guid.Parse("fd5abba0-3935-11e5-85a6-0002a5d5c51b"));
                //if (service != null)
                //{
                //    var characteristic = await service.GetCharacteristicAsync(Guid.Parse("fd5abba1-3935-11e5-85a6-0002a5d5c51b"));
                //    if (characteristic != null)
                //    {
                //        Console.WriteLine("Header characteristic found!");
                //        // You can now Read, Write, or Subscribe to notifications
                //    }
                //    else
                //    {
                //        Console.WriteLine("Characteristic not found!");
                //    }
                //}


                var servicess = await device.GetServicesAsync();
                //to see the services and characteristics on the device
                foreach (var service in servicess)
                {
                    Console.WriteLine($"Service: {service.Id}");
                    var characteristics = await service.GetCharacteristicsAsync();
                    foreach (var characteristic in characteristics)
                    {
                        Console.WriteLine($"  Characteristic: {characteristic.Id}");
                    }
                }


                var servicesss = await device.GetServicesAsync();
                foreach (var service in servicesss)
                {
                    Console.WriteLine($"Service: {service.Id}");
                    var characteristics = await service.GetCharacteristicsAsync();
                    foreach (var characteristic in characteristics)
                    {
                        Console.WriteLine($"  Characteristic: {characteristic.Id}");
                        Console.WriteLine($"    CanRead   : {characteristic.CanRead}");
                        Console.WriteLine($"    CanWrite  : {characteristic.CanWrite}");
                        Console.WriteLine($"    CanUpdate : {characteristic.CanUpdate}");
                    }
                }

                //                         fd5abba1 - 3935 - 11e5 - 85a6 - 0002a5d5c51b
                //[DOTNET] Characteristic: fd5abba1 - 3935 - 11e5 - 85a6 - 0002a5d5c51b
                //[DOTNET]     CanRead: False
                //[DOTNET]     CanWrite: False
                //[DOTNET]     CanUpdate: True
                //this is why you cant just read, you must do it by subscribing to updates on an ALTO






                var services = await device.GetServicesAsync();
                foreach (var service in services)
                {
                    var characteristics = await service.GetCharacteristicsAsync();
                    //Console.WriteLine($"Characteristic: {characteristics}");
                    foreach (var c in characteristics)
                    {
                        //if (c.Id == HeaderCharacteristicId)
                        if (c.Id == Globals.HeaderCharacteristicId)
                        {
                            Console.WriteLine($"[BLE] Found Header Characteristic: {c.Id}");
                            return c;
                        }
                    }
                }
                Console.WriteLine("[BLE] Header characteristic not found!");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BLE] Error in GetHeaderCharacteristicAsync: {ex.Message}");
                return null;
            }
        }
        private void ParsePuffData(List<string> dataPoints)
        {
            if (_currentDevice == null) return;
            int puffCounter = 1;
            foreach (var line in dataPoints)
            {
                try
                {
                    var parts = line.Split(',');
                    if (parts.Length < 11) continue; // sanity check for VUSE
                    var puff = new PuffData
                    {
                        PuffId = puffCounter++,
                        dataString = parts[0],
                        Start = DateTime.Parse(parts[1]),
                        indexPlaceholderIndex2 = double.TryParse(parts[2], out var indph2) ? indph2 : 0,
                        indexPlaceholderIndex3 = double.TryParse(parts[3], out var indph3) ? indph3 : 0,
                        indexPlaceholderIndex4 = double.TryParse(parts[4], out var indph4) ? indph4 : 0,
                        VAve = double.TryParse(parts[5], out var VAve) ? VAve : 0,
                        VHigh = double.TryParse(parts[6], out var VHigh) ? VHigh : 0,
                        Current7 = double.TryParse(parts[7], out var Current7) ? Current7 : 0,
                        Current8 = double.TryParse(parts[8], out var Current8) ? Current8 : 0,
                        Duration = double.TryParse(parts[9], out var Duration) ? Duration : 0,
                        End = DateTime.Parse(parts[10]),
                    };
                    Console.WriteLine($"[ParsePuffData] Total puffs parsed: {puff}");
                    _currentDevice.Puffs.Add(puff);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ParsePuffData] Error parsing line: {line} | {ex.Message}");
                }
            }
            Console.WriteLine($"[ParsePuffData] Total puffs parsed: {_currentDevice.Puffs.Count}");
        }
        //this is fine because even if writeMultiRecords only suypports PUBs its likely that the VUSE data will need to be in this structure as well
        public string[] ConvertPuffsToRawData(List<PuffData> puffs)
        {
            var rawData = new List<string>();
            foreach (var puff in puffs)
            {
                string line = $"{puff.PuffId}, {puff.Start:yyyy-MM-dd HH:mm:ss}, " +
                              $"{puff.indexPlaceholderIndex2}, {puff.indexPlaceholderIndex3}, {puff.indexPlaceholderIndex4}, " +
                              $"{puff.VAve}, {puff.VHigh}, {puff.Current7}, {puff.Current8}, " +
                              $"{puff.Duration}, {puff.End:yyyy-MM-dd HH:mm:ss}";
                rawData.Add(line);
            }
            return rawData.ToArray();
        }        
        private async Task ConfirmUploadAsync(ICharacteristic characteristic, int puffCount)
        {
            try
            {
                // Example format: "1,100" → ACK command + number of records to delete
                string confirmString = $"1,{puffCount}";
                byte[] payload = System.Text.Encoding.UTF8.GetBytes(confirmString);
                await characteristic.WriteAsync(payload);
                Console.WriteLine($"[Upload Confirm Sent] {confirmString}");
                await Application.Current.MainPage.DisplayAlert("Upload Confirmed", $"Sent: {confirmString}", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BLE] Error confirming upload: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Upload confirmation failed: {ex.Message}", "OK");
            }
        }
        private async void OnScanClicked(object sender, EventArgs e)
        {
            ScanButton.IsEnabled = false;
            ScanButton.Text = "Scanning...";
            try
            {
                var permissionStatus = await RequestBluetoothPermissions();
                if (permissionStatus != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Denied", "Bluetooth permissions are required", "OK");
                    return;
                }
                if (!_bluetoothLE.IsOn)
                {
                    await DisplayAlert("Bluetooth Off", "Please enable Bluetooth", "OK");
                    return;
                }
                Devices.Clear();
                _bluetoothAdapter.DeviceDiscovered += (s, a) =>
                {
                    if (!string.IsNullOrEmpty(a.Device.Name) && a.Device.Name.StartsWith("PUB"))
                    {
                        if (!Devices.Contains(a.Device))
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Devices.Add(a.Device);
                            });
                        }
                    }
                };
                await _bluetoothAdapter.StartScanningForDevicesAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to scan: {ex.Message}", "OK");
            }
            finally
            {
                ScanButton.IsEnabled = true;
                ScanButton.Text = "Scan";
            }
        }
        private async Task<PermissionStatus> RequestBluetoothPermissions()
        {
            try
            {
#if ANDROID
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
                {
                    var scanPermission = await Permissions.RequestAsync<Platforms.Android.Permissions.BluetoothScanPermission>();
                    if (scanPermission != PermissionStatus.Granted)
                        return scanPermission;
                    var connectPermission = await Permissions.RequestAsync<Platforms.Android.Permissions.BluetoothConnectPermission>();
                    if (connectPermission != PermissionStatus.Granted)
                        return connectPermission;
                }
                else
                {
                    var locationPermission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (locationPermission != PermissionStatus.Granted)
                        return locationPermission;
                }
#endif
                return PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Permission Error", $"Failed to request permissions: {ex.Message}", "OK");
                return PermissionStatus.Denied;
            }
        }
        private void OnClearClicked(object sender, EventArgs e)
        {
            Devices.Clear();
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await UpdateSurveyAsync();
        }
        public async Task UpdateSurveyAsync()
        {
            // existing survey update logic
        }
        private string GetSurveyDomain()
        {
            return currentEnvironment switch
            {
                EnvironmentType.DEV => "https://cme-pub-survey-dev.azurewebsites.net",
                EnvironmentType.QA => "https://cme-pub-survey-qa-e3bfg0g9bjcud5ew.eastus-01.azurewebsites.net",
                EnvironmentType.PROD => "https://mobilesurveys.azurewebsites.net",
                _ => ""
            };
        }
        private int GetEnvironmentCode()
        {
            return currentEnvironment switch
            {
                EnvironmentType.DEV => 0,
                EnvironmentType.QA => 1,
                EnvironmentType.PROD => 2,
                _ => -1
            };
        }
        private void DisplayQRCode()
        {
            string deviceId = Guid.NewGuid().ToString();
        }
        private void OnEnvironmentChanged(object sender, CheckedChangedEventArgs e)
        {
            var radio = sender as RadioButton;
        }
    }
}