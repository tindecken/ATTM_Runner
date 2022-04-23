using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Runner.Models
{
    [BsonIgnoreExtraElements]
    public class RegressionTest
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string TestCaseId { get; set; }
        public string RegressionName { get; set; }
        public string Build { get; set; }
        public string TestCaseCodeName { get; set; }
        public string TestCaseName { get; set; }
        public string TestCaseFullCodeName { get; set; }
        public string TestCaseType { get; set; }
        public string Description { get; set; }
        public string Team { get; set; }
        public string CategoryName { get; set; }
        public string TestSuiteFullName { get; set; }
        public string TestGroupFullName { get; set; }
        public bool IsHighPriority { get; set; }
        public string Status { get; set; }
        public string ClientName { get; set; }
        public string WorkItem { get; set; }
        public string Queue { get; set; }
        public string Owner { get; set; }
        public List<string> RegressionRunRecordIds { get; set; }
        public RegressionRunRecord LastRegressionRunRecord { get; set; }
        public string AnalyseBy { get; set; }
        public string Issue { get; set; }
        public string Comments { get; set; }
    }
}