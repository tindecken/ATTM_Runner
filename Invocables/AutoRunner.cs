using Coravel.Invocable;
using log4net;
using log4net.Config;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CommonModels;
using Microsoft.AspNetCore.SignalR.Client;
using MongoDB.Bson;
using MongoDB.Driver;
using Runner.WrapperFactory;

namespace Runner.Invocables
{
    public class AutoRunner : IInvocable
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string NUnitConsole = string.Empty;
        private string MongoDBConnectionString = string.Empty;
        private string MongoDBDatabaseName = string.Empty;
        private string DevQueueCollection = string.Empty;
        private string RegressionQueueCollection = string.Empty;
        private string RegressionName = string.Empty;
        private string ClientName = string.Empty;
        private string DevTestProjectFolder = string.Empty;
        private string DevTestProjectDLL = string.Empty;
        private string RegressionTestProjectFolder = string.Empty;
        private string RegressionTestProjectDLL = string.Empty;
        private readonly IMongoCollection<DevQueue> _devQueues;
        private readonly IMongoCollection<RegressionTest> _regQueues;

        public AutoRunner()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(@"log4net.config"));
            XmlDocument xmlSettings = new XmlDocument();
            xmlSettings.Load("settings.xml");
            XmlNode xmlRootNode = xmlSettings.DocumentElement;
            XmlNodeList lstSettingNode = xmlRootNode.SelectSingleNode($"/Settings").ChildNodes;
            foreach (XmlNode settingNode in lstSettingNode)
            {
                switch (settingNode.Attributes["Name"].Value)
                {
                    case "NUnitConsole":
                        NUnitConsole = settingNode.Attributes["Value"].Value;
                        break;
                    case "MongoDBConnectionString":
                        MongoDBConnectionString = settingNode.Attributes["Value"].Value;
                        break;
                    case "MongoDBDatabaseName":
                        MongoDBDatabaseName = settingNode.Attributes["Value"].Value;
                        break;
                    case "DevQueueCollection":
                        DevQueueCollection = settingNode.Attributes["Value"].Value;
                        break;
                    case "RegressionQueueCollection":
                        RegressionQueueCollection = settingNode.Attributes["Value"].Value;
                        break;
                    case "RegressionName":
                        RegressionName = settingNode.Attributes["Value"].Value;
                        break;
                    case "ClientName":
                        ClientName = settingNode.Attributes["Value"].Value;
                        break;
                    case "DevTestProjectDLL":
                        DevTestProjectDLL = settingNode.Attributes["Value"].Value;
                        break;
                    case "DevTestProjectFolder":
                        DevTestProjectFolder = settingNode.Attributes["Value"].Value;
                        break;
                    case "RegressionTestProjectDLL":
                        RegressionTestProjectDLL = settingNode.Attributes["Value"].Value;
                        break;
                    case "RegressionTestProjectFolder":
                        RegressionTestProjectFolder = settingNode.Attributes["Value"].Value;
                        break;
                    default:
                        break;
                }
            }

            var client = new MongoClient(MongoDBConnectionString);
            var database = client.GetDatabase(MongoDBDatabaseName);
            _devQueues = database.GetCollection<DevQueue>(DevQueueCollection);
            _regQueues = database.GetCollection<RegressionTest>(RegressionQueueCollection);
        }

        public Task Invoke()
        {
            HubConnectionFactory.InitHubConnection();
            HubConnectionFactory.SignalRConnection.StartAsync();
            if (!IsServerConnected(MongoDBConnectionString, MongoDBDatabaseName)) {
                Console.WriteLine($"");
                Console.WriteLine($"");
                Console.WriteLine($"{DateTime.UtcNow} Can't connect to MongoDB Server: {MongoDBConnectionString}, Database: {MongoDBDatabaseName}, check settings.xml or your MongoDB Server.");
                return Task.Delay(20000);
            }
            
            //Get TestCase need to run from table develop
            var devQueue = GetRunnableDevQueue();
            var regQueue = GetRunnableRegressionQueue();
            if (devQueue == null)
            {
                if (regQueue == null)
                {
                    Console.Write(".");
                    return Task.CompletedTask;
                }
                else
                {
                    Console.WriteLine("-----------------------------------------------------");
                    Console.WriteLine("-----------------------------------------------------");
                    Console.WriteLine("");
                    Console.WriteLine($"{DateTime.UtcNow}: Run RegressionTest: {regQueue.TestCaseFullCodeName}");
                    //Update isInQueue = false
                    UpdateRegressionQueueIsTaken(regQueue);

                    string NUnitCommandPath;
                    //Build NUnit Console Command
                    if (Environment.Is64BitOperatingSystem)
                    {
                        NUnitCommandPath = Path.Combine($"{NUnitConsole}", "nunit3-console.exe");
                    }
                    else
                    {
                        NUnitCommandPath = Path.Combine($"{NUnitConsole}", "nunit3-console.exe");
                    }

                    StringBuilder NUnitParams = new StringBuilder();
                    NUnitParams.Append($" --test={regQueue.TestCaseFullCodeName} {RegressionTestProjectDLL}");
                    Console.WriteLine($"Run command: {NUnitCommandPath} {NUnitParams}");
                    Process process = new Process();
                    process.StartInfo.FileName = $"cmd.exe"; // Specify exe name.
                    process.StartInfo.Arguments = $"/c {NUnitCommandPath} {NUnitParams}";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.WorkingDirectory = RegressionTestProjectFolder;
                    process.Start();
                    process.WaitForExit();
                }
            }
            else { //there's devqueue need to execute
                Console.WriteLine("-----------------------------------------------------");
                Console.WriteLine("-----------------------------------------------------");
                Console.WriteLine("");
                Console.WriteLine($"{DateTime.UtcNow}: Run DevTest: {devQueue.TestCaseFullName}");
                //Update isInQueue = false
                UpdateDevQueueIsTaken(devQueue);
                HubConnectionFactory.SignalRConnection.InvokeAsync("TakeDevQueue", devQueue);
                string NUnitCommandPath;
                //Build NUnit Console Command
                if (Environment.Is64BitOperatingSystem)
                {
                    NUnitCommandPath = Path.Combine($"{NUnitConsole}", "nunit3-console.exe");
                }
                else
                {
                    NUnitCommandPath = Path.Combine($"{NUnitConsole}", "nunit3-console.exe");
                }

                StringBuilder NUnitParams = new StringBuilder();
                NUnitParams.Append($" --test={devQueue.TestCaseFullName} {DevTestProjectDLL}");
                Console.WriteLine($"Run command: {NUnitCommandPath} {NUnitParams}");
                Process process = new Process();
                process.StartInfo.FileName = $"cmd.exe"; // Specify exe name.
                process.StartInfo.Arguments = $"/c {NUnitCommandPath} {NUnitParams}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.WorkingDirectory = DevTestProjectFolder;
                process.Start();
                process.WaitForExit();
            }

            //Runningg
            //Get TestCase need to run from table debug
            //Query database to get one test case for running
            return Task.CompletedTask;
        }

        private DevQueue GetRunnableDevQueue()
        {
            var queuesFilter = Builders<DevQueue>.Filter.Eq(x => x.QueueStatus, TestStatus.InQueue);
            var queues = _devQueues.Find(queuesFilter).ToList();
            if (queues.Count > 2)
            {
                var queueClientHighPriority = queues.Find(q => q.IsHighPriority == true && q.ClientName == ClientName);
                if (queueClientHighPriority != null)
                {
                    return queueClientHighPriority;
                }
                var queueClient = queues.Find(q => q.ClientName == ClientName);
                if (queueClient != null)
                {
                    return queueClient;
                }
                return queues.FirstOrDefault();
            }
            return queues.FirstOrDefault();
        }
        private  RegressionTest GetRunnableRegressionQueue()
        {
            var queuesFilter = Builders<RegressionTest>.Filter.Eq(x => x.Status, TestStatus.InQueue);
            queuesFilter &= Builders<RegressionTest>.Filter.Eq(x => x.RegressionName, RegressionName);
            queuesFilter &=
                Builders<RegressionTest>.Filter.Where(x =>
                    x.ClientName.Equals(ClientName) || x.ClientName == string.Empty);
            var queues = _regQueues.Find(queuesFilter).ToList();
            if (queues.Count > 2)
            {
                var queueClientHighPriority = queues.Find(q => q.IsHighPriority == true && q.ClientName == ClientName);
                if (queueClientHighPriority != null)
                {
                    return queueClientHighPriority;
                }
                else
                {
                    var queueHighPriority = queues.Find(qh => qh.IsHighPriority == true);
                    if (queueHighPriority != null)
                    {
                        return queueHighPriority;
                    }
                    else
                    {
                        var queueClient = queues.Find(q => q.ClientName == ClientName);
                        if (queueClient != null)
                        {
                            return queueClient;
                        }
                        else return queues.FirstOrDefault();
                    }
                }
            }
            else return queues.FirstOrDefault();
        }
        private void UpdateDevQueueIsTaken(DevQueue devQueue)
        {
            var update = Builders<DevQueue>.Update.Set(d => d.QueueStatus, TestStatus.Queued)
                .Set(x => x.RunAt, DateTime.UtcNow)
                .Set(x => x.ClientName, ClientName);
            _devQueues.UpdateOne(devQ => devQ.Id == devQueue.Id, update);
        }

        private void UpdateRegressionQueueIsTaken(RegressionTest regTest)
        {
            var update = Builders<RegressionTest>.Update.Set(d => d.Status, TestStatus.Running)
                .Set(x => x.ClientName, ClientName);
            _regQueues.UpdateOne(regQ => regQ.Id == regTest.Id, update);
        }

        /// <summary>
        /// Test that the server is connected
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        /// <returns>true if the connection is opened</returns>
        private static bool IsServerConnected(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            return database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
        }
    }
}
