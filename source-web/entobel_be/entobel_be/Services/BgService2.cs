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
    public class BgService2 : BackgroundService
    {
        private readonly AdsService _adsService;

        CancellationTokenSource cts = new CancellationTokenSource();

        public readonly string amsNetId = File.ReadAllText("Settings/netid.txt");
        public readonly int port = int.Parse(File.ReadAllText("Settings/port.txt"));
        //public readonly string amsNetId = "127.0.0.1.1.1";
        //public readonly int port = 851;
        public List<AdsEvent> events = new List<AdsEvent>();

        public BgService2(AdsService adsService)
        {
            _adsService = adsService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // continuous execution of background task
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    Task t1 = MonitorCups(1000, cts.Token);
              
                    await Task.WhenAll(new[] { t1 });
                }
                catch (TaskCanceledException ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
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
                    // read eventlogger
                    events = new List<AdsEvent>();
                    events = _adsService.AdsReadEvents(20);
                    // get live production data
                }
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
