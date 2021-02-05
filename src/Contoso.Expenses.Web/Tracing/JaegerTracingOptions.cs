using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contoso.Expenses.Web.Tracing
{
    public class JaegerTracingOptions
    {
        public double SamplingRate { get; set; }
        public double LowerBound { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }
        public string JaegerAgentHost { get; set; }
        public int JaegerAgentPort { get; set; }
        public string ServiceName { get; set; }

        public JaegerTracingOptions()
        {
            SamplingRate = 0.1d;
            LowerBound = 1d;
            LoggerFactory = new LoggerFactory();
            JaegerAgentHost = "localhost";
            JaegerAgentPort = 6831;
        }
    }
}
