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
                    //if (!string.IsNullOrEmpty(a.Device.Name) && a.Device.Name.StartsWith("PUB"))
                    //{
                        if (!Devices.Contains(a.Device))
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Devices.Add(a.Device);
                            });
                        }
                    //}
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


//collect the data from the characteristics of the pub, wrap them all up nicely and send to event hub
//right now you are not getting the characteristics correctly, find out the format they are delivered in