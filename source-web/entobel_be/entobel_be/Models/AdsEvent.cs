using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace entobel_be.Models
{
    public class AdsEvent
    {
        [BsonElement("eventId")]
        public uint EventId { get; set; }
        [BsonElement("eventClass")]
        public string EventClass { get; set; }
        [BsonElement("severity")]
        public string Severity { get; set; }
        [BsonElement("text")]
        public string Text { get; set; }
        [BsonElement("source")]
        public string Source { get; set; }
        [BsonElement("timeRaised")]
        public string TimeRaised { get; set; }
        //[BsonElement("timeConfirmed")]
        //public string TimeConfirmed { get; set; }
        //[BsonElement("timeClear")]
        //public string TimeClear { get; set; }
    }
}
