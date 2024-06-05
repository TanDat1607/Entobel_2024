using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;

using entobel_be.Models;
using static System.Collections.Specialized.BitVector32;
using static entobel_be.Models.Summary;
using System.Reactive;
using static entobel_be.Services.ReportService;
using SharpCompress.Common;
using TwinCAT.Ads;

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

        public BgService(DbService dbService, AdsService adsService, MailService maService)
        {
            _dbService = dbService;
            _adsService = adsService;
            _maService = maService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // continuous execution of background task
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    // ADS connection monitoring
                    //Task t0 = MonitorAdsConnection(5000, cts.Token);
                    // Cup Monitoring task
                    Task t1 = MonitorCups(1000, cts.Token);
                    // Report Monitoring task
                    Task t2 = MonitorReport(1000 * 30, cts.Token);
                    // FFT & RMS logging task
                    //Task t2 = LogVibrationData(loggingInterval, cts.Token); //30 mins
                    // Power monitoring task
                    //Task t3 = MonitorElectricData(monitoringInterval, cts.Token);
                    //await Task.WhenAll(new[] { t0, t1, t2, t3 });
                    await Task.WhenAll(new[] { t1 });
                }
                catch (TaskCanceledException ex)
                {
                    Console.WriteLine(ex.ToString());
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
        private async Task MonitorCups(int delay, CancellationToken token)
        {
            while (!cts.IsCancellationRequested)
            {
                //_adsService.AdsConnect(amsNetId, port);
                //var x = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fWeight_Food_line1", typeof(double));
                //System.Diagnostics.Debug.WriteLine(x);
                //_adsService.AdsDisconnect();
                // process every interval
                await Task.Delay(delay, token);

                //Logger.LogFile(logFile, "Hello");
                // connect ADS
                var adsConnect = _adsService.AdsConnect(amsNetId, port);
                //_adsService.AdsDisconnect();
                if (adsConnect)
                {
                    // monitor ads connection
                    MonitorAdsConnection(adsConnect);
                    // read eventlogger
                    events = new List<AdsEvent>();
                    events = _adsService.AdsReadEvents(20);
                    // get live production data
                    var timenow = DateTime.UtcNow;
                    var cups = _dbService.ListCup(timenow.AddHours(-1), timenow);
                    var weight = _dbService.ListTotalWeight(timenow.AddHours(-1), timenow);

                    // ============ INFORM PLC CONNECTION ============
                    try
                    {
                        _adsService.AdsWrite(_adsService.tcAdsClient, "GVL_IoT.bAlarm", true);
                    }
                    catch (Exception err)
                    {
                        System.Diagnostics.Debug.WriteLine(err.Message);
                        Logger.LogFile(logFile, err.Message);
                    }

                    // ============ PRODUCTION DATA ============
                    try
                    {
                        prodData = new ProductionData
                        {
                            Status = (string)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.status", typeof(string)),
                            Weight = (double)weight.weight,
                            //Weight = 0.0f,
                            Capacity = (uint)cups.Count(),
                            //Power = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_Retain.DesiredWeight", typeof(double)),
                            User = (string)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.username", typeof(string))
                        };
                        //Logger.LogFile(logFile, prodData.ToJson());
                    }
                    catch (Exception err)
                    {
                        prodData = new ProductionData { Status = "Offline", Weight = 0, Capacity = 0, User = "Unknown" };
                        System.Diagnostics.Debug.WriteLine(err.Message);
                        //Logger.LogFile(logFile, err.Message);
                    }


                    // ============ LINE 1 - FOOD ============
                    // read local timestamp
                    var timeStopF1 = (DateTime)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.Time_Stop_Food_line1", typeof(DateTime));
                    // read start measuring time
                    var timeStartF1 = (DateTime)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.Time_Start_Food_line1", typeof(DateTime));
                    // check time of last cup, if new data detected then push to DB
                    if (timeCupF1 < timeStopF1 && timeStopF1 != new DateTime(1970, 01, 01, 0, 0, 0))
                    {
                        // get data of cup
                        var cup = new Models.Cup
                        {
                            Station = 1,
                            User = (string)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.username", typeof(string)),
                            Type = "Food",
                            Timestamp = timeStartF1.AddHours(7),
                            TimeStart = timeStopF1.AddHours(7),
                            TimeWeight = (timeStopF1 - timeStartF1).TotalSeconds,
                            Weight = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fWeight_Food_line1", typeof(double)),
                            WeightCmd = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fDerisedWeight_Food_line1", typeof(double)),
                            Delta = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fActDelta_Food_line1", typeof(double)),
                            DeltaCmd = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fDelta_Food_line1", typeof(double)),
                            Status = (string)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.bStatus_Food_line1", typeof(string)),
                        };
                        System.Diagnostics.Debug.WriteLine(cup.ToJson());
                        Logger.LogFile(logFile, cup.ToJson());
                        // push to DB
                        if (cup.TimeWeight > 0 && cup.Weight > 0)
                        {
                            _dbService.InsertCup(cup);
                        }
                        // update time of last cup
                        timeCupF1 = timeStopF1;
                    }
                    // ============ LINE 1 - LARVAE ============
                    // read local timestamp
                    var timeStopL1 = (DateTime)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.Time_Stop_Worm_line1", typeof(DateTime));
                    // read start measuring time
                    var timeStartL1 = (DateTime)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.Time_Start_Worm_line1", typeof(DateTime));
                    // check time of last cup, if new data detected then push to DB
                    if (timeCupL1 < timeStopL1 && timeStopL1 != new DateTime(1970, 01, 01, 0, 0, 0))
                    {
                        // get data of cup
                        var cup = new Models.Cup
                        {
                            Station = 1,
                            User = (string)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.username", typeof(string)),
                            Type = "Larvae",
                            Timestamp = timeStartL1.AddHours(7),
                            TimeStart = timeStopL1.AddHours(7),
                            TimeWeight = (timeStopL1 - timeStartL1).TotalSeconds,
                            Weight = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fWeight_Worm_line1", typeof(double)),
                            WeightCmd = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fDerisedWeight_Worm_line1", typeof(double)),
                            Delta = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fActDelta_Worm_line1", typeof(double)),
                            DeltaCmd = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fDelta_Worm_line1", typeof(double)),
                            Status = (string)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.bStatus_Worm_line1", typeof(string)),
                        };
                        System.Diagnostics.Debug.WriteLine(cup.ToJson());
                        Logger.LogFile(logFile, cup.ToJson());
                        // push to DB
                        if (cup.TimeWeight > 0 && cup.Weight > 0)
                        {
                            _dbService.InsertCup(cup);
                        }
                        // update time of last cup
                        timeCupL1 = timeStopL1;
                    }
                    // ============ LINE 2 - FOOD ============
                    // read local timestamp
                    var timeStopF2 = (DateTime)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.Time_Stop_Food_line2", typeof(DateTime));
                    // read start measuring time
                    var timeStartF2 = (DateTime)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.Time_Start_Food_line2", typeof(DateTime));
                    // check time of last cup, if new data detected then push to DB
                    if (timeCupF2 < timeStopF2 && timeStopF2 != new DateTime(1970, 01, 01, 0, 0, 0))
                    {
                        // get data of cup
                        var cup = new Models.Cup
                        {
                            Station = 2,
                            User = (string)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.username", typeof(string)),
                            Type = "Food",
                            Timestamp = timeStartF2.AddHours(7),
                            TimeStart = timeStopF2.AddHours(7),
                            TimeWeight = (timeStopF2 - timeStartF2).TotalSeconds,
                            Weight = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fWeight_Food_line2", typeof(double)),
                            WeightCmd = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fDerisedWeight_Food_line2", typeof(double)),
                            Delta = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fActDelta_Food_line2", typeof(double)),
                            DeltaCmd = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fDelta_Food_line2", typeof(double)),
                            Status = (string)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.bStatus_Food_line2", typeof(string)),
                        };
                        System.Diagnostics.Debug.WriteLine(cup.ToJson());
                        Logger.LogFile(logFile, cup.ToJson());
                        // push to DB
                        if (cup.TimeWeight > 0 && cup.Weight > 0)
                        {
                            _dbService.InsertCup(cup);
                        }
                        // update time of last cup
                        timeCupF2 = timeStopF2;
                    }
                    // ============ LINE 2 - LARVAE ============
                    // read local timestamp
                    var timeStopL2 = (DateTime)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.Time_Stop_Worm_line2", typeof(DateTime));
                    // read start measuring time
                    var timeStartL2 = (DateTime)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.Time_Start_Worm_line2", typeof(DateTime));
                    // check time of last cup, if new data detected then push to DB
                    if (timeCupL2 < timeStopL2 && timeStopL2 != new DateTime(1970, 01, 01, 0, 0, 0))
                    {
                        // get data of cup
                        var cup = new Models.Cup
                        {
                            Station = 2,
                            User = (string)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.username", typeof(string)),
                            Type = "Larvae",
                            Timestamp = timeStartL2.AddHours(7),
                            TimeStart = timeStopL2.AddHours(7),
                            TimeWeight = (timeStopL2 - timeStartL2).TotalSeconds,
                            Weight = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fWeight_Worm_line2", typeof(double)),
                            WeightCmd = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fDerisedWeight_Worm_line2", typeof(double)),
                            Delta = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fActDelta_Worm_line2", typeof(double)),
                            DeltaCmd = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fDelta_Worm_line2", typeof(double)),
                            Status = (string)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.bStatus_Worm_line2", typeof(string)),
                        };
                        System.Diagnostics.Debug.WriteLine(cup.ToJson());
                        Logger.LogFile(logFile, cup.ToJson());
                        // push to DB
                        if (cup.TimeWeight > 0 && cup.Weight > 0)
                        {
                            _dbService.InsertCup(cup);
                        }
                        // update time of last cup
                        timeCupL2 = timeStopL2;
                    }
                    // disconnect ADS
                    _adsService.AdsDisconnect();
                }
                else
                {
                    prodData = new ProductionData { Status = "Offline", Weight = 0, Capacity = 0, User = "Unknown" };
                }
                
            }
        }

        // monitor sending report
        private async Task MonitorReport(int delay, CancellationToken token)
        {
            while (!cts.IsCancellationRequested)
            {
                // process every interval
                await Task.Delay(delay, token);
                // Check for internet connection
                //if (NetworkInterface.GetIsNetworkAvailable())
                //{
                //    // read last report
                //    var rp = _dbService.FindLastReport();
                //    // check last report . current time
                //    DateTime dt = DateTime.Now.AddHours(7);
                //    var startofMonth = new DateTime(dt.Year, dt.Month, 1, 0, 0, 0);

                //    if (rp == null || rp.Timestamp < startofMonth)
                //    {
                //        if (_maService.SendMailReport())
                //        {
                //            var rpHist = new ReportHistory
                //            {
                //                Timestamp = DateTime.Now.AddHours(7)
                //            };
                //            _dbService.InsertReportHistory(rpHist);
                //        }
                //    }
                //}
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
