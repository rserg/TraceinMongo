using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TraceinMongo
{
    abstract class AMongoListener: TraceListener
    {
        private void BuildWrite(string category, string message) { }
        private void WriteXML(TraceEventCache tracecache, string message, string category) { }
        private void WriteHTML(TraceEventCache trace, string category, string message) { }
    }
}
