﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Xml;
using System.Security.Permissions;
using System.Runtime.Serialization;
using System.Reflection;

using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.VisualStudio.TestTools.UITesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TraceinMongo
{

    abstract class IWriteProvider : IWriteInfo
    {
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


        public class MongoListener : AMongoListener
        {
            private ConsoleTraceListener consoleTracer = new ConsoleTraceListener();
            private TraceSource tracesrs;
            private BsonDocument bson = new BsonDocument();
            private string filename;
            private string dbname;
            Listeners outputfiles;
            //Current Info about DB
            private static MongoData mongodata;

            private object thislock = new object();

            MongoCollection mongoLogCollection = null;

            List<string> info = new List<string>();

            private static List<Log> log = new List<Log>();

            //Enable logging
            private bool logging = true;

            //private XDocument xdoc;
            //name - имя для бд
            public MongoListener(string name, Listeners outputfiles, MongoCollection coll)
            {
                mongoLogCollection = coll;
                this.outputfiles = outputfiles;
                this.dbname = name;
            }

            public MongoListener(string name)
            {
                filename = name;
                this.dbname = name;
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

            public static MongoListener MongoListenerLoad(string name)
            {
                return new MongoListener(name);
            }


            private static void ExceptionCase(string message, bool exit)
            {
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
                this.BuildWrite("None", o);
            }

            public override void Write(string message, string category)
            {
                this.BuildWrite(category, message);
            }

            public override void Write(object o, string category)
            {
                
            }

            private void BuildWrite(string category, string message)
            {
                if (logging && message != null)
                {
                    if (outputfiles.xml != null)
                        new MongoDataWriteXML(outputfiles.xml).Write(category, message);
                    if (outputfiles.html != null)
                        this.WriteHTML(null, category, message);
                    if (outputfiles.text != null)
                        new MongoDataWriteText(outputfiles.text).Write(category, message);

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
               #if DEBUG 
                Console.WriteLine("XML");
                #endif

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
                this.dbname= newname;
            }

            public string getName()
            {
                return this.Name;
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

       [TestMethod()]
        public void RunListenerTest()
        {
            MongoListener xml = MongoListener.MongoListenerLoad("Another", new Listeners { text = "output2.txt" });
            Trace.Listeners.Add(xml);
            Trace.WriteLine("First");
            Trace.WriteLine("Next");
            Trace.WriteLine("Last");
            Trace.Flush();
            xml.Stop();
            Trace.WriteLine("Not stored");

            MongoInfo info = MongoInfo.Load("pong");
            Console.WriteLine(info.ReadFromMongo());
        }

       [TestMethod()]
       public void CrushTest()
       {
           MongoListener listener = MongoListener.MongoListenerLoad("AnotherBase");
           Trace.WriteLine("CurrentEvent");
           Trace.WriteLine("NextEvent");
           Trace.WriteLine("LastEvent");
           Trace.WriteLineIf(true, "AA");
       }

       [TestMethod()]
       void TestListenerFail()
       {
           MongoListener xml = MongoListener.MongoListenerLoad("Another", new Listeners { text = "output2.txt" });
       }

       [TestMethod()]
       public void MongoGetTest()
       {
           MongoListener xml = MongoListener.MongoListenerLoad("AnotherBase");
           MongoData data = new MongoData("AnotherBase");
       }
        public static void Main(string[] args)
        {

         
        }
    }

}
