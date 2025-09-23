////using Android.Bluetooth;
//using Microsoft.Maui.ApplicationModel;
//using Plugin.BLE;
//using Plugin.BLE.Abstractions.Contracts;
//using QRCoder;
//using System.Collections.ObjectModel;
//using System.Globalization;
//using System.Net.Http;
//using System.Text;

//namespace PUBTransfer
//{
//   public class PuffData
//   {
//       public int PuffId { get; set; }
//       public DateTime Start { get; set; }
//       public DateTime End { get; set; }
//       public double Duration { get; set; }
//       public double Volume { get; set; }
//       public double Battery { get; set; }
//       public double XAngle { get; set; }
//       public double VBat { get; set; }
//       public double YAngle { get; set; }
//       public double ZAngle { get; set; }
//       public override string ToString()
//       {
//           return $"Puff {PuffId} | Start={Start:HH:mm:ss} | End={End:HH:mm:ss} | Duration={Duration:F2}s | Battery={Battery:F2}V | Angles=({XAngle:F2}, {YAngle:F2}, {ZAngle:F2})";
//       }
//   }
//   public class BLEDeviceDetails
//   {
//       public IDevice Device { get; set; }
//       public IService PrimaryService { get; set; }
//       public ICharacteristic PrimaryCharacteristic { get; set; }
//       public IDescriptor PrimaryDescriptor { get; set; }
//       public string SerialNumber { get; set; }
//       public int ModelNumber { get; set; }
//       public string FirmwareVersion { get; set; }
//       public string Status { get; set; }
//       public int PuffCountLeft { get; set; }
//       public int DevicePuffCount { get; set; }
//       public int TotalPuffCount { get; set; }
//       public int BatchPuffCount { get; set; }
//       public int BatchPuffCounter { get; set; }
//       public int PuffID { get; set; }
//       public int PuffNum { get; set; }
//       public int RemoveCurrentPuff { get; set; }
//       public int PuffDelay { get; set; }
//       public double VBat { get; set; }
//       // Replace PubRawData array with structured puffs
//       public List<PuffData> Puffs { get; set; } = new List<PuffData>();
//       public string[] Events { get; set; } = new string[500];
//       public int EventTotalCount { get; set; }
//       public int EventBatchSize { get; set; }
//       public int EventCounter { get; set; }
//       public double X_Angle { get; set; }
//       public double Y_Angle { get; set; }
//       public double Z_Angle { get; set; }
//   }
//   public static class Globals
//   {
//       public static string ScreenMode;
//       public static string TopPanel;
//       public static string BottomPanel;
//       public static bool Scanning;
//       public static string serialNumber;
//       public static string PassCode;
//       public static Timer YourTimer;
//       public static string surveySerialNumber;
//       public static DateTime surveySerialDate;
//       public static bool wifiConnected = true;
//       public static BLEDeviceDetails CurrentDevice;
//   }
//   public enum EnvironmentType
//   {
//       DEV,
//       QA,
//       PROD,
//       Nothing
//   }
//   public partial class MainPage : ContentPage
//   {
//       private EnvironmentType currentEnvironment = EnvironmentType.DEV;
//       private readonly IAdapter _bluetoothAdapter;
//       private readonly IBluetoothLE _bluetoothLE;
//       public ObservableCollection<IDevice> Devices { get; set; } = new();
//       public MainPage()
//       {
//           InitializeComponent();
//           DisplayQRCode();
//           _bluetoothLE = CrossBluetoothLE.Current;
//           _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;
//           DevicesListView.ItemsSource = Devices;
//       }
//       private async void OnScanClicked(object sender, EventArgs e)
//       {
//           // Disable the button while scanning
//           ScanButton.IsEnabled = false;
//           ScanButton.Text = "Scanning...";
//           try
//           {
//               var permissionStatus = await RequestBluetoothPermissions();
//               if (permissionStatus != PermissionStatus.Granted)
//               {
//                   await DisplayAlert("Permission Denied", "Bluetooth permissions are required", "OK");
//                   return;
//               }
//               if (!_bluetoothLE.IsOn)
//               {
//                   await DisplayAlert("Bluetooth Off", "Please enable Bluetooth", "OK");
//                   return;
//               }
//               Devices.Clear();
//               _bluetoothAdapter.DeviceDiscovered += (s, a) =>
//               {
//                   // Ensure the device has a non-null name and matches the desired prefix
//                   if (!string.IsNullOrEmpty(a.Device.Name) && a.Device.Name.StartsWith("PUB"))
//                   {
//                       if (!Devices.Contains(a.Device))
//                       {
//                           MainThread.BeginInvokeOnMainThread(() =>
//                           {
//                               Devices.Add(a.Device);
//                           });
//                       }
//                   }
//               };
//               await _bluetoothAdapter.StartScanningForDevicesAsync();
//               //await DisplayAlert("Scan Complete", $"{Devices.Count} devices found.", "OK");
//           }
//           catch (Exception ex)
//           {
//               await DisplayAlert("Error", $"Failed to scan: {ex.Message}", "OK");
//           }
//           finally
//           {
//               // Re-enable after scanning
//               ScanButton.IsEnabled = true;
//               ScanButton.Text = "Scan";
//           }
//       }
//       private async Task<PermissionStatus> RequestBluetoothPermissions()
//       {
//           try
//           {
//#if ANDROID
//               // For Android 12+ (API 31+), we need BLUETOOTH_SCAN and BLUETOOTH_CONNECT
//               if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
//               {
//                   //var scanPermission = await Permissions.RequestAsync<MAUI_Test_Bluetooth.Platforms.Android.BluetoothScanPermission>();
//                   var scanPermission = await Permissions.RequestAsync<Platforms.Android.Permissions.BluetoothScanPermission>();
//                   if (scanPermission != PermissionStatus.Granted)
//                       return scanPermission;
//                   var connectPermission = await Permissions.RequestAsync<Platforms.Android.Permissions.BluetoothConnectPermission>();
//                   if (connectPermission != PermissionStatus.Granted)
//                       return connectPermission;
//               }
//               else
//               {
//                   // For older Android versions, we need location permissions
//                   var locationPermission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
//                   if (locationPermission != PermissionStatus.Granted)
//                       return locationPermission;
//               }
//#endif
//               return PermissionStatus.Granted;
//           }
//           catch (Exception ex)
//           {
//               await DisplayAlert("Permission Error", $"Failed to request permissions: {ex.Message}", "OK");
//               return PermissionStatus.Denied;
//           }
//       }
//       private void OnClearClicked(object sender, EventArgs e)
//       {
//           Devices.Clear();
//       }
//       protected override async void OnAppearing()
//       {
//           base.OnAppearing();
//           await UpdateSurveyAsync();
//       }
//       public async Task UpdateSurveyAsync()
//       {
//#if !FACTORY_MODE
//           string fileName = "PUBserial.txt";
//           string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
//           Console.WriteLine($"----- updateSurvey: {Globals.serialNumber} -----");
//           if (!Globals.wifiConnected)
//           {
//               await UpdateNoSurveyUIAsync();
//               return;
//           }
//           var client = new HttpClient();
//           int envCode = GetEnvironmentCode();
//           string envDomain = GetSurveyDomain();
//           //string urlStr = $"{envDomain}/api/Survey?Serial={Globals.serialNumber}&DBID={envCode}";
//           //hardcode one here to test if you can see things in the webview
//           string urlStr = "https://cme-pub-survey-dev.azurewebsites.net/api/Survey?Serial=e43e12b8ecc515c9&DBID=0";
//           Console.WriteLine("--- URL: " + urlStr);
//           try
//           {
//               var response = await client.GetAsync(urlStr);
//               var responseString = await response.Content.ReadAsStringAsync();
//               int numSurvey = int.TryParse(responseString, out int parsed) ? parsed : 0;
//               await MainThread.InvokeOnMainThreadAsync(() =>
//               {
//                   if (numSurvey > 0)
//                   {
//                       //lblSurvey.Text = numSurvey == 1 ? "1 Survey Available" : $"{numSurvey} Surveys Available";
//                       //btnSurvey.IsVisible = true;
//                       //btnSurvey.Text = numSurvey == 1 ? "View Survey" : "View Surveys";
//                   }
//                   else
//                   {
//                       //lblSurvey.Text = "No Survey Available";
//                       //btnSurvey.IsVisible = false;
//                       //btnSurvey.Text = string.Empty;
//                   }
//               });
//               // Save the current survey count in file
//               await File.WriteAllTextAsync(filePath, Globals.serialNumber + "," + DateTime.UtcNow);
//           }
//           catch (Exception ex)
//           {
//               Console.WriteLine("Survey network error: " + ex.Message);
//               await UpdateNoSurveyUIAsync();
//           }
//#endif
//       }
//       private async Task UpdateNoSurveyUIAsync()
//       {
//           await MainThread.InvokeOnMainThreadAsync(() =>
//           {
//               //lblSurvey.Text = "No Survey Available";
//               //btnSurvey.IsVisible = false;
//               //btnSurvey.Text = string.Empty;
//           });
//           // Reset survey file
//           string fileName = "PUBserial.txt";
//           string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
//           if (File.Exists(filePath))
//               File.Delete(filePath);
//           Globals.surveySerialNumber = string.Empty;
//       }
//       private string GetSurveyDomain()
//       {
//           return currentEnvironment switch
//           {
//               EnvironmentType.DEV => "https://cme-pub-survey-dev.azurewebsites.net",
//               EnvironmentType.QA => "https://cme-pub-survey-qa-e3bfg0g9bjcud5ew.eastus-01.azurewebsites.net",
//               EnvironmentType.PROD => "https://mobilesurveys.azurewebsites.net",
//               _ => ""
//           };
//       }
//       private int GetEnvironmentCode()
//       {
//           return currentEnvironment switch
//           {
//               EnvironmentType.DEV => 0,
//               EnvironmentType.QA => 1,
//               EnvironmentType.PROD => 2,
//               _ => -1
//           };
//       }
//       private void BtnSurvey_Clicked(object sender, EventArgs e)
//       {
//           //if (!string.IsNullOrEmpty(Globals.serialNumber))
//           //{
//           //string surveyUrl = $"{GetSurveyDomain()}/SurveyPage?serial={Globals.serialNumber}";
//           //hard coded for test
//           //string surveyUrl = "https://cme-pub-survey-dev.azurewebsites.net/Survey?Serial=e43e12b8ecc515c9&Code=2333&DBID=0";
//           //webSurvey.Source = surveyUrl;
//           //}
//       }
//       private ImageSource GenerateQRCode(string data)
//       {
//           var qrGenerator = new QRCodeGenerator();
//           var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
//           var qrCode = new PngByteQRCode(qrCodeData);
//           byte[] qrCodeBytes = qrCode.GetGraphic(20);
//           return ImageSource.FromStream(() => new MemoryStream(qrCodeBytes));
//       }
//       private void DisplayQRCode()
//       {
//           string deviceId = Guid.NewGuid().ToString(); // or use Preferences to persist
//           //androidIdLabel.Text = $"Device ID: {deviceId}";
//           //qrCodeImage.Source = GenerateQRCode(deviceId);
//       }
//       private void OnEnvironmentChanged(object sender, CheckedChangedEventArgs e)
//       {
//           var radio = sender as RadioButton;
//           //if (radio.IsChecked)
//           //{
//           //    string selectedEnv = radio.Value.ToString();
//           //    Console.WriteLine($"Selected environment: {selectedEnv}");
//           //}
//       }
//       private void ParseAndStorePuff(string textValue)
//       {
//           try
//           {
//               string[] strSplit = textValue.Split(',');
//               //if (strSplit.Length < 5)
//               //{
//               //    MainThread.BeginInvokeOnMainThread(async () =>
//               //    {
//               //        await Application.Current.MainPage.DisplayAlert(
//               //            "Warning",
//               //            $"strSplit.Length: {strSplit.Length}\n",
//               //            "OK");
//               //    });
//               //    Console.WriteLine("Not enough fields in puff data");
//               //    return;
//               //}
//               // Parse fields (adjust mapping if device spec changes)
//               int puffId = int.Parse(strSplit[1]);
//               //int puffId = int.Parse(strSplit[0]);
//               // og (2)
//               double duration = double.Parse(strSplit[2]); // seconds
//               //double duration = double.Parse(strSplit[9]); // seconds
//               double volume = double.Parse(strSplit[3]);
//               double battery = double.Parse(strSplit[5]);
//               // og (6) is probably vbat
//               //double xAngle = double.Parse(strSplit[6]);
//               // i believe this is the one i have correct, voltage
//               double vbat = double.Parse(strSplit[6]);
//               DateTime start = DateTime.UtcNow;
//               DateTime end = start.AddSeconds(duration);
//               var puff = new PuffData
//               {
//                   PuffId = puffId,
//                   Start = start,
//                   End = end,
//                   Duration = duration,
//                   Volume = volume,
//                   Battery = battery,
//                   //XAngle = xAngle,
//                   VBat = vbat,
//                   YAngle = 0,
//                   ZAngle = 0
//               };
//               Globals.CurrentDevice?.Puffs.Add(puff);
//               Console.WriteLine($"Stored puff: {puff}");
//               MainThread.BeginInvokeOnMainThread(async () =>
//               {
//                   await Application.Current.MainPage.DisplayAlert(
//                       "Puff Data",
//                       $"PUB: {puff.PuffId}\n" +
//                       //$"Duration: {puff.Duration}\n" +
//                       //$"Volume: {puff.Volume}\n" +
//                       //$"Battery: {puff.Battery}\n" +
//                       //$"X Angle: {puff.XAngle}", //not vbat
//                       $"Voltage: {puff.VBat}",
//                       "OK");
//               });
//           }
//           catch (Exception ex)
//           {
//               Console.WriteLine($"Error parsing puff data: {ex.Message}");
//           }
//       }





