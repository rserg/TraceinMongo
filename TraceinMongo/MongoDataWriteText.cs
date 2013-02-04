using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace TraceinMongo
{
    class MongoDataWriteText : IWriteProvider
    {

        private string filename;
        public MongoDataWriteText(string filename) { this.filename = filename; }
        public override void Write(TraceEventCache cache, string data, string message)
        {
            this.CompleteWrite(cache, data, message);
        }

        public override void Write(string data, string message)
        {
            this.CompleteWrite(null,data, message);
        }

        private void CompleteWrite(TraceEventCache cache, string data, string message)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Event log. Current log of Event " + DateTime.Now);
            sb.Append("Data: " + data);
            sb.Append("Message: " + message);
            if (cache != null)
            {
                sb.Append("ProcessID " + cache.ProcessId);
                sb.Append("ThreadID " + cache.ThreadId);
                sb.Append("Callstack" + cache.Callstack);
            }

            this.Save(filename, sb);
        }

        private void Save(string path, StringBuilder sb)
        {
            using (var stream = new StreamWriter(path, true, Encoding.Default))
            {
                stream.WriteLine(sb.ToString());
            }

        }
    }
}
