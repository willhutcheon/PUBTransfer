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

        private bool IsCompleteHeaderVUSE(string data)
        {
            // Check if header looks complete - adjust this logic based on the header format
            return data.Contains(",") && data.Split(',').Length >= 7; // Expecting 7 comma-separated values for VUSE
        }

        private static readonly Guid HeaderCharacteristicId = Guid.Parse("fd5abba0-3935-11e5-85a6-0002a5d5c51b");

        private async Task<(string Header, ICharacteristic HeaderChar)> ReadHeaderAsync(IDevice device)
        {
            var allData = new StringBuilder();
            //try
            //{
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
            //if (headerChar == null)
            //{
            //    return "Header characteristic not found.";
            //}
            // Step 2: Try to read directly
            if (headerChar.CanRead)
            {
                var (data, resultCode) = await headerChar.ReadAsync();
                if (resultCode == 0 && data != null && data.Length > 0)
                {
                    string textValue = Encoding.UTF8.GetString(data);
                    allData.AppendLine($"[Read] Text: {textValue}");
                    allData.AppendLine($"[Read] Hex: {BitConverter.ToString(data)}");

                    if (IsCompleteHeaderVUSE(textValue))
                        //return textValue;
                        return (textValue, headerChar);
                }
            }
            return ("Header characteristic does not support Read or Update.", headerChar);
            //}
            //catch (Exception ex)
            //{
            //    return $"Error reading header: {ex.Message}";
            //}
        }

        private async Task AcknowledgeHeaderAsync(ICharacteristic characteristic, string serialNumber)
        {
            string timeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            string responseString = $"4,{serialNumber},{timeStamp},005";
            byte[] payload = Encoding.UTF8.GetBytes(responseString);
            await characteristic.WriteAsync(payload);
            Console.WriteLine($"[Header Ack Sent] {responseString}");
            await DisplayAlert("Sending Header Response Data", responseString, "OK");
        }

        //private async Task<List<string>> ReadDataBatchAsync(ICharacteristic characteristic, int batchSize, int puffCount)
        //{
        //    var dataPoints = new List<string>();
        //    if (puffCount <= 0 || batchSize <= 0)
        //    {
        //        Console.WriteLine("No data to read (puffCount=0 or batchSize=0).");
        //        return dataPoints;
        //    }
        //    for (int i = 0; i < batchSize; i++)
        //    {
        //        //what is result code??
        //        //It comes from the underlying Bluetooth GATT read operation. 0 usually means success. Any non - zero means an error(e.g.device disconnected, permission denied, read not allowed, etc.)
        //        //data is the wrong characteristic here, the characteristic you are reading from is the incorrect one. find out which characteristic to read from here
        //        var (data, resultCode) = await characteristic.ReadAsync();
        //        //if (resultCode == 0 && data != null && data.Length > 0)
        //        if (data != null && data.Length > 0)
        //        {
        //            string textValue = Encoding.UTF8.GetString(data);
        //            //Console.WriteLine($"[textValue {textValue}");
        //            //i believe this is correct but im checking the wrong characteristic here
        //            if (textValue.StartsWith("DATA"))
        //            {
        //                dataPoints.Add(textValue);
        //                Console.WriteLine($"[Dataa {textValue}");
        //            }
        //            else
        //            {
        //                Console.WriteLine($"[Unexpected Response] {textValue}");
        //                break; // stop if device sends something unexpected
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine($"[Error] Failed to read data point {i + 1}.");
        //            break;
        //        }
        //    }
        //    return dataPoints;
        //}
        private async Task<List<string>> ReadDataBatchAsync(
    ICharacteristic headerChar,
    int batchSize,
    int puffCount,
    string serialNumber)
        {
            var dataPoints = new List<string>();

            try
            {
                // Step 2: ACK the header
                //var ack = $"4,{serialNumber},{DateTime.Now:MM/dd/yyyy HH:mm:ss},005";
                //var ackBytes = Encoding.UTF8.GetBytes(ack);
                //await headerChar.WriteAsync(ackBytes);
                //Console.WriteLine($"[BLE] Sent Header ACK: {ack}");

                // Step 3: Read puff data repeatedly from same characteristic
                for (int i = 0; i < puffCount; i++)
                {

                    //var (headerBytes, resultCode) = await headerChar.ReadAsync();
                    //var header = Encoding.UTF8.GetString(headerBytes);

                    var (dataBytes, resultCode) = await headerChar.ReadAsync();
                    //reading from the data line here, it contains the data point that was sent from the acknowledge header, it seems acknowledge header
                    //successfully sent the correctly formatted response and at  this point i can see that the response is on the device through the read i do here
                    var dataLine = Encoding.UTF8.GetString(dataBytes);

                    //if (string.IsNullOrWhiteSpace(dataLine) || !dataLine.StartsWith("DATA"))
                    if (string.IsNullOrWhiteSpace(dataLine))
                    {
                        Console.WriteLine($"[BLE] Invalid or empty puff data at index {i}: {dataLine}");
                        continue;
                    }

                    Console.WriteLine($"[BLE] Puff {i + 1}/{puffCount}: {dataLine}");
                    dataPoints.Add(dataLine);
                }

                // Step 4: ACK batch
                var batchAck = $"5,{serialNumber},{DateTime.Now:MM/dd/yyyy HH:mm:ss},000";
                await headerChar.WriteAsync(Encoding.UTF8.GetBytes(batchAck));
                Console.WriteLine($"[BLE] Sent Batch ACK: {batchAck}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BLE] Error in ReadDataBatchAsync: {ex.Message}");
            }

            return dataPoints;
        }


        // Step 1: Find characteristics (during connection)
        //private async Task<(ICharacteristic headerChar, ICharacteristic dataChar)> GetCharacteristicsAsync(IDevice device)
        //{
        //    ICharacteristic headerChar = null;
        //    ICharacteristic dataChar = null;
        //    var services = await device.GetServicesAsync();
        //    foreach (var service in services)
        //    {
        //        var characteristics = await service.GetCharacteristicsAsync();
        //        foreach (var c in characteristics)
        //        {
        //            if (c.Id.ToString().Equals("fd5abba0-3935-11e5-85a6-0002a5d5c51b", StringComparison.OrdinalIgnoreCase))
        //                headerChar = c;
        //            else if (c.CanRead && !c.CanWrite) // heuristic: data is often read-only
        //                dataChar = c;
        //        }
        //    }
        //    if (headerChar == null || dataChar == null)
        //        throw new Exception("Could not find both header and data characteristics.");
        //    return (headerChar, dataChar);
        //}
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





                    //await DisplayAlert("Connected", $"Connected to {selectedDevice.Name}", "OK");
                    // === Step 1: Read the full header ===
                    //all this function does is get the header data, nothing is said back to the pub here
                    //var header = await ReadHeaderAsync(selectedDevice);
                    //await DisplayAlert("Header Data", header, "OK");
                    //if (!IsCompleteHeaderVUSE(header))
                    //{
                    //    throw new Exception("Incomplete header received.");
                    //}
                    //now that we have the header we need to send the header acknowledgement
                    //await AcknowledgeHeaderAsync(writeChar, serial);





                    ////STEP 1: PHONE READS FROM CHARACTERISTIC
                    //var (header, headerChar) = await ReadHeaderAsync(selectedDevice);
                    //await DisplayAlert("Header Data", header, "OK");
                    //if (!IsCompleteHeaderVUSE(header))
                    //    throw new Exception("Incomplete header received.");
                    ////STEP 2: PHONE WRITES TO CHARACTERISTIC
                    //var parts = header.Split(',');
                    //string serial = parts.Length > 1 ? parts[1] : "";
                    //await AcknowledgeHeaderAsync(headerChar, serial);
                    ////STEP 3: PHONE READS CHARACTERISTIC (IF DATA AVAILABLE, REPEAT FOR NUMBER OF DATA POINTS)
                    //int batchSize = int.Parse(parts[3]);   // Batch_Size
                    //int puffCount = int.Parse(parts[4]);   // Puff_Count
                    //var dataPoints = await ReadDataBatchAsync(headerChar, batchSize, puffCount);
                    //if (dataPoints.Count > 0)
                    //{
                    //    string preview = string.Join("\n", dataPoints.Take(5));
                    //    await DisplayAlert("First Data Points", preview, "OK");
                    //}
                    //else
                    //{
                    //    await DisplayAlert("Info", "No data points returned.", "OK");
                    //}





                    //var (headerChar, dataChar) = await GetCharacteristicsAsync(selectedDevice);
                    var headerChar = await GetHeaderCharacteristicAsync(selectedDevice);
                    // STEP 1: Read header
                    //var (header, _) = await ReadHeaderAsync(selectedDevice);
                    //if (!IsCompleteHeaderVUSE(header))
                    //    throw new Exception("Incomplete header received.");
                    var (headerBytes, resultCode) = await headerChar.ReadAsync();
                    var header = Encoding.UTF8.GetString(headerBytes);
                    Console.WriteLine($"[BLE] Header: {header}");
                    await DisplayAlert("Header Data", header, "OK");
                    // STEP 2: Ack header
                    var parts = header.Split(',');
                    string serial = parts.Length > 1 ? parts[1] : "";
                    await AcknowledgeHeaderAsync(headerChar, serial);

                    // STEP 3: Read data (from *dataChar*, not headerChar)
                    int batchSize = int.Parse(parts[3]);   // from header
                    int puffCount = int.Parse(parts[4]);   // from header
                    //batch size and puffcount seem to be correct, is it the wrong characteristic?
                    Console.WriteLine($"batchSize {batchSize}");
                    Console.WriteLine($"puffCount {puffCount}");
                    //Console.WriteLine($"dataChar {dataChar}");
                    //var dataPoints = await ReadDataBatchAsync(dataChar, batchSize, puffCount);
                    var dataPoints = await ReadDataBatchAsync(headerChar, batchSize, puffCount, serial);














                    //var parts = header.Split(',');
                    //string serial = parts[1];
                    //int batchSize = int.Parse(parts[3]);   // Batch_Size
                    //int puffCount = int.Parse(parts[4]);   // Puff_Count
                    // === Step 2: Find a writable characteristic for commands ===
                    //ICharacteristic writeChar = null;
                    //var services = await selectedDevice.GetServicesAsync();
                    //foreach (var service in services)
                    //{
                    //var characteristics = await service.GetCharacteristicsAsync();
                    //writeChar = characteristics.FirstOrDefault(c => c.CanWrite);
                    //if (writeChar != null) break;
                    //}
                    //if (writeChar == null)
                    //throw new Exception("No writable characteristic found for ACK and data transfer.");
                    // === Step 3: Acknowledge the header ===
                    //await AckHeaderAsync(writeChar, serial);
                    // === Step 4: Read the data batch ===
                    //var dataPoints = await ReadDataBatchAsync(writeChar, batchSize);
                    // === Step 5: Confirm the batch ===
                    //await ConfirmBatchAsync(writeChar, batchSize);
                    //Console.WriteLine($"[Transfer Complete] {dataPoints.Count} data points received.");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to connect or read data: {ex.Message}", "OK");
                }
                //finally
                //{
                //_isCollectingData = false;
                //}
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