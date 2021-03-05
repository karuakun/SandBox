using System;
using System.Threading.Tasks;
using ConsoleApp5;
using Xunit;

namespace XUnitTestProject1
{
    public class AppHttpParserTest
    {
        [Fact]
        public async Task Test1()
        {
            var target = new AppHtmlParser();
            {
                var actual = await target.ExtractUrls(new Uri("https://abc/1/"), @"
<html><body>
<a></a> <!-- null -->
<a href='aaa'></a> <!-- https://abc/1/aaa -->
<a class='' href='./a'></a> <!-- https://abc/1/a -->
<a class='' href='../a'></a> <!-- https://abc/a -->
<a class='' href='http://aaa'></a> <!-- http://aaa/ -->
<a class='' href='http://aaa/'></a> <!-- 重複 -->
<a class='' href='http://aaa/bbb'></a> <!-- http://aaa/bbb -->
<a class='' href='https://aaa'></a> <!-- https://aaa/ -->
<a class='' href='#'></a> <!-- https://abc/1/ -->
<a class='' href='#abc'></a> <!-- skip -->
<a class='' href='./a#abc'></a> <!-- skip -->
<a href='javascript:aaa()'></a> <!-- skip -->
<a class='' href='/'></a> <!-- https://abc/ -->
<a href='Aaa'></a> <!-- 重複 -->
</body></html>");

                Assert.Equal(8, actual.Length);
                Assert.Equal("https://abc/1/aaa", actual[0]);
                Assert.Equal("https://abc/1/a", actual[1]);
                Assert.Equal("https://abc/a", actual[2]);
                Assert.Equal("http://aaa/", actual[3]);
                Assert.Equal("http://aaa/bbb", actual[4]);
                Assert.Equal("https://aaa/", actual[5]);
                Assert.Equal("https://abc/1/", actual[6]);
                Assert.Equal("https://abc/", actual[7]);
            }
            {
                var actual = await target.ExtractUrls(new Uri("https://abc/1/aaaa"), @"
<html><body>
<a></a> <!-- null -->
<a href='aaa'></a> <!-- https://abc/1/aaa -->
<a class='' href='./a'></a> <!-- https://abc/1/a -->
<a class='' href='../a'></a> <!-- https://abc/a -->
<a class='' href='http://aaa'></a> <!-- http://aaa/ -->
<a class='' href='http://aaa/'></a> <!-- 重複 -->
<a class='' href='http://aaa/bbb'></a> <!-- http://aaa/bbb -->
<a class='' href='https://aaa'></a> <!-- https://aaa/ -->
<a class='' href='#'></a> <!-- https://abc/1/ -->
<a class='' href='#abc'></a> <!-- skip -->
<a class='' href='./a#abc'></a> <!-- skip -->
<a class='' href='/'></a> <!-- https://abc/ -->
<a href='javascript:aaa()'></a> <!-- skip -->
<a href='Aaa'></a> <!-- 重複 -->
</body></html>");

                Assert.Equal(8, actual.Length);
                Assert.Equal("https://abc/1/aaa", actual[0]);
                Assert.Equal("https://abc/1/a", actual[1]);
                Assert.Equal("https://abc/a", actual[2]);
                Assert.Equal("http://aaa/", actual[3]);
                Assert.Equal("http://aaa/bbb", actual[4]);
                Assert.Equal("https://aaa/", actual[5]);
                Assert.Equal("https://abc/1/", actual[6]);
                Assert.Equal("https://abc/", actual[7]);
            }
        }
    }
}
