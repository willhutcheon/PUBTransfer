using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using QRCoder;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;


namespace PUBTransfer
{
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
        public static readonly Guid HeaderCharacteristicId = Guid.Parse("fd5abba0-3935-11e5-85a6-0002a5d5c51b");
        public static readonly string streamName = "End_Pub_KinesisStreams";
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



        private AmazonKinesisClient awsKinesisClient;
        private CancellationTokenSource kinesisCancellationTokenSource;


        public MainPage()
        {
            InitializeComponent();

            InitKinesisClient();

            DisplayQRCode();
            _bluetoothLE = CrossBluetoothLE.Current;
            _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;
            DevicesListView.ItemsSource = Devices;
        }

        private void InitKinesisClient()
        {
            //find out what these are
            var accessKey = "MY_ACCESS_KEY";
            var secretKey = "MY_SECRET_KEY";
            awsKinesisClient = new AmazonKinesisClient(
                accessKey,
                secretKey,
                Amazon.RegionEndpoint.USEast1 // Change to our region
            );
        }


        //private static readonly Guid HeaderCharacteristicId = Guid.Parse("fd5abba0-3935-11e5-85a6-0002a5d5c51b");
        private async Task AcknowledgeHeaderAsync(ICharacteristic characteristic, string serialNumber)
        {
            string timeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            string responseString = $"4,{serialNumber},{timeStamp},005";
            byte[] payload = Encoding.UTF8.GetBytes(responseString);
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
                    var dataLine = Encoding.UTF8.GetString(dataBytes);
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
                    var allData = string.Join(Environment.NewLine, dataPoints);
                    await page.DisplayAlert("Puff Data", allData, "OK");
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
        private async Task<ICharacteristic?> GetHeaderCharacteristicAsync(IDevice device)
        {
            try
            {
                var services = await device.GetServicesAsync();
                foreach (var service in services)
                {
                    var characteristics = await service.GetCharacteristicsAsync();
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
                    var headerChar = await GetHeaderCharacteristicAsync(selectedDevice);

                    //xamarin flow was
                    //1. Read header (done)
                    //2. Ack header (done)
                    //3. Read puff data → parse into PuffData objects (done)
                    //4. Confirm upload (done)
                    //5. Push parsed data to database via REST API

                    // STEP 1: Read header
                    var (headerBytes, resultCode) = await headerChar.ReadAsync();
                    var header = Encoding.UTF8.GetString(headerBytes);
                    Console.WriteLine($"[BLE] Header: {header}");
                    //await DisplayAlert("Header Data", header, "OK");
                    // STEP 2: Ack header
                    var parts = header.Split(',');
                    string serial = parts.Length > 1 ? parts[1] : "";
                    await AcknowledgeHeaderAsync(headerChar, serial);
                    // STEP 3: Read data
                    int batchSize = int.Parse(parts[3]);
                    int puffCount = int.Parse(parts[4]);
                    Console.WriteLine($"batchSize {batchSize}");
                    Console.WriteLine($"puffCount {puffCount}");
                    //var dataPoints = await ReadDataBatchAsync(headerChar, batchSize, puffCount, serial);
                    var dataPoints = await ReadDataBatchAsync(headerChar, batchSize, puffCount, serial, this);
                    //STEP 3: Put data into puffdata objects
                    ParsePuffData(dataPoints);
                    //STEP 4: Confirm upload
                    //if (dataPoints.Count > 0)
                    //{
                    //    await ConfirmUploadAsync(headerChar, puffCount);
                    //}
                    //STEP 5: Send data to db, find out how to send the PuffData up to the db/event hub
                    // STEP 5: Push parsed data to backend
                    //i think this needs to be packaged as some json form to get sent to the event hub

                    //if (_currentDevice?.Puffs?.Count > 0)
                    //{
                    //    await PuffUploader.UploadPuffsAsync(_currentDevice.Puffs, currentEnvironment);
                    //}
                    if (_currentDevice?.Puffs?.Count > 0)
                    {
                        string[] rawData = ConvertPuffsToRawData(_currentDevice.Puffs);
                        //make writeMultiRecords
                        //await writeMultiRecords(rawData, "rawdata1");
                    }







                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to connect or read data: {ex.Message}", "OK");
                }
            }
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
        public async Task<bool> writeRecord(string rawData)
        {
            if (string.IsNullOrEmpty(rawData))
                return true;
            byte[] byteRawData = Encoding.ASCII.GetBytes(rawData);
            PutRecordRequest request = new PutRecordRequest
            {
                Data = new System.IO.MemoryStream(),
                StreamName = Globals.streamName,
                PartitionKey = "1"
            };
            await request.Data.WriteAsync(byteRawData, 0, byteRawData.Length);
            PutRecordResponse response = await awsKinesisClient.PutRecordAsync(
                request,
                kinesisCancellationTokenSource.Token
            );
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        public bool writeMultiRecords(string[] rawData, string typeOfData)
        {
            try
            {
                List<PutRecordsRequestEntry> entry = new List<PutRecordsRequestEntry>();
                for (int i = 0; i < rawData.Length; i++)
                {
                    if (string.IsNullOrEmpty(rawData[i]))
                        continue;
                    byte[] byteRawData = Encoding.ASCII.GetBytes(rawData[i]);
                    entry.Add(new PutRecordsRequestEntry
                    {
                        Data = new System.IO.MemoryStream(),
                        PartitionKey = "1"
                    });
                    entry[^1].Data.WriteAsync(byteRawData, 0, byteRawData.Length).Wait();
                }
                PutRecordsRequest multirequest = new PutRecordsRequest
                {
                    Records = entry,
                    StreamName = Globals.streamName
                };
                awsKinesisClient.PutRecordsAsync(multirequest, kinesisCancellationTokenSource.Token).Wait();
                return true;
            }
            catch
            {
                return false;
            }
        }












        private async Task ConfirmUploadAsync(ICharacteristic characteristic, int puffCount)
        {
            try
            {
                // Example format: "1,100" → ACK command + number of records to delete
                string confirmString = $"1,{puffCount}";
                byte[] payload = Encoding.UTF8.GetBytes(confirmString);
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