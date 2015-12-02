using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Threading;


namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
           
            WebServer ws = new WebServer(SendResponse, "http://localhost:8080/api/events/", "http://localhost:8080/api/event/1/sessions/", "http://localhost:8080/api/event/2/sessions/", "http://localhost:8080/api/event/3/sessions/");
            ws.Run();
            Console.WriteLine("UEP Test webserver. Press a key to quit.");
            Console.ReadKey();
            ws.Stop();
        }

        /*
        The digisign API URLs are:
            /api/events - all events
            /api/event/{id}/sessions - sessions for an event
            /api/event/{id}/people - people registered for an event
            /api/session/{id} - session details
            /api/session/{id}/people - people registered for a session
            /api/session/{session id}/checkin/{person id} - checks a person into a session
            /api/session/{id}/status?active={boolean}&open={boolean}

        */
        public static string SendResponse(HttpListenerRequest request)
        {
            Console.WriteLine("Sending response to: " + request.RawUrl);
            switch(request.RawUrl) {
                case "/api/events":
                    return @"{""events"": [
                        {""id"": 1, ""name"": ""Barcelona"", ""client"": ""Merlin""}, 
                        {""id"": 2, ""name"": ""Paris"", ""client"": ""Parexel""}, 
                        {""id"": 3, ""name"": ""Biomet PROS 2016"", ""client"": ""Biomet""}]}";
                case "/api/event/1/sessions":
                    return @"{""sessions"": [
                        {""id"": 1, ""name"": ""Test Session One"", ""location"": ""Main Hall""}, 
                        {""id"": 2, ""name"": ""Test Session Two"", ""location"": ""Granat Room""}, 
                        {""id"": 3, ""name"": ""Test Session Three"", ""location"": ""Welcome Dinner""}]}";
                case "/api/event/2/sessions":
                    return @"{""sessions"": [
                        {""id"": 4, ""name"": ""Investigator Meeting"", ""location"": ""Saphir Room""}, 
                        {""id"": 5, ""name"": ""CRA Hands on Training"", ""location"": ""Diamond Room""}]}";
                case "/api/event/3/sessions":
                    return @"{""sessions"": [
                        {""id"": 6, ""name"": ""Hematology Franchise Plenary"", ""location"": ""Congress Hall I""}, 
                        {""id"": 7, ""name"": ""Kadcyla Launch"", ""location"": ""Grand Ballroom""}, 
                        {""id"": 8, ""name"": ""MabThera GIM"", ""location"": ""Congress Hall III""}, 
                        {""id"": 9, ""name"": ""Zelboraf & cobimetinib GIM"", ""location"": ""Congress Hall I""}, 
                        {""id"": 10, ""name"": ""MabThera Dinner"", ""location"": ""Congress Hall III""}, 
                        {""id"": 11, ""name"": ""Erivedge GIM"", ""location"": ""Vikarka Resturant""}, 
                        {""id"": 12, ""name"": ""GAZYVA Launch"", ""location"": ""Grand Ballroom""}, 
                        {""id"": 13, ""name"": ""Avastin CRC GIM"", ""location"": ""Congress Hall II""}, 
                        {""id"": 14, ""name"": ""PERJETA Drinks Reception"", ""location"": ""Cloud 9 Bar""}, 
                        {""id"": 15, ""name"": ""GAZYVA Launch Dinner"", ""location"": ""National House of Vinohrady""}, 
                        {""id"": 16, ""name"": ""Roche Dinner"", ""location"": ""SaSaZu Club""}]}";
                default:
                    return "{}";
            }
        }
    }

    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;

        public WebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "Needs Windows XP SP2, Server 2003 or later.");

            // URI prefixes are required, for example 
            // "http://localhost:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            // A responder method is required
            if (method == null)
                throw new ArgumentException("method");

            foreach (string s in prefixes)
                _listener.Prefixes.Add(s);

            _responderMethod = method;
            _listener.Start();
        }

        public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
            : this(prefixes, method)
        { }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Console.WriteLine("Webserver running (at http://localhost:8080/) ...");
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                string rstr = _responderMethod(ctx.Request);
                                byte[] buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch { } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
}
