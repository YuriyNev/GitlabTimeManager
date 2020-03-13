using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GitLabTimeManager.Services
{
    public class WorkingCalendar
    {
        public async Task<string> LoadingCalendarAsync()
        {
            using var wc = new WebClient();
            //wc.DownloadFileAsync(new Uri("https://www.officeholidays.com/ics/russia"),
            //    AppDomain.CurrentDomain.BaseDirectory
            //);
            return await wc.DownloadStringTaskAsync(new Uri("https://www.officeholidays.com/ics/russia"));
        }
    }
}
