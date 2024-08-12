using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;

using entobel_be.Models;
using static System.Collections.Specialized.BitVector32;

namespace entobel_be.Services
{
    public class DbService
    {
        public readonly List<IMongoCollection<BsonDocument>> collection = new List<IMongoCollection<BsonDocument>>();

        private readonly IMongoCollection<User> _user;
        private readonly IMongoCollection<Cup> _cup;
        private readonly IMongoCollection<OpTime> _optime;
        private readonly IMongoCollection<ReportHistory> _rpHist;
        private readonly IMongoCollection<MailAccount> _mail;

        private readonly string logFile = "Logs/log.txt";

        // Init connection to DB and get all collection
        public DbService()
        {
            // init connection
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("db_Entobel");
            // symbol collection
            _user = database.GetCollection<User>("col_User");
            _cup = database.GetCollection<Cup>("col_Cup");
            _optime = database.GetCollection<OpTime>("col_OpTime");
            _rpHist = database.GetCollection<ReportHistory>("col_ReportHistory");
            _mail = database.GetCollection<MailAccount>("col_Mail");
        }

        // ----- READ COMMAND -----
        // get all users
        public List<User> GetUsers()
        {
            var users = _user.Find(_ => true).ToList();
            return users;
        }

        // find cup by id
        public Cup FindCup(string id) =>
            _cup.Find(cup => cup.Id == id).FirstOrDefault();

        // list all cup data by datetime
        public List<Cup> ListCup(DateTime startDate, DateTime endDate)
        {
            var builder = Builders<Cup>.Filter;
            var filter = builder.Gte(cup => cup.Timestamp, startDate) & builder.Lt(cup => cup.Timestamp, endDate.AddDays(1));
            var result = _cup.Find(filter).ToList();
            return result;
        }

        // list all weight data by datetime
        public Summary.Weight ListTotalWeight(DateTime startDate, DateTime endDate)
        {
            var builder = Builders<Cup>.Filter;
            var filter = builder.Eq(cup => cup.Type, "Larvae") & 
                         builder.Gte(cup => cup.Timestamp, startDate.AddHours(7)) & 
                         builder.Lt(cup => cup.Timestamp, endDate.AddDays(1).AddHours(7));
            var result = _cup.Aggregate()
                    .Match(filter)
                    .Group(new BsonDocument { { "_id", 0 }, { "weight", new BsonDocument("$sum", "$weight") } })
                    .Project(new BsonDocument { { "_id", 0 } })
                    .ToListAsync().Result;

            //Logger.LogFile(logFile, result.ToJson());
            //Logger.LogFile(logFile, result[0].ToJson());

            if (result.Count == 0) 
            {
                var weight = new Summary.Weight();
                weight.weight = 0;
                return weight;
            }
            else 
                return BsonSerializer.Deserialize<Summary.Weight>(result[0].ToJson());
        }

        // list all cup cmd values
        public List<double> ListWeightCmd()
        {
            var result = _cup.Distinct<double>("weightCmd", FilterDefinition<Cup>.Empty).ToList();
            return result;
        }

