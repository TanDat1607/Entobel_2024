using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace entobel_be.Models
{
    public class ProductionData
    {
        [BsonElement("status")]
        public string Status { get; set; }
        [BsonElement("weight")]
        public double Weight { get; set; }
        [BsonElement("capacity")]
        public uint Capacity { get; set; }
        [BsonElement("user")]
        public string User { get; set; }
        //[BsonElement("power")]
        //public double Power { get; set; }
    }
}
