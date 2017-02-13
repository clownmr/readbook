using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace Com.WebSite.Controllers
{
    public class ReadController : Controller
    {

        public IActionResult Index()
        {
            // 
            return View();
        }

        public IActionResult SearchResult(string q) {
            HttpClient httpClient = new HttpClient();       
            var t = httpClient.GetByteArrayAsync("http://zhannei.baidu.com/cse/search?s=1682272515249779940&entry=1&q="+ q);
            t.Wait();
            var ret = System.Text.Encoding.GetEncoding("utf-8").GetString(t.Result);
            ViewBag.SearchResult = ret;
            return View();
        }

        public IActionResult ChapterList(string bookuri) {
            HttpClient httpClient = new HttpClient();
            bookuri = bookuri.Replace("~", "/");
            var t = httpClient.GetByteArrayAsync(bookuri);
            t.Wait();
            var ret = System.Text.Encoding.GetEncoding("utf-8").GetString(t.Result);
            ViewBag.ReadValue = ret;
            return View();
        }

        public IActionResult ChapterRead(string ChapterUri) {
            HttpClient httpClient = new HttpClient();
            ChapterUri = ChapterUri.Replace("~", "/");
            var t = httpClient.GetByteArrayAsync(ChapterUri);
            t.Wait();
            var ret = System.Text.Encoding.GetEncoding("utf-8").GetString(t.Result);
            ViewBag.ReadValue = ret;
            return View();
        }
    }
}