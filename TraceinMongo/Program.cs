using System;
using System.Collections.Generic;
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

    //Лог программы

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
            //private Object filelock = new Object();
            private TraceSource tracesrs;
            private BsonDocument bson = new BsonDocument();
            private string filename;
            Listeners outputfiles;
            //Текущая информация о бд
            MongoData mongodata;

            //Возможен лок
            private object thislock = new object();

            MongoCollection mongoLogCollection;

            List<string> info = new List<string>();

            List<Log> log = new List<Log>();

            //Включить логгирование
            private bool logging = true;

            //private XDocument xdoc;
            //name - имя для бд
            public MongoListener(string name, Listeners outputfiles)
            {
                MongoListenerLoad(name, outputfiles);
            }

            //Дефолтный згрузчик
            public MongoListener(string name)
            {
                MongoListenerLoad(name, new Listeners { xml = name });
            }

            private void MongoListenerLoad(string name, Listeners outputfiles)
            {
                filename = name;
                this.outputfiles = outputfiles;
                mongodata = new MongoData(name);
                try
                {
                    MongoServer server = MongoServer.Create();
                    server.Connect();
                    var db = server.GetDatabase(name);
                    mongoLogCollection = db.GetCollection<BsonDocument>(name);
                }
                catch (MongoAuthenticationException)
                {
                    string message = "Incorrect autorizaion in MongoDB. Will continue without logging";
                    log.Add(new Log
                    {
                        message = message,
                        time = DateTime.Now
                    });
                }
                catch (MongoConnectionException)
                {
                    Console.WriteLine("Error connection in mongodb. But program still continue");
                }
                catch (Exception)
                {
                    Console.WriteLine("Unknown Exception");
                }
                finally
                {

                }
            }



            #region Write methods
            public void Error(string name)
            {
                WriteLine(name);

            }

            public void Start(string name)
            {
                WriteLine(name);

            }

            public void Critical(string name)
            {
                WriteLine(name);
            }


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
                    //WriteFactory<WriteXML>(filename);
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
            public string GetAllItems(string message, string category,
                TraceEventCache tracecache,
                IWriteProvider wr)
            {
                //var rrr = construct(name);
                wr.Write(tracecache, category, message);
                return "SSS";
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
                StringBuilder sb = new StringBuilder();
                sb.Append("Category: " + category);
                sb.Append("Message: " + message);
                File.WriteAllText(outputfiles.text, sb.ToString(), Encoding.Default);

            }

            #endregion
            //Вывод сообщения в случае ошибки
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


            //Изменить имя логгера
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

        public void InfoAboutSystem()
        {
            Trace.WriteLine("Start tracing");
            Trace.WriteLine(Environment.CurrentDirectory);
            Trace.WriteLine(Environment.OSVersion);
            Trace.WriteLine(Environment.SystemPageSize);
            Trace.Unindent();

            //Func<int, int,int> los = (x, y) => x + y;
            //Func<int,int,int>add = (int x, int y) => x + y;

        }

        delegate string MongoDelegate();

        //Получить информацию из базы
        public void testmongo()
        {
            MongoInfo info = new MongoInfo();
            info.ReadFromMongo("pong");
            info.Show("pong");
        }

        public static void Main(string[] args)
        {

            //Переделать, чтобы инфа не затералась
            MongoListener xml = new MongoListener("pong",
                new Listeners { text = "pong.txt" });

            Trace.Listeners.Add(xml);
            Trace.WriteLine("test write");
            Trace.WriteLine("Polk");
            //Trace.WriteLine("Porra", "cat");
            //Trace.WriteLine("Error", "message");
            //Trace.WriteLine("Piza", "porra");
            //Trace.WriteLineIf(true, "zoom");
            //Trace.WriteIf(true, "Value", "Write");
            Trace.Flush();
            xml.Stop();

            MongoInfo info = new MongoInfo();
            info.ClearBase("pong");
            info.ReadFromMongo("pong");
            info.Show("pong");
        }
    }

    //Класс для доступа к базе
    class MongoInfo
    {
        public MongoInfo() { }
        public void ReadFromMongo(string _dbname)
        {
            Func<MongoData, Func<string, int>> curss =
                delegate(MongoData data)
                {
                    return delegate(string dbname)
                    {
                        Console.WriteLine(data.GetDataFromMongo(dbname).Database);
                        return 5;
                    };
                };


            curss(new MongoData(_dbname))(_dbname);
        }


        public void Show(string dbname)
        {
            MongoData data = new MongoData(dbname);
            foreach (var tt in data.GetDataFromMongo(dbname).FindAll())
            {
                Console.WriteLine(tt["Message"].AsString);
            }
        }

        public void ClearBase(string dbname)
        {
            MongoData data = new MongoData(dbname);
            data.GetDataFromMongo(dbname).RemoveAll();
        }
    }
}
