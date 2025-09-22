//using Android.Bluetooth;
using Microsoft.Maui.ApplicationModel;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using QRCoder;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
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
       public override string ToString()
       {
           return $"Puff {PuffId} | Start={Start:HH:mm:ss} | End={End:HH:mm:ss} | Duration={Duration:F2}s | Battery={Battery:F2}V | Angles=({XAngle:F2}, {YAngle:F2}, {ZAngle:F2})";
       }
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
       public int RemoveCurrentPuff { get; set; }
       public int PuffDelay { get; set; }
       public double VBat { get; set; }
       // Replace PubRawData array with structured puffs
       public List<PuffData> Puffs { get; set; } = new List<PuffData>();
       public string[] Events { get; set; } = new string[500];
       public int EventTotalCount { get; set; }
       public int EventBatchSize { get; set; }
       public int EventCounter { get; set; }
       public double X_Angle { get; set; }
       public double Y_Angle { get; set; }
       public double Z_Angle { get; set; }
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
   public partial class MainPage : ContentPage
   {
       private EnvironmentType currentEnvironment = EnvironmentType.DEV;
       private readonly IAdapter _bluetoothAdapter;
       private readonly IBluetoothLE _bluetoothLE;
       public ObservableCollection<IDevice> Devices { get; set; } = new();
       public MainPage()
       {
           InitializeComponent();
           DisplayQRCode();
           _bluetoothLE = CrossBluetoothLE.Current;
           _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;
           DevicesListView.ItemsSource = Devices;
       }
       private async void OnScanClicked(object sender, EventArgs e)
       {
           // Disable the button while scanning
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
                   // Ensure the device has a non-null name and matches the desired prefix
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
               //await DisplayAlert("Scan Complete", $"{Devices.Count} devices found.", "OK");
           }
           catch (Exception ex)
           {
               await DisplayAlert("Error", $"Failed to scan: {ex.Message}", "OK");
           }
           finally
           {
               // Re-enable after scanning
               ScanButton.IsEnabled = true;
               ScanButton.Text = "Scan";
           }
       }
       private async Task<PermissionStatus> RequestBluetoothPermissions()
       {
           try
           {
#if ANDROID
               // For Android 12+ (API 31+), we need BLUETOOTH_SCAN and BLUETOOTH_CONNECT
               if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
               {
                   //var scanPermission = await Permissions.RequestAsync<MAUI_Test_Bluetooth.Platforms.Android.BluetoothScanPermission>();
                   var scanPermission = await Permissions.RequestAsync<Platforms.Android.Permissions.BluetoothScanPermission>();
                   if (scanPermission != PermissionStatus.Granted)
                       return scanPermission;
                   var connectPermission = await Permissions.RequestAsync<Platforms.Android.Permissions.BluetoothConnectPermission>();
                   if (connectPermission != PermissionStatus.Granted)
                       return connectPermission;
               }
               else
               {
                   // For older Android versions, we need location permissions
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
#if !FACTORY_MODE
           string fileName = "PUBserial.txt";
           string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
           Console.WriteLine($"----- updateSurvey: {Globals.serialNumber} -----");
           if (!Globals.wifiConnected)
           {
               await UpdateNoSurveyUIAsync();
               return;
           }
           var client = new HttpClient();
           int envCode = GetEnvironmentCode();
           string envDomain = GetSurveyDomain();
           //string urlStr = $"{envDomain}/api/Survey?Serial={Globals.serialNumber}&DBID={envCode}";
           //hardcode one here to test if you can see things in the webview
           string urlStr = "https://cme-pub-survey-dev.azurewebsites.net/api/Survey?Serial=e43e12b8ecc515c9&DBID=0";
           Console.WriteLine("--- URL: " + urlStr);
           try
           {
               var response = await client.GetAsync(urlStr);
               var responseString = await response.Content.ReadAsStringAsync();
               int numSurvey = int.TryParse(responseString, out int parsed) ? parsed : 0;
               await MainThread.InvokeOnMainThreadAsync(() =>
               {
                   if (numSurvey > 0)
                   {
                       //lblSurvey.Text = numSurvey == 1 ? "1 Survey Available" : $"{numSurvey} Surveys Available";
                       //btnSurvey.IsVisible = true;
                       //btnSurvey.Text = numSurvey == 1 ? "View Survey" : "View Surveys";
                   }
                   else
                   {
                       //lblSurvey.Text = "No Survey Available";
                       //btnSurvey.IsVisible = false;
                       //btnSurvey.Text = string.Empty;
                   }
               });
               // Save the current survey count in file
               await File.WriteAllTextAsync(filePath, Globals.serialNumber + "," + DateTime.UtcNow);
           }
           catch (Exception ex)
           {
               Console.WriteLine("Survey network error: " + ex.Message);
               await UpdateNoSurveyUIAsync();
           }
#endif
       }
       private async Task UpdateNoSurveyUIAsync()
       {
           await MainThread.InvokeOnMainThreadAsync(() =>
           {
               //lblSurvey.Text = "No Survey Available";
               //btnSurvey.IsVisible = false;
               //btnSurvey.Text = string.Empty;
           });
           // Reset survey file
           string fileName = "PUBserial.txt";
           string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
           if (File.Exists(filePath))
               File.Delete(filePath);
           Globals.surveySerialNumber = string.Empty;
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
       private void BtnSurvey_Clicked(object sender, EventArgs e)
       {
           //if (!string.IsNullOrEmpty(Globals.serialNumber))
           //{
           //string surveyUrl = $"{GetSurveyDomain()}/SurveyPage?serial={Globals.serialNumber}";
           //hard coded for test
           //string surveyUrl = "https://cme-pub-survey-dev.azurewebsites.net/Survey?Serial=e43e12b8ecc515c9&Code=2333&DBID=0";
           //webSurvey.Source = surveyUrl;
           //}
       }
       private ImageSource GenerateQRCode(string data)
       {
           var qrGenerator = new QRCodeGenerator();
           var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
           var qrCode = new PngByteQRCode(qrCodeData);
           byte[] qrCodeBytes = qrCode.GetGraphic(20);
           return ImageSource.FromStream(() => new MemoryStream(qrCodeBytes));
       }
       private void DisplayQRCode()
       {
           string deviceId = Guid.NewGuid().ToString(); // or use Preferences to persist
           //androidIdLabel.Text = $"Device ID: {deviceId}";
           //qrCodeImage.Source = GenerateQRCode(deviceId);
       }
       private void OnEnvironmentChanged(object sender, CheckedChangedEventArgs e)
       {
           var radio = sender as RadioButton;
           //if (radio.IsChecked)
           //{
           //    string selectedEnv = radio.Value.ToString();
           //    Console.WriteLine($"Selected environment: {selectedEnv}");
           //}
       }
       private void ParseAndStorePuff(string textValue)
       {
           try
           {
               string[] strSplit = textValue.Split(',');
               //if (strSplit.Length < 5)
               //{
               //    MainThread.BeginInvokeOnMainThread(async () =>
               //    {
               //        await Application.Current.MainPage.DisplayAlert(
               //            "Warning",
               //            $"strSplit.Length: {strSplit.Length}\n",
               //            "OK");
               //    });
               //    Console.WriteLine("Not enough fields in puff data");
               //    return;
               //}
               // Parse fields (adjust mapping if device spec changes)
               int puffId = int.Parse(strSplit[1]);
               //int puffId = int.Parse(strSplit[0]);
               // og (2)
               double duration = double.Parse(strSplit[2]); // seconds
               //double duration = double.Parse(strSplit[9]); // seconds
               double volume = double.Parse(strSplit[3]);
               double battery = double.Parse(strSplit[5]);
               // og (6) is probably vbat
               //double xAngle = double.Parse(strSplit[6]);
               // i believe this is the one i have correct, voltage
               double vbat = double.Parse(strSplit[6]);
               DateTime start = DateTime.UtcNow;
               DateTime end = start.AddSeconds(duration);
               var puff = new PuffData
               {
                   PuffId = puffId,
                   Start = start,
                   End = end,
                   Duration = duration,
                   Volume = volume,
                   Battery = battery,
                   //XAngle = xAngle,
                   VBat = vbat,
                   YAngle = 0,
                   ZAngle = 0
               };
               Globals.CurrentDevice?.Puffs.Add(puff);
               Console.WriteLine($"Stored puff: {puff}");
               MainThread.BeginInvokeOnMainThread(async () =>
               {
                   await Application.Current.MainPage.DisplayAlert(
                       "Puff Data",
                       $"PUB: {puff.PuffId}\n" +
                       //$"Duration: {puff.Duration}\n" +
                       //$"Volume: {puff.Volume}\n" +
                       //$"Battery: {puff.Battery}\n" +
                       //$"X Angle: {puff.XAngle}", //not vbat
                       $"Voltage: {puff.VBat}",
                       "OK");
               });
           }
           catch (Exception ex)
           {
               Console.WriteLine($"Error parsing puff data: {ex.Message}");
           }
       }
       private async void OnDeviceSelected(object sender, ItemTappedEventArgs e)
       {
           if (e.Item is IDevice selectedDevice)
           {
               try
               {
                   await DisplayAlert("Connecting", $"Connecting to {selectedDevice.Name}...", "OK");
                   // Connect
                   await _bluetoothAdapter.ConnectToDeviceAsync(selectedDevice);
                   await DisplayAlert("Connected", $"Connected to {selectedDevice.Name}", "OK");
                   var allData = new StringBuilder();
                   // Discover services
                   var services = await selectedDevice.GetServicesAsync();
                   foreach (var service in services)
                   {
                       allData.AppendLine($"Service: {service.Id}");
                       var characteristics = await service.GetCharacteristicsAsync();
                       foreach (var characteristic in characteristics)
                       {
                           allData.AppendLine($"  Characteristic: {characteristic.Id}, CanRead={characteristic.CanRead}, CanUpdate={characteristic.CanUpdate}");
                           // 1. Read once if readable
                           if (characteristic.CanRead)
                           {
                               try
                               {
                                   var (data, resultCode) = await characteristic.ReadAsync();
                                   if (resultCode == 0 && data != null && data.Length > 0)
                                   {
                                       string textValue = Encoding.UTF8.GetString(data);
                                       string hexValue = BitConverter.ToString(data);
                                       allData.AppendLine($"    [Read] Text: {textValue}");
                                       allData.AppendLine($"    [Read] Hex: {hexValue}");
                                       // Example: detect puff data
                                       if (textValue.StartsWith("PUB"))
                                       {
                                           ParseAndStorePuff(textValue);
                                       }
                                   }
                                   else
                                   {
                                       allData.AppendLine($"    No data. ResultCode={resultCode}");
                                   }
                               }
                               catch (Exception readEx)
                               {
                                   allData.AppendLine($"    Failed to read {characteristic.Id}: {readEx.Message}");
                               }
                           }
                           // 2. Subscribe to notifications if possible
                           if (characteristic.CanUpdate)
                           {
                               characteristic.ValueUpdated += (s, args) =>
                               {
                                   var updatedData = args.Characteristic.Value;
                                   if (updatedData != null && updatedData.Length > 0)
                                   {
                                       string updatedHex = BitConverter.ToString(updatedData);
                                       string updatedText = Encoding.UTF8.GetString(updatedData);
                                       //Device.BeginInvokeOnMainThread(() =>
                                       //{
                                       //    allData.AppendLine($"    [Notify] {characteristic.Id}: {updatedHex} / {updatedText}");
                                       //});
                                       Dispatcher.Dispatch(() =>
                                       {
                                           allData.AppendLine($"    [Notify] {characteristic.Id}: {updatedHex} / {updatedText}");
                                       });
                                   }
                               };
                               await characteristic.StartUpdatesAsync();
                               allData.AppendLine($"    Subscribed to notifications for {characteristic.Id}");
                           }
                       }
                   }
                   // Show snapshot of what was read (notifications will keep flowing after)
                   await DisplayAlert("Device Data", allData.ToString(), "OK");
                   // Save globally if needed
                   Globals.serialNumber = selectedDevice.Id.ToString();
               }
               catch (Exception ex)
               {
                   await DisplayAlert("Error", $"Failed to connect: {ex.Message}", "OK");
               }
           }
       }







       //private void ShowCollectedData()
       //{
       //    var device = Globals.CurrentDevice;
       //    if (device == null)
       //    {
       //        DisplayAlert("No Device", "No device connected or data collected yet.", "OK");
       //        return;
       //    }

       //    var sb = new StringBuilder();
       //    sb.AppendLine($"X Angle: {device.X_Angle:F2}");
       //    sb.AppendLine($"Y Angle: {device.Y_Angle:F2}");
       //    sb.AppendLine($"Z Angle: {device.Z_Angle:F2}");
       //    sb.AppendLine($"Serial: {device.SerialNumber}"); //this seems to be correct
       //    sb.AppendLine($"Model: {device.ModelNumber}");
       //    sb.AppendLine($"Firmware: {device.FirmwareVersion}");
       //    sb.AppendLine($"Status: {device.Status}");
       //    sb.AppendLine($"Total Puffs: {device.TotalPuffCount}");
       //    sb.AppendLine($"Puffs Left: {device.PuffCountLeft}");
       //    sb.AppendLine($"Battery: {device.VBat} V");
       //    sb.AppendLine($"Event Count: {device.EventTotalCount}");
       //    sb.AppendLine();
       //    sb.AppendLine("=== Puff Data ===");

       //    //for (int i = 0; i < device.Puffs.Count; i++)
       //    //{
       //    //    var puff = device.Puffs[i];
       //    //    sb.AppendLine($"{i + 1}: Start={puff.Start:yyyy-MM-dd HH:mm:ss}, End={puff.End:yyyy-MM-dd HH:mm:ss}, X={puff.XAngle:F2}, Y={puff.YAngle:F2}, Z={puff.ZAngle:F2}");
       //    //}
       //    foreach (var puff in device.Puffs)
       //    {
       //        sb.AppendLine(puff.ToString());
       //    }

       //    string output = sb.ToString();

       //    Navigation.PushAsync(new ContentPage
       //    {
       //        Title = "Collected Data",
       //        Content = new ScrollView
       //        {
       //            Content = new Label
       //            {
       //                Text = output,
       //                FontSize = 14,
       //                Margin = 10
       //            }
       //        }
       //    });
       //}
       //private void OnShowDataClicked(object sender, EventArgs e)
       //{
       //    ShowCollectedData();
       //}
   }
}

//the current todo is find out how to confirm the header so you can get all of the data from a VUSE











/* using Microsoft.Maui.ApplicationModel;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using QRCoder;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
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
        public double YAngle { get; set; }
        public double ZAngle { get; set; }
        public double VBat { get; set; }
        public double Pressure { get; set; }
        public double FlowRate { get; set; }
        public double Temperature1 { get; set; }
        public double Temperature2 { get; set; }
        public double Current { get; set; }
        public double Power { get; set; }
        public string SerialNumber { get; set; }

        public override string ToString()
        {
            return $"Puff {PuffId} | Start={Start:HH:mm:ss} | Duration={Duration:F2}s | Battery={Battery:F2}V | VBat={VBat:F2}V | Pressure={Pressure:F2} | Flow={FlowRate:F2}";
        }
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
        public double VBat { get; set; }
        public List<PuffData> Puffs { get; set; } = new List<PuffData>();
        public DateTime PuffDateTime { get; set; }
        public double X_Angle { get; set; }
        public double Y_Angle { get; set; }
        public double Z_Angle { get; set; }
        public bool IsVusePro { get; set; }
    }

    public static class Globals
    {
        public static BLEDeviceDetails CurrentDevice;
        public static string serialNumber;
        public static bool Scanning;
    }

    public partial class MainPage : ContentPage
    {
        private readonly IAdapter _bluetoothAdapter;
        private readonly IBluetoothLE _bluetoothLE;
        public ObservableCollection<IDevice> Devices { get; set; } = new();

        // Known UUIDs from the first code
        private static readonly Guid PrimaryServiceUUID = Guid.Parse("6E400001-B5A3-F393-E0A9-E50E24DCCA9E");
        private static readonly Guid CharacteristicUUID = Guid.Parse("6E400003-B5A3-F393-E0A9-E50E24DCCA9E");
        private static readonly Guid VUSEPROCharacteristicUUID = Guid.Parse("0000FFF1-0000-1000-8000-00805F9B34FB");
        private static readonly Guid CypressPrimaryServiceUUID = Guid.Parse("00001802-0000-1000-8000-00805F9B34FB");

        private static readonly Guid VuseProHeaderUUID = Guid.Parse("fd5abba0-3935-11e5-85a6-0002a5d5c51b");


        public MainPage()
        {
            InitializeComponent();
            DisplayQRCode();
            _bluetoothLE = CrossBluetoothLE.Current;
            _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;
            DevicesListView.ItemsSource = Devices;
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
                    // Look for PUB devices or any device for testing
                    if (!string.IsNullOrEmpty(a.Device.Name) &&
                        (a.Device.Name.StartsWith("PUB") || a.Device.Name.ToLower().Contains("vuse")))
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

        private async void OnDeviceSelected(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is IDevice selectedDevice)
            {
                try
                {
                    await DisplayAlert("Connecting", $"Connecting to {selectedDevice.Name}...", "OK");
                    // Connect
                    await _bluetoothAdapter.ConnectToDeviceAsync(selectedDevice);
                    await SetupVuseProAsync(selectedDevice);
                    await DisplayAlert("Connected", $"Connected to {selectedDevice.Name}", "OK");
                    // Initialize device details
                    Globals.CurrentDevice = new BLEDeviceDetails
                    {
                        Device = selectedDevice,
                        Status = "Connected",
                        Puffs = new List<PuffData>()
                    };
                    // Start reading device data
                    await ReadDeviceDataAsync(selectedDevice);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to connect: {ex.Message}", "OK");
                }
            }
        }

        private async Task ReadDeviceDataAsync(IDevice device)
        {
            var allData = new StringBuilder();
            try
            {
                // Discover services
                var services = await device.GetServicesAsync();
                foreach (var service in services)
                {
                    Console.WriteLine($"Found service: {service.Id}");

                    // Check if this is a known service
                    bool isPrimaryService = service.Id == PrimaryServiceUUID ||
                                          service.Id == CypressPrimaryServiceUUID;

                    if (isPrimaryService)
                    {
                        Globals.CurrentDevice.PrimaryService = service;
                        allData.AppendLine($"Primary Service Found: {service.Id}");
                    }

                    var characteristics = await service.GetCharacteristicsAsync();
                    foreach (var characteristic in characteristics)
                    {
                        Console.WriteLine($"  Found characteristic: {characteristic.Id}");

                        // Check if this is a VUSE PRO characteristic
                        if (characteristic.Id == VUSEPROCharacteristicUUID)
                        {
                            Globals.CurrentDevice.IsVusePro = true;
                            Globals.CurrentDevice.PrimaryCharacteristic = characteristic;
                            allData.AppendLine("VUSE PRO Device Detected!");

                            // Don't call ConfirmHeaderAsync here - wait for data first
                            await SetupVuseProCommunication(characteristic);
                        }
                        else if (characteristic.Id == CharacteristicUUID)
                        {
                            Globals.CurrentDevice.PrimaryCharacteristic = characteristic;
                            allData.AppendLine("Standard PUB Device Detected!");
                        }


                        //start test
                        if (characteristic.Id == VuseProHeaderUUID)
                        {
                            Globals.CurrentDevice.IsVusePro = true;
                            Globals.CurrentDevice.PrimaryCharacteristic = characteristic;
                            allData.AppendLine("VUSE PRO Header Characteristic Detected!");

                            // subscribe to notifications — this is the Xamarin onCharacteristicChanged equivalent
                            if (characteristic.CanUpdate)
                            {
                                characteristic.ValueUpdated += OnCharacteristicValueUpdated;
                                await characteristic.StartUpdatesAsync();
                            }

                            // you might also kick off SetupVuseProCommunication here
                            await SetupVuseProCommunication(characteristic);
                        }
                        else if (characteristic.Id == VUSEPROCharacteristicUUID)
                        {
                            // fallback, keep your existing handling
                            Globals.CurrentDevice.IsVusePro = true;
                            Globals.CurrentDevice.PrimaryCharacteristic = characteristic;
                            allData.AppendLine("VUSE PRO Device Detected!");
                            await SetupVuseProCommunication(characteristic);
                        }
                        //end test

                        // Subscribe to notifications for data reception
                        if (characteristic.CanUpdate)
                        {
                            characteristic.ValueUpdated += OnCharacteristicValueUpdated;
                            await characteristic.StartUpdatesAsync();
                            allData.AppendLine($"Subscribed to notifications: {characteristic.Id}");
                        }

                        // Try to read current value
                        if (characteristic.CanRead)
                        {
                            try
                            {
                                var (data, resultCode) = await characteristic.ReadAsync();
                                if (resultCode == 0 && data != null && data.Length > 0)
                                {
                                    string textValue = Encoding.UTF8.GetString(data);
                                    allData.AppendLine($"Initial read: {textValue}");
                                    ProcessIncomingData(textValue);
                                }
                            }
                            catch (Exception readEx)
                            {
                                Console.WriteLine($"Failed to read characteristic: {readEx.Message}");
                            }
                        }
                    }
                }

                await DisplayAlert("Device Connected", allData.ToString(), "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to read device data: {ex.Message}", "OK");
            }
        }

        //work off of this
        //private async Task ReadDeviceDataAsync(IDevice device)
        //{
        //    try
        //    {
        //        var services = await device.GetServicesAsync();
        //        foreach (var service in services)
        //        {
        //            var characteristics = await service.GetCharacteristicsAsync();
        //            foreach (var characteristic in characteristics)
        //            {
        //                Console.WriteLine($"Found characteristic: {characteristic.Id}");

        //                // Subscribe to updates instead of reading
        //                if (characteristic.CanUpdate)
        //                {
        //                    characteristic.ValueUpdated += OnCharacteristicValueUpdated;
        //                    await characteristic.StartUpdatesAsync();

        //                    Console.WriteLine($"[BLE] Subscribed to {characteristic.Id}");
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[BLE] Error subscribing: {ex.Message}");
        //    }
        //}


        //you need to work on stuff in this function
        private async Task SetupVuseProCommunication(ICharacteristic characteristic)
        {
            try
            {
                Console.WriteLine("Setting up VUSE Pro communication...");

                // Start periodic reading to get the header first
                _ = Task.Run(async () =>
                {
                    bool headerReceived = false;
                    int attempts = 0;
                    const int maxAttempts = 10;

                    while (!headerReceived && attempts < maxAttempts &&
                           Globals.CurrentDevice?.Device?.State == DeviceState.Connected)
                    {
                        try
                        {
                            if (characteristic.CanRead)
                            {
                                var (data, resultCode) = await characteristic.ReadAsync();
                                if (resultCode == 0 && data != null && data.Length > 0)
                                {
                                    string textValue = Encoding.UTF8.GetString(data);
                                    //value in this is good, its first point (header?)
                                    Console.WriteLine($"[VUSE PRO Setup] Received: {textValue}");

                                    //if (textValue.StartsWith("2,")) // PUB Header
                                    //this looks better
                                    if (textValue.StartsWith("PUB"))
                                    {
                                        //this now hits for a break
                                        //so now you need to make this thing start reading what the pubs got by subscribing to its updates
                                        //grab the header, put it in the correct place, and then make ProcessIncomingData() start listening for updates
                                        ProcessIncomingData(textValue);
                                        headerReceived = true;

                                        // Now we can send the confirmation
                                        await Task.Delay(500);
                                        await ConfirmHeaderAsync();

                                        // Start the main data reading loop
                                        _ = StartVuseProDataReading();
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[VUSE PRO Setup] Read error: {ex.Message}");
                        }

                        attempts++;
                        await Task.Delay(1000); // Wait 1 second between attempts
                    }

                    //this is where were ending as of now, what really needs to happen at this point is i need to start listening for (subscribe to) updates (rather than reading the characteristics)
                    //you have the header here, put it in the right place and confirm the header like old code to proceeed with subscribing to receive the next data points
                    if (!headerReceived)
                    {
                        Console.WriteLine("[VUSE PRO Setup] Failed to receive header after maximum attempts");
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await DisplayAlert("Error", "Failed to receive device header from VUSE Pro", "OK");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VUSE PRO Setup] Error: {ex.Message}");
            }
        }


        private async Task SetupVuseProAsync(IDevice device)
        {
            try
            {
                // 1. Connect
                await _bluetoothAdapter.ConnectToDeviceAsync(device);

                var services = await device.GetServicesAsync();
                var vuseService = services.FirstOrDefault(s =>
                    s.Id == Guid.Parse("0000fe59-0000-1000-8000-00805f9b34fb"));

                if (vuseService == null)
                {
                    Console.WriteLine("[VUSE PRO Setup] Service not found");
                    return;
                }

                // 2. Get the characteristic
                var vuseCharacteristic = (await vuseService.GetCharacteristicsAsync())
                    .FirstOrDefault(c =>
                        c.Id == Guid.Parse("8ec90004-f315-4f60-9fb8-838830daea50"));

                if (vuseCharacteristic == null)
                {
                    Console.WriteLine("[VUSE PRO Setup] Characteristic not found");
                    return;
                }

                // 3. Subscribe to notifications
                vuseCharacteristic.ValueUpdated += (s, args) =>
                {
                    var data = args.Characteristic.Value;
                    if (data != null && data.Length > 0)
                    {
                        string text = Encoding.UTF8.GetString(data);
                        Console.WriteLine($"[DOTNET] Incoming: {text}");

                        if (text.StartsWith("PUB"))
                        {
                            //ParseAndStorePuff(text);
                        }
                        else
                        {
                            Console.WriteLine($"[DOTNET] Unknown message type: {text}");
                        }
                    }
                };

                await vuseCharacteristic.StartUpdatesAsync();
                Console.WriteLine("[VUSE PRO Setup] Notifications enabled");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VUSE PRO Setup] Error: {ex.Message}");
            }
        }


        private async Task ConfirmHeaderAsync()
        {
            try
            {
                // Ensure we have a valid device with header data
                if (Globals.CurrentDevice?.SerialNumber == null)
                {
                    Console.WriteLine("[ConfirmHeader] Device not properly initialized");
                    return;
                }

                string timeStamp = DateTime.UtcNow.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss");
                if (Globals.CurrentDevice.IsVusePro)
                {
                    timeStamp = DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss");
                }

                string responseString;
                if (Globals.CurrentDevice.DevicePuffCount == 0)
                {
                    Console.WriteLine("[ConfirmHeader] Device puff count = 0, sending response type 2");
                    responseString = $"2,{Globals.CurrentDevice.SerialNumber},{timeStamp},005";
                }
                else
                {
                    Console.WriteLine("[ConfirmHeader] Device puff count > 0, sending response type 4");
                    responseString = $"4,{Globals.CurrentDevice.SerialNumber},{timeStamp},005";
                }

                byte[] data = Encoding.UTF8.GetBytes(responseString);
                var characteristic = Globals.CurrentDevice.PrimaryCharacteristic;

                if (characteristic == null)
                {
                    Console.WriteLine("[ConfirmHeader] Characteristic is null");
                    return;
                }

                if (characteristic.CanWrite)
                {
                    var result = await characteristic.WriteAsync(data);
                    Console.WriteLine($"[ConfirmHeader] Write result: {result}, Data: {responseString}");
                }
                else
                {
                    Console.WriteLine("[ConfirmHeader] Characteristic is not writable");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfirmHeader] Error: {ex}");
            }
        }

        private async Task StartVuseProDataReading()
        {
            Console.WriteLine("[VUSE PRO Data] Starting data reading loop...");

            while (Globals.CurrentDevice?.Device?.State == DeviceState.Connected)
            {
                try
                {
                    if (Globals.CurrentDevice.PrimaryCharacteristic?.CanRead == true)
                    {
                        var (data, resultCode) = await Globals.CurrentDevice.PrimaryCharacteristic.ReadAsync();
                        if (resultCode == 0 && data != null && data.Length > 0)
                        {
                            string textValue = Encoding.UTF8.GetString(data);
                            if (!string.IsNullOrWhiteSpace(textValue))
                            {
                                Console.WriteLine($"[VUSE PRO Data] Received: {textValue}");
                                ProcessIncomingData(textValue);

                                // Check for completion
                                if (textValue.Contains("finish") || textValue.EndsWith("finish"))
                                {
                                    Console.WriteLine("[VUSE PRO Data] Transfer complete");
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[VUSE PRO Data] Read error: {ex.Message}");
                    break;
                }

                await Task.Delay(100); // Adjust timing as needed
            }

            Console.WriteLine("[VUSE PRO Data] Data reading loop ended");
            ShowAllPuffsInUI();
        }

        private async Task DownloadAllStoredPuffData()
        {
            bool finished = false;
            while (!finished && Globals.CurrentDevice?.Device?.State == DeviceState.Connected)
            {
                try
                {
                    if (Globals.CurrentDevice.PrimaryCharacteristic.CanRead)
                    {
                        var (data, resultCode) = await Globals.CurrentDevice.PrimaryCharacteristic.ReadAsync();
                        if (resultCode == 0 && data != null && data.Length > 0)
                        {
                            string textValue = Encoding.UTF8.GetString(data);
                            Console.WriteLine($"[BatchRead] Received: {textValue}");
                            ProcessIncomingData(textValue);
                            if (textValue.Contains("finish"))
                            {
                                Console.WriteLine("[BatchRead] Final batch received.");
                                finished = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BatchRead] Error: {ex.Message}");
                    break;
                }
                await Task.Delay(5); // Fast polling like VUSEPROTimer
            }
            Console.WriteLine("All Vuse Pro puff data downloaded.");
            Console.WriteLine($"[DEBUG] Total puffs stored: {Globals.CurrentDevice.Puffs.Count}");
            foreach (var puff in Globals.CurrentDevice.Puffs)
            {
                Console.WriteLine($"[PUFF] {puff}");
            }
            ShowAllPuffsInUI();
        }

        //private void ShowAllPuffsInUI()
        //{
        //    if (Globals.CurrentDevice?.Puffs == null || Globals.CurrentDevice.Puffs.Count == 0)
        //        return;

        //    var sb = new StringBuilder();
        //    foreach (var puff in Globals.CurrentDevice.Puffs)
        //    {
        //        sb.AppendLine(puff.ToString());
        //    }

        //    MainThread.BeginInvokeOnMainThread(() =>
        //    {
        //        PuffDataEditor.Text = sb.ToString(); // Set the text in the UI
        //    });
        //}
        private void ShowAllPuffsInUI()
        {
            if (Globals.CurrentDevice?.Puffs == null || Globals.CurrentDevice.Puffs.Count == 0)
                return;
            var sb = new StringBuilder();
            foreach (var puff in Globals.CurrentDevice.Puffs)
            {
                sb.AppendLine(puff.ToString());
            }
            var allPuffData = sb.ToString();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Show it in the UI text box
                //PuffDataEditor.Text = allPuffData;
                // OR: Show it as a popup/alert
                await DisplayAlert("All Puff Data", allPuffData, "OK");
            });
        }



        //private async Task StartVuseProPeriodicReading()
        //{
        //    // VUSE PRO devices need periodic reading rather than notifications
        //    while (Globals.CurrentDevice?.Device?.State == DeviceState.Connected)
        //    {
        //        try
        //        {
        //            if (Globals.CurrentDevice.PrimaryCharacteristic.CanRead)
        //            {
        //                var (data, resultCode) = await Globals.CurrentDevice.PrimaryCharacteristic.ReadAsync();
        //                if (resultCode == 0 && data != null && data.Length > 0)
        //                {
        //                    string textValue = Encoding.UTF8.GetString(data);
        //                    ProcessIncomingData(textValue);
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"VUSE PRO read error: {ex.Message}");
        //        }

        //        await Task.Delay(1000); // Read every second
        //    }
        //}
        private async Task StartVuseProPeriodicReading()
        {
            while (Globals.CurrentDevice?.Device?.State == DeviceState.Connected)
            {
                try
                {
                    if (Globals.CurrentDevice.PrimaryCharacteristic.CanRead)
                    {
                        var (data, resultCode) = await Globals.CurrentDevice.PrimaryCharacteristic.ReadAsync();
                        if (resultCode == 0 && data != null && data.Length > 0)
                        {
                            string textValue = Encoding.UTF8.GetString(data);
                            Console.WriteLine($"[VUSE PRO] Received: {textValue}");
                            ProcessIncomingData(textValue);
                            // Check for finish/continue markers
                            if (textValue.Contains("finish"))
                            {
                                Console.WriteLine("[VUSE PRO] Final batch received.");
                                break; // stop reading
                            }
                            else if (!textValue.Contains("continue"))
                            {
                                // Possibly end if unknown or invalid data
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[VUSE PRO] Read error: {ex.Message}");
                }
                await Task.Delay(5); // Fast polling (like Android timer)
            }
            Console.WriteLine("[VUSE PRO] Data dump complete.");
        }

        //work off of this
        //private void OnCharacteristicValueUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        //{
        //    var data = e.Characteristic.Value;
        //    if (data != null && data.Length > 0)
        //    {
        //        string textValue = Encoding.UTF8.GetString(data);
        //        Console.WriteLine($"Notification received: {textValue}");
        //        ProcessIncomingData(textValue);
        //    }
        //}
        private void OnCharacteristicValueUpdated(object sender, CharacteristicUpdatedEventArgs e)
        {
            var data = e.Characteristic.Value;
            if (data != null && data.Length > 0)
            {
                string textValue = Encoding.UTF8.GetString(data);
                Console.WriteLine($"[VUSE PRO Notification] {textValue}");

                if (textValue.StartsWith("2,")) // header
                {
                    ProcessIncomingData(textValue);
                    _ = ConfirmHeaderAsync(); // respond just like Xamarin
                }
                else
                {
                    ProcessIncomingData(textValue);
                }
            }
        }
        //private void OnCharacteristicValueUpdated(object sender, CharacteristicUpdatedEventArgs e)
        //{
        //    var data = e.Characteristic.Value;
        //    if (data == null || data.Length == 0)
        //        return;

        //    string textValue = Encoding.UTF8.GetString(data);
        //    Console.WriteLine($"[BLE Notification] {textValue}");

        //    // feed it into your existing logic
        //    ProcessIncomingData(textValue);
        //}


        //check getpubheader in xamarin to see how they handle a vuse header and what response it needs
        //it seems that getpubheader is used in oncharacteristicchange in mainactivity.cs
        //do you need to handle the vuse pros though an oncharacteristicchanged approach
        //the characteristic you need to look for with a vuse is uuid: fd5abba0-3935-11e5-85a6-0002a5d5c51b, look for this and then do something like confirmheader or something
        //once you get it to get the rest of the data
        private void ProcessIncomingData(string data)
        {
            try
            {
                Console.WriteLine($"Processing data: {data}");
                if (string.IsNullOrEmpty(data)) return;
                string[] parts = data.Split(',');
                if (parts.Length < 2) return;
                //this is just which part of the response youre targeting, the thought here is that there will be some index with some value that indecates what to respond with or do
                //ie confirmheader() or time to read or write or someting like this
                string messageType = parts[0];
                //figure out the flow of this transaction and understand how its supposed to go, check what the og code is doing with the response string
                switch (messageType)
                {
                    case "2": // PUB Header
                        ProcessPubHeader(parts);
                        //test, remove
                        _ = ConfirmHeaderAsync();
                        //end test remove
                        break;
                    case "3": // Puff Header
                        ProcessPuffHeader(parts);
                        break;
                    case "4": // Raw Data
                        ProcessRawData(parts);
                        break;
                    case "5": // Event
                        ProcessEvent(parts);
                        break;
                    default:
                        //why is this hitting
                        //its because in this code, im treating the first value in the data as a flag for whether to continue processing, its not this index that needs to be examined
                        //as its only the "PUB" name, its some other index that i need to talk to and read from to know how to continue processing the data
                        Console.WriteLine($"Unknown message type: {messageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing data: {ex.Message}");
            }
        }

        private void ProcessPubHeader(string[] parts)
        {
            try
            {
                // Format: 2,PUB,1234,5,0,0500,5.0,3.700
                if (parts.Length >= 7)
                {
                    // Initialize the device if it's null
                    if (Globals.CurrentDevice == null)
                    {
                        Globals.CurrentDevice = new BLEDeviceDetails
                        {
                            Puffs = new List<PuffData>()
                        };
                    }

                    Globals.CurrentDevice.SerialNumber = parts[2];
                    Globals.CurrentDevice.ModelNumber = int.Parse(parts[3]);
                    Globals.CurrentDevice.DevicePuffCount = int.Parse(parts[4]);
                    Globals.CurrentDevice.TotalPuffCount = Globals.CurrentDevice.DevicePuffCount;
                    Globals.CurrentDevice.FirmwareVersion = parts[5];
                    Globals.CurrentDevice.VBat = double.Parse(parts[6]);

                    Globals.serialNumber = Globals.CurrentDevice.SerialNumber;

                    Console.WriteLine($"[PUB Header] Serial: {Globals.CurrentDevice.SerialNumber}, " +
                                    $"Model: {Globals.CurrentDevice.ModelNumber}, " +
                                    $"Puffs: {Globals.CurrentDevice.DevicePuffCount}");

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlert("Device Info",
                            $"Serial: {Globals.CurrentDevice.SerialNumber}\n" +
                            $"Model: {Globals.CurrentDevice.ModelNumber}\n" +
                            $"Puff Count: {Globals.CurrentDevice.DevicePuffCount}\n" +
                            $"Firmware: {Globals.CurrentDevice.FirmwareVersion}\n" +
                            $"Battery: {Globals.CurrentDevice.VBat}V", "OK");
                    });
                }
                else
                {
                    Console.WriteLine($"[PUB Header] Invalid header format, parts count: {parts.Length}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PUB Header] Error processing header: {ex.Message}");
            }
        }

        private void ProcessPuffHeader(string[] parts)
        {
            // Format: 3,PUFF,1234,5,0200,12/12/2020 12:59:59.100,359.9,359.9,359.9
            if (parts.Length >= 6)
            {
                Globals.CurrentDevice.BatchPuffCount = int.Parse(parts[4]);
                if (parts.Length >= 6)
                {
                    try
                    {
                        DateTime puffTime;
                        if (DateTime.TryParseExact(parts[5], "M/d/yyyy H:m:s",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out puffTime))
                        {
                            Globals.CurrentDevice.PuffDateTime = puffTime;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing puff datetime: {ex.Message}");
                    }
                }
                if (parts.Length >= 9)
                {
                    Globals.CurrentDevice.X_Angle = double.Parse(parts[6]);
                    Globals.CurrentDevice.Y_Angle = double.Parse(parts[7]);
                    Globals.CurrentDevice.Z_Angle = double.Parse(parts[8]);
                }
            }
        }

        private void ProcessRawData(string[] parts)
        {
            try
            {
                // This is where puff data comes in - format varies by model
                var puff = new PuffData
                {
                    SerialNumber = Globals.CurrentDevice.SerialNumber,
                    PuffId = Globals.CurrentDevice.PuffID++
                };
                // Parse based on model number (simplified version)
                if (parts.Length >= 10)
                {
                    puff.Duration = double.Parse(parts[2]);
                    puff.Volume = parts.Length > 3 ? double.Parse(parts[3]) : 0;
                    puff.Battery = parts.Length > 5 ? double.Parse(parts[5]) : 0;
                    puff.VBat = Globals.CurrentDevice.VBat;
                    // Set timestamps
                    if (parts.Length > 1 && double.TryParse(parts[1], out double offsetMs))
                    {
                        puff.Start = Globals.CurrentDevice.PuffDateTime.AddMilliseconds(offsetMs);
                        puff.End = puff.Start.AddSeconds(puff.Duration);
                    }
                    else
                    {
                        puff.Start = DateTime.Now;
                        puff.End = puff.Start.AddSeconds(puff.Duration);
                    }
                    // Add model-specific data
                    if (Globals.CurrentDevice.ModelNumber == 2) // GLO
                    {
                        if (parts.Length >= 19)
                        {
                            puff.Temperature1 = double.Parse(parts[2]);
                            puff.Temperature2 = double.Parse(parts[3]);
                            puff.Current = double.Parse(parts[5]);
                            puff.Pressure = double.Parse(parts[8]);
                            puff.FlowRate = double.Parse(parts[9]);
                            puff.Power = double.Parse(parts[6]);
                        }
                    }
                    else if (Globals.CurrentDevice.ModelNumber == 3) // Combustible
                    {
                        if (parts.Length >= 8)
                        {
                            puff.Pressure = double.Parse(parts[3]);
                            puff.FlowRate = double.Parse(parts[4]);
                            puff.XAngle = double.Parse(parts[5]);
                            puff.YAngle = double.Parse(parts[6]);
                            puff.ZAngle = double.Parse(parts[7]);
                        }
                    }
                    else if (Globals.CurrentDevice.ModelNumber == 5) // Harmony
                    {
                        if (parts.Length >= 7)
                        {
                            puff.Current = double.Parse(parts[3]);
                            puff.Pressure = double.Parse(parts[4]);
                            puff.Power = double.Parse(parts[5]);
                            puff.XAngle = Globals.CurrentDevice.X_Angle;
                            puff.YAngle = Globals.CurrentDevice.Y_Angle;
                            puff.ZAngle = Globals.CurrentDevice.Z_Angle;
                        }
                    }
                    Globals.CurrentDevice.Puffs.Add(puff);
                    // Show notification with puff data
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlert("New Puff Data", puff.ToString(), "OK");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing raw data: {ex.Message}");
            }
        }

        private void ProcessEvent(string[] parts)
        {
            // Format: 5,EVENT,1000,02/06/2021 12:30:59.100,1
            if (parts.Length >= 4)
            {
                try
                {
                    string eventData = string.Join(",", parts);
                    Console.WriteLine($"Event received: {eventData}");

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlert("Device Event", eventData, "OK");
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing event: {ex.Message}");
                }
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

        //make the confirm header function

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
            // Survey logic remains the same
        }

        private async Task UpdateNoSurveyUIAsync()
        {
            // Survey logic remains the same
        }

        private string GetSurveyDomain()
        {
            return "https://cme-pub-survey-dev.azurewebsites.net";
        }

        private int GetEnvironmentCode()
        {
            return 0;
        }

        private void BtnSurvey_Clicked(object sender, EventArgs e)
        {
            // Survey logic remains the same
        }

        private ImageSource GenerateQRCode(string data)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);
            return ImageSource.FromStream(() => new MemoryStream(qrCodeBytes));
        }

        private void DisplayQRCode()
        {
            string deviceId = Guid.NewGuid().ToString();
        }

        private void OnEnvironmentChanged(object sender, CheckedChangedEventArgs e)
        {
            // Environment logic remains the same
        }
    }
} */