//        //confirm the header here to get all of the data, right now you only get the header data point
//        //private async void OnDeviceSelected(object sender, ItemTappedEventArgs e)
//        //{
//        //    if (e.Item is IDevice selectedDevice)
//        //    {
//        //        try
//        //        {
//        //            await DisplayAlert("Connecting", $"Connecting to {selectedDevice.Name}...", "OK");
//        //            // Connect
//        //            await _bluetoothAdapter.ConnectToDeviceAsync(selectedDevice);
//        //            await DisplayAlert("Connected", $"Connected to {selectedDevice.Name}", "OK");
//        //            var allData = new StringBuilder();
//        //            // Discover services
//        //            var services = await selectedDevice.GetServicesAsync();
//        //            foreach (var service in services)
//        //            {
//        //                allData.AppendLine($"Service: {service.Id}");
//        //                var characteristics = await service.GetCharacteristicsAsync();
//        //                foreach (var characteristic in characteristics)
//        //                {
//        //                    allData.AppendLine($"  Characteristic: {characteristic.Id}, CanRead={characteristic.CanRead}, CanUpdate={characteristic.CanUpdate}");
//        //                    // 1. Read once if readable
//        //                    if (characteristic.CanRead)
//        //                    {
//        //                        try
//        //                        {
//        //                            var (data, resultCode) = await characteristic.ReadAsync();
//        //                            if (resultCode == 0 && data != null && data.Length > 0)
//        //                            {
//        //                                string textValue = Encoding.UTF8.GetString(data);
//        //                                string hexValue = BitConverter.ToString(data);
//        //                                allData.AppendLine($"    [Read] Text: {textValue}");
//        //                                allData.AppendLine($"    [Read] Hex: {hexValue}");
//        //                                // Example: detect puff data
//        //                                if (textValue.StartsWith("PUB"))
//        //                                {
//        //                                    ParseAndStorePuff(textValue);
//        //                                }
//        //                            }
//        //                            else
//        //                            {
//        //                                allData.AppendLine($"    No data. ResultCode={resultCode}");
//        //                            }
//        //                        }
//        //                        catch (Exception readEx)
//        //                        {
//        //                            allData.AppendLine($"    Failed to read {characteristic.Id}: {readEx.Message}");
//        //                        }
//        //                    }
//        //                    // 2. Subscribe to notifications if possible
//        //                    if (characteristic.CanUpdate)
//        //                    {
//        //                        characteristic.ValueUpdated += (s, args) =>
//        //                        {
//        //                            var updatedData = args.Characteristic.Value;
//        //                            if (updatedData != null && updatedData.Length > 0)
//        //                            {
//        //                                string updatedHex = BitConverter.ToString(updatedData);
//        //                                string updatedText = Encoding.UTF8.GetString(updatedData);
//        //                                //Device.BeginInvokeOnMainThread(() =>
//        //                                //{
//        //                                //    allData.AppendLine($"    [Notify] {characteristic.Id}: {updatedHex} / {updatedText}");
//        //                                //});
//        //                                Dispatcher.Dispatch(() =>
//        //                                {
//        //                                    allData.AppendLine($"    [Notify] {characteristic.Id}: {updatedHex} / {updatedText}");
//        //                                });
//        //                            }
//        //                        };
//        //                        await characteristic.StartUpdatesAsync();
//        //                        allData.AppendLine($"    Subscribed to notifications for {characteristic.Id}");
//        //                    }
//        //                }
//        //            }
//        //            // Show snapshot of what was read (notifications will keep flowing after)
//        //            await DisplayAlert("Device Data", allData.ToString(), "OK");
//        //            // Save globally if needed
//        //            Globals.serialNumber = selectedDevice.Id.ToString();
//        //        }
//        //        catch (Exception ex)
//        //        {
//        //            await DisplayAlert("Error", $"Failed to connect: {ex.Message}", "OK");
//        //        }
//        //    }
//        //}
//        private async void OnDeviceSelected(object sender, ItemTappedEventArgs e)
//        {
//            if (e.Item is IDevice selectedDevice)
//            {
//                try
//                {
//                    await DisplayAlert("Connecting", $"Connecting to {selectedDevice.Name}...", "OK");

