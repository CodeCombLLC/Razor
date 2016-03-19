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
        public static string substring(this string self, int begin, int end)
        {
            return self.Substring(begin, end - begin);
        }

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
                if (!char.IsLetter(x) && !char.IsWhiteSpace(x))
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
            var start = '\0';
            for (var index = 0; index < code.Length && !hasFailedMatching; ++index)
            {
                if (allowString)
                {
                    if (innerString)
                    {
                        matchedIndex[index] = -2;
                        if (!escapeChar && code[index] == start)
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
                        else
                        {
                            brackets.Push(index);
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
                        else
                        {
                            hasFailedMatching = true;
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
                        razorList.Add(code.substring(index, nextIndex + 1));
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
                            razorList.Add(code.substring(index, nextIndex + 1));
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

        public static int AnalyzeDom(Dom root, string Code, int sta, int[] pos, CodeType nowType, bool flag)
        {
            Dom now = new Dom { Type = nowType };
            int end;
            if (nowType == CodeType.Html)
            {
                end = pos[sta]; // '<' at sta, '>' at end
                if (Code[sta] != '<' || Code[end] != '>')
                    throw new Exception();
                if (sta + 2 < end && " /".Equals(Code.substring(end - 2, end)) == true // match <someTag />
                || sta + 5 < end && "!--".Equals(Code.substring(sta + 1, sta + 4)) == true && "--".Equals(Code.substring(end - 2, end)) == true)
                { // match <!--...--> (may ignored)
                    now.Begin = Code.substring(sta, end + 1);
                }
                else {
                    // may match <someTag>
                    int tempIndex = Code.IndexOf(' ', sta + 2); // fetch someTag : <someTag> or <someTag ...>
                    var someTag = (tempIndex != -1 && tempIndex < end) ? Code.substring(sta + 1, tempIndex) : Code.substring(sta + 1, end);
                    if (IsTag(someTag) == false)
                    {
                        throw new Exception("Syntax Error.");
                    }
                    now.Begin = Code.substring(sta, end + 1); // <someTag...>
                                                                // then try to find child and </someTag>
                    for (int index = end + 1, nextIndex = -1; index < Code.Length; index = nextIndex + 1)
                    { // find now.child
                        char tempChar = Code[index];
                        if (tempChar == '<')
                        { // match HTML child or </someTag>
                            if (index + 1 < Code.Length && Code[index + 1]== '/')
                            { // match </someTag>
                                tempIndex = pos[index]; // fetch someTag and check it
                                if (tempIndex < 0 || someTag.Equals(Code.substring(index + 2, tempIndex)) == false)
                                {
                                    throw new Exception("Syntax Error.");
                                }
                                end = tempIndex;
                                now.End = Code.substring(index, end + 1); // <someTag ...> ... </someTag>
                                break;
                            }
                            else { // match HTML child
                                nextIndex = AnalyzeDom(now, Code, index, pos, CodeType.Html, false);
                            }
                        }
                        else if (tempChar == '@')
                        { // match Razor child
                            nextIndex = AnalyzeDom(now, Code, index, pos, CodeType.Razor, false);
                        }
                        else { // match InnerText child
                            nextIndex = AnalyzeDom(now, Code, index, pos, CodeType.Text, false);
                        }
                    }
                }
                root.AppendChild(now);
            }
            else if (nowType == CodeType.Razor)
            { // may not start at @
                if (!(flag == true || Code[sta] == '@' && sta + 1 < Code.Length))
                    throw new Exception();
                var isIfElse = false;
                if (flag == false && Code[sta + 1] == '*')
                { // rule 7 : @*...*@
                    end = Code.IndexOf("*@", sta + 2);
                    if (end == -1)
                    {
                        throw new Exception("Syntax Error.");
                    }
                    ++end;
                    now.Begin = Code.substring(sta, end + 1);  // @*...*@ (may ignored)
                }
                else if (flag == false && Code[sta + 1] == '@')
                { // rule 3 : @@
                    end = sta + 1;
                    now.Begin = Code.substring(sta, end + 1); // @@
                }
                else { // other rules
                    if (flag == true)
                    {
                        --sta; // for assuming sta+1 is the first index after @
                    }
                    int tempIndex = Code.Length - 1;
                    for (int index = sta + 1; index < Code.Length; ++index)
                    { // find the first non-blank char after @
                        if (char.IsWhiteSpace(Code[index]) == false)
                        {
                            // the first position after space
                            tempIndex = index;
                            break;
                        }
                    }
                    char tempChar = Code[tempIndex];
                    if (char.IsLetter(tempChar) == true || tempChar == '$' || tempChar == '_')
                    { // rule 1 & 5 (contain rule 6)
                        end = Code.Length - 1;
                        // find the first variable name
                        for (int index = sta + 1; index < Code.Length; ++index)
                        {
                            tempChar = Code[index];
                            if (char.IsLetter(tempChar) == false && char.IsDigit(tempChar) == false && tempChar != '_' && tempChar != '$')
                            {
                                end = index - 1;
                                break;
                            }
                        }
                        String variable = Code.substring(sta + 1, end + 1);
                        if ("if".Equals(variable) == true || "else".Equals(variable) == true || "for".Equals(variable) == true || "foreach".Equals(variable) || "while".Equals(variable) == true || "do".Equals(variable) == true)
                        {
                            // rule 5 : for() {...} or if(expression) {...} else {...}
                            int leftIndex = Code.IndexOf('{', end + 1);
                            while (leftIndex != -1 && pos[leftIndex] == -2)
                            {
                                leftIndex = Code.IndexOf('{', leftIndex + 1);
                            }
                            if (leftIndex == -1)
                            {
                                throw new Exception("Syntax Error.");
                            }
                            int rightIndex = pos[leftIndex];
                            now.Begin = Code.substring(tempIndex, leftIndex + 1);
                            for (int index = leftIndex + 1, nextIndex; index < rightIndex; index = nextIndex + 1)
                            {
                                tempChar = Code[index];
                                if (char.IsWhiteSpace(tempChar) == true)
                                { // ignore space
                                    nextIndex = index;
                                }
                                else if (tempChar == '<')
                                { // HTML
                                    nextIndex = AnalyzeDom(now, Code, index, pos, CodeType.Html, false);
                                }
                                else { // no innerText, all is razor
                                    nextIndex = AnalyzeDom(now, Code, index, pos, CodeType.Razor, true);
                                }
                            }
                            if ("if".Equals(variable) == true)
                            { // check "else" and "else if"
                                isIfElse = true;
                                end = rightIndex;
                                now.End = Code.substring(rightIndex, end + 1);
                                root.AppendChild(now);
                                // prestore if-else-if in nowList.children
                                for (int index = rightIndex + 1; index < Code.Length; index = rightIndex + 1)
                                {
                                    // find a whole word "else"
                                    tempIndex = Code.IndexOf("else", index);
                                    while (tempIndex != -1)
                                    {
                                        if (tempIndex + 4 < Code.Length)
                                        {
                                            tempChar = Code[tempIndex + 4];
                                            if (char.IsLetter(tempChar) == false && char.IsDigit(tempChar) == false && tempChar != '_' && tempChar != '$')
                                            {
                                                break;
                                            }
                                        }
                                        tempIndex = Code.IndexOf("else", tempIndex + 4);
                                    }
                                    if (tempIndex != -1)
                                    {
                                        // add "else" or "else if" to nowList
                                        rightIndex = AnalyzeDom(root, Code, tempIndex, pos, CodeType.Razor, true);
                                        // check "else if" format
                                        leftIndex = Code.IndexOf('{', tempIndex + 4);
                                        while (leftIndex != -1 && pos[leftIndex] == -2)
                                        {
                                            leftIndex = Code.IndexOf('{', leftIndex + 1);
                                        }
                                        /* if(leftIndex == -1) {
                                            throw new Exception("cheng xu chu bug la!");
                                        } */
                                        int tempIndex2 = Code.Length - 1;
                                        for (int index2 = tempIndex + 4; index2 < Code.Length; ++index2)
                                        {
                                            if (char.IsWhiteSpace(Code[index2]) == false)
                                            {
                                                tempIndex2 = index2;
                                                break;
                                            }
                                        }
                                        if (tempIndex2 + 1 >= leftIndex || "if".Equals(Code.substring(tempIndex2, tempIndex + 2)) == false)
                                        {
                                            // not "else if", end the condition
                                            end = rightIndex;
                                            break;
                                        }
                                    }
                                    else {
                                        break;
                                    }
                                }
                            }
                            else if ("do".Equals(variable) == true)
                            { // check "while (...)"
                                tempIndex = Code.IndexOf('(', rightIndex + 1);
                                while (tempIndex != -1 && pos[tempIndex] == -2)
                                {
                                    tempIndex = Code.IndexOf('(', tempIndex + 1);
                                }
                                if (tempIndex == -1)
                                {
                                    throw new Exception("Syntax Error.");
                                }
                                end = pos[tempIndex];
                                now.End = Code.substring(rightIndex, end + 1);
                            }
                            else {
                                end = rightIndex;
                                now.End = Code.substring(rightIndex, end + 1);
                            }
                        }
                        else {
                            // rule 1 : @variable[index].method(parameters)
                            if (end + 1 < Code.Length && Code[end + 1] == '[')
                            { // variable[index]
                                end = pos[end + 1];
                            }
                            if (end + 1 < Code.Length && Code[end + 1] == '.')
                            { // object.property or object.method
                                ++end;
                                tempIndex = Code.Length - 1;
                                for (int index = end + 1; index < Code.Length; ++index)
                                {
                                    tempChar = Code[index];
                                    if (char.IsLetter(tempChar) == false && char.IsDigit(tempChar) == false && tempChar != '_' && tempChar != '$')
                                    {
                                        tempIndex = index - 1;
                                        break;
                                    }
                                }
                                end = tempIndex;
                                tempIndex = Code.Length - 1;
                                for (int index = end + 1; index < Code.Length; ++index)
                                {
                                    if (char.IsWhiteSpace(Code[index]) == false)
                                    {
                                        tempIndex = index;
                                        break;
                                    }
                                }
                                if (Code[tempIndex] == '(')
                                { // object.method()
                                    int leftIndex = tempIndex, rightIndex = pos[leftIndex];
                                    end = rightIndex;
                                    tempIndex = Code.IndexOf('{', leftIndex + 1);
                                    while (tempIndex != -1 && tempIndex < rightIndex && pos[tempIndex] == -2)
                                    {
                                        tempIndex = Code.IndexOf('{', tempIndex + 1);
                                    }
                                    if (tempIndex != -1 && tempIndex < rightIndex)
                                    { // object.method( {...} ) like loop
                                        leftIndex = tempIndex;
                                        rightIndex = pos[tempIndex];
                                        now.Begin = Code.substring(sta + 1, leftIndex + 1);
                                        for (int index = leftIndex + 1, nextIndex = -1; index < rightIndex; index = nextIndex + 1)
                                        {
                                            tempChar = Code[index];
                                            if (char.IsWhiteSpace(tempChar) == true)
                                            { // ignore space
                                                nextIndex = index;
                                            }
                                            else if (tempChar == '<')
                                            { // HTML
                                                nextIndex = AnalyzeDom(now, Code, index, pos, CodeType.Html, false);
                                            }
                                            else { // no innerText, all is razor
                                                nextIndex = AnalyzeDom(now, Code, index, pos, CodeType.Razor, true);
                                            }
                                        }
                                        now.End = Code.substring(rightIndex, end + 1);
                                    }
                                    else { // object.method(...)
                                        now.Begin = Code.substring(sta + 1, end + 1);
                                    }
                                }
                                else { // object.property
                                    now.Begin = Code.substring(sta + 1, end + 1);
                                }
                            }
                            else {
                                now.Begin = Code.substring(sta + 1, end + 1);
                            }
                        }
                    }
                    else if (tempChar == '(')
                    { // rule 2 : @(expression) (mind the "({})")
                      // ( {...} ) not need to split
                        end = pos[tempIndex];
                        now.Begin = Code.substring(tempIndex, end + 1); // (expression)
                    }
                    else if (tempChar == '{')
                    { // rule 4 : @{JS Code}
                      // {...} not need to split
                        end = pos[tempIndex];
                        now.Begin = Code.substring(tempIndex, end + 1);
                    }
                    else {
                        throw new Exception("Syntax Error.");
                    }
                }
                if (isIfElse == false)
                {
                    root.AppendChild(now);
                }
            }
            else { // nowType == CodeType.Text
                if (!(Code[sta] != '<' && Code[sta] != '@'))
                    throw new Exception();
                int tempIndex1 = Code.IndexOf('<', sta + 1), tempIndex2 = Code.IndexOf('@', sta + 1);
                while (tempIndex1 != -1 && pos[tempIndex1] < 0)
                { // skip comparison '<' and inner '<'
                    tempIndex1 = Code.IndexOf('<', tempIndex1 + 1);
                }
                // find minimum in {IndexOfMatched('<'), IndexOf('@'), Length}
                end = Code.Length - 1;
                if (tempIndex1 != -1)
                {
                    end = Math.Min(end, tempIndex1 - 1);
                }
                if (tempIndex2 != -1)
                {
                    end = Math.Min(end, tempIndex2 - 1);
                }
                now.Begin = Code.substring(sta, end + 1);
                root.AppendChild(now);
            }
            return end;
        }

        public static Dom AnalyzeDom(string Code) 
        {
            int[] pos = BracketMatching(Code, true);
            Dom root = new Dom();
            for (int index = 0, nextIndex; index < Code.Length; index = nextIndex + 1)
            {
                char tempChar = Code[index];
                if (char.IsWhiteSpace(tempChar) == true)
                { // ignore space
                    nextIndex = index;
                }
                else if (tempChar == '<')
                {
                    nextIndex = AnalyzeDom(root, Code, index, pos, CodeType.Html, false);
                }
                else if (tempChar == '@')
                {
                    nextIndex = AnalyzeDom(root, Code, index, pos, CodeType.Razor, false);
                }
                else {
                    nextIndex = AnalyzeDom(root, Code, index, pos, CodeType.Text, false);
                    // throw new Exception("Syntax Error");
                }
            }
            return root;
	}
    }
}
