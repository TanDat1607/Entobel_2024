using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

using entobel_be.Models;
using entobel_be.Services;
using TwinCAT.Ads;
using System.Net.NetworkInformation;

namespace entobel_be.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HttpController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly DbService _dbService;
        private readonly AdsService _adsService;
        private readonly ReportService _rpService;
        private readonly MailService _maService;
        private readonly BgService _bgService;

        public HttpController(UserService userService, DbService dbService, AdsService adsService, ReportService rpService, MailService maService, BgService bgService)
        {
            _userService = userService;
            _dbService = dbService;
            _adsService = adsService;
            _rpService = rpService;
            _maService = maService;
            _bgService = bgService;
        }

        // ----- READ COMMAND -----
        // login user
        [HttpPost("login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            System.Diagnostics.Debug.WriteLine(username);
            try
            {
                var value = _userService.Login(username, password);
                return Ok(value);
            }
            catch (Exception err)
            {
                Console.WriteLine("Action failed: " + err.Message);
                return StatusCode(500);
            }
        }

        // Authenicate user
        [HttpPost("auth")]
        public IActionResult Auth()
        {
            try
            {
                _userService.Auth(HttpContext.ToString());
                return Ok();
            }
            catch (Exception err)
            {
                Console.WriteLine("Action failed: " + err.Message);
                return StatusCode(500);
            }
        }

        // get cup by id
        [HttpGet("FindCup/{id:length(24)}", Name = "FindCup")]
        public ActionResult<Cup> FindCup(string id)
        {
            var cup = _dbService.FindCup(id);
            if (cup == null)
            {
                return NotFound();
            }
            return cup;
        }

        // get mail by id
        [HttpGet("FindMail/{id:length(24)}", Name = "FindMail")]
        public ActionResult<MailAccount> FindMail(string id)
        {
            var mail = _dbService.FindMail(id);
            if (mail == null)
            {
                return NotFound();
            }
            return mail;
        }

        // get cup cmd list
        [HttpGet("ListWeightCmd")]
        public ActionResult<List<double>> ListWeightCmd()
        {
            var cupCmd = _dbService.ListWeightCmd();
            if (cupCmd == null)
            {
                return NotFound();
            }
            return cupCmd;
        }

        // get realtime production data
        [HttpGet("GetProductionData")]
        public ActionResult<ProductionData> GetProductionData()
        {
            return _bgService.prodData;
            //// calculate capacity from DB
            //var timenow = DateTime.UtcNow;
            //var cups = _dbService.ListCup(timenow.AddHours(-1), timenow);
            //var weight = _dbService.ListTotalWeight(timenow.AddHours(-1), timenow);
            //// read from ADS
            //_adsService.AdsConnect(_bgService.amsNetId, _bgService.port);
            //if (_adsService.AdsCheckConnection())
            //{
            //    var prodData = new ProductionData
            //    {
            //        Status = (string)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.status", typeof(string)),
            //        Weight = (double)weight.weight,
            //        Capacity = (uint)cups.Count(),
            //        //Power = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_Retain.DesiredWeight", typeof(double)),
            //        User = (string)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.username", typeof(string))
            //    };
            //    // close connection
            //    _adsService.AdsDisconnect();
            //    return prodData;
            //}
            //else
            //{
            //    return new ProductionData { Status = "Offline", Weight = 0, Capacity = 0, User = "Unknown" };
            //}
        }

        // get capacity history of cups data from db
        [HttpGet("ListCupOutput/{station}")]
        public ActionResult<List<Summary.Cup>> ListCupOutput(int station, DateTime startDate, DateTime endDate, string timeRange, double weightCmd)
        {
            System.Diagnostics.Debug.WriteLine("EWRGERGREGERG:" + station);
            var result = _dbService.ListCupOutput(station, startDate, endDate, timeRange, weightCmd);
            if (result == null)
            {
                return NotFound();
            }
            return result;
        }

        //// get capacity history of cups rejected data from db
        //[HttpGet("ListCupReject")]
        //public ActionResult<List<Summary.Cup>> ListCupReject(DateTime startDate, DateTime endDate, string timeRange, double weightCmd)
        //{
        //    var result = _dbService.ListCupReject(startDate, endDate, timeRange, weightCmd);
        //    if (result == null)
        //    {
        //        return NotFound();
        //    }
        //    return result;
        //}

        // get capacity history of weight data from db
        [HttpGet("ListWeight/{station}")]
        public ActionResult<List<Summary.Weight>> ListWeight(int station, DateTime startDate, DateTime endDate, string timeRange, double weightCmd)
        {
            var result = _dbService.ListWeight(station, "Larvae", startDate, endDate, timeRange, weightCmd);
            if (result == null)
            {
                return NotFound();
            }
            return result;
        }

        // get capacity history of weight data from db
        [HttpGet("ListFoodWeight/{station}")]
        public ActionResult<List<Summary.Weight>> ListFoodWeight(int station, DateTime startDate, DateTime endDate, string timeRange, double weightCmd)
        {
            var result = _dbService.ListWeight(station, "Food", startDate, endDate, timeRange, weightCmd);
            if (result == null)
            {
                return NotFound();
            }
            return result;
        }

        // get optime history data from db
        [HttpGet("ListOptime")]
        public ActionResult<List<Summary.OpTime>> ListOptime(DateTime startDate, DateTime endDate, string timeRange)
        {
            var result = _dbService.ListOptime(startDate, endDate, timeRange);
            if (result == null)
            {
                return NotFound();
            }
            return result;
        }

        // get optime history data from db
        [HttpGet("ListDowntime")]
        public ActionResult<List<Summary.OpTime>> ListDowntime(DateTime startDate, DateTime endDate, string timeRange)
        {
            var result = _dbService.ListDowntime(startDate, endDate, timeRange);
            if (result == null)
            {
                return NotFound();
            }
            return result;
        }

        // read ADS events
        [HttpGet("ReadLoggedEvents")]
        public ActionResult<List<AdsEvent>> ReadLoggedEvents()
        {
            //_adsService.AdsConnect(_bgService.amsNetId, _bgService.port);
            //var results = _adsService.AdsReadEvents(10);
            //_adsService.AdsDisconnect();
            return _bgService.events;
        }
            

        // export report csv
        [HttpGet("DownloadReport")]
        public FileStreamResult DownloadReport(DateTime startDate, DateTime endDate, string timeRange, string type)
        {
            return _rpService.WriteCsv(startDate, endDate, timeRange);
        }

        // export report csv
        [HttpGet("DownloadEvents")]
        public FileStreamResult DownloadEvents()
        {
            var events = _bgService.events;
            if (events == null)
            {
                return null;
            }
            else return _rpService.WriteCsv(events);
        }

        // get optime history data from db
        [HttpGet("GetCupData")]
        public ActionResult<DateTime> GetCupData()
        {
            return (DateTime)_adsService.AdsRead(_adsService.tcAdsClient, "GVL.PI.dtTimestamp", typeof(DateTime));
        }

        // get mail data from db
        [HttpGet("ListMail")]
        public ActionResult<List<MailAccount>> ListMail() =>
            _dbService.ListMail();


        // get optime history data from db
        [HttpGet("SendMail")]
        public ActionResult<string> SendMail()
        {
            _maService.SendMailReport();
            return NoContent();
        }

        // get optime history data from db
        [HttpGet("Test")]
        public void Test()
        {
            _adsService.AdsConnect("127.0.0.1.1.1", 851);
            System.Diagnostics.Debug.WriteLine("========================================");
            var x = (double)_adsService.AdsRead(_adsService.tcAdsClient, "GVL_IoT.fWeight_Food_line1", typeof(double));
            System.Diagnostics.Debug.WriteLine(x);
            _adsService.AdsDisconnect();
            //System.Diagnostics.Debug.WriteLine(prodData.ToJson());
        }

        [HttpGet("testmail")]
        public void Testmail()
        {
            // Check for internet connection
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                // read last report
                var rp = _dbService.FindLastReport();
                // check last report . current time
                DateTime dt = DateTime.Now.AddHours(7);
                var startofMonth = new DateTime(dt.Year, dt.Month, 1, 0, 0, 0);
                System.Diagnostics.Debug.WriteLine("==========================================");
                System.Diagnostics.Debug.WriteLine(rp == null);
                if (rp == null || rp.Timestamp < startofMonth)
                {
                    if (_maService.SendMailReport())
                    {
                        var rpHist = new ReportHistory
                        {
                            Timestamp = DateTime.Now.AddHours(7)
                        };
                        _dbService.InsertReportHistory(rpHist);
                    }
                }
            }
        }

        // ----- CREATE COMMAND -----
        //Cup collection
        [HttpPost("InsertMail")]
        public ActionResult<MailAccount> InsertMail(MailAccount mail)
        {
            _dbService.InsertMail(mail);
            return CreatedAtRoute("FindMail", new { id = mail.Id.ToString() }, mail);
        }

        // ----- UPDATE COMMAND -----

        // ----- DELETE COMMAND -----
        [HttpDelete("DeleteMail/{id:length(24)}")]
        public void DeleteMail(string id)
        {
            _dbService.DeleteMail(id);
        }

    }
}
