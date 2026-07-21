using CRM.EndPoint.FrontEnd.Models.ViewModel.Login;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace CRM.EndPoint.FrontEnd.Services
{
    public class DateTimeServices
    {

        public DateTimeServices()
        {
            //System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("fa-IR");
            CultureInfo newCulture;
            newCulture = new CultureInfo("en-US");
            CultureInfo.CurrentCulture = newCulture;
            
        }


        public CheckTimeModel CheckTime(DateTime _date)
        {
            double AllowedTime = 1;
            var currenTime = DateTime.Now;
            var SmsTime = _date;
            var d = currenTime - SmsTime;
            if (d.TotalMinutes > AllowedTime)
            {
                var ChkModel = new CheckTimeModel() { Status = false, ElsapsTime = 0.ToString() };
                return ChkModel;
            }
            else
            {
                var els = AllowedTime - d.TotalMinutes;
                var ChkModel = new CheckTimeModel() { Status = true, ElsapsTime = Math.Floor(els * 60).ToString() };
                return ChkModel;
            }

      
        }

        public string ElapsTime(DateTime inputDate)
        {
            //DateTime inputDate;
            //if (!DateTime.TryParse(dateTimeStr, out inputDate))
            //{
            //    return "تاریخ نامعتبر";
            //}

            DateTime now = DateTime.Now;
            TimeSpan diff = now - inputDate;

            int diffMinutes = (int)diff.TotalMinutes;
            int diffHours = (int)diff.TotalHours;
            int diffDays = (int)diff.TotalDays;

            if (diffMinutes < 1)
            {
                return "لحظاتی پیش";
            }

            if (diffHours < 24)
            {
                int hours = diffMinutes / 60;
                int minutes = diffMinutes % 60;

                if (hours > 0 && minutes > 0)
                {
                    return $"{hours} ساعت و {minutes} دقیقه پیش";
                }
                else if (hours > 0)
                {
                    return $"{hours} ساعت پیش";
                }
                else
                {
                    return $"{minutes} دقیقه پیش";
                }
            }
            else
            {
                int days = diffDays;
                int remainingHours = diffHours - days * 24;

                if (remainingHours > 0)
                {
                    return $"{days} روز و {remainingHours} ساعت پیش";
                }
                else
                {
                    return $"{days} روز پیش";
                }
            }
        }





        public string GetTodayDatePersian()
        {
            PersianCalendar pc = new PersianCalendar();
            DateTime dt = DateTime.Now;

            // محاسبه اجزای تاریخ
            int year = pc.GetYear(dt);
            int month = pc.GetMonth(dt);
            int day = pc.GetDayOfMonth(dt);

            // دریافت نام روز هفته
            string[] persianWeekDays = new string[]
            {
            "شنبه", "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنج‌شنبه", "جمعه"
            };
            string weekDayName = persianWeekDays[(int)dt.DayOfWeek];

            // دریافت نام ماه‌های شمسی
            string[] persianMonths = new string[]
            {
            "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
            "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
            };
            string monthName = persianMonths[month - 1];

            // ساخت تاریخ نهایی
            string formattedDate = $"{weekDayName} {day} {monthName} {year}";
            return formattedDate;
        }


        public string GetTowmarrow()
        {
            PersianCalendar pc = new PersianCalendar();
            DateTime dt = DateTime.Now.AddDays(1);
            string date = pc.GetYear(dt).ToString("0000") + "/" + pc.GetMonth(dt).ToString("00") + "/" + pc.GetDayOfMonth(dt).ToString("00");
            return date;
        }

        public DateTime NextDate(DateTime? date)
        {
            DateTime dt = DateTime.Parse(date.ToString()).AddDays(1);
            return dt;
        }
        public DateTime NextDate(DateTime date)
        {
            DateTime dt = date.AddDays(1);
            return dt;
        }

        public DateTime NextDate()
        {
            DateTime dt = DateTime.Now.AddDays(1);
            return dt;
        }
        public string GetDatePersian()
        {
            PersianCalendar pc = new PersianCalendar();
            DateTime dt = DateTime.Now;
            string date = pc.GetYear(dt).ToString("0000") + "/" + pc.GetMonth(dt).ToString("00") + "/" + pc.GetDayOfMonth(dt).ToString("00");
            return date;
        }

        public string GetDatePersian(DateTime? dt)
        {

            var dtn = DateTime.Parse(dt.ToString());
            PersianCalendar pc = new PersianCalendar();
            string date = pc.GetYear(dtn).ToString("0000") + "/" + pc.GetMonth(dtn).ToString("00") + "/" + pc.GetDayOfMonth(dtn).ToString("00");
            return date;





        }
        public string GetDatePersian(DateTime dt)
        {
            //string GregorianDate = "Thursday, October 24, 2013";
            //DateTime d = DateTime.Parse(GregorianDate);
            //PersianCalendar pc1 = new PersianCalendar();
            //Console.WriteLine(string.Format("{0}/{1}/{2}", pc1.GetYear(d), pc1.GetMonth(d), pc1.GetDayOfMonth(d)));

            PersianCalendar pc = new PersianCalendar();
            string date = pc.GetYear(dt).ToString("0000") + "/" + pc.GetMonth(dt).ToString("00") + "/" + pc.GetDayOfMonth(dt).ToString("00");
            return date;
        }
        public string GetDateTimeMilisecondsPersian()
        {
            PersianCalendar pc = new PersianCalendar();
            DateTime dt = DateTime.Now;
            string date = pc.GetYear(dt).ToString("0000") + "/" + pc.GetMonth(dt).ToString("00") + "/" + pc.GetDayOfMonth(dt).ToString("00") +
                " " + pc.GetHour(dt).ToString("00") + ":" + pc.GetMinute(dt).ToString("00")
                + ":" + pc.GetSecond(dt).ToString("00") + ":" + pc.GetMilliseconds(dt).ToString();

            return date;
        }
        public string GetDateTimePersian()
        {
            PersianCalendar pc = new PersianCalendar();
            DateTime dt = DateTime.Now;
            string date = pc.GetYear(dt).ToString("0000") + "/" + pc.GetMonth(dt).ToString("00") + "/" + pc.GetDayOfMonth(dt).ToString("00") +
                " " + pc.GetHour(dt).ToString("00") + ":" + pc.GetMinute(dt).ToString("00")
                + ":" + pc.GetSecond(dt).ToString("00");

            return date;
        }

        public string GetDateTimePersian(DateTime dt)
        {
            PersianCalendar pc = new PersianCalendar();
            string date = pc.GetYear(dt).ToString("0000") + "/" + pc.GetMonth(dt).ToString("00") + "/" + pc.GetDayOfMonth(dt).ToString("00") +
                " " + pc.GetHour(dt).ToString("00") + ":" + pc.GetMinute(dt).ToString("00")
                + ":" + pc.GetSecond(dt).ToString("00");

            return date;
        }

        public string GetDateTimePersianWithDayOfWeek()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fa-IR");
            PersianCalendar pc = new PersianCalendar();
            DateTime dt = DateTime.Now;

            var dayOfWeek = dt.ToString("dddd", new CultureInfo("fa-IR")); // گرفتن نام روز هفته فارسی

            string date = pc.GetYear(dt).ToString("0000") + "/" +
                          pc.GetMonth(dt).ToString("00") + "/" +
                          pc.GetDayOfMonth(dt).ToString("00") + " " +
                          dayOfWeek + " " +
                          dt.Hour.ToString("00") + ":" +
                          dt.Minute.ToString("00");

            return date;
        }


        public DateTime? GetDateMiladi(string date)
        {
            try
            {
                date = TextService.ToEnglishNumber(date); ;


                if (!string.IsNullOrEmpty(date))
                {
                    int y = Int32.Parse(date.Substring(0, 4));
                    int m = Int32.Parse(date.Substring(5, 2));
                    int d = Int32.Parse(date.Substring(8, 2));

                    PersianCalendar pc = new PersianCalendar();
                    DateTime MDate = new DateTime(y, m, d, pc);
                    //DateTime MDate = new DateTime(ye, mo, day, hh, mm, ss, tz, pc);

                    return MDate;
                }
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }
        public DateTime? GetDateMiladi()
        {
            try
            {
                var date = DateTime.Now.ToString();


                if (!string.IsNullOrEmpty(date))
                {
                    int y = Int32.Parse(date.Substring(0, 4));
                    int m = Int32.Parse(date.Substring(5, 2));
                    int d = Int32.Parse(date.Substring(8, 2));

                    PersianCalendar pc = new PersianCalendar();
                    DateTime MDate = new DateTime(y, m, d, pc);
                    //DateTime MDate = new DateTime(ye, mo, day, hh, mm, ss, tz, pc);

                    return MDate;
                }
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }

        public string GetNowDate()
        {
            PersianCalendar pCal = new PersianCalendar();
            return pCal.GetYear(DateTime.Now).ToString("0000") + "/" + pCal.GetMonth(DateTime.Now).ToString("00") + "/" + pCal.GetDayOfMonth(DateTime.Now).ToString("00");
        }
        public string GetNowDateAsync()
        {
            PersianCalendar pCal = new PersianCalendar();
            return pCal.GetYear(DateTime.Now).ToString("0000") + "/" + pCal.GetMonth(DateTime.Now).ToString("00") + "/" + pCal.GetDayOfMonth(DateTime.Now).ToString("00");
        }
        public string GetNowTime()
        {
            return DateTime.Now.ToString("HH:mm");
        }
        public string GetNowTime(string TimeFormat)
        {
            return DateTime.Now.ToString(TimeFormat);
        }
        public int ConvertBirthdateToAge(string birthdate)
        {
            birthdate = TextService.ToEnglishNumber(birthdate);
            if (!string.IsNullOrEmpty(birthdate))
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("fa-Ir");

                var items = birthdate.Split('/').ToArray();
                var bdY = int.Parse(items[0]);

                var cuY = int.Parse(DateTime.Now.ToString("yyyy"));

                //return Int32.Parse(dd);
                var age = cuY - bdY;
                //if (birthdate.Date > today.AddYears(-age)) age--;
                return age;
            }
            return 0;

        }
        public string ConvertAgeToBirthDate(int age)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("fa-Ir");
            PersianCalendar date = new PersianCalendar();
            int CuYear = date.GetYear(DateTime.Now);
            var BirthDate = CuYear - age;
            //string Fbt = BirthDate + "/" + date.GetMonth(DateTime.Now).ToString("00") + "/" + date.GetDayOfMonth(DateTime.Now).ToString("00"); 
            string Fbt = BirthDate + "/01/01";
            return Fbt;
        }

        public string GetCurrentYear()
        {
            int[] Kabise = new int[30];

            Kabise = [1399,1403,1408,1412,1416,1420,1424,
                      1428,1432,1436,1441,1445,1449,1453,
                      1457,1461,1465,1469,1474,1478,1482,
                      1486,1490,1494,1498];

            var fYear = GetDatePersian().Substring(0, 4);

            string fdateS = fYear.ToString() + "/" + "01" + "/" + "01";

            string tdateS = "";

            if (Kabise.Contains(Int32.Parse(fYear)))
            {
                tdateS = fYear.ToString() + "/" + "12" + "/" + "30";
            }
            else
            {
                tdateS = fYear.ToString() + "/" + "12" + "/" + "29";
            }

            return (fdateS + "_" + tdateS);
        }

        public string GetCurrentMounth()
        {
            int[] Kabise = new int[]{1399,1403,1408,1412,1416,1420,1424,
                      1428,1432,1436,1441,1445,1449,1453,
                      1457,1461,1465,1469,1474,1478,1482,
                      1486,1490,1494,1498 };

            var fYear = GetDatePersian().Substring(0, 4);
            var fmount = GetDatePersian().Substring(5, 2);

            string fdateS = $"{fYear}/{fmount}/01";
            string tdateS = "";

            int mou = Int32.Parse(fmount);
            switch (mou)
            {
                case int n when (n > 0 && n <= 6):
                    tdateS = $"{fYear}/{fmount}/31";
                    break;
                case int n when (n > 6 && n <=11):
                    tdateS = $"{fYear}/{fmount}/30";
                    break;
                case int n when (n == 12):
                    if (Kabise.Contains(Int32.Parse(fYear)))
                    {
                        tdateS = $"{fYear}/{fmount}/30";
                    }
                    else
                    {
                        tdateS = $"{fYear}/{fmount}/29";
                    }
                    break;
                  
            }
            return ($"{fdateS}_{tdateS}");
        }

        public string GetCurrentWeek()
        {
            
            DateTime today = DateTime.Today;
                        PersianCalendar pc = new PersianCalendar();
            DayOfWeek dayOfWeek = pc.GetDayOfWeek(today); 

           // فاصله روز جاری با شنبه جاری رو محاسبه میکنه
            int daysToSaturday = ((int)dayOfWeek + 1) % 7;

          
            DateTime saturday = today.AddDays(-daysToSaturday);
                      
            DateTime friday = saturday.AddDays(6);


            string fromDate = $"{pc.GetYear(saturday):0000}/{pc.GetMonth(saturday):00}/{pc.GetDayOfMonth(saturday):00}";
            string toDate = $"{pc.GetYear(friday):0000}/{pc.GetMonth(friday):00}/{pc.GetDayOfMonth(friday):00}";

            return $"{fromDate}_{toDate}";

            
        }
    }
}
