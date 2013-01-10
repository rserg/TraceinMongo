using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;

namespace TraceinMongo
{
    class MongoDataWriteXML : IWriteProvider
    {
        private string xmlname;
        public MongoDataWriteXML(string xmlname)
        {
            this.xmlname = xmlname;
        }

        public MongoDataWriteXML() { }

        public override void Write(string data, string message)
        {
            base.Write(data, message);
        }

        //Загрузка всего, что есть
        private dynamic LoadCurrentData(string path)
        {
            Console.WriteLine(path);
            if (File.Exists(path))
                return XDocument.Load(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read));
            else
                return XDocument.Load(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read));
        }
        //Записать информаию о процессах
        public override void Write(TraceEventCache tracecache, string data, string message)
        {
            //Расширенный трейс
            try
            {
                //http://forums.asp.net/t/1470779.aspx/1
                XDocument xdoc = LoadCurrentData(xmlname + ".xml");
                xdoc.Add(
                 new XComment("Base info about trace"),
                      new XElement("trace1",
                      new XElement("ID", "MYID",
                      new XElement("DBName", message,//filename,
                      new XElement("Message", message),
                      new XElement("Category", data

                      )))));
                Console.WriteLine("A");
                xdoc.Save(xmlname + ".xml", SaveOptions.None);

            }
            catch (XmlException e)
            {
                Console.WriteLine(e.HelpLink);
            }
        }

        public void Write(string message)
        {
            this.Write(message, "SSSS");
        }

    }
}