        // list capacity of cups by time range
        public List<Summary.Cup> ListCupOutput(int station, DateTime startDate, DateTime endDate, string timeRange, double weightCmd)
        {
            System.Diagnostics.Debug.WriteLine("EWRGERGREGERG:"+station+startDate+endDate+timeRange+weightCmd);
            var builder = Builders<Cup>.Filter;
            var filter = builder.Eq(cup => cup.Station, station) &
                         builder.Gte(cup => cup.Timestamp, startDate.AddHours(7)) &
                         builder.Lt(cup => cup.Timestamp, endDate.AddDays(1).AddHours(7)) &
                         builder.Eq(cup => cup.WeightCmd, weightCmd);
                         //builder.Eq(cup => cup.Status, true);
            var group = new BsonDocument();
            // assign group query based on timeRange option
            switch (timeRange) 
            {
                case "Day":
                    group = new BsonDocument("$concat", new BsonArray{
                        new BsonDocument("$toString", new BsonDocument("$year", "$timestamp")),
                        "-",
                        new BsonDocument("$cond", new BsonDocument{
                            { "if", new BsonDocument("$lt", new BsonArray{ new BsonDocument("$month", "$timestamp"), 10 }) },
                            { "then", new BsonDocument("$concat", new BsonArray{ "0", new BsonDocument("$toString", new BsonDocument("$month", "$timestamp")) }) },
                            { "else", new BsonDocument("$toString", new BsonDocument("$month", "$timestamp")) }
                        }),
                        "-",
                        new BsonDocument("$cond", new BsonDocument{
                            { "if", new BsonDocument("$lt", new BsonArray{ new BsonDocument("$dayOfMonth", "$timestamp"), 10 }) },
                            { "then", new BsonDocument("$concat", new BsonArray{ "0", new BsonDocument("$toString", new BsonDocument("$dayOfMonth", "$timestamp")) }) },
                            { "else", new BsonDocument("$toString", new BsonDocument("$dayOfMonth", "$timestamp")) }
                        })
                    });
                    break;
  
                case "Month":
                    group = new BsonDocument("$concat", new BsonArray{
                        new BsonDocument("$toString", new BsonDocument("$year", "$timestamp")),
                        "-",
                        new BsonDocument("$cond", new BsonDocument{
                            { "if", new BsonDocument("$lt", new BsonArray{ new BsonDocument("$month", "$timestamp"), 10 }) },
                            { "then", new BsonDocument("$concat", new BsonArray{ "0", new BsonDocument("$toString", new BsonDocument("$month", "$timestamp")) }) },
                            { "else", new BsonDocument("$toString", new BsonDocument("$month", "$timestamp")) }
                        })
                    });
                    break;

            }
            // aggregate query
            var result = _cup.Aggregate()
                    .Match(filter)
                    .Group(new BsonDocument { { "_id", group }, { "count", new BsonDocument("$sum", 1) } })
                    .Sort("{_id: 1}")
                    .ToListAsync().Result;

            var allTimeRanges = GenerateTimeRanges(startDate, endDate, timeRange);

            // Merge the results with the full range
            var resultDictionary = result.ToDictionary(r => r["_id"].AsString, r => r["count"].AsInt32);
            var finalResults = allTimeRanges.Select(tr =>
            {
                return new Summary.Cup
                {
                    Id = tr.Id,
                    count = resultDictionary.ContainsKey(tr.Id) ? resultDictionary[tr.Id] : 0
                };
            }).ToList();
            //System.Diagnostics.Debug.WriteLine("Result: " + result.ToJson());
            //return BsonSerializer.Deserialize<List<Summary.Cup>>(result.ToJson());
            return finalResults;
        }
        
        private List<Summary.Cup> GenerateTimeRanges(DateTime startDate, DateTime endDate, string timeRange)
        {
            var timeRanges = new List<Summary.Cup>();

            switch (timeRange)
            {
                case "Day":
                    for (DateTime dt = startDate.Date; dt <= endDate.Date; dt = dt.AddDays(1))
                    {
                        timeRanges.Add(new Summary.Cup
                        {
                            Id = dt.ToString("yyyy-MM-dd")
                        });
                    }
                    break;

                case "Month":
                    for (DateTime dt = new DateTime(startDate.Year, startDate.Month, 1); dt <= new DateTime(endDate.Year, endDate.Month, 1); dt = dt.AddMonths(1))
                    {
                        timeRanges.Add(new Summary.Cup
                        {
                            Id = dt.ToString("yyyy-MM")
                        });
                    }
                    break;
            }
            return timeRanges;
        }

