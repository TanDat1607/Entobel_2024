using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using CsvHelper;
using MongoDB.Bson;

using entobel_be.Models;
using entobel_be.Services;

namespace entobel_be.Services
{
    public class ReportService
    {
        private readonly DbService _dbService;
        public ReportService(DbService dbService)
        {
            _dbService = dbService;
        }

        public class Foo
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        // write data & return csv file
        public FileStreamResult WriteCsv(DateTime startDate, DateTime endDate, string timeRange, string type)
        {
            //var records = new List<Foo>
            //{
            //    new Foo { Id = 1, Name = "one" },
            //    new Foo { Id = 2, Name = "two" },
            //};
            // init memory stream
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream);
            //using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
            //{
            //    csvWriter.WriteRecords(records);
            //    streamWriter.Flush();
            //}
            //
            var weightCmds = _dbService.ListWeightCmd();
            // check report type & write csv
            if (type == "cups")
            {
                // ===== FIX HERE !!! =====
                var recordCups = _dbService.ListCupOutput(1, startDate, endDate, timeRange, weightCmds[0]);
                System.Diagnostics.Debug.WriteLine(startDate + " " + endDate);
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(recordCups);
                    streamWriter.Flush();
                }
            }
            else if (type == "weight")
            {
                // ===== FIX HERE !!! =====
                var recordWeight = _dbService.ListWeight(1, "Larvae", startDate, endDate, timeRange, weightCmds[0]);
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(recordWeight);
                    streamWriter.Flush();
                }
            }
            else if (type == "optime")
            {
                var recordOptime = _dbService.ListOptime(startDate, endDate, timeRange);
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(recordOptime);
                    streamWriter.Flush();
                }
            }
            // return result
            return new FileStreamResult(new MemoryStream(memoryStream.ToArray()), "text/csv") { FileDownloadName = "export.csv" };
        }

        // write data & return csv file of all
        public FileStreamResult WriteCsv(DateTime startDate, DateTime endDate, string timeRange)
        {
            // init memory stream
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream);
            // check report type & write csv
            if (timeRange == "All")
            {
                var recordsAll = _dbService.ListProduction(startDate, endDate);
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(recordsAll);
                    streamWriter.Flush();
                }
                // return result
                return new FileStreamResult(new MemoryStream(memoryStream.ToArray()), "text/csv") { FileDownloadName = "export.csv" };
            }
            else 
            { 
                var records = _dbService.ListProduction(startDate, endDate, timeRange);
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(records);
                    streamWriter.Flush();
                }
                // return result
                return new FileStreamResult(new MemoryStream(memoryStream.ToArray()), "text/csv") { FileDownloadName = "export.csv" };
            }
        }

        // write data & return csv file of all
        public FileStreamResult WriteCsv(List<AdsEvent> events)
        {
            // init memory stream
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream);
            // check report type & write csv
            using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
            {
                csvWriter.WriteRecords(events);
                streamWriter.Flush();
            }
            // return result
            return new FileStreamResult(new MemoryStream(memoryStream.ToArray()), "text/csv") { FileDownloadName = "events.csv" };
        }

        // write data & return memory stream only
        public MemoryStream WriteMemoryStream(DateTime startDate, DateTime endDate, string timeRange)
        {
            // init memory stream
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream);
            // check report type & write csv
            if (timeRange == "All")
            {
                var recordsAll = _dbService.ListProduction(startDate, endDate);
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(recordsAll);
                    streamWriter.Flush();
                }
                // return result
                return new MemoryStream(memoryStream.ToArray());
            }
            else
            {
                var records = _dbService.ListProduction(startDate, endDate, timeRange);
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(records);
                    streamWriter.Flush();
                }
                // return result
                return new MemoryStream(memoryStream.ToArray());
            }
        }

    }
}
