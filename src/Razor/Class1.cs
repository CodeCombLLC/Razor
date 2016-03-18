using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Razor
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public static class Analyze
    {
        /// <summary>
        /// 判断一个字符串是否可以符合标签起名的标准
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>是否符合变量起名的标准</returns>
        public static bool IsTag(string str)
        {
            if (str.Length == 0)
                return false;
            foreach (var x in str)
                if (!char.IsLetter(x))
                    return false;
            return true;
        }

        /// <summary>
        /// 判断一个字符串是否可以符合变量起名的标准
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>是否符合变量起名的标准</returns>
        public static bool IsVariable(string str)
        {
            if (str.Length == 0)
                return false;
            if (char.IsDigit(str[0]))
                return false;
            foreach (var x in str)
                if (!char.IsLetter(x) && !char.IsDigit(x) && !"_$".Contains(x))
                    return false;
            return true;
        }

        /// <summary>
        /// 将代码中的括号进行匹配，合法则返回匹配的位置数组，不合法则抛出异常
        /// </summary>
        /// <param name="code">代码</param>
        /// <param name="allowString"></param>
        /// <returns>位置数组</returns>
        public static int[] BracketMatching(string code, bool allowString)
        {
            var matchedIndex = new int[code.Length];
            var brackets = new Stack<int>();
            var hasFailedMatching = false;
            var innerString = false;
            var escapeChar = false;
            char start = '\0';
            for (var index = 0; index < code.Length && !hasFailedMatching; ++index)
            {
                if (allowString)
                {
                    if (innerString)
                    {
                        matchedIndex[index] = -2;
                        if (!escapeChar)
                            innerString = false;
                        else
                            escapeChar = false;
                        continue;
                    }
                    else if (code[index] == '\'' || code[index] == '"')
                    {
                        start = code[index];
                        innerString = true;
                        matchedIndex[index] = -2;
                        continue;
                    }
                }
                // consider bracket
                matchedIndex[index] = -1;
                switch (code[index])
                {
                    case '(':
                    case '[':
                    case '{':
                        if (brackets.Count > 0)
                        {
                            var tempIndex = brackets.Peek();
                            if (code[tempIndex] == '[' && code[index] == '{')
                            {
                                hasFailedMatching = true;
                            }
                            else if (code[tempIndex] != '<')
                            {
                                brackets.Push(index);
                            } // else ignored : <...>
                        }
                        break;
                    case '<':
                        if (brackets.Count == 0 || code[brackets.Peek()] == '{')
                            brackets.Push(index);
                        break;
                    case '>':
                        if (brackets.Count > 0)
                        {
                            var tempIndex = brackets.Peek();
                            if (code[tempIndex] == '<')
                            {
                                matchedIndex[tempIndex] = index;
                                matchedIndex[index] = tempIndex;
                                brackets.Pop();
                            }
                            else if (code[tempIndex] == '{')
                            {
                                hasFailedMatching = true;
                            }
                        }
                        else
                        {
                            hasFailedMatching = true;
                        }
                        break;
                    case ')':
                    case ']':
                    case '}':
                        if (brackets.Count > 0)
                        {
                            var tempIndex = brackets.Peek();
                            var matchedChar = (char)(code[index] - (code[index] == ')' ? 1 : 2));
                            if (code[tempIndex] == matchedChar)
                            {
                                matchedIndex[tempIndex] = index;
                                matchedIndex[index] = tempIndex;
                                brackets.Pop();
                            }
                            else
                            {
                                hasFailedMatching = true;
                            }
                        }
                        break;
                }
            }
            if (!hasFailedMatching && brackets.Count == 0)
                return matchedIndex;
            else
                throw new Exception("Mismatched bracket or syntax error.");
        }

        /// <summary>
        /// 输入一个HTML的标签，给出标签中的Razor信息
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static List<string> PullRazorInHtml(string code)
        {
            if (code.Length < 5 || code.First() != '<' || code.Last() != '>')
                throw new Exception();
            var pos = BracketMatching(code, false);
            var razorList = new List<string>();
            for (int index = 0, nextIndex = -1; index < code.Length; index = nextIndex + 1)
            {
                index = code.IndexOf('@', index);
                if (index < 0)
                    break;
                ++index;
                if (index < code.Length && code[index] == '@')
                {
                    razorList.Add("@");
                }
                else if (index < code.Length)
                {
                    var tempIndex = code.Length - 1;
                    for (var index2 = index; index2 < code.Length; ++index2)
                    {
                        if (!char.IsWhiteSpace(code[index2]))
                        {
                            tempIndex = index2;
                            break;
                        }
                    }
                    index = tempIndex;
                    if (code[tempIndex] == '(')
                    {
                        nextIndex = pos[index];
                        razorList.Add(code.Substring(index, nextIndex + 1));
                    }
                    else
                    {
                        var tempChar = code[index];
                        if (char.IsLetter(tempChar) || "$_".Contains(tempChar))
                        {
                            nextIndex = code.Length - 1;
                            for (var index2 = index + 1; index2 < code.Length; ++index2)
                            {
                                tempChar = code[index2];
                                if (!char.IsLetter(tempChar) && !char.IsDigit(tempChar) && !"$_".Contains(tempChar))
                                {
                                    nextIndex = index2 - 1;
                                    break;
                                }
                            }
                            if (nextIndex + 1 < code.Length && code[nextIndex + 1] == '[')
                            {
                                nextIndex = pos[nextIndex + 1];
                            }
                            if (nextIndex + 1 < code.Length && code[nextIndex + 1] == '.')
                            {
                                ++nextIndex;
                                tempIndex = code.Length - 1;
                                for (var index2 = nextIndex + 1; index2 < code.Length; ++index2)
                                {
                                    tempChar = code[index2];
                                    if (!char.IsLetter(tempChar) && !char.IsDigit(tempChar) && !"$_".Contains(tempChar))
                                    {
                                        tempIndex = index2 - 1;
                                        break;
                                    }
                                }
                                nextIndex = tempIndex;
                                tempIndex = code.Length - 1;
                                for (var index2 = nextIndex; index2 < code.Length; ++index2)
                                {
                                    if (!char.IsWhiteSpace(code[index2]))
                                    {
                                        tempIndex = index2;
                                        break;
                                    }
                                }
                                if (code[tempIndex] == '(')
                                {
                                    nextIndex = pos[tempIndex];
                                }
                            }
                            razorList.Add(code.Substring(index, nextIndex + 1));
                        }
                        else
                        {
                            throw new Exception("Syntax Error.");
                        }
                    }
                }
                else
                {
                    throw new Exception("Syntax Error.");
                }
            }
            return razorList;
        }

        public static int AnalyzeHtml()
        {
        }
    }
}
