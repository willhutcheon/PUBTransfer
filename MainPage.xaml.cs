using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;
using System.Net.Http;
using QRCoder;
using System.Text;

namespace PUBTransfer
{
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
            //DisplayQRCode();
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
                // Run your scan logic
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
        //protected override async void OnAppearing()
        //{
        //    base.OnAppearing();
        //    await UpdateSurveyAsync();
        //}
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
//                        lblSurvey.Text = numSurvey == 1 ? "1 Survey Available" : $"{numSurvey} Surveys Available";
//                        btnSurvey.IsVisible = true;
//                        btnSurvey.Text = numSurvey == 1 ? "View Survey" : "View Surveys";
//                    }
//                    else
//                    {
//                        lblSurvey.Text = "No Survey Available";
//                        btnSurvey.IsVisible = false;
//                        btnSurvey.Text = string.Empty;
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
//                lblSurvey.Text = "No Survey Available";
//                btnSurvey.IsVisible = false;
//                btnSurvey.Text = string.Empty;
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
        //private void BtnSurvey_Clicked(object sender, EventArgs e)
        //{
        //    //if (!string.IsNullOrEmpty(Globals.serialNumber))
        //    //{
        //        //string surveyUrl = $"{GetSurveyDomain()}/SurveyPage?serial={Globals.serialNumber}";
        //        //hard coded for test
        //        string surveyUrl = "https://cme-pub-survey-dev.azurewebsites.net/Survey?Serial=e43e12b8ecc515c9&Code=2333&DBID=0";
        //        webSurvey.Source = surveyUrl;
        //    //}
        //}
        private ImageSource GenerateQRCode(string data)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);
            return ImageSource.FromStream(() => new MemoryStream(qrCodeBytes));
        }
        //private void DisplayQRCode()
        //{
        //    string deviceId = Guid.NewGuid().ToString(); // or use Preferences to persist
        //    androidIdLabel.Text = $"Device ID: {deviceId}";
        //    qrCodeImage.Source = GenerateQRCode(deviceId);
        //}
        private void OnEnvironmentChanged(object sender, CheckedChangedEventArgs e)
        {
            var radio = sender as RadioButton;
            //if (radio.IsChecked)
            //{
            //    string selectedEnv = radio.Value.ToString();
            //    Console.WriteLine($"Selected environment: {selectedEnv}");
            //}
        }
        //private async void OnDeviceSelected(object sender, ItemTappedEventArgs e)
        //{
        //    if (e.Item is IDevice selectedDevice)
        //    {
        //        try
        //        {
        //            await DisplayAlert("Connecting", $"Connecting to {selectedDevice.Name}...", "OK");
        //            // Connect
        //            await _bluetoothAdapter.ConnectToDeviceAsync(selectedDevice);
        //            await DisplayAlert("Connected", $"Connected to {selectedDevice.Name}", "OK");
        //            // Optional: Discover services
        //            var services = await selectedDevice.GetServicesAsync();
        //            foreach (var service in services)
        //            {
        //                Console.WriteLine($"Service: {service.Id}");
        //                var characteristics = await service.GetCharacteristicsAsync();
        //                foreach (var characteristic in characteristics)
        //                {
        //                    Console.WriteLine($"  Characteristic: {characteristic.Id}");
        //                }
        //            }
        //            // Store globally if needed
        //            Globals.serialNumber = selectedDevice.Id.ToString();
        //        }
        //        catch (Exception ex)
        //        {
        //            await DisplayAlert("Error", $"Failed to connect: {ex.Message}", "OK");
        //        }
        //    }
        //}

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
                    // Discover services
                    var services = await selectedDevice.GetServicesAsync();
                    foreach (var service in services)
                    {
                        Console.WriteLine($"Service: {service.Id}");
                        var characteristics = await service.GetCharacteristicsAsync();
                        foreach (var characteristic in characteristics)
                        {
                            Console.WriteLine($"  Characteristic: {characteristic.Id}, CanRead={characteristic.CanRead}");
                            if (characteristic.CanRead)
                            {
                                try
                                {
                                    // Deconstruct the tuple result
                                    var (data, resultCode) = await characteristic.ReadAsync();
                                    if (resultCode == 0 && data != null && data.Length > 0)
                                    {
                                        // Try to decode as UTF-8
                                        string textValue = Encoding.UTF8.GetString(data);
                                        string hexValue = BitConverter.ToString(data);
                                        Console.WriteLine($"Read from {characteristic.Id}: {textValue}");
                                        Console.WriteLine($"Raw Hex: {hexValue}");
                                        await DisplayAlert("Device Data",
                                            $"Characteristic {characteristic.Id}\n" +
                                            $"Text: {textValue}\n" +
                                            $"Hex: {hexValue}",
                                            "OK");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Characteristic {characteristic.Id} returned no data. ResultCode={resultCode}");
                                    }
                                }
                                catch (Exception readEx)
                                {
                                    Console.WriteLine($"Failed to read {characteristic.Id}: {readEx.Message}");
                                }
                            }
                        }
                    }
                    // Save device globally if you need it
                    Globals.serialNumber = selectedDevice.Id.ToString();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to connect: {ex.Message}", "OK");
                }
            }
        }
    }
}