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
//    public class PuffData
//    {
//        public int PuffId { get; set; }
//        public DateTime Start { get; set; }
//        public DateTime End { get; set; }
//        public double Duration { get; set; }
//        public double Volume { get; set; }
//        public double Battery { get; set; }
//        public double XAngle { get; set; }
//        public double VBat { get; set; }
//        public double YAngle { get; set; }
//        public double ZAngle { get; set; }
//        public override string ToString()
//        {
//            return $"Puff {PuffId} | Start={Start:HH:mm:ss} | End={End:HH:mm:ss} | Duration={Duration:F2}s | Battery={Battery:F2}V | Angles=({XAngle:F2}, {YAngle:F2}, {ZAngle:F2})";
//        }
//    }
//    public class BLEDeviceDetails
//    {
//        public IDevice Device { get; set; }
//        public IService PrimaryService { get; set; }
//        public ICharacteristic PrimaryCharacteristic { get; set; }
//        public IDescriptor PrimaryDescriptor { get; set; }
//        public string SerialNumber { get; set; }
//        public int ModelNumber { get; set; }
//        public string FirmwareVersion { get; set; }
//        public string Status { get; set; }
//        public int PuffCountLeft { get; set; }
//        public int DevicePuffCount { get; set; }
//        public int TotalPuffCount { get; set; }
//        public int BatchPuffCount { get; set; }
//        public int BatchPuffCounter { get; set; }
//        public int PuffID { get; set; }
//        public int PuffNum { get; set; }
//        public int RemoveCurrentPuff { get; set; }
//        public int PuffDelay { get; set; }
//        public double VBat { get; set; }
//        // Replace PubRawData array with structured puffs
//        public List<PuffData> Puffs { get; set; } = new List<PuffData>();
//        public string[] Events { get; set; } = new string[500];
//        public int EventTotalCount { get; set; }
//        public int EventBatchSize { get; set; }
//        public int EventCounter { get; set; }
//        public double X_Angle { get; set; }
//        public double Y_Angle { get; set; }
//        public double Z_Angle { get; set; }
//    }
//    public static class Globals
//    {
//        public static string ScreenMode;
//        public static string TopPanel;
//        public static string BottomPanel;
//        public static bool Scanning;
//        public static string serialNumber;
//        public static string PassCode;
//        public static Timer YourTimer;
//        public static string surveySerialNumber;
//        public static DateTime surveySerialDate;
//        public static bool wifiConnected = true;
//        public static BLEDeviceDetails CurrentDevice;
//    }
//    public enum EnvironmentType
//    {
//        DEV,
//        QA,
//        PROD,
//        Nothing
//    }
//    public partial class MainPage : ContentPage
//    {
//        private EnvironmentType currentEnvironment = EnvironmentType.DEV;
//        private readonly IAdapter _bluetoothAdapter;
//        private readonly IBluetoothLE _bluetoothLE;
//        public ObservableCollection<IDevice> Devices { get; set; } = new();
//        public MainPage()
//        {
//            InitializeComponent();
//            DisplayQRCode();
//            _bluetoothLE = CrossBluetoothLE.Current;
//            _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;
//            DevicesListView.ItemsSource = Devices;
//        }
//        private async void OnScanClicked(object sender, EventArgs e)
//        {
//            // Disable the button while scanning
//            ScanButton.IsEnabled = false;
//            ScanButton.Text = "Scanning...";
//            try
//            {
//                var permissionStatus = await RequestBluetoothPermissions();
//                if (permissionStatus != PermissionStatus.Granted)
//                {
//                    await DisplayAlert("Permission Denied", "Bluetooth permissions are required", "OK");
//                    return;
//                }
//                if (!_bluetoothLE.IsOn)
//                {
//                    await DisplayAlert("Bluetooth Off", "Please enable Bluetooth", "OK");
//                    return;
//                }
//                Devices.Clear();
//                _bluetoothAdapter.DeviceDiscovered += (s, a) =>
//                {
//                    // Ensure the device has a non-null name and matches the desired prefix
//                    if (!string.IsNullOrEmpty(a.Device.Name) && a.Device.Name.StartsWith("PUB"))
//                    {
//                        if (!Devices.Contains(a.Device))
//                        {
//                            MainThread.BeginInvokeOnMainThread(() =>
//                            {
//                                Devices.Add(a.Device);
//                            });
//                        }
//                    }
//                };
//                await _bluetoothAdapter.StartScanningForDevicesAsync();
//                //await DisplayAlert("Scan Complete", $"{Devices.Count} devices found.", "OK");
//            }
//            catch (Exception ex)
//            {
//                await DisplayAlert("Error", $"Failed to scan: {ex.Message}", "OK");
//            }
//            finally
//            {
//                // Re-enable after scanning
//                ScanButton.IsEnabled = true;
//                ScanButton.Text = "Scan";
//            }
//        }
//        private async Task<PermissionStatus> RequestBluetoothPermissions()
//        {
//            try
//            {
//#if ANDROID
//                // For Android 12+ (API 31+), we need BLUETOOTH_SCAN and BLUETOOTH_CONNECT
//                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
//                {
//                    //var scanPermission = await Permissions.RequestAsync<MAUI_Test_Bluetooth.Platforms.Android.BluetoothScanPermission>();
//                    var scanPermission = await Permissions.RequestAsync<Platforms.Android.Permissions.BluetoothScanPermission>();
//                    if (scanPermission != PermissionStatus.Granted)
//                        return scanPermission;
//                    var connectPermission = await Permissions.RequestAsync<Platforms.Android.Permissions.BluetoothConnectPermission>();
//                    if (connectPermission != PermissionStatus.Granted)
//                        return connectPermission;
//                }
//                else
//                {
//                    // For older Android versions, we need location permissions
//                    var locationPermission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
//                    if (locationPermission != PermissionStatus.Granted)
//                        return locationPermission;
//                }
//#endif
//                return PermissionStatus.Granted;
//            }
//            catch (Exception ex)
//            {
//                await DisplayAlert("Permission Error", $"Failed to request permissions: {ex.Message}", "OK");
//                return PermissionStatus.Denied;
//            }
//        }
//        private void OnClearClicked(object sender, EventArgs e)
//        {
//            Devices.Clear();
//        }
//        protected override async void OnAppearing()
//        {
//            base.OnAppearing();
//            await UpdateSurveyAsync();
//        }
//        public async Task UpdateSurveyAsync()
//        {
//#if !FACTORY_MODE
//            string fileName = "PUBserial.txt";
//            string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
//            Console.WriteLine($"----- updateSurvey: {Globals.serialNumber} -----");
//            if (!Globals.wifiConnected)
//            {
//                await UpdateNoSurveyUIAsync();
//                return;
//            }
//            var client = new HttpClient();
//            int envCode = GetEnvironmentCode();
//            string envDomain = GetSurveyDomain();
//            //string urlStr = $"{envDomain}/api/Survey?Serial={Globals.serialNumber}&DBID={envCode}";
//            //hardcode one here to test if you can see things in the webview
//            string urlStr = "https://cme-pub-survey-dev.azurewebsites.net/api/Survey?Serial=e43e12b8ecc515c9&DBID=0";
//            Console.WriteLine("--- URL: " + urlStr);
//            try
//            {
//                var response = await client.GetAsync(urlStr);
//                var responseString = await response.Content.ReadAsStringAsync();
//                int numSurvey = int.TryParse(responseString, out int parsed) ? parsed : 0;
//                await MainThread.InvokeOnMainThreadAsync(() =>
//                {
//                    if (numSurvey > 0)
//                    {
//                        //lblSurvey.Text = numSurvey == 1 ? "1 Survey Available" : $"{numSurvey} Surveys Available";
//                        //btnSurvey.IsVisible = true;
//                        //btnSurvey.Text = numSurvey == 1 ? "View Survey" : "View Surveys";
//                    }
//                    else
//                    {
//                        //lblSurvey.Text = "No Survey Available";
//                        //btnSurvey.IsVisible = false;
//                        //btnSurvey.Text = string.Empty;
//                    }
//                });
//                // Save the current survey count in file
//                await File.WriteAllTextAsync(filePath, Globals.serialNumber + "," + DateTime.UtcNow);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Survey network error: " + ex.Message);
//                await UpdateNoSurveyUIAsync();
//            }
//#endif
//        }
//        private async Task UpdateNoSurveyUIAsync()
//        {
//            await MainThread.InvokeOnMainThreadAsync(() =>
//            {
//                //lblSurvey.Text = "No Survey Available";
//                //btnSurvey.IsVisible = false;
//                //btnSurvey.Text = string.Empty;
//            });
//            // Reset survey file
//            string fileName = "PUBserial.txt";
//            string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
//            if (File.Exists(filePath))
//                File.Delete(filePath);
//            Globals.surveySerialNumber = string.Empty;
//        }
//        private string GetSurveyDomain()
//        {
//            return currentEnvironment switch
//            {
//                EnvironmentType.DEV => "https://cme-pub-survey-dev.azurewebsites.net",
//                EnvironmentType.QA => "https://cme-pub-survey-qa-e3bfg0g9bjcud5ew.eastus-01.azurewebsites.net",
//                EnvironmentType.PROD => "https://mobilesurveys.azurewebsites.net",
//                _ => ""
//            };
//        }
//        private int GetEnvironmentCode()
//        {
//            return currentEnvironment switch
//            {
//                EnvironmentType.DEV => 0,
//                EnvironmentType.QA => 1,
//                EnvironmentType.PROD => 2,
//                _ => -1
//            };
//        }
//        private void BtnSurvey_Clicked(object sender, EventArgs e)
//        {
//            //if (!string.IsNullOrEmpty(Globals.serialNumber))
//            //{
//            //string surveyUrl = $"{GetSurveyDomain()}/SurveyPage?serial={Globals.serialNumber}";
//            //hard coded for test
//            //string surveyUrl = "https://cme-pub-survey-dev.azurewebsites.net/Survey?Serial=e43e12b8ecc515c9&Code=2333&DBID=0";
//            //webSurvey.Source = surveyUrl;
//            //}
//        }
//        private ImageSource GenerateQRCode(string data)
//        {
//            var qrGenerator = new QRCodeGenerator();
//            var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
//            var qrCode = new PngByteQRCode(qrCodeData);
//            byte[] qrCodeBytes = qrCode.GetGraphic(20);
//            return ImageSource.FromStream(() => new MemoryStream(qrCodeBytes));
//        }
//        private void DisplayQRCode()
//        {
//            string deviceId = Guid.NewGuid().ToString(); // or use Preferences to persist
//                                                         //androidIdLabel.Text = $"Device ID: {deviceId}";
//                                                         //qrCodeImage.Source = GenerateQRCode(deviceId);
//        }
//        private void OnEnvironmentChanged(object sender, CheckedChangedEventArgs e)
//        {
//            var radio = sender as RadioButton;
//            //if (radio.IsChecked)
//            //{
//            //    string selectedEnv = radio.Value.ToString();
//            //    Console.WriteLine($"Selected environment: {selectedEnv}");
//            //}
//        }
//        private void ParseAndStorePuff(string textValue)
//        {
//            try
//            {
//                string[] strSplit = textValue.Split(',');
//                //if (strSplit.Length < 5)
//                //{
//                //    MainThread.BeginInvokeOnMainThread(async () =>
//                //    {
//                //        await Application.Current.MainPage.DisplayAlert(
//                //            "Warning",
//                //            $"strSplit.Length: {strSplit.Length}\n",
//                //            "OK");
//                //    });
//                //    Console.WriteLine("Not enough fields in puff data");
//                //    return;
//                //}
//                // Parse fields (adjust mapping if device spec changes)
//                int puffId = int.Parse(strSplit[1]);
//                //int puffId = int.Parse(strSplit[0]);
//                // og (2)
//                double duration = double.Parse(strSplit[2]); // seconds
//                                                             //double duration = double.Parse(strSplit[9]); // seconds
//                double volume = double.Parse(strSplit[3]);
//                double battery = double.Parse(strSplit[5]);
//                // og (6) is probably vbat
//                //double xAngle = double.Parse(strSplit[6]);
//                // i believe this is the one i have correct, voltage
//                double vbat = double.Parse(strSplit[6]);
//                DateTime start = DateTime.UtcNow;
//                DateTime end = start.AddSeconds(duration);
//                var puff = new PuffData
//                {
//                    PuffId = puffId,
//                    Start = start,
//                    End = end,
//                    Duration = duration,
//                    Volume = volume,
//                    Battery = battery,
//                    //XAngle = xAngle,
//                    VBat = vbat,
//                    YAngle = 0,
//                    ZAngle = 0
//                };
//                Globals.CurrentDevice?.Puffs.Add(puff);
//                Console.WriteLine($"Stored puff: {puff}");
//                MainThread.BeginInvokeOnMainThread(async () =>
//                {
//                    await Application.Current.MainPage.DisplayAlert(
//                        "Puff Data",
//                        $"PUB: {puff.PuffId}\n" +
//                        //$"Duration: {puff.Duration}\n" +
//                        //$"Volume: {puff.Volume}\n" +
//                        //$"Battery: {puff.Battery}\n" +
//                        //$"X Angle: {puff.XAngle}", //not vbat
//                        $"Voltage: {puff.VBat}",
//                        "OK");
//                });
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error parsing puff data: {ex.Message}");
//            }
//        }





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