//                    // Connect
//                    await _bluetoothAdapter.ConnectToDeviceAsync(selectedDevice);
//                    await DisplayAlert("Connected", $"Connected to {selectedDevice.Name}", "OK");

//                    var allData = new StringBuilder();
//                    var services = await selectedDevice.GetServicesAsync();

//                    foreach (var service in services)
//                    {
//                        allData.AppendLine($"Service: {service.Id}");
//                        var characteristics = await service.GetCharacteristicsAsync();

//                        foreach (var characteristic in characteristics)
//                        {
//                            allData.AppendLine($"  Characteristic: {characteristic.Id}, CanRead={characteristic.CanRead}, CanUpdate={characteristic.CanUpdate}");

//                            // >>> If this is the characteristic you normally wrote to, send confirm header
//                            if (characteristic.CanWrite) // <-- adjust if your library uses different flag
//                            {
//                                await SendConfirmHeaderAsync(
//                                    characteristic,
//                                    vusePROFlag: true,                    // or false depending on device type
//                                    devicePuffCount: 0,                    // replace with actual puff count if you track it
//                                    serialNumber: selectedDevice.Id.ToString()
//                                );
//                            }

//                            // Read once if readable
//                            if (characteristic.CanRead)
//                            {
//                                try
//                                {
//                                    var (data, resultCode) = await characteristic.ReadAsync();
//                                    if (resultCode == 0 && data != null && data.Length > 0)
//                                    {
//                                        string textValue = Encoding.UTF8.GetString(data);
//                                        string hexValue = BitConverter.ToString(data);
//                                        allData.AppendLine($"    [Read] Text: {textValue}");
//                                        allData.AppendLine($"    [Read] Hex: {hexValue}");

//                                        if (textValue.StartsWith("PUB"))
//                                        {
//                                            ParseAndStorePuff(textValue);
//                                        }
//                                    }
//                                    else
//                                    {
//                                        allData.AppendLine($"    No data. ResultCode={resultCode}");
//                                    }
//                                }
//                                catch (Exception readEx)
//                                {
//                                    allData.AppendLine($"    Failed to read {characteristic.Id}: {readEx.Message}");
//                                }
//                            }

//                            // Subscribe to notifications
//                            if (characteristic.CanUpdate)
//                            {
//                                characteristic.ValueUpdated += (s, args) =>
//                                {
//                                    var updatedData = args.Characteristic.Value;
//                                    if (updatedData != null && updatedData.Length > 0)
//                                    {
//                                        string updatedHex = BitConverter.ToString(updatedData);
//                                        string updatedText = Encoding.UTF8.GetString(updatedData);

//                                        Dispatcher.Dispatch(() =>
//                                        {
//                                            allData.AppendLine($"    [Notify] {characteristic.Id}: {updatedHex} / {updatedText}");
//                                        });
//                                    }
//                                };

//                                await characteristic.StartUpdatesAsync();
//                                allData.AppendLine($"    Subscribed to notifications for {characteristic.Id}");
//                            }
//                        }
//                    }

