using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace entobel_be.Models
{
    public class Summary
    {
        public class Cup
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }

            [BsonElement("count")]
            public int count { get; set; }
        }

        public class Weight
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }

            [BsonElement("weight")]
            public double weight { get; set; }
        }

        public class OpTime
        {
            [BsonId]
            [BsonRepresentation(BsonType.String)]
            public string Id { get; set; }

            [BsonElement("optime")]
            public double optime { get; set; }
        }

        public class Downtime
        {
            [BsonId]
            [BsonRepresentation(BsonType.String)]
            public string Id { get; set; }

            [BsonElement("downtime")]
            public double downtime { get; set; }
        }

        public class Production
        {
            [BsonId]
            [BsonRepresentation(BsonType.String)]
            public string Id { get; set; }

            [BsonElement("recipe")]
            public double Recipe { get; set; }

            [BsonElement("cup")]
            public double Cup { get; set; }

            [BsonElement("weight")]
            public double Weight { get; set; }
        }

        public class SingleProduction
        {
            [BsonElement("station")]
            public int Station { get; set; }

            [BsonElement("user")]
            public string User { get; set; }

            [BsonElement("type")]
            public string Type { get; set; }

            [BsonElement("timeStart")]
            public DateTime TimeStart { get; set; }

            [BsonElement("timeStop")]
            public DateTime TimeStop { get; set; }

            [BsonElement("timeWeight")]
            public double TimeWeight { get; set; }

            [BsonElement("index")]
            public double Index { get; set; }

            [BsonElement("setupWeight")]
            public double SetupWeight { get; set; }

            [BsonElement("currentWeight")]
            public double CurrentWeight { get; set; }

            [BsonElement("targetError")]
            public double TargetError { get; set; }

            [BsonElement("error")]
            public double Error { get; set; }

            [BsonElement("status")]
            public string Status { get; set; }
        }
    }
}
