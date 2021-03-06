﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Collections.Specialized;
using System.Xml;
using JSNLog.LogHandling;
using System.IO;
using JSNLog.Infrastructure;
using System.Web.SessionState;

// Be sure to leave the namespace at JSNLog. Web.config relies on this.
namespace JSNLog
{
    // Implementing IReadOnlySessionState on the handler enables it to access session state.
    // This interface is just a marker, no need to implement anything.
    // Without this interface, the framework doesn't retrieve the session state to save CPU cycles
    // and possibly database accesses.
    // Note that this could also have used IRequiresSessionState, but that places an exclusive lock
    // on the session state because the interface allows the handler to update the session state.
    // See 
    // http://stackoverflow.com/questions/7247409/regarding-the-usage-of-irequiressessionstate
    // http://stackoverflow.com/questions/8039014/irequiressessionstate-vs-ireadonlysessionstate

    public class LoggerHandler : IHttpHandler, IReadOnlySessionState
    {
        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            string userAgent = context.Request.UserAgent;
            string userHostAddress = context.Request.UserHostAddress;
            DateTime serverSideTimeUtc = DateTime.UtcNow;
            string url = (context.Request.UrlReferrer ?? context.Request.Url).ToString();
            string requestId = JSNLog.Infrastructure.RequestId.GetFromRequest();

            string httpMethod = context.Request.HttpMethod;
            string origin = context.Request.Headers["Origin"];

            string json;
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                json = reader.ReadToEnd();
            }

            HttpResponse response = context.Response;
            ILogger logger = new Logger();
            XmlElement xe = XmlHelpers.RootElement();

            LoggerProcessor.ProcessLogRequest(json, userAgent, userHostAddress,
                serverSideTimeUtc, url, requestId,
                httpMethod, origin, new HttpResponseWrapper(response), logger, xe);

            // Send dummy response. That way, the log request will not remain "pending"
            // in eg. Chrome dev tools.
            //
            // This must be given a MIME type of "text/plain"
            // Otherwise, the browser may try to interpret the empty string as XML.
            // When the user uses Firefox, and right clicks on the page and chooses "Inspect Element",
            // then in that debugger's console it will say "no element found".
            // See
            // http://www.acnenomor.com/307387p1/how-do-i-setup-my-ajax-post-request-to-prevent-no-element-found-on-empty-response
            // http://stackoverflow.com/questions/975929/firefox-error-no-element-found/976200#976200

            response.ContentType = "text/plain";
            response.ClearContent();
            response.Write("");
        }
    }
}
