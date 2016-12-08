//-----------------------------------------------------------------------
// <copyright file="ExceptionHandler.cs" company="aTech Media Ltd">
//     Copyright (c) aTech Media Ltd. All rights reserved.
// </copyright>
// <author>Jack Hayter</author>
//-----------------------------------------------------------------------
namespace Airbrake
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// Airbrake API compatible exception handler for C#
    /// First published on Sep 12, 2012 at: <c>https://github.com/atech/airbrake-dotnet</c>
    /// </summary>
    public class ExceptionHandler
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandler"/> class.
        /// </summary>
        /// <param name="ssl">if set to <c>true</c> [SSL].</param>
        /// <param name="host">The host.</param>
        /// <param name="path">The path.</param>
        /// <param name="apikey">The API key.</param>
        /// <param name="environment">The environment.</param>
        public ExceptionHandler(bool ssl, string host, string path, string apikey, string environment)
        {
            this.SSL = ssl;
            this.Host = host;
            this.Path = path;
            this.Apikey = apikey;
            this.Environment = environment;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the API Key for the exception reporter.
        /// </summary>
        /// <value>
        /// The API Key.
        /// </value>
        private string Apikey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [SSL] should be used for the connection.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [SSL]; otherwise, <c>false</c>.
        /// </value>
        private bool SSL
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the hostname/IP of the exceptions server.
        /// </summary>
        /// <value>
        /// The host.
        /// </value>
        private string Host
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the exception reporting API path.
        /// </summary>
        /// <value>
        /// The path of the exceptions handling service.
        /// </value>
        private string Path
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the environment name to be reported for the application.
        /// </summary>
        /// <value>
        /// The environment name.
        /// </value>
        private string Environment
        {
            get;
            set;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sends the specified exception to the server.
        /// </summary>
        /// <param name="ex">The raw exception to report.</param>
        /// <param name="customParams">The custom parameters that should be sent with the exception.</param>
        /// <returns>Reporting success (HTTP 200 causes true, otherwise false).</returns>
        public bool Send(Exception ex, List<KeyValuePair<string, string>> customParams = null)
        {
            XmlDocument xml = this.GetXML(ex, customParams);

            // Create a request using a URL that can receive a post. 
            string url;
            if (this.SSL)
            {
                url = "https://" + this.Host + this.Path; 
            }
            else
            {
                url = "http://" + this.Host + this.Path;
            }

            WebRequest request = WebRequest.Create(url);

            // Set our POST data
            request.Method = "POST";

            // Encode our post data
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

            // Make sure we got a 200
            if (r.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            // Clean up the streams.
            response.Close();
            return true;
        }

        /// <summary>
        /// Renders an exception (and custom parameters) into an XML blob conforming to the Airbrake API specification.
        /// </summary>
        /// <param name="ex">The exception to digest.</param>
        /// <param name="customParams">The custom parameters add.</param>
        /// <returns>The full XML that will be sent to the exceptions service.</returns>
        private XmlDocument GetXML(Exception ex, List<KeyValuePair<string, string>> customParams = null)
        {
            // Create the xml document and Notice element
            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(dec);
            XmlElement root = doc.CreateElement("notice");
            root.SetAttribute("version", "2.3");

            // API Key
            XmlElement apikey = doc.CreateElement("api-key");
            apikey.InnerText = this.Apikey;
            root.AppendChild(apikey);

            // Notifier information
            XmlElement notifier = doc.CreateElement("notifier");

            // Notifier name
            XmlElement name = doc.CreateElement("name");
            name.InnerText = "aTech Media .Net Airbrake Client";
            notifier.AppendChild(name);

            // Notifier version
            XmlElement version = doc.CreateElement("version");
            version.InnerText = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            notifier.AppendChild(version);

            // Notifier url
            XmlElement url = doc.CreateElement("url");
            url.InnerText = "http://www.atechmedia.com";
            notifier.AppendChild(url);

            // Append the notifier
            root.AppendChild(notifier);

            // Error information
            XmlElement error = doc.CreateElement("error");

            // error class
            XmlElement cls = doc.CreateElement("class");
            cls.InnerText = ex.GetType().Name;
            error.AppendChild(cls);

            // error message
            XmlElement message = doc.CreateElement("message");
            message.InnerText = ex.Message;
            error.AppendChild(message);

            // error backtrace
            XmlElement backtrace = doc.CreateElement("backtrace");

            // Add backtrace lines
            foreach (XmlElement line in this.ParseBacktrace(doc, ex.StackTrace))
            {
                backtrace.AppendChild(line);
            }

            error.AppendChild(backtrace);

            // Append the error
            root.AppendChild(error);

            // Reuqest information
            XmlElement request = doc.CreateElement("request");

            // Request component
            XmlElement component = doc.CreateElement("component");
            component.InnerText = ex.Source;
            request.AppendChild(component);

            // Request action
            XmlElement action = doc.CreateElement("action");
            action.InnerText = ex.TargetSite.Name;
            request.AppendChild(action);

            // CGI data
            XmlElement cgi = doc.CreateElement("cgi-data");

            // CGI OS
            XmlElement os = doc.CreateElement("var");
            os.SetAttribute("key", "Operating System");
            os.InnerText = System.Environment.OSVersion.VersionString;
            cgi.AppendChild(os);

            // CGI Bits
            XmlElement bits = doc.CreateElement("var");
            bits.SetAttribute("key", "Platform");
            bits.InnerText = System.Environment.OSVersion.Platform.ToString();
            cgi.AppendChild(bits);

            Assembly entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly != null) {
                // CGI App
                XmlElement app = doc.CreateElement("var");
                app.SetAttribute("key", "Application Version");
                app.InnerText = entryAssembly.GetName().Version.ToString();
                cgi.AppendChild(app);

                // CGI App-plat
                XmlElement plat = doc.CreateElement("var");
                plat.SetAttribute("key", "Application Platform");
                plat.InnerText = entryAssembly.GetName().ProcessorArchitecture.ToString();
                cgi.AppendChild(plat);
            }

            if (customParams != null)
            {
                // Custom Params
                foreach (KeyValuePair<string, string> param in customParams)
                {
                    XmlElement p = doc.CreateElement("var");
                    p.SetAttribute("key", param.Key);
                    p.InnerText = param.Value;
                    cgi.AppendChild(p);
                }
            }

            // Append the CGI and request
            request.AppendChild(cgi);
            root.AppendChild(request);

            // Environment info
            XmlElement env = doc.CreateElement("server-environment");

            if (entryAssembly != null) {
                // Project root
                XmlElement proj = doc.CreateElement("project-root");
                proj.InnerText = entryAssembly.Location;
                env.AppendChild(proj);
            }

            // Environment
            XmlElement envname = doc.CreateElement("environment-name");
            envname.InnerText = this.Environment;
            env.AppendChild(envname);

            // Append the environment to the root and doc
            root.AppendChild(env);
            doc.AppendChild(root);
            return doc;
        }

        /// <summary>
        /// Parses the back trace string from an exception into an XML document.
        /// </summary>
        /// <param name="doc">The document namespace context.</param>
        /// <param name="stack">The stack trace from the application.</param>
        /// <returns>A list of properly formatted XML objects that represent the stack trace for use in an XML document.</returns>
        private List<XmlElement> ParseBacktrace(XmlDocument doc, string stack)
        {
            List<XmlElement> backtrace = new List<XmlElement>();
            string[] lines = Regex.Split(stack, "\r\n");

            foreach (string line in lines)
            {
                XmlElement l = doc.CreateElement("line");

                string number = Regex.Match(line, @":line \d+").Value.Replace(":line", string.Empty);
                string file = Regex.Match(line, @"in (.*):").Value.Replace("in ", string.Empty).Replace(":", string.Empty);
                string method = Regex.Match(line, @"at .*\)").Value.Replace("at ", string.Empty);

                if (!string.IsNullOrEmpty(number))
                {
                    l.SetAttribute("number", number);
                }

                if (!string.IsNullOrEmpty(file))
                {
                    l.SetAttribute("file", file);
                }

                if (!string.IsNullOrEmpty(method))
                {
                    l.SetAttribute("method", method);
                }

                backtrace.Add(l);
            }

            return backtrace;
        }

        #endregion
    }
}
