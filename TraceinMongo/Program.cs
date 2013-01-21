using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Web;
using Microsoft.Win32;
using System.Xml;
using System.Security.Permissions;
using System.Runtime.Serialization;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Reflection;

using System.Threading.Tasks;

namespace TraceinMongo
{
    interface WriteStream
    {
        void Write(string data, string message);
    }

    abstract class IWriteProvider : WriteStream
    {
        //private string filename;
        //public WriteProvider(string filename) { this.filename = filename; }
        public virtual void Write(string data, string message) { }
        public virtual void Write(TraceEventCache cache, string data, string message) { }
    }

    //Singlenong


    struct Listeners
    {
        public string xml { get; set; }
        public string text { get; set; }
        public string html { get; set; }
    }

    struct WriteInfo
    {
        public string message { get; set; }
        public string category { get; set; }
        public string catingo { get; set; }
    }

    //Current Log

    struct Log
    {
        public string message { get; set; }
        public DateTime time;
    }


    class Program
    {

        interface ITrace
        {
            string GetInfo();
            bool IsTrace();
        }

        enum MOD { XML, JSON };


        public class MongoListener : TraceListener
        {
            private ConsoleTraceListener consoleTracer = new ConsoleTraceListener();
            private TraceSource tracesrs;
            private BsonDocument bson = new BsonDocument();
            private string filename;
            Listeners outputfiles;
            //Current Info about DB
            private static MongoData mongodata;

            private object thislock = new object();

            MongoCollection mongoLogCollection = null;

            List<string> info = new List<string>();

            private static List<Log> log = new List<Log>();

            //Включить логгирование
            private bool logging = true;

            //private XDocument xdoc;
            //name - имя для бд
            public MongoListener(string name, Listeners outputfiles, MongoCollection coll)
            {
                mongoLogCollection = coll;
            }

            //Дефолтный згрузчик
            public MongoListener(string name)
            {
                filename = name;
            }

            public static MongoListener MongoListenerLoad(string name, Listeners outputfiles)
            {
                mongodata = new MongoData(name);
                MongoCollection collection = null;
                try
                {
                    MongoServer server = MongoServer.Create();
                    server.Connect();
                    var db = server.GetDatabase(name);
                    collection = db.GetCollection<BsonDocument>(name);
                }
                catch (MongoAuthenticationException)
                {
                    ExceptionCase("MongoAuthenticationException", false);
                }
                catch (MongoConnectionException)
                {
                    ExceptionCase("Trouble with Connection to MongoDB", false);
                }
                catch (Exception)
                {
                    ExceptionCase("Unknown Exception", false);
                }

                return new MongoListener(name, outputfiles,collection);
            }


            private static void ExceptionCase(string message, bool exit)
            {
                Console.WriteLine(message);
                mongodata = null;
                if (exit) Environment.Exit(0);
            }


            #region Write methods
            

            public void Dangerus(string name)
            {
                info.Add(name);
                tracesrs.TraceEvent(TraceEventType.Suspend, 2, System.DateTime.Now + " " + name);
            }

            public void Info(string name)
            {
                lock (thislock)
                {
                    info.Add(name);
                    bson.Add("_id", BsonValue.Create(BsonType.ObjectId));
                }
                tracesrs.TraceEvent(TraceEventType.Information, 1, "INFO");
                //tracesrs.TraceInformation("This is");
                //tracesrs.TraceData(TraceEventType.Information, 2, System.DateTime.Now + " " + name);
            }

            public override void Write(string o)
            {
                Console.WriteLine("ONE " + o);
            }

            public override void WriteLine(string o)
            {

                this.BuildWrite(null, o);
            }

            public override void Write(string message, string category)
            {
                this.BuildWrite(category, message);
            }


            //Главный класс, который распределяет все Дальнейшие функции
            private void BuildWrite(string category, string message)
            {
                if (category == null) category = "None";
                if (logging && message != null)
                {
                    if (outputfiles.xml != null)
                        this.WriteXML(null, category, message);
                    if (outputfiles.html != null)
                        this.WriteHTML(null, category, message);
                    if (outputfiles.text != null)
                        this.WriteText(null, category, message);

                }
            }

            public delegate TResult Func<T1, T2, TResult>(string name);
            //Главная функция распределения
            public void GetAllItems(string message, string category,
                TraceEventCache tracecache,
                IWriteProvider wr)
            {
                wr.Write(tracecache, category, message);
            }

            //Тестовая реализация интерфейса
            //Простой вывод ифнцормации

            private void WriteXML(TraceEventCache tracecache, string message, string category)
            {
                GetAllItems(message, category, tracecache,
                  new MongoDataWriteXML(filename));
                mongodata.WriteDatainMongo(mongoLogCollection, new WriteInfo
                {
                    category = category,
                    message = message
                });
            }

