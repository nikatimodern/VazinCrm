using System;
using System.Globalization;

namespace CRM.Framework
{
    public static class BaseServices
    {




        public static string GetDatePersian(DateTime? dt)
        {
            if (!dt.HasValue)
            {
                return null;
            }
            else
            {
                PersianCalendar pc = new PersianCalendar();
                string date = pc.GetYear(dt.Value).ToString("0000") + "/" + pc.GetMonth(dt.Value).ToString("00") + "/" + pc.GetDayOfMonth(dt.Value).ToString("00");
                return date;
            }


        }
        public static string GetDateTimeMilisecondPersian()
        {
            PersianCalendar pc = new PersianCalendar();
            DateTime dt = DateTime.Now;
            string date = pc.GetYear(dt).ToString("0000") + "/" + pc.GetMonth(dt).ToString("00") + "/" + pc.GetDayOfMonth(dt).ToString("00") +
                " " + pc.GetHour(dt).ToString("00") + ":" + pc.GetMinute(dt).ToString("00") + ":" + pc.GetSecond(dt).ToString("00") + "," + pc.GetMilliseconds(dt).ToString();

            return date;
        }
        public static string GetDateTimePersian()
        {
            PersianCalendar pc = new PersianCalendar();
            DateTime dt = DateTime.Now;
            string date = pc.GetYear(dt).ToString("0000") + "/" + pc.GetMonth(dt).ToString("00") + "/" + pc.GetDayOfMonth(dt).ToString("00") +
                " " + pc.GetHour(dt).ToString("00") + ":" + pc.GetMinute(dt).ToString("00") + ":" + pc.GetSecond(dt).ToString("00");

            return date;
        }
        public static string GetYearPersian()
        {
            PersianCalendar pc = new PersianCalendar();
            DateTime dt = DateTime.Now;
            string date = pc.GetYear(dt).ToString("0000");
            return date;
        }
        public static string GetDatePersian()
        {
            PersianCalendar pc = new PersianCalendar();
            DateTime dt = DateTime.Now;
            string date = pc.GetYear(dt).ToString("0000") + "/" + pc.GetMonth(dt).ToString("00") + "/" + pc.GetDayOfMonth(dt).ToString("00");
            return date;
        }
        public static DateTime? GetDateMiladi(string date)
        {
            if (!string.IsNullOrEmpty(date))
            {
                var MDate = DateTime.Parse(ToEnglishNumber(date));
                return MDate;
            }
            else
                return null;
        }
        public static DateTime? GetDateMiladi()
        {
            var date = GetDateTimePersian();
            if (!string.IsNullOrEmpty(date))
            {
                var MDate = DateTime.Parse(ToEnglishNumber(date));
                return MDate;
            }
            else
                return null;
        }

        public static string ToEnglishNumber(string val)
        {
            if (val == "" || val == null)
            {
                return "";
            }
            else
            {
                return val
                .Replace("۰", "0")
                .Replace("۱", "1")
                .Replace("۲", "2")
                .Replace("۳", "3")
                .Replace("۴", "4")
                .Replace("۵", "5")
                .Replace("۶", "6")
                .Replace("۷", "7")
                .Replace("۸", "8")
                .Replace("۹", "9");

            }
        }

        public static string ToPersianNumber(string val)
        {
            if (val == "" || val == null)
            {
                return "";
            }
            else
            {
                return val
                .Replace("0", "۰")
                .Replace("1", "۱")
                .Replace("2", "۲")
                .Replace("3", "۳")
                .Replace("4", "۴")
                .Replace("5", "۵")
                .Replace("6", "۶")
                .Replace("7", "۷")
                .Replace("8", "۸")
                .Replace("9", "۹");

            }

        }


    }
}
