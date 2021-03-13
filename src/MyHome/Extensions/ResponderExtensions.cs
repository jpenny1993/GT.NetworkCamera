using System;
using Microsoft.SPOT;
using Gadgeteer.Networking;

namespace MyHome.Extensions
{
    public static class ResponderExtensions
    {
        public static bool HasQueryParameter(this Responder responder, string parameterName)
        {
            return responder.UrlParameters != null &&
                   responder.UrlParameters.Contains(parameterName);
        }

        public static string QueryParameter(this Responder responder, string parameterName)
        {
            return HasQueryParameter(responder, parameterName)
                ? responder.UrlParameters[parameterName].ToString()
                : string.Empty;
        }
    }
}
