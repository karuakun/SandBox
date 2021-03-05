using System;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;

namespace ConsoleApp5
{
    public class AppHtmlParser
    {
        public async Task<string[]> ExtractUrls(Uri requestUrl, string html)
        {
            var currentPath = requestUrl.AbsoluteUri.Substring(0, requestUrl.AbsoluteUri.LastIndexOf('/')) + "/";
            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(html);
            var anchorTags = document.GetElementsByTagName("a");
            return anchorTags
                .Select(t => t.GetAttribute("href"))
                .Where(t => t != null && !t.ToLower().StartsWith("javascript:")) // filter script
                .Select(url => { 
                    var pos = url.IndexOf("#", StringComparison.Ordinal);
                    return url.StartsWith("#")
                        ? string.Empty
                        : 0 < pos
                            ? url.Substring(0, pos)
                            : url;
                }) // remove page anchor
                .Select(url => url.ToLower().StartsWith("http") 
                    ? url
                    : url.StartsWith("/")
                        ? $"{requestUrl.Scheme}://{requestUrl.Host}{url}"
                        : $"{currentPath}{url}"
                ) // 
                .Select(_ =>
                {
                    try
                    {
                        return new Uri(_).ToString().ToLower();
                    }
                    catch (Exception)
                    {
                        // skip
                        return string.Empty;
                    }
                })
                .Where(_ => !string.IsNullOrEmpty(_))
                .Distinct()
                .ToArray();
        }

    }
}