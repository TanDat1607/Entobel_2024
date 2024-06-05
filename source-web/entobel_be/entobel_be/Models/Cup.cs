using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace entobel_be.Models
{
    public class Cup
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("station")]
        public int Station { get; set; }

        [BsonElement("user")]
        public string User { get; set; }

        [BsonElement("type")]
        public string Type { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("timeStart")]
        public DateTime TimeStart { get; set; }

        [BsonElement("timeWeight")]
        public double TimeWeight { get; set; }

        [BsonElement("weight")]
        public double Weight { get; set; }

        [BsonElement("weightCmd")]
        public double WeightCmd { get; set; }

        [BsonElement("delta")]
        public double Delta { get; set; }

        [BsonElement("deltaCmd")]
        public double DeltaCmd { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }
    }
}
