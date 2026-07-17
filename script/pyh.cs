using System;
using System.Text;

namespace AmongUsFilterMod
{
    public static class PinyinHelper
    {
        // 转换为全拼
        public static string GetPinyin(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
            {
                if (c >= 0x4e00 && c <= 0x9fbb) // 简单汉字区间判断
                {
                    string pinyin = GetCharPinyin(c);
                    sb.Append(pinyin);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().ToLower();
        }

        // 转换为首字母缩写
        public static string GetPinyinAbbr(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
            {
                if (c >= 0x4e00 && c <= 0x9fbb)
                {
                    string pinyin = GetCharPinyin(c);
                    if (pinyin.Length > 0) sb.Append(pinyin[0]);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().ToLower();
        }

        // 基础汉字转拼音逻辑
        private static string GetCharPinyin(char c)
        {
            try
            {
                byte[] arr = Encoding.GetEncoding("GB2312").GetBytes(c.ToString());
                if (arr.Length < 2) return c.ToString();
                int value = (arr[0] << 8) + arr[1];

                if (value >= 45217 && value <= 45252) return "a";
                if (value >= 45253 && value <= 45760) return "b";
                if (value >= 45761 && value <= 46317) return "c";
                if (value >= 46318 && value <= 46825) return "d";
                if (value >= 46826 && value <= 47009) return "e";
                if (value >= 47010 && value <= 47296) return "f";
                if (value >= 47297 && value <= 47613) return "g";
                if (value >= 47614 && value <= 48118) return "h";
                if (value >= 48119 && value <= 49061) return "j";
                if (value >= 49062 && value <= 49323) return "k";
                if (value >= 49324 && value <= 49895) return "l";
                if (value >= 49896 && value <= 50370) return "m";
                if (value >= 50371 && value <= 50613) return "n";
                if (value >= 50614 && value <= 50621) return "o";
                if (value >= 50622 && value <= 50905) return "p";
                if (value >= 50906 && value <= 51386) return "q";
                if (value >= 51387 && value <= 51445) return "r";
                if (value >= 51446 && value <= 52217) return "s";
                if (value >= 52218 && value <= 52697) return "t";
                if (value >= 52698 && value <= 52979) return "w";
                if (value >= 52980 && value <= 53640) return "x";
                if (value >= 53641 && value <= 54480) return "y";
                if (value >= 54481 && value <= 55289) return "z";
            }
            catch { }
            return c.ToString();
        }
    }
}