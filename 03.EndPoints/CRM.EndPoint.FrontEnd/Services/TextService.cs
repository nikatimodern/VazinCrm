using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CRM.EndPoint.FrontEnd.Services
{
    public static class TextService
    {
        public static string ToCamelJson(this object obj)
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }



        static string[] ones = { "", "یک", "دو", "سه", "چهار", "پنج", "شش", "هفت", "هشت", "نه", "ده", "یازده", "دوازده", "سیزده", "چهارده", "پانزده", "شانزده", "هفده", "هجده", "نوزده" };
        static string[] tens = { "", "", "بیست", "سی", "چهل", "پنجاه", "شصت", "هفتاد", "هشتاد", "نود" };
        static string[] hundreds = { "", "یکصد", "دویست", "سیصد", "چهارصد", "پانصد", "ششصد", "هفتصد", "هشتصد", "نهصد" };

        public static string NumberToWords(decimal number)
        {
            if (number == 0)
                return "صفر";

            if (number < 0)
                return "منفی " + NumberToWords(Math.Abs(number));

            string words = "";

            // برای تریلیارد
            if (number / 1000000000000000 >= 1)
            {
                words += NumberToWords((long)(number / 1000000000000000)) + " تریلیارد ";
                number %= 1000000000000000;
            }

            // برای تریلیون
            if (number / 1000000000000 >= 1)
            {
                words += NumberToWords((long)(number / 1000000000000)) + " تریلیون ";
                number %= 1000000000000;
            }

            // برای میلیارد
            if (number / 1000000000 >= 1)
            {
                words += NumberToWords((long)(number / 1000000000)) + " میلیارد ";
                number %= 1000000000;
            }

            // برای میلیون
            if (number / 1000000 >= 1)
            {
                words += NumberToWords((long)(number / 1000000)) + " میلیون ";
                number %= 1000000;
            }

            // برای هزار
            if (number / 1000 >= 1)
            {
                words += NumberToWords((long)(number / 1000)) + " هزار ";
                number %= 1000;
            }

            // برای صد
            if (number / 100 >= 1)
            {
                words += hundreds[(int)(number / 100)] + " ";
                number %= 100;
            }

            // برای اعداد کمتر از صد
            if (number > 0)
            {
                if (words != "")
                    words += "و ";

                if (number < 20)
                    words += ones[(int)number];
                else
                {
                    words += tens[(int)(number / 10)];
                    if ((number % 10) > 0)
                        words += " و " + ones[(int)(number % 10)];
                }
            }

            return words.Trim();
        }

        // تابع برای رسیدگی به اعشار
        public static string ConvertDecimalToWords(decimal? number)
        {
            // جدا کردن قسمت صحیح و اعشار
            decimal integerPart = Math.Floor(number.Value);
            decimal fractionalPart = number.Value - integerPart;

            string result = NumberToWords(integerPart);

            if (fractionalPart > 0)
            {
                // تبدیل بخش اعشاری به رشته
                string fractionalString = ((fractionalPart * 100).ToString("0")).TrimEnd('0'); // مثلا دو رقم اعشار
                result += " ممیز " + NumberToWords(decimal.Parse(fractionalString));
            }

            return result;
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


        public static int convertToInt(string val)
        {
            try
            {
                return Convert.ToInt32(val);
            }
            catch
            {
                return 0;
            }
        }

        public static int ExtractNumber(string val)
        {
            if (string.IsNullOrWhiteSpace(val))
                return 0;

            var match = Regex.Match(val, @"\d+");
            return int.TryParse(match.Value, out var result)
                ? result
                : 0;
        }



        public static string NormalizeText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = input.Trim();

            // جایگزینی حروف عربی به فارسی
            input = input
                .Replace("آ", "ا")
                .Replace("أ", "ا")
                .Replace("إ", "ا")
                .Replace("ي", "ی")
                .Replace("ئ", "ی")
                .Replace("ى", "ی")
                .Replace("ك", "ک")
                .Replace("ؤ", "و")
                .Replace("ۀ", "ه")
                .Replace("ة", "ه")
                .Replace("‌", " ") // ZWNJ → space
                .Replace("\u200C", " ") // ZWNJ → space
                .Replace("\u200F", "") // حذف RLM
                .Replace("\u200E", ""); // حذف LRM

            // حذف فاصله‌های تکراری
            while (input.Contains("  "))
                input = input.Replace("  ", " ");

            // تبدیل اعداد فارسی و عربی به انگلیسی
            var enDigits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            var faDigits = new[] { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };
            var arDigits = new[] { '٠', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩' };

            for (int i = 0; i < 10; i++)
            {
                input = input.Replace(faDigits[i], enDigits[i])
                    .Replace(arDigits[i], enDigits[i]);
            }

            return input;
        }


    }


}
