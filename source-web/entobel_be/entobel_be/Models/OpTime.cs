using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace entobel_be.Models
{
    public class OpTime
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("timeStart")]
        public DateTime TimeStart { get; set; }

        [BsonElement("timeEnd")]
        public DateTime TimeStop { get; set; }

        [BsonElement("timeWindow")]
        public double TimeWindow { get; set; }
    }
}