        // list capacity of weight by time range
        public List<Summary.Weight> ListWeight(int station, string type, DateTime startDate, DateTime endDate, string timeRange, double weightCmd)
        {
            var builder = Builders<Cup>.Filter;
            var filter = builder.Eq(cup => cup.Station, station) &
                         builder.Eq(cup => cup.Type, type) &
                         builder.Gte(cup => cup.Timestamp, startDate.AddHours(7)) & 
                         builder.Lt(cup => cup.Timestamp, endDate.AddDays(1).AddHours(7)) & 
                         builder.Eq(cup => cup.WeightCmd, weightCmd);
            var group = new BsonDocument();
            // assign group query based on timeRange option
            switch (timeRange)
            {
                case "Day":
                    group = new BsonDocument("$concat", new BsonArray{
                        new BsonDocument("$toString", new BsonDocument("$year", "$timestamp")),
                        "-",
                        new BsonDocument("$cond", new BsonDocument{
                            { "if", new BsonDocument("$lt", new BsonArray{ new BsonDocument("$month", "$timestamp"), 10 }) },
                            { "then", new BsonDocument("$concat", new BsonArray{ "0", new BsonDocument("$toString", new BsonDocument("$month", "$timestamp")) }) },
                            { "else", new BsonDocument("$toString", new BsonDocument("$month", "$timestamp")) }
                        }),
                        "-",
                        new BsonDocument("$cond", new BsonDocument{
                            { "if", new BsonDocument("$lt", new BsonArray{ new BsonDocument("$dayOfMonth", "$timestamp"), 10 }) },
                            { "then", new BsonDocument("$concat", new BsonArray{ "0", new BsonDocument("$toString", new BsonDocument("$dayOfMonth", "$timestamp")) }) },
                            { "else", new BsonDocument("$toString", new BsonDocument("$dayOfMonth", "$timestamp")) }
                        })
                    });
                    break;
   
                case "Month":
                    group = new BsonDocument("$concat", new BsonArray{
                        new BsonDocument("$toString", new BsonDocument("$year", "$timestamp")),
                        "-",
                        new BsonDocument("$cond", new BsonDocument{
                            { "if", new BsonDocument("$lt", new BsonArray{ new BsonDocument("$month", "$timestamp"), 10 }) },
                            { "then", new BsonDocument("$concat", new BsonArray{ "0", new BsonDocument("$toString", new BsonDocument("$month", "$timestamp")) }) },
                            { "else", new BsonDocument("$toString", new BsonDocument("$month", "$timestamp")) }
                        })
                    });
                    break;
   
                case "Year":
                    group = new BsonDocument("$concat", new BsonArray{
                        new BsonDocument("$toString", new BsonDocument("$year", "$timestamp"))
                    });
                    break;

            }
            // aggregate query
            var result = _cup.Aggregate()
                    .Match(filter)
                    .Group(new BsonDocument { { "_id", group }, { "weight", new BsonDocument("$sum", "$weight") } })
                    .Sort("{_id: 1}")
                    .ToListAsync().Result;
            var allTimeRanges = GenerateTimeRanges(startDate, endDate, timeRange);

            // Merge the results with the full range
            var resultDictionary = result.ToDictionary(r => r["_id"].AsString, r => r["weight"].AsDouble);
            var finalResults = allTimeRanges.Select(tr =>
            {
                return new Summary.Weight
                {
                    Id = tr.Id,
                    weight = resultDictionary.ContainsKey(tr.Id) ? resultDictionary[tr.Id] : 0
                };
            }).ToList();
            return finalResults;
        }

        // find latest optime record
        public OpTime FindLatestOptime()
        {
            var sort = Builders<OpTime>.Sort.Descending(op => op.TimeStart);
            var latestRecord = _optime.Find(_ => true)
                                .Sort(sort)
                                .FirstOrDefault();
            return latestRecord;
        }

