using System.Text;
using MyHome.Extensions;

namespace MyHome.Utilities
{
    public static class MyPath
    {
        public static string Combine(params string[] items)
        {
            var builder = new StringBuilder();
            for (var index = 0; index < items.Length; index++)
            {
                var str = items[index];
                if (str.IsNullOrEmpty()) continue;

                // Replace things that would break the path
                str = str
                    .ReplaceAll("..", string.Empty)
                    .ReplaceAll("/", "\\")
                    .ReplaceAll("\\\\", "\\");

                // Add directory separators
                if (builder.Length > 0)
                {
                    builder.Append("\\");
                }

                builder.Append(str);
            }

            return builder.ToString();
        }
    }
}
