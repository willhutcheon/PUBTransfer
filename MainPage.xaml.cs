using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using QRCoder;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace PUBTransfer
{
    public class PuffData
    {
        public int PuffId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public double Duration { get; set; }
        public double Volume { get; set; }
        public double Battery { get; set; }
        public double XAngle { get; set; }
        public double VBat { get; set; }
        public double YAngle { get; set; }
        public double ZAngle { get; set; }
        public double Pressure { get; set; }
        public double Flow { get; set; }
        public double Current { get; set; }
        public double Power { get; set; }
        public int RTD { get; set; }
        public override string ToString()
        {
            return $"Puff {PuffId} | Start={Start:HH:mm:ss} | Duration={Duration:F2}s | Battery={Battery:F2}V | Pressure={Pressure:F2}";
        }
    }

    public static class Globals
    {
        public static string ScreenMode;
        public static string TopPanel;
        public static string BottomPanel;
        public static bool Scanning;
        public static string serialNumber;
        public static string PassCode;
        public static Timer YourTimer;
        public static string surveySerialNumber;
        public static DateTime surveySerialDate;
        public static bool wifiConnected = true;
        public static BLEDeviceDetails CurrentDevice;
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
        public IService PrimaryService { get; set; }
        public ICharacteristic PrimaryCharacteristic { get; set; }
        public IDescriptor PrimaryDescriptor { get; set; }
        public string SerialNumber { get; set; }
        public int ModelNumber { get; set; }
        public string FirmwareVersion { get; set; }
        public string Status { get; set; }
        public int PuffCountLeft { get; set; }
        public int DevicePuffCount { get; set; }
        public int TotalPuffCount { get; set; }
        public int BatchPuffCount { get; set; }
        public int BatchPuffCounter { get; set; }
        public int PuffID { get; set; }
        public int PuffNum { get; set; }
        public int PUBDataCounter { get; set; }
        public double VBat { get; set; }
        public DateTime PuffDateTime { get; set; }
        public DateTime TransferTime { get; set; }
        // For different model support
        public double X_Angle { get; set; }
        public double Y_Angle { get; set; }
        public double Z_Angle { get; set; }
        public List<PuffData> Puffs { get; set; } = new List<PuffData>();
        public string[] Events { get; set; } = new string[500];
        public int EventTotalCount { get; set; }
        public int EventBatchSize { get; set; }
        public int EventCounter { get; set; }
        // Raw data storage like Xamarin version
        public string[] PubRawData { get; set; }
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
            _bluetoothLE = CrossBluetoothLE.Current;
            _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;
            DevicesListView.ItemsSource = Devices;
        }

        private static readonly Guid HeaderCharacteristicId = Guid.Parse("fd5abba0-3935-11e5-85a6-0002a5d5c51b");

        private async Task AcknowledgeHeaderAsync(ICharacteristic characteristic, string serialNumber)
        {
            string timeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            string responseString = $"4,{serialNumber},{timeStamp},005";
            byte[] payload = Encoding.UTF8.GetBytes(responseString);
            await characteristic.WriteAsync(payload);
            Console.WriteLine($"[Header Ack Sent] {responseString}");
            await DisplayAlert("Sending Header Response Data", responseString, "OK");
        }

        //private async Task<List<string>> ReadDataBatchAsync(ICharacteristic headerChar, int batchSize, int puffCount, string serialNumber)
        //{
        //    var dataPoints = new List<string>();
        //    try
        //    {
        //        // Read puff data repeatedly from same characteristic
        //        for (int i = 0; i < puffCount; i++)
        //        {
        //            var (dataBytes, resultCode) = await headerChar.ReadAsync();
        //            var dataLine = Encoding.UTF8.GetString(dataBytes);
        //            if (string.IsNullOrWhiteSpace(dataLine) || !dataLine.StartsWith("DATA"))
        //            {
        //                Console.WriteLine($"[BLE] Invalid or empty puff data at index {i}: {dataLine}");
        //                continue;
        //            }
        //            Console.WriteLine($"[BLE] Puff {i + 1}/{puffCount}: {dataLine}");
        //            dataPoints.Add(dataLine);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[BLE] Error in ReadDataBatchAsync: {ex.Message}");
        //    }
        //    return dataPoints;
        //}
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
                        if (c.Id == HeaderCharacteristicId)
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
                    // STEP 1: Read header
                    var (headerBytes, resultCode) = await headerChar.ReadAsync();
                    var header = Encoding.UTF8.GetString(headerBytes);
                    Console.WriteLine($"[BLE] Header: {header}");
                    await DisplayAlert("Header Data", header, "OK");
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
                    //STEP 4: Batch Acknowledgement
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to connect or read data: {ex.Message}", "OK");
                }
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