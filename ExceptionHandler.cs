using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace airbrake
{
    public class ExceptionHandler
    {
        private readonly string _apikey;
        private readonly bool _ssl;
        private readonly string _host;
        private readonly string _path;
        private readonly string _environment;

        public ExceptionHandler(bool ssl, string host, string path, string apikey, string environment)
        {
            _ssl = ssl;
            _host = host;
            _path = path;
            _apikey = apikey;
            _environment = environment;
        }

        public bool Send(Exception ex, List<KeyValuePair<string, string>> customParams = null)
        {
            XmlDocument xml = GetXML(ex, customParams);

            // Create a request using a URL that can receive a post. 
            string url;
            if (_ssl){ url = "https://" + _host + _path; }
            else{ url = "http://" + _host + _path;}
            WebRequest request = WebRequest.Create(url);

            // Set our POST data
            request.Method = "POST";
            
            //Encode our post data
            byte[] byteArray = Encoding.UTF8.GetBytes(xml.OuterXml);
            request.ContentType = "text/xml";
            request.ContentLength = byteArray.Length;

            // Get the request stream (open for writing)
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            // Get the response.
            WebResponse response = null;
            HttpWebResponse r = null;
            try
            {
                response = request.GetResponse();
                r = (HttpWebResponse)response;
            }
            catch
            {
                return false;
            }

            //Make sure we got a 200
            if (r.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            // Clean up the streams.
            dataStream.Close();
            response.Close();
            return true;
        }

        private XmlDocument GetXML(Exception ex, IEnumerable<KeyValuePair<String, String>> customParams = null)
        {

            //Create the xml document and Notice element
            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(dec);
            XmlElement root = doc.CreateElement("notice");
            root.SetAttribute("version", "2.3");


            //API Key
            XmlElement apikey = doc.CreateElement("api-key");
            apikey.InnerText = _apikey;
            root.AppendChild(apikey);

            //Notifier information
            XmlElement notifier = doc.CreateElement("notifier");

            //Notifier name
            XmlElement name = doc.CreateElement("name");
            name.InnerText = "aTech Media .Net Airbrake Client";
            notifier.AppendChild(name);

            //Notifier version
            XmlElement version = doc.CreateElement("version");
            version.InnerText = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            notifier.AppendChild(version);

            //Notifier url
            XmlElement url = doc.CreateElement("url");
            url.InnerText = "http://www.atechmedia.com";
            notifier.AppendChild(url);

            //Append the notifier
            root.AppendChild(notifier);

            //Error information
            XmlElement error = doc.CreateElement("error");

            //error class
            XmlElement cls = doc.CreateElement("class");
            cls.InnerText = ex.GetType().Name;
            error.AppendChild(cls);

            //error message
            XmlElement message = doc.CreateElement("message");
            message.InnerText = BuildCompleteMessage(ex);
            error.AppendChild(message);

            //error backtrace
            XmlElement backtrace = doc.CreateElement("backtrace");
            
            //Add backtrace lines
            foreach(XmlElement line in ParseBacktrace(doc, ex)){
                backtrace.AppendChild(line);
            }
            error.AppendChild(backtrace);

            //Append the error
            root.AppendChild(error);

            //Reuqest information
            XmlElement request = doc.CreateElement("request");

            //Request component
            XmlElement component = doc.CreateElement("component");
            component.InnerText = ex.Source;
            request.AppendChild(component);

            //Request action
            XmlElement action = doc.CreateElement("action");
            action.InnerText = ex.TargetSite != null ? ex.TargetSite.Name: string.Empty;
            request.AppendChild(action);

            //CGI data
            XmlElement cgi = doc.CreateElement("cgi-data");

            //CGI OS
            XmlElement os = doc.CreateElement("var");
            os.SetAttribute("key", "Operating System");
            os.InnerText = Environment.OSVersion.VersionString;
            cgi.AppendChild(os);

            //CGI Bits
            XmlElement bits = doc.CreateElement("var");
            bits.SetAttribute("key", "Platform");
            bits.InnerText = Environment.OSVersion.Platform.ToString();
            cgi.AppendChild(bits);
            
            //CGI App
            XmlElement app = doc.CreateElement("var");
            bits.SetAttribute("key", "Application Version");
            bits.InnerText = Assembly.GetEntryAssembly().GetName().Version.ToString();
            cgi.AppendChild(bits);

            //CGI App-plat
            XmlElement plat = doc.CreateElement("var");
            plat.SetAttribute("key", "Application Platform");
            plat.InnerText = Assembly.GetEntryAssembly().GetName().ProcessorArchitecture.ToString();
            cgi.AppendChild(plat);
	    
            if (customParams != null)
	        {
            	//Custom Params
            	foreach (KeyValuePair<string,string> param in customParams)
            	{
                	XmlElement p = doc.CreateElement("var");
                	p.SetAttribute("key", param.Key);
                	p.InnerText = param.Value;
                	cgi.AppendChild(p);
            	}
	        }

            //Append the CGI and request
            request.AppendChild(cgi);
            root.AppendChild(request);

            //Environment info
            XmlElement env = doc.CreateElement("server-environment");
            
            //Project root
            XmlElement proj = doc.CreateElement("project-root");
            proj.InnerText = Assembly.GetEntryAssembly().Location;
            env.AppendChild(proj);

            //Environment
            XmlElement envname = doc.CreateElement("environment-name");
            envname.InnerText = _environment;
            env.AppendChild(envname);

            //Append the environment to the root and doc
            root.AppendChild(env);
            doc.AppendChild(root);
            return doc;

        }

        private string BuildCompleteMessage(Exception exception)
        {
            string message = exception.Message + " - ";

            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                message += (exception.Message + " - ");
            }
            return message;
        }

        private IEnumerable<XmlElement> ParseBacktrace(XmlDocument doc, Exception ex)
        {
            List<XmlElement> backtrace = new List<XmlElement>();
            while (ex != null)
            {
                string stack = ex.StackTrace;

                if (stack != null)
                {
                    string[] lines = Regex.Split(stack, "\r\n");

                    foreach (string line in lines)
                    {
                        XmlElement l = doc.CreateElement("line");

                        String number = Regex.Match(line, @":line \d+").Value.Replace(":line", "");
                        String file = Regex.Match(line, @"in (.*):").Value.Replace("in ", "").Replace(":", "");
                        String method = Regex.Match(line, @"at .*\)").Value.Replace("at ", "");

                        if (!String.IsNullOrEmpty(number))
                        {
                            l.SetAttribute("number", number);
                        }
                        if (!String.IsNullOrEmpty(file))
                        {
                            l.SetAttribute("file", file);
                        }
                        if (!String.IsNullOrEmpty(method))
                        {
                            l.SetAttribute("method", method);
                        }
                        backtrace.Add(l);

                    }
                }
                ex = ex.InnerException;
                if (ex != null)
                {
                    XmlElement l = doc.CreateElement("line");
                    l.SetAttribute("file", ex.GetType().Name);
                    backtrace.Add(l);
                }
            }
            return backtrace;
        }
    }
}
