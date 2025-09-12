using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;
using System.Net.Http;
//using ZXing.Net.Maui.Controls;
using QRCoder;

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

        //private readonly IAdapter _bluetoothAdapter;
        //private readonly IBluetoothLE _bluetoothLE;
        //public ObservableCollection<IDevice> Devices { get; set; } = new();
        // WiFi switching state management
        //private Timer _wifiTimer;
        //private bool _isOnSecondNetwork = false;
        //private bool _waitingForUserSwitch = false;
        public MainPage()
        {
            InitializeComponent();
            DisplayQRCode();
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
            //string urlStr = "https://cme-pub-survey-dev.azurewebsites.net/Survey?Serial=e43e12b8ecc515c9&Code=2333&DBID=0";


            Console.WriteLine("--- URL: " + urlStr);
            try
            {
                var response = await client.GetAsync(urlStr);
                var responseString = await response.Content.ReadAsStringAsync();
                int numSurvey = int.TryParse(responseString, out int parsed) ? parsed : 0;
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    //remove
                    //numSurvey = 1;

                    if (numSurvey > 0)
                    {
                        lblSurvey.Text = numSurvey == 1 ? "1 Survey Available" : $"{numSurvey} Surveys Available";
                        btnSurvey.IsVisible = true;
                        btnSurvey.Text = numSurvey == 1 ? "View Survey" : "View Surveys";
                    }
                    else
                    {
                        lblSurvey.Text = "No Survey Available";
                        btnSurvey.IsVisible = false;
                        btnSurvey.Text = string.Empty;
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
                lblSurvey.Text = "No Survey Available";
                btnSurvey.IsVisible = false;
                btnSurvey.Text = string.Empty;
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
                string surveyUrl = "https://cme-pub-survey-dev.azurewebsites.net/Survey?Serial=e43e12b8ecc515c9&Code=2333&DBID=0";
                webSurvey.Source = surveyUrl;
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
            androidIdLabel.Text = $"Device ID: {deviceId}";
            qrCodeImage.Source = GenerateQRCode(deviceId);
        }
    }
}