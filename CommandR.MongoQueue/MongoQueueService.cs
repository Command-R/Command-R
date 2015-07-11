using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using CfgDotNet;
using CommandR.Authentication;
using CommandR.Services;
using MediatR;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace CommandR.MongoQueue
{
    public class MongoQueueService : IQueueService
    {
        private readonly Settings _settings;
        private readonly Commander _commander;
        private readonly MongoDatabase _database;

        public MongoQueueService(Settings settings, Commander commander)
        {
            _settings = settings;
            _commander = commander;
            _database = GetMongoDatabase(settings);
        }

        private static MongoDatabase GetMongoDatabase(Settings settings)
        {
            var mongoUrl = MongoUrl.Create(settings.ConnectionString);
            var client = new MongoClient(settings.ConnectionString);
            var server = client.GetServer();
            var database = server.GetDatabase(mongoUrl.DatabaseName);
            return database;
        }

        private void CreateCollection()
        {
            if (_database.CollectionExists(_settings.CollectionName))
                return;

            var options = CollectionOptions
                .SetCapped(true)
                .SetMaxSize(_settings.MaxSize)
                .SetMaxDocuments(_settings.MaxDocuments)
                .SetAutoIndexId(true);

            _database.CreateCollection(_settings.CollectionName, options);

            //TailableCursor requires at least one item
            var collection = _database.GetCollection(_settings.CollectionName);
            collection.Insert(new QueueJob(new Noop(), new AppContext()));
        }

        public virtual void Enqueue<T>(IRequest<T> command, AppContext appContext)
        {
            CreateCollection();
            var queueJob = new QueueJob(command, appContext);
            var collection = _database.GetCollection<QueueJob>(_settings.CollectionName);
            collection.Insert(queueJob);
        }

        public virtual void Enqueue<T>(IAsyncRequest<T> command, AppContext appContext)
        {
            CreateCollection();
            var queueJob = new QueueJob(command, appContext);
            var collection = _database.GetCollection<QueueJob>(_settings.CollectionName);
            collection.Insert(queueJob);
        }

        public virtual void StartProcessing(CancellationToken cancellationToken, Action<object, AppContext> execute)
        {
            RegisterClassMaps();
            if (_settings.ResetCollection) DropCollection();
            CreateCollection();

            //Create query based on latest document id
            BsonValue lastId = BsonMinKey.Value;
            var collection = _database.GetCollection<QueueJob>(_settings.CollectionName);
            var query = collection
                .FindAs<QueueJob>(Query.GT("_id", lastId))
                .SetFlags(QueryFlags.AwaitData | QueryFlags.TailableCursor);

            var cursor = new MongoCursorEnumerator<QueueJob>(query);
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!cursor.MoveNext())
                    continue;

                var queueJob = cursor.Current;
                if (queueJob.IsComplete || !string.IsNullOrWhiteSpace(queueJob.Error))
                    continue;

                try
                {
                    execute(queueJob.Command, queueJob.Context);
                    queueJob.IsComplete = true;
                    collection.Save(queueJob);
                }
                catch (ThreadAbortException)
                {
                    //ignore
                }
                catch (Exception ex)
                {
                    queueJob.SetError(ex.Message);
                    collection.Save(queueJob);
                }
            }
        }

        private void RegisterClassMaps()
        {
            var registeredTypes = BsonClassMap.GetRegisteredClassMaps().Select(x => x.ClassType).ToList();

            var commands = _commander.GetRegisteredCommands().Values;
            var registerClassMapMethod = typeof(BsonClassMap).GetMethod("RegisterClassMap",
                BindingFlags.Public | BindingFlags.Static, null,
                Type.EmptyTypes, new ParameterModifier[] {});

            foreach (var cmdType in commands)
            {
                if (registeredTypes.Contains(cmdType))
                    continue;

                registerClassMapMethod.MakeGenericMethod(cmdType)
                                      .Invoke(this, null);
            }
        }

        private void DropCollection()
        {
            if (_database.CollectionExists(_settings.CollectionName))
                _database.DropCollection(_settings.CollectionName);
        }

        public class Settings : BaseSettings
        {
            public string CollectionName { get; set; }
            public string ConnectionString { get; set; }
            public int MaxSize { get; set; }
            public int MaxDocuments { get; set; }
            public bool ResetCollection { get; set; }

            public override void Validate()
            {
                if (string.IsNullOrWhiteSpace(CollectionName))
                    CollectionName = "Queue";

                if (string.IsNullOrWhiteSpace(ConnectionString))
                    ConnectionString = "mongodb://127.0.0.1/test";

                if (MaxSize == 0)
                    MaxSize = 2000000;

                if (MaxDocuments == 0)
                    MaxDocuments = 2000;

                CollectionName = CollectionName.Replace("_MACHINE", "_" + Environment.MachineName);
                TestMongoConnection(ConnectionString);
            }

            private static void TestMongoConnection(string connectionString)
            {
                var settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
                settings.ConnectTimeout = TimeSpan.FromSeconds(5);
                var client = new MongoClient(settings);
                var server = client.GetServer();
                server.Ping();
            }
        };
    };
}
