////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using Microsoft.SPOT;
using System.Text;

namespace Gadgeteer.Networking
{
    /// <summary>
    /// Internal class used as a template for http responses. 
    /// </summary>
    internal class BinaryResponseTemplate
    {
        public HttpStatusCode StatusCode;

        /// <summary>
        /// the body content to be returned. 
        /// </summary>
        public byte[] Content;

        /// <summary>
        /// The header content to be returned. 
        /// </summary>
        public byte[] Header;

        /// <summary>
        /// The content type of the body content. 
        /// </summary>
        protected string ContentType;

        /// <summary>
        /// The refresh interval.
        /// </summary>
        protected uint refreshAfter;

        /// <summary>
        /// Constructor that prepares the response.
        /// </summary>
        /// <param name="contentType">The content type to be published.</param>
        /// <param name="refreshAfter">Specifies the refresh interval of the web page.</param>
        /// <param name="content">The binary response data.</param>
        public BinaryResponseTemplate(HttpStatusCode statusCode, string contentType, byte[] content, uint refreshAfter)
        {
            this.StatusCode = statusCode;
            this.Content = content;
            this.ContentType = contentType;
            this.refreshAfter = refreshAfter;
            string header = "";
            var statusMessage = GetStatusMessage(StatusCode);
            if (content == null && refreshAfter <= 0)
            {
                header = "HTTP/1.0 " + (int)StatusCode + " " + statusMessage + "\r\nCache-Control: no-cache\r\nConnection: Close\r\nContent-Length: 0\r\nContent-Type: " + contentType + "\r\n\r\n";
            }
            else if (content != null && refreshAfter <= 0)
            {
                header = "HTTP/1.0 " + (int)StatusCode + " " + statusMessage + "\r\nCache-Control: no-cache\r\nConnection: Close\r\nContent-Length: " + content.Length + "\r\nContent-Type: " + contentType + "\r\n\r\n";
            }
            else if (content == null && refreshAfter > 0)
            {
                header = "HTTP/1.0 " + (int)StatusCode + " " + statusMessage + "\r\nCache-Control: no-cache\r\nConnection: Close\r\nRefresh: " + refreshAfter + "\r\nContent-Length: 0\r\nContent-Type: " + contentType + "\r\n\r\n";
            }
            else if (content != null && refreshAfter > 0)
            {
                header = "HTTP/1.0 " + (int)StatusCode + " " + statusMessage + "\r\nCache-Control: no-cache\r\nConnection: Close\r\nRefresh: " + refreshAfter + "\r\nContent-Length: " + content.Length + "\r\nContent-Type: " + contentType + "\r\n\r\n";
            }

            Header = Encoding.UTF8.GetBytes(header);
        }

        private static string GetStatusMessage(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                default: throw new NotImplementedException("Unhandled HttpStatusCode");
                case HttpStatusCode.OK: return "OK";
                case HttpStatusCode.NoContent: return "No Content";
                case HttpStatusCode.BadRequest: return "Bad Request";
                case HttpStatusCode.Unauthorized: return "Unauthorized";
                case HttpStatusCode.Forbidden: return "Forbidden";
                case HttpStatusCode.NotFound: return "Not Found";
                case HttpStatusCode.InternalServerError: return "Internal Server Error";
                case HttpStatusCode.ServiceUnavailable: return "Service Unavailable";
            }
        }
    }

}
