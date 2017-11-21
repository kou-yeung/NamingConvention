// WhatIsThisツールの Entities を定義されます
// 主に XML ファイルの構造を定義されます

using System.Text.RegularExpressions;

namespace NamingConvention.Detail
{
    public class Patterns
    {
        public Pattern[] patterns;
    }
    public class Pattern
    {
        public string match;    // マッチ式
        public Group[] groups;

        Regex regex;

        // 式をキャッシュするためのメソッド提供です。直接に match メンバーを使用してもよし
        public Match Match(string str)
        {
            if (regex == null) regex = new Regex(match);
            return regex.Match(str);
        }
    }
    public class Group
    {
        public int index;       // Group番号
        public string equal;    // 等価文字列
        public string value;    // 出力する文字列
    }
}
