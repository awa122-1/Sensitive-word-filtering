using System;
using System.Collections.Generic;
using System.Text;

namespace AmongUsFilterMod
{
    public class DfaNode
    {
        // 关键修复：字典的 Key 必须是 char 字符，而不是 string
        public Dictionary<char, DfaNode> Children = new Dictionary<char, DfaNode>();
        public bool IsEnd = false;
    }

    public class NewDfaFilter
    {
        private readonly DfaNode _root = new DfaNode();

        // 添加敏感词到 DFA 树
        public void AddWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return;

            // 自动提取原文、拼音全拼、拼音首字母
            string raw = word.Trim().ToLower();
            string pinyin = PinyinHelper.GetPinyin(raw);
            string pinyinAbbr = PinyinHelper.GetPinyinAbbr(raw);

            InsertNode(raw);
            if (pinyin != raw) InsertNode(pinyin);
            if (pinyinAbbr != raw && pinyinAbbr != pinyin) InsertNode(pinyinAbbr);
        }

        private void InsertNode(string txt)
        {
            var current = _root;
            foreach (char c in txt)
            {
                // 关键修复：使用 char 传入
                if (!current.Children.ContainsKey(c))
                {
                    current.Children[c] = new DfaNode();
                }
                current = current.Children[c];
            }
            current.IsEnd = true;
        }

        // 检查文本是否包含敏感词
        public bool ContainsSensitiveWord(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            // 归一化清洗：转小写，并移除空格及常见的干扰字符（如 *, @, 间隔符等）
            string cleanText = CleanInput(text);

            for (int i = 0; i < cleanText.Length; i++)
            {
                var current = _root;
                for (int j = i; j < cleanText.Length; j++)
                {
                    char c = cleanText[j]; // 关键修复：转为 char 逐字检索
                    if (!current.Children.TryGetValue(c, out var nextNode))
                    {
                        break;
                    }
                    if (nextNode.IsEnd)
                    {
                        return true; // 命中敏感词结构
                    }
                    current = nextNode;
                }
            }
            return false;
        }

        private string CleanInput(string input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in input.ToLower())
            {
                // 只保留字母、数字和汉字，剔除标点符号和空格干扰
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
