using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

using entobel_be.Models;
using System.Globalization;

namespace entobel_be.Services
{
    public class BgService : BackgroundService
    {
        private readonly DbService _dbService;
        private readonly AdsService _adsService;
        private readonly MailService _maService;

        CancellationTokenSource cts = new CancellationTokenSource();

        private DateTime timeCupF1, timeCupL1, timeCupF2, timeCupL2;
        private bool timeFlag;

        public readonly string amsNetId = File.ReadAllText("Settings/netid.txt");
        public readonly int port = int.Parse(File.ReadAllText("Settings/port.txt"));
        //public readonly string amsNetId = "127.0.0.1.1.1";
        //public readonly int port = 851;

        private readonly string logFile = "Logs/log.txt";

        public ProductionData prodData;
        public List<AdsEvent> events = new List<AdsEvent>();

        public bool startInit1 = true;
        public bool startInit2 = true;
        public bool startInit3 = true;
        public bool startInit4 = true;

        private static long lastReadPosition1;
        private static long lastReadPosition2;
        private static long lastReadPosition3;
        private static long lastReadPosition4;

        public BgService(DbService dbService)
        {
            _dbService = dbService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // continuous execution of background task
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    Task t1 = MonitorCups1(15000, cts.Token);
                    await Task.WhenAll(new[] { t1 });
                }
                catch (TaskCanceledException ex)
                {
                    //Console.WriteLine(ex.ToString());
                }
            }
        }

        // monitor ADS connection & detect op time
        //private async Task MonitorAdsConnection(int delay, CancellationToken token)
        private void MonitorAdsConnection(bool adsConnect)
        {
            //while (!cts.IsCancellationRequested)
            //{
            // process every interval
            //await Task.Delay(delay, token);

                if (adsConnect)
                {
                    // add new record if last connection failed
                    if (!timeFlag)
                    {
                        // switch on flag
                        timeFlag = true;
                        // get current time & add new record
                        var optime = new Models.OpTime
                        {
                            TimeStart = DateTime.Now.AddHours(7),
                            TimeStop = DateTime.Now.AddHours(7),
                            TimeWindow = 0
                        };
                        _dbService.InsertOpTime(optime);
                    }
                    // update latest record if connection still alive
                    else
                    {
                        // get latest record
                        var latestOptime = _dbService.FindLatestOptime();
                        var timeStart = latestOptime.TimeStart;
                        var timeStop = DateTime.Now;
                        var timeWindow = Math.Round((timeStop- timeStart).TotalHours, 2);
                        // get current time & add new record
                        var optime = new Models.OpTime
                        {
                            Id = latestOptime.Id,
                            TimeStart = timeStart,
                            TimeStop = timeStop.AddHours(7),
                            TimeWindow = timeWindow
                        };
                        _dbService.UpdateOpTime(optime);
                    }
                    //_adsService.AdsDisconnect();
                }
                else
                {
                    if (timeFlag)
                    {
                        // switch off flag
                        timeFlag = false;
                        //// get current time
                        //timeStop = DateTime.Now.AddHours(7);
                        //// push online time range to DB
                        //var optime = new Models.OpTime
                        //{
                        //    TimeStart = timeStart,
                        //    TimeStop = timeStop,
                        //    TimeWindow = Math.Round(timeStop.Subtract(timeStart).TotalHours, 2)
                        //};
                        //_dbService.InsertOpTime(optime);                  
                    }
                    // try reconnecting
                    //_adsService.AdsConnect(amsNetId, port);
                }
            //}
        }

        // monitor cup data

        private async Task MonitorCups1(int delay, CancellationToken token)
        {
            string fileName = @"Settings/ProductData.txt"; // Tên tệp cần giám sát
            Logger.LogFile(logFile, "Running");
            try
            {
                var timenow = DateTime.UtcNow;
                var cups = _dbService.ListCup(timenow.AddHours(-1), timenow);
                var weight = _dbService.ListTotalWeight(timenow.AddHours(-1), timenow);
                if (!File.Exists(fileName))
                {
                    Logger.LogFile(logFile, "File does not exist.");
                    return;
                }

                // Sử dụng List để lưu trữ các dòng đọc được từ tệp
                List<string> lines = new List<string>();
                // Đọc dữ liệu từ tệp
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = new StreamReader(fs))
                    {  string data;
                        while ((data = reader.ReadLine()) != null)
                        {
                            lines.Add(data);
                            MonitorAdsConnection(true);
                            var values = data.Split(',');
                            prodData = new ProductionData
                            {
                                Status = values[0],
                                Weight = (double)weight.weight,
                                Capacity = (uint)cups.Count(),
                                User = values[1]
                            };
                        }
                    }
                }
                // Kiểm tra nếu dữ liệu khác null hoặc không trống
                if (lines.Count > 0 && lines.Any(line => !string.IsNullOrWhiteSpace(line)))
                {

                    // Xóa nội dung của tệp
                    try
                    {
                        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                        {
                            fs.SetLength(0); // Đặt độ dài của tệp về 0 để xóa nội dung
                            //System.Diagnostics.Debug.WriteLine($"Content of the file {fileName} has been cleared.");
                        }
                    }
                    catch (Exception ex)
                    {
                       // System.Diagnostics.Debug.WriteLine($"Error clearing file content: {ex.Message}");
                    }
                }
                else
                {
                    prodData = new ProductionData { Status = "Offline", Weight = 0, Capacity = 0, User = "Unknown" };
                }

                //add data=========================================================
                string fileName1 = @"Settings/Food1.txt"; // Tên tệp cần giám sát
                using (var fs1 = new FileStream(fileName1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (startInit1)
                    {
                        // Di chuyển con trỏ tệp tin đến cuối tệp tin lần đầu tiên
                        fs1.Seek(0, SeekOrigin.End);
                        lastReadPosition1 = fs1.Position;
                        startInit1 = false; // Đặt cờ để biết rằng đã khởi động lần đầu
                    }
                    else
                    {
                        fs1.Seek(lastReadPosition1, SeekOrigin.Begin);

                        using (var reader = new StreamReader(fs1))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                // Xử lý dòng dữ liệu mới được thêm
                                //System.Diagnostics.Debug.WriteLine(line);

                                // Ví dụ: phân tích cú pháp và chuyển đổi sang đối tượng Cup
                                var values = line.Split(',');
                                var cup = new Models.Cup
                                {
                                    Station = int.Parse(values[0]),
                                    User = values[1],
                                    Type = values[2],
                                    Timestamp = DateTime.ParseExact(values[3].Replace("DT#", ""), "yyyy-MM-dd-HH:mm:ss", CultureInfo.InvariantCulture).AddHours(7),
                                    TimeStart = DateTime.ParseExact(values[4].Replace("DT#", ""), "yyyy-MM-dd-HH:mm:ss", CultureInfo.InvariantCulture).AddHours(7),
                                    TimeWeight = double.Parse(values[5]),
                                    Weight = double.Parse(values[6]),
                                    WeightCmd = double.Parse(values[7]),
                                    Delta = double.Parse(values[8]),
                                    DeltaCmd = double.Parse(values[9]),
                                    Status = values[10]
                                };

                                // Lưu vào cơ sở dữ liệu
                                if (cup.TimeWeight > 0 && cup.Weight > 0)
                                {
                                    _dbService.InsertCup(cup);
                                }
                            }
                            // Cập nhật vị trí đã đọc cuối cùng
                            lastReadPosition1 = fs1.Position;
                        }
                    }
                }
                //food2
                string fileName2 = @"Settings/Food2.txt"; // Tên tệp cần giám sát
                using (var fs2 = new FileStream(fileName2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (startInit2)
                    {
                        // Di chuyển con trỏ tệp tin đến cuối tệp tin lần đầu tiên
                        fs2.Seek(0, SeekOrigin.End);
                        lastReadPosition2 = fs2.Position;
                        startInit2 = false; // Đặt cờ để biết rằng đã khởi động lần đầu
                    }
                    else
                    {
                        fs2.Seek(lastReadPosition2, SeekOrigin.Begin);

                        using (var reader = new StreamReader(fs2))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                // Xử lý dòng dữ liệu mới được thêm
                                // System.Diagnostics.Debug.WriteLine(line);

                                // Ví dụ: phân tích cú pháp và chuyển đổi sang đối tượng Cup
                                var values = line.Split(',');
                                var cup = new Models.Cup
                                {
                                    Station = int.Parse(values[0]),
                                    User = values[1],
                                    Type = values[2],
                                    Timestamp = DateTime.ParseExact(values[3].Replace("DT#", ""), "yyyy-MM-dd-HH:mm:ss", CultureInfo.InvariantCulture).AddHours(7),
                                    TimeStart = DateTime.ParseExact(values[4].Replace("DT#", ""), "yyyy-MM-dd-HH:mm:ss", CultureInfo.InvariantCulture).AddHours(7),
                                    TimeWeight = double.Parse(values[5]),
                                    Weight = double.Parse(values[6]),
                                    WeightCmd = double.Parse(values[7]),
                                    Delta = double.Parse(values[8]),
                                    DeltaCmd = double.Parse(values[9]),
                                    Status = values[10]
                                };

                                // Lưu vào cơ sở dữ liệu
                                if (cup.TimeWeight > 0 && cup.Weight > 0)
                                {
                                    _dbService.InsertCup(cup);
                                }
                            }

                            // Cập nhật vị trí đã đọc cuối cùng
                            lastReadPosition2 = fs2.Position;
                        }
                    }
                }
                //larvea1
                string fileName3 = @"Settings/Larvea1.txt"; // Tên tệp cần giám sát
                using (var fs3 = new FileStream(fileName3, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (startInit3)
                    {
                        // Di chuyển con trỏ tệp tin đến cuối tệp tin lần đầu tiên
                        fs3.Seek(0, SeekOrigin.End);
                        lastReadPosition3 = fs3.Position;
                        startInit3 = false; // Đặt cờ để biết rằng đã khởi động lần đầu
                    }
                    else
                    {
                        fs3.Seek(lastReadPosition3, SeekOrigin.Begin);

                        using (var reader = new StreamReader(fs3))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                // Xử lý dòng dữ liệu mới được thêm
                                //  System.Diagnostics.Debug.WriteLine(line);

                                // Ví dụ: phân tích cú pháp và chuyển đổi sang đối tượng Cup
                                var values = line.Split(',');
                                var cup = new Models.Cup
                                {
                                    Station = int.Parse(values[0]),
                                    User = values[1],
                                    Type = values[2],
                                    Timestamp = DateTime.ParseExact(values[3].Replace("DT#", ""), "yyyy-MM-dd-HH:mm:ss", CultureInfo.InvariantCulture).AddHours(7),
                                    TimeStart = DateTime.ParseExact(values[4].Replace("DT#", ""), "yyyy-MM-dd-HH:mm:ss", CultureInfo.InvariantCulture).AddHours(7),
                                    TimeWeight = double.Parse(values[5]),
                                    Weight = double.Parse(values[6]),
                                    WeightCmd = double.Parse(values[7]),
                                    Delta = double.Parse(values[8]),
                                    DeltaCmd = double.Parse(values[9]),
                                    Status = values[10]
                                };

                                // Lưu vào cơ sở dữ liệu
                                if (cup.TimeWeight > 0 && cup.Weight > 0)
                                {
                                    _dbService.InsertCup(cup);
                                }
                            }
                            // Cập nhật vị trí đã đọc cuối cùng
                            lastReadPosition3 = fs3.Position;
                        }
                    }
                }
                //larvea2
                string fileName4 = @"Settings/Larvea2.txt"; // Tên tệp cần giám sát
                using (var fs4 = new FileStream(fileName4, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (startInit4)
                    {
                        // Di chuyển con trỏ tệp tin đến cuối tệp tin lần đầu tiên
                        fs4.Seek(0, SeekOrigin.End);
                        lastReadPosition4 = fs4.Position;
                        startInit4 = false; // Đặt cờ để biết rằng đã khởi động lần đầu
                    }
                    else
                    {
                        fs4.Seek(lastReadPosition4, SeekOrigin.Begin);

                        using (var reader = new StreamReader(fs4))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                // Xử lý dòng dữ liệu mới được thêm
                                // System.Diagnostics.Debug.WriteLine(line);

                                // Ví dụ: phân tích cú pháp và chuyển đổi sang đối tượng Cup
                                var values = line.Split(',');
                                var cup = new Models.Cup
                                {
                                    Station = int.Parse(values[0]),
                                    User = values[1],
                                    Type = values[2],
                                    Timestamp = DateTime.ParseExact(values[3].Replace("DT#", ""), "yyyy-MM-dd-HH:mm:ss", CultureInfo.InvariantCulture).AddHours(7),
                                    TimeStart = DateTime.ParseExact(values[4].Replace("DT#", ""), "yyyy-MM-dd-HH:mm:ss", CultureInfo.InvariantCulture).AddHours(7),
                                    TimeWeight = double.Parse(values[5]),
                                    Weight = double.Parse(values[6]),
                                    WeightCmd = double.Parse(values[7]),
                                    Delta = double.Parse(values[8]),
                                    DeltaCmd = double.Parse(values[9]),
                                    Status = values[10]
                                };

                                // Lưu vào cơ sở dữ liệu
                                if (cup.TimeWeight > 0 && cup.Weight > 0)
                                {
                                    _dbService.InsertCup(cup);
                                }
                            }
                            // Cập nhật vị trí đã đọc cuối cùng
                            lastReadPosition4 = fs4.Position;
                        }
                    }
                }
                //delete
                string fileF1 = @"Settings/Food1.txt";
                string fileF2 = @"Settings/Food2.txt";
                string fileL1 = @"Settings/Larvea1.txt";
                string fileL2 = @"Settings/Larvea2.txt";

                // Lấy thời gian hiện tại
                DateTime now = DateTime.Now;

                // Tạo đối tượng DateTime đại diện cho 0h sáng của ngày hiện tại
                DateTime startOfDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);

                // Tạo đối tượng DateTime đại diện cho 0h01 sáng của ngày hiện tại
                DateTime endOfWindow = startOfDay.AddMinutes(1);

                // Kiểm tra nếu giờ hiện tại nằm trong khoảng 0h đến 0h05 sáng
                if (now >= startOfDay && now <= endOfWindow)
                {
                    DeleteFile(fileF1);
                    DeleteFile(fileF2);
                    DeleteFile(fileL1);
                    DeleteFile(fileL2);
                    startInit1 = true;
                    startInit2 = true;
                    startInit3 = true;
                    startInit4 = true;
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Error processing file: {ex.Message}");
                Logger.LogFile(logFile, $"Error processing file: {ex.Message}");
            }

            // Đợi một khoảng thời gian trước khi kiểm tra lại
            await Task.Delay(delay, token);
        }

        private static bool DeleteFile(string fileName)
        {
            // Xóa nội dung của tệp
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                {
                    fs.SetLength(0); // Đặt độ dài của tệp về 0 để xóa nội dung
                    //System.Diagnostics.Debug.WriteLine($"Content of the file {fileName} has been cleared.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"Error clearing file content: {ex.Message}");
                return false;
            }
        }


        public async void ResetTask()
        {
            cts.Cancel();
            cts.Dispose();
            cts = new CancellationTokenSource();
            await ExecuteAsync(cts.Token);
        }

       
    }
}