        // list optime by time range
        public List<Summary.OpTime> ListOptime(DateTime startDate, DateTime endDate, string timeRange)
        {
            var builder = Builders<OpTime>.Filter;
            var filter = builder.Gte(optime => optime.TimeStart, startDate.AddHours(7)) & builder.Lt(optime => optime.TimeStop, endDate.AddDays(1).AddHours(7));
            var group = new BsonDocument();
            // assign group query based on timeRange option
            switch (timeRange)
            {      
                case "Day":
                    group = new BsonDocument("$concat", new BsonArray{
                        new BsonDocument("$toString", new BsonDocument("$year", "$timeStart")),
                        "-",
                        new BsonDocument("$cond", new BsonDocument{
                            { "if", new BsonDocument("$lt", new BsonArray{ new BsonDocument("$month", "$timeStart"), 10 }) },
                            { "then", new BsonDocument("$concat", new BsonArray{ "0", new BsonDocument("$toString", new BsonDocument("$month", "$timeStart")) }) },
                            { "else", new BsonDocument("$toString", new BsonDocument("$month", "$timeStart")) }
                        }),
                        "-",
                        new BsonDocument("$cond", new BsonDocument{
                            { "if", new BsonDocument("$lt", new BsonArray{ new BsonDocument("$dayOfMonth", "$timeStart"), 10 }) },
                            { "then", new BsonDocument("$concat", new BsonArray{ "0", new BsonDocument("$toString", new BsonDocument("$dayOfMonth", "$timeStart")) }) },
                            { "else", new BsonDocument("$toString", new BsonDocument("$dayOfMonth", "$timeStart")) }
                        })
                    });
                    break;
   
                case "Month":
                    group = new BsonDocument("$concat", new BsonArray{
                        new BsonDocument("$toString", new BsonDocument("$year", "$timeStart")),
                        "-",
                        new BsonDocument("$cond", new BsonDocument{
                            { "if", new BsonDocument("$lt", new BsonArray{ new BsonDocument("$month", "$timeStart"), 10 }) },
                            { "then", new BsonDocument("$concat", new BsonArray{ "0", new BsonDocument("$toString", new BsonDocument("$month", "$timeStart")) }) },
                            { "else", new BsonDocument("$toString", new BsonDocument("$month", "$timeStart")) }
                        })
                    });
                    break;
   
                case "Year":
                    group = new BsonDocument("$concat", new BsonArray{
                        new BsonDocument("$toString", new BsonDocument("$year", "$timeStart"))
                    });
                    break;

            }
            // aggregate query
            var result = _optime.Aggregate()
                    .Match(filter)
                    .Group(new BsonDocument {
                        { "_id", group },
                        { "optime", new BsonDocument("$sum", "$timeWindow") } })
                    .Sort("{_id: 1}")
                    .ToListAsync().Result;
            var allTimeRanges = GenerateTimeRanges(startDate, endDate, timeRange);
            // Merge the results with the full range
            var resultDictionary = result.ToDictionary(r => r["_id"].AsString, r => r["optime"].AsDouble);
            var finalResults = allTimeRanges.Select(tr =>
            {
                return new Summary.OpTime
                {
                    Id = tr.Id,
                    optime = resultDictionary.ContainsKey(tr.Id) ? resultDictionary[tr.Id] : 0
                };
            }).ToList();
            return finalResults;
        }

        // list optime by time range
        public List<Summary.OpTime> ListDowntime(DateTime startDate, DateTime endDate, string timeRange)
        {
            var _optime = ListOptime(startDate, endDate, timeRange);
            var optime = BsonSerializer.Deserialize<List<Summary.OpTime>>(_optime.ToJson());
            List<Summary.OpTime> listDowntime = new List<Summary.OpTime>();
            Summary.OpTime downtime = new Summary.OpTime();
            for (var i = 0; i < optime.Count; i++)
            {
                switch (timeRange)
                {
                    case "Day":
                        downtime = new Summary.OpTime
                        {
                            Id = optime[i].Id,
                            optime = ((optime[i].optime) < 24)? (24 - optime[i].optime) : 0
                        };
                        listDowntime.Add(downtime);
                        break;

                    case "Month":
                        downtime = new Summary.OpTime
                        {
                            Id = optime[i].Id,
                            optime = (optime[i].optime < 24*30)? (24*30 - optime[i].optime) : 0
                        };
                        listDowntime.Add(downtime);
                        break;

                    case "Year":
                        downtime = new Summary.OpTime
                        {
                            Id = optime[i].Id,
                            optime = 24*30*365 - optime[i].optime
                        };
                        listDowntime.Add(downtime);
                        break;
                }
            }
            return listDowntime;
        }

        // list production data raw
        public List<Summary.SingleProduction> ListProduction(DateTime startDate, DateTime endDate)
        {
            var builder = Builders<Cup>.Filter;
            var filter = builder.Gte(cup => cup.Timestamp, startDate.AddHours(7)) &
                         builder.Lt(cup => cup.Timestamp, endDate.AddDays(1).AddHours(7));
            // aggregate query
            var result = _cup.Find(filter)
                         .Project(new BsonDocument {
                            { "_id", 0 },
                            { "station", "$station" },
                            { "user", "$user" },
                            { "timeStart", "$timestamp" },
                            { "timeStop", "$timeStart" },
                            { "timeWeight", "$timeWeight" },
                            { "setupWeight", "$weightCmd" },
                            { "currentWeight", "$weight" },
                            { "type", "$type" },
                            { "targetError", "$deltaCmd" },
                            { "status", "$status" },
                            //{ "error", "$delta" }
                            })
                         .ToListAsync().Result;
            // fullfil all data
            var listCup = BsonSerializer.Deserialize<List<Summary.SingleProduction>>(result.ToJson());
            for (var i = 0; i < listCup.Count; i++)
            {
                // convert error string
                listCup[i].Index = i+1;
                listCup[i].Error = Math.Round((listCup[i].CurrentWeight / listCup[i].SetupWeight - 1)*100, 2);
            }
            return listCup;
        }