            #region HTML area
            private void WriteHTML(TraceEventCache trace, string category, string message)
            {
                StringBuilder output = CreateHTMLOutput(trace, category, message);

                File.Create(outputfiles.html).Write(
                    Encoding.ASCII.GetBytes(output.ToString()), 0, output.Length);


                mongodata.WriteDatainMongo(mongoLogCollection, new WriteInfo
                {
                    category = category,
                    message = message
                });

            }

            private StringBuilder CreateHTMLOutput(TraceEventCache trace, string category, string message)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("div class=maininfo");
                sb.Append(string.Format("<div id = {0}>", category));
                sb.Append("</div");
                sb.Append("div id=message>");
                sb.Append(message);
                sb.Append("</div>");
                if (trace != null)
                {
                    sb.Append(trace.DateTime.Date);
                    sb.Append(trace.ProcessId);
                }

                return sb;
            }

            public void TroubleHTML(string message)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<div class = error>");
                sb.Append(message);
                sb.Append("</div>");
            }
            #endregion

            #region Text area
            public void WriteText(TraceEventCache trace, string category, string message)
            {
                Console.WriteLine("THIS");
                StringBuilder sb = new StringBuilder();
                sb.Append("Category: " + category);
                sb.Append("Message: " + message);
                File.WriteAllText(outputfiles.text, sb.ToString(), Encoding.Default);

            }

            #endregion
            //Show this message in the error case
            public void TroubleXML(string message)
            {
                try
                {
                    XDocument troubleTree =
                        new XDocument(
                            new XComment("Failure exception"),
                                new XElement("info", message));

                    troubleTree.Save(IOStats.GenerateFileName(".", "trouble"));

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            #endregion


            //Change the logger name
            void changeLogName(string newname)
            {
                this.Name = newname;
            }


            //Save Current log

            void SaveCurrentLog()
            {
                log.ForEach(x => Console.WriteLine(
                    String.Format("{time: {0}, message {1}}", x.time.ToString(), x.message)
                ));
            }


            #region Settings for Listener
            //Stop logging
            public void Stop()
            {
                logging = false;
            }

            public void Start()
            {
                logging = true;
            }

            //Сменить имя 
            public void ChangeListenerName(string newname)
            {
                this.Name = newname;
            }

            #endregion
        }

        delegate string MongoDelegate();

        public static void Main(string[] args)
        {

            //Переделать, чтобы инфа не затералась
            MongoListener xml = MongoListener.MongoListenerLoad("pong",
                new Listeners { text = "pong.txt", xml="pong.xml" });

            Trace.Listeners.Add(xml);
            Trace.WriteLine("test write");
            Trace.WriteLine("Polk");
            Trace.Flush();
            xml.Stop();
            Trace.WriteLine("Not stored");

            MongoInfo info = MongoInfo.Load("pong");
            Console.WriteLine(info.ServersCount());
            //info.ReadFromMongo();
           // info.Show();
        }
    }

    //Access for mongo
    class MongoInfo
    {
        private string dbname;
        private MongoData data;
        private int servercount;
        public MongoInfo(string dbname, MongoData data)
        {
            this.dbname = dbname;
            this.data = data;
            this.servercount = data.GetServerCount();
        }

        public static MongoInfo Load(string dbname)
        {
            return new MongoInfo(dbname, new MongoData(dbname));
        }
        public MongoCollection<BsonDocument> ReadFromMongo()
        {
            Func<MongoData, Func<string, 
                MongoCollection<BsonDocument>>> curss =
                delegate(MongoData data)
                {
                    return delegate(string dbname)
                    {
                        return data.GetDataFromMongo(dbname);
                    };
                };


            return curss(new MongoData(this.dbname))(this.dbname);
        }


        public void Show()
        {
            MongoData data = new MongoData(dbname);
            foreach (var tt in data.GetDataFromMongo(dbname).FindAll())
            {
                Console.WriteLine(tt["Message"].AsString);
            }
        }

        public int ServersCount()
        {
            return this.servercount;
        }

        public IEnumerable<BsonDocument> Find(string key)
        {
            var e = from store in data.GetDataFromMongo(dbname).FindAll()
                    where store["Message"].AsString == key 
                    select store;

            return e;
                    
        }

        public void ClearBase(string dbname)
        {
            MongoData data = new MongoData(dbname);
            data.GetDataFromMongo(dbname).RemoveAll();
        }

        public long Size()
        {
            MongoData data = new MongoData(dbname);
            return data.GetDataFromMongo(dbname).Count();
        }

        public void ChangeDBName(string newdbname)
        {
            this.dbname = dbname;
        }
    }
}
