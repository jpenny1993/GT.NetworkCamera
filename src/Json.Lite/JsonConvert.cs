using System;
using System.Text;

namespace Json.Lite
{
    public static class JsonConvert
    {
        public static string SerializeObject(object value)
        {
            var sb = new StringBuilder();
            sb.WriteSomething(value, 0);
            return sb.ToString();
        }
    }
}