        // list production data
        public List<Summary.Production> ListProduction(DateTime startDate, DateTime endDate, string timeRange)
        {
            var builder = Builders<Cup>.Filter;
            var filter = builder.Gte(cup => cup.Timestamp, startDate.AddHours(7)) &
                         builder.Lt(cup => cup.Timestamp, endDate.AddDays(1).AddHours(7));
            var group = new BsonDocument();
            // assign group query based on timeRange option
            switch (timeRange)
            {
                case "Day":
                    group = new BsonDocument("$concat", new BsonArray{
                        new BsonDocument("$toString", new BsonDocument("$year", "$timestamp")),
                        "-",
                        new BsonDocument("$toString", new BsonDocument("$month", "$timestamp")),
                        "-",
                        new BsonDocument("$toString", new BsonDocument("$dayOfMonth", "$timestamp"))
                    });
                    break;

                case "Month":
                    group = new BsonDocument("$concat", new BsonArray{
                        new BsonDocument("$toString", new BsonDocument("$year", "$timestamp")),
                        "-",
                        new BsonDocument("$toString", new BsonDocument("$month", "$timestamp"))
                    });
                    break;

                case "Year":
                    group = new BsonDocument("$concat", new BsonArray{
                        new BsonDocument("$toString", new BsonDocument("$year", "$timestamp"))
                    });
                    break;

            }
            // aggregate query
            var result = _cup.Aggregate()
                    .Match(filter)
                    .Group(new BsonDocument {
                        { "_id", new BsonDocument{{"timestamp", group }, {"recipe", "$weightCmd" } } },
                        //{ "recipe", new BsonDocument("$push", "$weightCmd") },
                        { "cup", new BsonDocument("$sum", 1) },
                        { "weight", new BsonDocument("$sum", "$weight") }})
                    .Project(new BsonDocument {
                        { "_id", "$_id.timestamp" },
                        { "recipe", "$_id.recipe" },
                        { "cup", "$cup" },
                        { "weight", "$weight" }
                        })
                    .Sort("{_id: 1}")
                    .ToListAsync().Result;
            return BsonSerializer.Deserialize<List<Summary.Production>>(result.ToJson());
        }

        // get latest report
        public ReportHistory FindLastReport() =>
            _rpHist.Find(rp => true).SortByDescending(rp => rp.Timestamp).Limit(1).FirstOrDefault();

        // find mail by id
        public MailAccount FindMail(string id) =>
            _mail.Find(mail => mail.Id == id).FirstOrDefault();
        // get all mails
        public List<MailAccount> ListMail() =>
            _mail.Find(mail => true).ToList();


        // ----- CREATE COMMAND -----
        // Cup collection
        public Cup InsertCup(Cup cup)
        {
            cup.WeightCmd = Math.Round(cup.WeightCmd,1);
            cup.Weight = Math.Round(cup.Weight, 2);
            cup.Delta = Math.Round(cup.Delta, 2);
            _cup.InsertOne(cup);
            return cup;
        }
        // OpTime collection
        public OpTime InsertOpTime(OpTime optime)
        {
            _optime.InsertOne(optime);
            return optime;
        }
        // Report History collection
        public ReportHistory InsertReportHistory(ReportHistory rp)
        {
            _rpHist.InsertOne(rp);
            return rp;
        }
        // Mail collection
        public MailAccount InsertMail(MailAccount mail)
        {
            _mail.InsertOne(mail);
            return mail;
        }

        // ----- UPDATE COMMAND -----
        public void UpdateOpTime(OpTime newData) =>
            _optime.ReplaceOne(op => op.Id == newData.Id, newData);
        


        // ----- DELETE COMMAND -----
        // mail 
        public void DeleteMail(string mailId) =>
            _mail.DeleteOne(mail => mail.Id == mailId);



        // ----- AGGREGATE COMMAND -----


    }
}
