using System;
using System.Collections;
using Gadgeteer.Networking;
using MyHome.Utilities;

namespace MyHome.Extensions
{
    public static class ResponderExtensions
    {
        private static readonly string[] Booleans = new[] { "1", "true", "yes" };

        public static bool HasQueryParameter(this Responder responder, string parameterName)
        {
            return responder.UrlParameters != null &&
                   responder.UrlParameters.Contains(parameterName);
        }

        private static string QueryParameter(this Responder responder, string parameterName)
        {
            return responder.UrlParameters[parameterName].ToString();
        }

        public static bool QueryBoolean(this Responder responder, string parameterName)
        {
            if (HasQueryParameter(responder, parameterName))
            {
                var valueStr = QueryParameter(responder, parameterName);
                return Booleans.ContainsCaseInsensitive(valueStr);
            }

            return false;
        }

        public static int QueryInteger(this Responder responder, string parameterName)
        {
            if (HasQueryParameter(responder, parameterName))
            {
                try
                {
                    return int.Parse(QueryParameter(responder, parameterName));
                }
                catch 
                {
                }
            }

            return 0;
        }

        public static DateTime QueryDate(this Responder responder, string parameterName)
        {
            if (!HasQueryParameter(responder, parameterName))
            {
                return DateTime.MinValue;
            }

            var valueStr = QueryParameter(responder, parameterName);
            DateTime date;

            if (DateTimeParser.ISO8601(valueStr, out date))
            {
                return date;
            }

            if (DateTimeParser.RFC822(valueStr, out date))
            {
                return date;
            }

            return DateTime.MinValue;
        }

        public static string QueryString(this Responder responder, string parameterName)
        {
            return HasQueryParameter(responder, parameterName)
                ? QueryParameter(responder, parameterName)
                : string.Empty;
        }
    }
}
