using System.Linq;
using CommandR.Authentication;
using MongoDB.Bson;

namespace CommandR.MongoQueue
{
    public class QueueJob
    {
        public QueueJob()
        {
            //empty constructor necessary to be serialized back out of mongo
        }

        public QueueJob(object command, AppContext context)
        {
            Name = command.GetType().Name;
            Command = command;
            Context = context;
            Error = EnsureStringLength("", 100);
        }

        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public AppContext Context { get; set; }
        public object Command { get; set; }
        public bool IsComplete { get; set; }
        public string Error { get; set; }

        public void SetError(string error)
        {
            Error = EnsureStringLength(error, Error.Length);
        }

        //Mongo capped collection objects can't change size
        private static string EnsureStringLength(string txt, int length)
        {
            if (txt == null)
                txt = string.Empty;

            return txt.Length >= length
                ? txt.Substring(0, length)
                : txt + string.Join("", Enumerable.Repeat(" ", length - txt.Length));
        }
    };
}