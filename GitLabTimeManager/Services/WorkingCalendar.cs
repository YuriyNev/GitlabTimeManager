﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using GitLabTimeManager.Helpers;

namespace GitLabTimeManager.Services;

public class WorkingCalendar : ICalendar
{
    private static int Year { get; } = DateTime.Today.Year;
    private static string CalendarName { get; } = "calendar.xml";
    private static string RemotePath { get; } = $"http://xmlcalendar.ru/data/ru/{Year}/{CalendarName}";
    private static string LocalDirectory { get; } = $"{AppDomain.CurrentDomain.BaseDirectory}";
    private static string LocalPath { get; } = $"{LocalDirectory}{CalendarName}";

    private bool _initialized;

    private Dictionary<DateTime, TimeSpan> Holidays { get; set; } = new();

    public async Task InitializeAsync()
    {
        try
        {
            if (_initialized)
                return;

            // If file don't exist
            if (!CalendarIsExist(LocalPath))
            {
                var downloaded = await DownloadFileAsync(RemotePath, LocalPath).ConfigureAwait(true);
                if (!downloaded) return;
            }
            var doc = LoadXmlDocument(LocalPath);

            // If not actual year, try update 
            if (!YearIsAvailable(doc, Year))
            {
                var downloaded = await DownloadFileAsync(RemotePath, LocalPath).ConfigureAwait(true);
                if (!downloaded) return;

                // reload document
                doc = LoadXmlDocument(LocalPath);
            }

            // Check year again
            if (!YearIsAvailable(doc, Year))
                return;
                
            Holidays = TryParse(doc);
            _initialized = true;
        }
        catch
        {
            _initialized = false;
        }
    }

    public TimeSpan GetHolidays(DateTime from, DateTime to) =>
        Holidays
            .Where(x => x.Key >= from && x.Key < to)
            .Select(x => x.Value)
            .Sum();


    public TimeSpan GetWorkingTime(DateTime from, DateTime to)
    {
        if (from > to)
            throw new ArgumentOutOfRangeException();

        var workTime = TimeHelper.GetWeekdaysTime(from, to);

        var holidayTime = GetHolidays(from, to);

        workTime -= holidayTime;
        if (workTime < TimeSpan.Zero)
            workTime = TimeSpan.Zero;

        return workTime;
    }

    private static async Task<bool> DownloadFileAsync(string remotePath, string localPath)
    {
        try
        {
            await DownloadFileCoreAsync(remotePath, localPath).ConfigureAwait(true);
            return true;
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.Message);
            return false;
        }
    }

    private static async Task DownloadFileCoreAsync(string fileUrl, string savePath)
    {
        using var client = new HttpClient();
        using var response = await client.GetAsync(fileUrl);
        if (response.IsSuccessStatusCode)
        {
            await using var fileStream = await response.Content.ReadAsStreamAsync();
            await using var outputFile = new FileStream(savePath, FileMode.Create);
            await fileStream.CopyToAsync(outputFile);
        }
        else
        {
            throw new HttpRequestException("Ошибка при загрузке файла: " + response.ReasonPhrase);
        }
    }
        
    private static bool CalendarIsExist(string location) => File.Exists(location);

    private static bool YearIsAvailable(XmlDocument document, int year)
    {
        XmlNode root = document.DocumentElement;
        var rootAttributes = root?.Attributes;

        if (rootAttributes == null) return false;

        foreach (var attribute in rootAttributes)
        {
            if (!(attribute is XmlAttribute xmlAttribute)) continue;
            if (xmlAttribute.LocalName == "year" && xmlAttribute.Value == year.ToString())
                return true;
        }

        return false;
    }

    private static XmlDocument LoadXmlDocument(string path)
    {
        var doc = new XmlDocument();
        doc.Load(path);
        return doc;
    }

    private static Dictionary<DateTime, TimeSpan> TryParse(XmlDocument document)
    {
        var dictionary = new Dictionary<DateTime, TimeSpan>();

        const string dayXPath = "/calendar/days";

        XmlNode root = document.DocumentElement;
        if (root == null)
            throw new Exception("Calendar not loaded!");

        var holidayNodes = root.SelectNodes(dayXPath);

        if (holidayNodes == null) return dictionary;
        {
            foreach (var holiday in holidayNodes)
            {
                if (!(holiday is XmlElement xmlElement)) continue;
                if (!xmlElement.HasChildNodes) continue;
                var xmlElementChildNodes = xmlElement.ChildNodes;
                foreach (var day in xmlElementChildNodes)
                {
                    if (!(day is XmlElement dayXml)) continue;

                    var data = Extract(dayXml);
                    if (data == null)
                        continue;
                    dictionary.Add(data.DateTime, data.Duration);
                }
            }
        }

        return dictionary;
    }

    private static DatePair Extract(XmlNode xmlElement)
    {
        const string dayNode = "d";
        const string typeNode = "t";
            
        const int fullDayType = 1;
        const int shortenedDayType = 2;
        var fullDayDuration = TimeSpan.FromHours(8);
        var shortenedDayDuration = TimeSpan.FromHours(1);

        var attributes = xmlElement.Attributes;
        if (attributes == null)
            return null;

        ushort monthValue = 0;
        ushort dayValue = 0;
        var duration = TimeSpan.Zero;

        foreach (var attribute in attributes)
        {
            if (!(attribute is XmlAttribute a)) continue;
            if (a.LocalName == dayNode)
            {
                var stringDate = a.Value.Split('.');
                if (!ushort.TryParse(stringDate[0], out monthValue))
                    continue;
                if (!ushort.TryParse(stringDate[1], out dayValue))
                    continue;
            }
            else if (a.LocalName == typeNode)
            {
                if (!ushort.TryParse(a.Value, out var type))
                    continue;

                switch (type)
                {
                    case fullDayType:
                        duration = fullDayDuration;
                        break;
                    case shortenedDayType:
                        duration = shortenedDayDuration;
                        break;
                    default:
                        continue;
                }
            }
        }

        var date = new DateTime(Year, monthValue, dayValue);
        if (duration > TimeSpan.Zero &&
            date.DayOfWeek != DayOfWeek.Saturday &&
            date.DayOfWeek != DayOfWeek.Sunday)
            return new DatePair { DateTime = date, Duration = duration };

        return null;
    }
}

public class DatePair
{
    public DateTime DateTime { get; set; }
    public TimeSpan Duration { get; set; }
}

public interface ICalendar
{
    TimeSpan GetHolidays(DateTime from, DateTime to);
    TimeSpan GetWorkingTime(DateTime from, DateTime to);
}