//                    await DisplayAlert("Device Data", allData.ToString(), "OK");
//                    Globals.serialNumber = selectedDevice.Id.ToString();
//                }
//                catch (Exception ex)
//                {
//                    await DisplayAlert("Error", $"Failed to connect: {ex.Message}", "OK");
//                }
//            }
//        }
//        private async Task SendConfirmHeaderAsync(ICharacteristic characteristic, bool vusePROFlag, int devicePuffCount, string serialNumber)
//        {
//            try
//            {
//                string timeStamp = vusePROFlag
//                    ? DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss")
//                    : DateTime.UtcNow.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss");
//                string responseString;
//                if (devicePuffCount == 0)
//                    responseString = $"2,{serialNumber},{timeStamp},005";
//                else
//                    responseString = $"4,{serialNumber},{timeStamp},005";
//                byte[] data = Encoding.UTF8.GetBytes(responseString);
//                await characteristic.WriteAsync(data);
//                Console.WriteLine($"[ConfirmHeader Sent] {responseString}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[ConfirmHeader Error] {ex}");
//            }
//        }








//        //private void ShowCollectedData()
//        //{
//        //    var device = Globals.CurrentDevice;
//        //    if (device == null)
//        //    {
//        //        DisplayAlert("No Device", "No device connected or data collected yet.", "OK");
//        //        return;
//        //    }

//        //    var sb = new StringBuilder();
//        //    sb.AppendLine($"X Angle: {device.X_Angle:F2}");
//        //    sb.AppendLine($"Y Angle: {device.Y_Angle:F2}");
//        //    sb.AppendLine($"Z Angle: {device.Z_Angle:F2}");
//        //    sb.AppendLine($"Serial: {device.SerialNumber}"); //this seems to be correct
//        //    sb.AppendLine($"Model: {device.ModelNumber}");
//        //    sb.AppendLine($"Firmware: {device.FirmwareVersion}");
//        //    sb.AppendLine($"Status: {device.Status}");
//        //    sb.AppendLine($"Total Puffs: {device.TotalPuffCount}");
//        //    sb.AppendLine($"Puffs Left: {device.PuffCountLeft}");
//        //    sb.AppendLine($"Battery: {device.VBat} V");
//        //    sb.AppendLine($"Event Count: {device.EventTotalCount}");
//        //    sb.AppendLine();
//        //    sb.AppendLine("=== Puff Data ===");

//        //    //for (int i = 0; i < device.Puffs.Count; i++)
//        //    //{
//        //    //    var puff = device.Puffs[i];
//        //    //    sb.AppendLine($"{i + 1}: Start={puff.Start:yyyy-MM-dd HH:mm:ss}, End={puff.End:yyyy-MM-dd HH:mm:ss}, X={puff.XAngle:F2}, Y={puff.YAngle:F2}, Z={puff.ZAngle:F2}");
//        //    //}
//        //    foreach (var puff in device.Puffs)
//        //    {
//        //        sb.AppendLine(puff.ToString());
//        //    }

//        //    string output = sb.ToString();

//        //    Navigation.PushAsync(new ContentPage
//        //    {
//        //        Title = "Collected Data",
//        //        Content = new ScrollView
//        //        {
//        //            Content = new Label
//        //            {
//        //                Text = output,
//        //                FontSize = 14,
//        //                Margin = 10
//        //            }
//        //        }
//        //    });
//        //}
//        //private void OnShowDataClicked(object sender, EventArgs e)
//        //{
//        //    ShowCollectedData();
//        //}
//    }
//}

//TODO: confirm header to allow all data to come in
//oncharacteristicchanged is not used for VUSE

































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



