//                            //THIS IS HOW I NEED TO GET THE HEADER IN MY 3RD BLOCK
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

//                                        //if (textValue.StartsWith("PUB"))
//                                        //{
//                                        //    ParseAndStorePuff(textValue);
//                                        //}
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
//                            //if (characteristic.CanUpdate)
//                            //{
//                            //    characteristic.ValueUpdated += (s, args) =>
//                            //    {
//                            //        var updatedData = args.Characteristic.Value;
//                            //        if (updatedData != null && updatedData.Length > 0)
//                            //        {
//                            //            string updatedHex = BitConverter.ToString(updatedData);
//                            //            string updatedText = Encoding.UTF8.GetString(updatedData);

//                            //            Dispatcher.Dispatch(() =>
//                            //            {
//                            //                allData.AppendLine($"    [Notify] {characteristic.Id}: {updatedHex} / {updatedText}");
//                            //            });
//                            //        }
//                            //    };

//                            //    await characteristic.StartUpdatesAsync();
//                            //    allData.AppendLine($"    Subscribed to notifications for {characteristic.Id}");
//                            //}
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
//using static Android.Preferences.PreferenceActivity;

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

        private bool IsCompleteHeader(string data)
        {
            // Check if header looks complete - adjust this logic based on the header format
            return data.Contains(",") && data.Split(',').Length >= 7; // Expecting 7 comma-separated values for VUSE
        }

        private static readonly Guid HeaderCharacteristicId = Guid.Parse("fd5abba0-3935-11e5-85a6-0002a5d5c51b");

        private async Task<string> ReadHeaderAsync(IDevice device)
        {
            var allData = new StringBuilder();
            try
            {
                // Step 1: Get services and find the header characteristic
                var services = await device.GetServicesAsync();
                ICharacteristic headerChar = null;
                foreach (var service in services)
                {
                    var characteristics = await service.GetCharacteristicsAsync();
                    headerChar = characteristics.FirstOrDefault(c => c.Id == HeaderCharacteristicId);
                    if (headerChar != null)
                    {
                        allData.AppendLine($"Found header characteristic in service {service.Id}");
                        break;
                    }
                }
                if (headerChar == null)
                {
                    return "Header characteristic not found.";
                }
                // Step 2: If writable, send confirm header first
                if (headerChar.CanWrite)
                {
                    await SendConfirmHeaderAsync(
                        headerChar,
                        vusePROFlag: true,    // or false depending on your device
                        devicePuffCount: 0,
                        serialNumber: device.Id.ToString()
                    );
                    // Give device time to prepare the full header
                    await Task.Delay(200);
                }
                // Step 3: Try to read directly
                if (headerChar.CanRead)
                {
                    var (data, resultCode) = await headerChar.ReadAsync();
                    if (resultCode == 0 && data != null && data.Length > 0)
                    {
                        string textValue = Encoding.UTF8.GetString(data);
                        allData.AppendLine($"[Read] Text: {textValue}");
                        allData.AppendLine($"[Read] Hex: {BitConverter.ToString(data)}");

                        if (IsCompleteHeader(textValue))
                            return textValue;
                    }
                }
                // Step 4: Fallback to notifications
                if (headerChar.CanUpdate)
                {
                    var tcs = new TaskCompletionSource<string>();
                    var buffer = new StringBuilder();
                    headerChar.ValueUpdated += (s, args) =>
                    {
                        var updatedData = args.Characteristic.Value;
                        if (updatedData != null && updatedData.Length > 0)
                        {
                            string updatedText = Encoding.UTF8.GetString(updatedData);
                            buffer.Append(updatedText);

                            if (IsCompleteHeader(buffer.ToString()))
                                tcs.TrySetResult(buffer.ToString());
                        }
                    };
                    await headerChar.StartUpdatesAsync();
                    var completeHeader = await Task.WhenAny(tcs.Task, Task.Delay(5000)) == tcs.Task
                        ? await tcs.Task
                        : "Timeout waiting for complete header";
                    await headerChar.StopUpdatesAsync();
                    return completeHeader;
                }
                return "Header characteristic does not support Read or Update.";
            }
            catch (Exception ex)
            {
                return $"Error reading header: {ex.Message}";
            }
        }

        //get this working, this is what youre fixing now
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
                    await DisplayAlert("Connected", $"Connected to {selectedDevice.Name}", "OK");
                    // === Step 1: Read the full header ===
                    var header = await ReadHeaderAsync(selectedDevice);  // NEW: device passed in
                    await DisplayAlert("Header Data", header, "OK");
                    if (!IsCompleteHeader(header))
                    {
                        throw new Exception("Incomplete header received.");
                    }
                    var parts = header.Split(',');
                    string serial = parts[1];
                    int batchSize = int.Parse(parts[3]);   // Batch_Size
                    int puffCount = int.Parse(parts[4]);   // Puff_Count
                    // === Step 2: Find a writable characteristic for commands ===
                    ICharacteristic writeChar = null;
                    var services = await selectedDevice.GetServicesAsync();
                    foreach (var service in services)
                    {
                        var characteristics = await service.GetCharacteristicsAsync();
                        writeChar = characteristics.FirstOrDefault(c => c.CanWrite);
                        if (writeChar != null) break;
                    }
                    if (writeChar == null)
                        throw new Exception("No writable characteristic found for ACK and data transfer.");
                    // === Step 3: Acknowledge the header ===
                    await AckHeaderAsync(writeChar, serial);
                    // === Step 4: Read the data batch ===
                    var dataPoints = await ReadDataBatchAsync(writeChar, batchSize);
                    // === Step 5: Confirm the batch ===
                    await ConfirmBatchAsync(writeChar, batchSize);
                    Console.WriteLine($"[Transfer Complete] {dataPoints.Count} data points received.");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to connect or read data: {ex.Message}", "OK");
                }
                finally
                {
                    _isCollectingData = false;
                }
            }
        }

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