using Microsoft.Maui.ApplicationModel;
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
using static Android.Preferences.PreferenceActivity;

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







        //new stuff testing
        //make this read the whole header, all its getting now is PUB4825
        //the pub is sending the header in chunks, right now you only catch the first chunk but you need to get all of it
        private async Task<string> ReadHeaderAsync(ICharacteristic characteristic)
        {
            //Console.WriteLine($"[Characteristic] {await characteristic.ReadAsync()}");
            //right now data only had PUB4825 in it, thats why it is length 7. It needs to have PUB,0065,6,100,33,6.0,4.370 in it


            var (data, result) = await characteristic.ReadAsync();
            await DisplayAlert("Connecting", $"data {data}", "OK");
            if (result == 0 && data != null && data.Length > 0)
            {
                string header = Encoding.UTF8.GetString(data);
                Console.WriteLine($"[Header Received] {header}");
                return header;
            }
            throw new Exception("Failed to read header.");
        }







        //private TaskCompletionSource<string> _headerTcs;

        //private async Task<string> ReadHeaderAsync(ICharacteristic characteristic)
        //{
        //    _headerTcs = new TaskCompletionSource<string>();

        //    void Handler(object sender, CharacteristicUpdatedEventArgs args)
        //    {
        //        string chunk = Encoding.UTF8.GetString(args.Characteristic.Value);
        //        Console.WriteLine($"[Header Chunk] {chunk}");

        //        // accumulate until you have all 7 fields (6 commas)
        //        if (chunk.Count(c => c == ',') >= 6)
        //        {
        //            characteristic.ValueUpdated -= Handler;
        //            _headerTcs.SetResult(chunk);
        //        }
        //    }

        //    characteristic.ValueUpdated += Handler;
        //    await characteristic.StartUpdatesAsync(); // subscribe to notifications

        //    return await _headerTcs.Task;
        //}
        //private async Task<string> ReadHeaderAsync(ICharacteristic characteristic)
        //{
        //    string header = "";
        //    int maxAttempts = 5;

        //    for (int i = 0; i < maxAttempts; i++)
        //    {
        //        var (data, result) = await characteristic.ReadAsync();
        //        if (result == 0 && data != null && data.Length > 0)
        //        {
        //            header += Encoding.UTF8.GetString(data);

        //            // Check if we have all 7 fields (6 commas)
        //            if (header.Count(c => c == ',') >= 6)
        //            {
        //                Console.WriteLine($"[Full Header Received] {header}");
        //                return header;
        //            }
        //        }

        //        // Small delay before trying again
        //        await Task.Delay(100);
        //    }

        //    throw new Exception("Failed to read full header after multiple attempts.");
        //}





        private async Task AckHeaderAsync(ICharacteristic characteristic, string serialNumber)
        {
            string timeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            string responseString = $"4,{serialNumber},{timeStamp},005";
            byte[] payload = Encoding.UTF8.GetBytes(responseString);
            await characteristic.WriteAsync(payload);
            Console.WriteLine($"[Header Ack Sent] {responseString}");
        }
        private async Task<List<string>> ReadDataBatchAsync(ICharacteristic characteristic, int expectedCount)
        {
            var dataPoints = new List<string>();
            for (int i = 0; i < expectedCount; i++)
            {
                var (data, result) = await characteristic.ReadAsync();
                if (result == 0 && data != null && data.Length > 0)
                {
                    string record = Encoding.UTF8.GetString(data);
                    Console.WriteLine($"[Data Received] {record}");
                    if (record.StartsWith("DATA"))
                        dataPoints.Add(record);
                }
                else
                {
                    Console.WriteLine($"[Read Error] iteration {i}, result={result}");
                }
            }
            return dataPoints;
        }
        private async Task ConfirmBatchAsync(ICharacteristic characteristic, int batchSize)
        {
            // Protocol: "1,<BatchSize>" tells device to erase the uploaded data
            string responseString = $"1,{batchSize}";
            byte[] payload = Encoding.UTF8.GetBytes(responseString);
            await characteristic.WriteAsync(payload);
            Console.WriteLine($"[Batch Confirm Sent] {responseString}");
        }
        private async void OnDeviceSelected(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is IDevice selectedDevice && !_isCollectingData)
            {
                _isCollectingData = true;

                try
                {
                    await DisplayAlert("Connecting", $"Connecting to {selectedDevice.Name}...", "OK");

                    // 1. Connect to device
                    await _bluetoothAdapter.ConnectToDeviceAsync(selectedDevice);



                    // Ensure the device name is long enough before slicing
                    //string serialNumber = selectedDevice.Name.Length > 3
                    //    ? selectedDevice.Name.Substring(3)
                    //    : string.Empty;

                    //_currentDevice = new BLEDeviceDetails
                    //{
                    //    Device = selectedDevice,
                    //    Status = "Connected",
                    //    SerialNumber = serialNumber, //only works for pubs with a name formatted like a VUSE Pro
                    //    TransferTime = DateTime.UtcNow
                    //};
                    //put a break here to see what the structure of this is
                    _currentDevice = new BLEDeviceDetails
                    {
                        Device = selectedDevice,
                        Status = "Connected",
                        SerialNumber = selectedDevice.Name.Substring(3), // Extract from PUBxxxx
                        TransferTime = DateTime.UtcNow
                    };


                    Globals.CurrentDevice = _currentDevice;

                    await DisplayAlert("Connected", $"Connected to {selectedDevice.Name}", "OK");

                    // 2. Discover services and find the characteristic
                    var services = await selectedDevice.GetServicesAsync();
                    foreach (var service in services)
                    {
                        var characteristics = await service.GetCharacteristicsAsync();

                        // Find the characteristic that supports both Read + Write
                        var commChar = characteristics.FirstOrDefault(c => c.CanRead && c.CanWrite);
                        if (commChar != null)
                        {
                            // === Step 1: Read Header ===
                            //this just has the first point in the header in it but the code below assumes it was all of the data and then goes on to try to split that by comma
                            //if you make sure the header has the rest of the data in it here and not the first value, this will likely work
                            var header = await ReadHeaderAsync(commChar);
                            await DisplayAlert("Error", $"Header: {header}", "fuck off");
                            var parts = header.Split(',');
                            string serial = parts[1];
                            int batchSize = int.Parse(parts[3]);   // Batch_Size
                            int puffCount = int.Parse(parts[4]);   // Puff_Count

                            // === Step 2: Ack Header ===
                            await AckHeaderAsync(commChar, serial);

                            // === Step 3: Read Data Points ===
                            var dataPoints = await ReadDataBatchAsync(commChar, batchSize);

                            // === Step 4: Confirm Batch ===
                            await ConfirmBatchAsync(commChar, batchSize);

                            // (Optional) Process/store results
                            Console.WriteLine($"[Transfer Complete] {dataPoints.Count} data points received.");

                            break; // done with this service
                        }
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to connect: {ex.Message}", "fuck off");
                }
                finally
                {
                    _isCollectingData = false;
                }
            }
        }


        //private async void OnDeviceSelected(object sender, ItemTappedEventArgs e)
        //{
        //    if (e.Item is IDevice selectedDevice && !_isCollectingData)
        //    {
        //        _isCollectingData = true;

        //        try
        //        {
        //            await DisplayAlert("Connecting", $"Connecting to {selectedDevice.Name}...", "OK");

        //            // Connect to device
        //            await _bluetoothAdapter.ConnectToDeviceAsync(selectedDevice);

        //            // Initialize device details
        //            _currentDevice = new BLEDeviceDetails
        //            {
        //                Device = selectedDevice,
        //                Status = "Connected",
        //                SerialNumber = selectedDevice.Name.Substring(3), // Extract from PUB1234
        //                TransferTime = DateTime.UtcNow
        //            };

        //            Globals.CurrentDevice = _currentDevice;

        //            await DisplayAlert("Connected", $"Connected to {selectedDevice.Name}", "OK");

        //            // Discover services
        //            var services = await selectedDevice.GetServicesAsync();
        //            ICharacteristic commChar = null;

        //            foreach (var service in services)
        //            {
        //                var characteristics = await service.GetCharacteristicsAsync();

        //                // Find the writable characteristic used for PUB handshakes
        //                commChar = characteristics.FirstOrDefault(c => c.CanWrite && c.CanUpdate);
        //                if (commChar != null)
        //                    break;
        //            }

        //            if (commChar == null)
        //            {
        //                await DisplayAlert("Error", "Could not find suitable characteristic", "OK");
        //                return;
        //            }

        //            _writeCharacteristic = commChar;
        //            _currentDevice.PrimaryCharacteristic = commChar;

        //            // Start updates to receive notifications
        //            await commChar.StartUpdatesAsync();

        //            // Send initial ACK header
        //            await SendConfirmHeaderAsync(
        //                commChar,
        //                vusePROFlag: true,
        //                devicePuffCount: 0,
        //                serialNumber: _currentDevice.SerialNumber
        //            );

        //            // Read the full header
        //            //stops here?
        //            //await DisplayAlert("Error", $"commChar: {commChar}", "OK");
        //            var header = await ReadFullHeaderAsync(commChar);

        //            // Split header into fields safely
        //            var parts = header.Split(',');
        //            if (parts.Length < 7)
        //            {
        //                await DisplayAlert("Error", $"Invalid header received: {header}", "OK");
        //                return;
        //            }

        //            // Parse the header into device info
        //            _currentDevice.SerialNumber = parts[1];
        //            _currentDevice.ModelNumber = int.Parse(parts[2]);
        //            _currentDevice.BatchPuffCount = int.Parse(parts[3]);
        //            _currentDevice.DevicePuffCount = int.Parse(parts[4]);
        //            _currentDevice.FirmwareVersion = parts[5];
        //            _currentDevice.VBat = double.Parse(parts[6]);

        //            _currentDevice.TotalPuffCount = _currentDevice.DevicePuffCount;
        //            _currentDevice.PuffCountLeft = _currentDevice.DevicePuffCount;

        //            _logData.AppendLine($"Header received: {header}");

        //            // Start the main data collection loop
        //            await StartDataCollection(selectedDevice);
        //        }
        //        catch (Exception ex)
        //        {
        //            await DisplayAlert("Error", $"Failed to connect: {ex.Message}", "OK");
        //        }
        //        finally
        //        {
        //            _isCollectingData = false;
        //        }
        //    }
        //}
        // New helper to accumulate header chunks
        //this is not processing things correctly
        //private async Task<string> ReadFullHeaderAsync(ICharacteristic characteristic)
        //{
        //    var tcs = new TaskCompletionSource<string>();
        //    var buffer = new StringBuilder();

        //    EventHandler<CharacteristicUpdatedEventArgs> handler = null;
        //    handler = (s, e) =>
        //    {
        //        if (e.Characteristic.Id == characteristic.Id)
        //        {
        //            var chunk = Encoding.UTF8.GetString(e.Characteristic.Value);
        //            buffer.Append(chunk);

        //            // Header complete? (7 fields always)
        //            if (buffer.ToString().Count(c => c == ',') >= 6)
        //            {
        //                characteristic.ValueUpdated -= handler;
        //                tcs.TrySetResult(buffer.ToString());
        //            }
        //        }
        //    };

        //    characteristic.ValueUpdated += handler;

        //    return await tcs.Task;
        //}
        //end new stuff testing




        //find out what format this response needs to be in
        private async Task SendConfirmHeaderAsync(ICharacteristic characteristic, bool vusePROFlag, int devicePuffCount, string serialNumber)
        {
            try
            {
                string timeStamp;

                if (vusePROFlag)
                    timeStamp = DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss"); // UTC for VusePRO
                else
                    timeStamp = DateTime.UtcNow.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss"); // Local for others

                //string responseString = devicePuffCount == 0
                //    ? $"2,{serialNumber},{timeStamp},005"
                //    : $"4,{serialNumber},{timeStamp},005";

                string responseString = "4," + serialNumber + "," + timeStamp + ",005";
                //string responseString = "2," + serialNumber + "," + timeStamp + ",005";
                //after this is sent i need to start reading again

                byte[] data = Encoding.UTF8.GetBytes(responseString);
                await characteristic.WriteAsync(data);

                Console.WriteLine($"[Handshake Sent] {responseString}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Handshake Error] {ex}");
            }
        }

        //private async void OnDeviceSelected(object sender, ItemTappedEventArgs e)
        //{
        //    if (e.Item is IDevice selectedDevice && !_isCollectingData)
        //    {
        //        _isCollectingData = true;

        //        try
        //        {
        //            await DisplayAlert("Connecting", $"Connecting to {selectedDevice.Name}...", "OK");

        //            // Connect to device
        //            await _bluetoothAdapter.ConnectToDeviceAsync(selectedDevice);

        //            // Initialize device details
        //            _currentDevice = new BLEDeviceDetails
        //            {
        //                Device = selectedDevice,
        //                Status = "Connected",
        //                SerialNumber = selectedDevice.Name.Substring(3), // Extract from PUBxxxx
        //                TransferTime = DateTime.UtcNow
        //            };

        //            Globals.CurrentDevice = _currentDevice;

        //            await DisplayAlert("Connected", $"Connected to {selectedDevice.Name}", "OK");

        //            // Discover services
        //            var services = await selectedDevice.GetServicesAsync();
        //            foreach (var service in services)
        //            {
        //                var characteristics = await service.GetCharacteristicsAsync();

        //                // Find the writable characteristic used for PUB handshakes
        //                var writable = characteristics.FirstOrDefault(c => c.CanWrite);
        //                if (writable != null)
        //                {
        //                    // Send confirm header before starting data collection
        //                    await SendConfirmHeaderAsync(
        //                        writable,
        //                        vusePROFlag: true,   // set based on your device type
        //                        devicePuffCount: 0,   // or pull from your device object if you track it
        //                        serialNumber: _currentDevice.SerialNumber
        //                    );
        //                    break; // handshake sent, no need to continue searching
        //                }
        //            }

        //            // Start the data collection process
        //            await StartDataCollection(selectedDevice);
        //        }
        //        catch (Exception ex)
        //        {
        //            await DisplayAlert("Error", $"Failed to connect: {ex.Message}", "OK");
        //        }
        //        finally
        //        {
        //            _isCollectingData = false;
        //        }
        //    }
        //}

        private async Task StartDataCollection(IDevice device)
        {
            try
            {
                var services = await device.GetServicesAsync();

                foreach (var service in services)
                {
                    var characteristics = await service.GetCharacteristicsAsync();

                    foreach (var characteristic in characteristics)
                    {
                        // Find the primary characteristic for communication
                        if (characteristic.CanUpdate && characteristic.CanWrite)
                        {
                            _writeCharacteristic = characteristic;
                            _currentDevice.PrimaryCharacteristic = characteristic;
                            _currentDevice.PrimaryService = service;

                            // Subscribe to notifications
                            characteristic.ValueUpdated += OnCharacteristicValueUpdated;
                            await characteristic.StartUpdatesAsync();

                            _logData.AppendLine($"Subscribed to characteristic: {characteristic.Id}");

                            // Start the protocol by sending an initial read or waiting for first message

                            //i need to send the first handshake back here, find out what it wants back (never mind handshake was already sent before this function was called)
                            break;
                        }
                    }

                    if (_writeCharacteristic != null)
                        break;
                }

                if (_writeCharacteristic == null)
                {
                    await DisplayAlert("Error", "Could not find suitable characteristic", "OK");
                    return;
                }

                _logData.AppendLine("Starting data collection protocol...");

                // Wait for initial header or send a trigger command
                //the header has already been sent and the handshake was completed at this point
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to start data collection: {ex.Message}", "OK");
            }
        }

        private async void OnCharacteristicValueUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            var data = e.Characteristic.Value;
            if (data == null || data.Length == 0) return;

            string textValue = Encoding.UTF8.GetString(data);
            string hexValue = BitConverter.ToString(data);

            _logData.AppendLine($"Received: {textValue}");

            // Process the received data based on the protocol
            await ProcessReceivedData(textValue.Trim());
        }

        private async Task ProcessReceivedData(string data)
        {
            try
            {
                string[] strSplit = data.Split(',');

                if (strSplit.Length < 2) return;

                string messageType = strSplit[0].Trim();

                switch (messageType)
                {
                    case "2": // PUB Header
                        await ProcessPUBHeader(strSplit);
                        break;

                    case "3": // PUFF Header  
                        await ProcessPUFFHeader(strSplit);
                        break;

                    case "4": // Raw Data
                        await ProcessRawData(strSplit);
                        break;

                    case "5": // Event Data
                        await ProcessEventData(strSplit);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logData.AppendLine($"Error processing data: {ex.Message}");
            }
        }

        private async Task ProcessPUBHeader(string[] strSplit)
        {
            try
            {
                // 2,PUB,1234,5,0,0500,5.0,3.700
                _currentDevice.ModelNumber = Convert.ToInt32(strSplit[2].Trim());

                string serial = strSplit[1].Length < 4
                    ? strSplit[1].PadLeft(4, '0')
                    : strSplit[1];

                _currentDevice.SerialNumber = serial;

                if (_currentDevice.ModelNumber == 2 || _currentDevice.ModelNumber == 3 || _currentDevice.ModelNumber == 5)
                {
                    _currentDevice.EventTotalCount = Convert.ToInt32(strSplit[3].Trim());
                    _currentDevice.EventBatchSize = 100;
                    _currentDevice.Events = new string[_currentDevice.EventTotalCount + 1];
                    _currentDevice.EventCounter = 0;
                    _currentDevice.BatchPuffCount = 0;
                }
                else
                {
                    _currentDevice.BatchPuffCount = Convert.ToInt32(strSplit[3].Trim());
                }

                _currentDevice.DevicePuffCount = Convert.ToInt32(strSplit[4].Trim());
                _currentDevice.TotalPuffCount = _currentDevice.DevicePuffCount;
                _currentDevice.PuffCountLeft = _currentDevice.DevicePuffCount;

                if (_currentDevice.ModelNumber == 2 || _currentDevice.ModelNumber == 3 || _currentDevice.ModelNumber == 5)
                    _currentDevice.PuffCountLeft++;

                _currentDevice.PuffID = _currentDevice.DevicePuffCount + 1;
                _currentDevice.PuffNum = 0;
                _currentDevice.PubRawData = new string[_currentDevice.BatchPuffCount + 1];
                _currentDevice.FirmwareVersion = strSplit[5].Trim();
                _currentDevice.VBat = Convert.ToDouble(strSplit[6].Trim());

                _logData.AppendLine($"PUB Header processed - Model: {_currentDevice.ModelNumber}, Serial: {_currentDevice.SerialNumber}, Puffs: {_currentDevice.DevicePuffCount}");

                // Send confirmation
                await SendConfirmHeader();
            }
            catch (Exception ex)
            {
                _logData.AppendLine($"Error processing PUB header: {ex.Message}");
            }
        }

        private async Task ProcessPUFFHeader(string[] strSplit)
        {
            try
            {
                // 3,PUFF,1234,5,0200,12/12/2020 12:59:59.100,359.9,359.9,359.9
                await Task.Delay(500);

                _currentDevice.BatchPuffCount = Convert.ToInt32(strSplit[4].Trim());
                _currentDevice.PubRawData = new string[_currentDevice.BatchPuffCount + 1];

                // Parse datetime based on model
                if (_currentDevice.ModelNumber == 1 || _currentDevice.ModelNumber == 4)
                {
                    DateTime time = DateTime.ParseExact(strSplit[5], "M/d/yyyy H:m:s", CultureInfo.InvariantCulture);
                    _currentDevice.PuffDateTime = Convert.ToDateTime(time.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else if (_currentDevice.ModelNumber == 5)
                {
                    DateTime time = DateTime.ParseExact(strSplit[5], "MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    _currentDevice.PuffDateTime = Convert.ToDateTime(time.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    _currentDevice.PuffCountLeft--;
                }
                else if (_currentDevice.ModelNumber == 2 || _currentDevice.ModelNumber == 3)
                {
                    string fixedDatetime = strSplit[5].Substring(0, 23);
                    DateTime time = DateTime.ParseExact(fixedDatetime, "MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    _currentDevice.PuffDateTime = Convert.ToDateTime(time.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    _currentDevice.PuffCountLeft--;
                }

                // Store angles for models that provide them
                if (_currentDevice.ModelNumber == 1 || _currentDevice.ModelNumber == 4 || _currentDevice.ModelNumber == 5)
                {
                    _currentDevice.X_Angle = Convert.ToDouble(strSplit[6]);
                    _currentDevice.Y_Angle = Convert.ToDouble(strSplit[7]);
                    _currentDevice.Z_Angle = Convert.ToDouble(strSplit[8]);
                }

                _currentDevice.TransferTime = DateTime.UtcNow;
                _currentDevice.PuffID--;
                _currentDevice.PuffNum++;

                _logData.AppendLine($"PUFF Header processed - Batch Count: {_currentDevice.BatchPuffCount}");
            }
            catch (Exception ex)
            {
                _logData.AppendLine($"Error processing PUFF header: {ex.Message}");
            }
        }

        private async Task ProcessRawData(string[] strSplit)
        {
            try
            {
                _currentDevice.PUBDataCounter++;
                _currentDevice.PuffCountLeft--;

                string insertString = "";

                if (_currentDevice.ModelNumber == 2) // GLO
                {
                    insertString = await ProcessGLOData(strSplit);
                }
                else if (_currentDevice.ModelNumber == 3) // Combustible
                {
                    insertString = await ProcessCombustibleData(strSplit);
                }
                else if (_currentDevice.ModelNumber == 5) // Harmony
                {
                    insertString = await ProcessHarmonyData(strSplit);
                }
                else // Models 1, 4, 6
                {
                    insertString = await ProcessModel1Data(strSplit);
                }

                if (!string.IsNullOrEmpty(insertString))
                {
                    _currentDevice.PubRawData[_currentDevice.PUBDataCounter] = insertString;

                    // Convert to PuffData object
                    var puffData = ParseInsertStringToPuffData(insertString);
                    if (puffData != null)
                    {
                        _currentDevice.Puffs.Add(puffData);
                    }
                }

                _logData.AppendLine($"Got {_currentDevice.PuffNum} of {_currentDevice.TotalPuffCount}");

                // Check if we need to send batch confirmation or finish
                if (_currentDevice.PUBDataCounter == _currentDevice.BatchPuffCount)
                {
                    if (_currentDevice.TotalPuffCount != _currentDevice.PUBDataCounter)
                    {
                        await SendBatchConfirmation();
                    }
                }

                if (_currentDevice.TotalPuffCount == _currentDevice.PUBDataCounter)
                {
                    await SendFinalConfirmation();
                    await ShowCollectedData();
                }
            }
            catch (Exception ex)
            {
                _logData.AppendLine($"Error processing raw data: {ex.Message}");
            }
        }

        private async Task<string> ProcessHarmonyData(string[] strSplit)
        {
            // 4, DATA, 999, 5.000, 2.123, 10000, 1000.0, 99
            double volts = Math.Round(double.Parse(strSplit[2]) / 1000, 3);
            double current = Math.Floor(double.Parse(strSplit[3]));
            const float HARMONY_PRESSURE_FLOW = 0.175f;
            float harmonyFlow = float.Parse(strSplit[4]) * HARMONY_PRESSURE_FLOW;

            double offsetTime = double.Parse(strSplit[1]);
            string dateTimePlusOffset = _currentDevice.PuffDateTime.AddMilliseconds(offsetTime).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");

            var puffData = new PuffData
            {
                PuffId = _currentDevice.PuffID,
                Start = DateTime.Parse(dateTimePlusOffset),
                End = DateTime.Parse(dateTimePlusOffset).AddMilliseconds(offsetTime),
                Battery = volts,
                Current = current,
                Pressure = double.Parse(strSplit[4]),
                Flow = harmonyFlow,
                Power = double.Parse(strSplit[5]),
                RTD = int.Parse(strSplit[6].Trim()),
                XAngle = _currentDevice.X_Angle,
                YAngle = _currentDevice.Y_Angle,
                ZAngle = _currentDevice.Z_Angle,
                VBat = _currentDevice.VBat
            };

            return $"{puffData.PuffId},{_currentDevice.SerialNumber},{offsetTime},{dateTimePlusOffset},{volts},{current},{puffData.Pressure},{harmonyFlow},{puffData.Power},{puffData.RTD}";
        }

        private async Task<string> ProcessModel1Data(string[] strSplit)
        {
            // Handle Model 1/4/6 data format
            // This would be similar to your getRawData1 method

            if (strSplit[1].Contains("0/0/2000"))
            {
                _currentDevice.PuffCountLeft--;
                _currentDevice.TotalPuffCount--;
                return "";
            }

            DateTime start = DateTime.ParseExact(strSplit[1], "M/d/yyyy H:m:s", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime end = start.AddSeconds(Double.Parse(strSplit[9])).ToUniversalTime();

            var puffData = new PuffData
            {
                PuffId = int.Parse(strSplit[2]),
                Start = start,
                End = end,
                Duration = double.Parse(strSplit[9]),
                Volume = double.Parse(strSplit[3]),
                Battery = double.Parse(strSplit[4]),
                XAngle = double.Parse(strSplit[6]),
                VBat = _currentDevice.VBat
            };

            string insertString = $"{_currentDevice.SerialNumber},{start:yyyy-MM-dd HH:mm:ss},{strSplit[2]},{strSplit[3]},{strSplit[4]},{strSplit[6]},{strSplit[5]},{strSplit[8]},{strSplit[7]},{end:yyyy-MM-dd HH:mm:ss},{strSplit[9]},{_currentDevice.VBat}";

            return insertString;
        }

        private async Task<string> ProcessGLOData(string[] strSplit)
        {
            // Similar to Harmony but with GLO-specific fields
            // Implementation would follow your getRawData2 GLO section
            return "";
        }

        private async Task<string> ProcessCombustibleData(string[] strSplit)
        {
            // Similar processing for combustible model
            // Implementation would follow your getRawData2 CMB section
            return "";
        }

        private PuffData ParseInsertStringToPuffData(string insertString)
        {
            try
            {
                // Parse the insert string back to PuffData
                // This depends on the format of your insertString
                return new PuffData(); // Placeholder
            }
            catch
            {
                return null;
            }
        }

        private async Task SendConfirmHeader()
        {
            try
            {
                string timeStamp = DateTime.UtcNow.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss");
                string responseString;

                if (_currentDevice.DevicePuffCount == 0)
                    responseString = $"2,{_currentDevice.SerialNumber},{timeStamp},005";
                else
                    responseString = $"4,{_currentDevice.SerialNumber},{timeStamp},005";

                await Task.Delay(500);

                byte[] data = Encoding.UTF8.GetBytes(responseString);
                await _writeCharacteristic.WriteAsync(data);

                _logData.AppendLine($"Sent confirm header: {responseString}");
            }
            catch (Exception ex)
            {
                _logData.AppendLine($"Error sending confirm header: {ex.Message}");
            }
        }

        private async Task SendBatchConfirmation()
        {
            try
            {
                string timeStamp = DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss");
                string responseString = $"1,{_currentDevice.SerialNumber},{timeStamp},005,000000000000000000000000000000000000000000";

                byte[] data = Encoding.UTF8.GetBytes(responseString);
                await _writeCharacteristic.WriteAsync(data);

                _logData.AppendLine($"Sent batch confirmation: {responseString}");

                // Reset for next batch
                _currentDevice.PUBDataCounter = 0;
            }
            catch (Exception ex)
            {
                _logData.AppendLine($"Error sending batch confirmation: {ex.Message}");
            }
        }

        private async Task SendFinalConfirmation()
        {
            try
            {
                string timeStamp = DateTime.UtcNow.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss");
                string responseString = $"1,{_currentDevice.SerialNumber},{timeStamp},005,000000000000000000000000000000000000000000";

                byte[] data = Encoding.UTF8.GetBytes(responseString);
                await _writeCharacteristic.WriteAsync(data);

                _logData.AppendLine($"Sent final confirmation: {responseString}");

                await Task.Delay(1000);

                // Disconnect
                if (_currentDevice.Device.State == DeviceState.Connected)
                {
                    await _bluetoothAdapter.DisconnectDeviceAsync(_currentDevice.Device);
                }

                _logData.AppendLine("Transfer complete - device disconnected");
            }
            catch (Exception ex)
            {
                _logData.AppendLine($"Error sending final confirmation: {ex.Message}");
            }
        }

        private async Task ProcessEventData(string[] strSplit)
        {
            try
            {
                // 5,EVENT,1000,02/06/2021 12:30:59.100,1
                if (strSplit[2] == "00/00/2000 00:00:00.000")
                {
                    _currentDevice.EventTotalCount--;
                    return;
                }

                DateTime date = DateTime.ParseExact(strSplit[2], "MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
                string time = date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
                int eventID = int.Parse(strSplit[3]);

                string insertString = $"{_currentDevice.SerialNumber},{time},{eventID}";
                _currentDevice.EventCounter++;
                _currentDevice.Events[_currentDevice.EventCounter] = insertString;

                if (_currentDevice.EventCounter == _currentDevice.EventTotalCount)
                {
                    await SendFinalConfirmation();
                }
                else if ((_currentDevice.EventCounter % _currentDevice.EventBatchSize) == 0)
                {
                    await SendBatchConfirmation();
                }
            }
            catch (Exception ex)
            {
                _logData.AppendLine($"Error processing event data: {ex.Message}");
            }
        }

        private async Task ShowCollectedData()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Device Information ===");
            sb.AppendLine($"Serial: {_currentDevice.SerialNumber}");
            sb.AppendLine($"Model: {_currentDevice.ModelNumber}");
            sb.AppendLine($"Firmware: {_currentDevice.FirmwareVersion}");
            sb.AppendLine($"Battery: {_currentDevice.VBat} V");
            sb.AppendLine($"Total Puffs Collected: {_currentDevice.Puffs.Count}");
            sb.AppendLine();

            sb.AppendLine("=== Puff Data ===");
            foreach (var puff in _currentDevice.Puffs.Take(10)) // Show first 10
            {
                sb.AppendLine(puff.ToString());
            }

            if (_currentDevice.Puffs.Count > 10)
                sb.AppendLine($"... and {_currentDevice.Puffs.Count - 10} more puffs");

            sb.AppendLine();
            sb.AppendLine("=== Collection Log ===");
            sb.AppendLine(_logData.ToString());

            await Navigation.PushAsync(new ContentPage
            {
                Title = "Collected Data",
                Content = new ScrollView
                {
                    Content = new Label
                    {
                        Text = sb.ToString(),
                        FontSize = 12,
                        Margin = 10
                    }
                }
            });
        }

        // Keep your existing methods for scanning, permissions, etc.
        private async void OnScanClicked(object sender, EventArgs e)
        {
            // Your existing scan implementation
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
            // Your existing permission logic
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

        // Keep your other existing methods (UpdateSurveyAsync, etc.)
        public async Task UpdateSurveyAsync()
        {
            // Your existing survey update logic
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