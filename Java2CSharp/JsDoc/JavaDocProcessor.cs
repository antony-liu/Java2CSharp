using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Java2CSharp.JsDoc
{
    public class JavaDocComment
    {
        private string comment;
        private readonly List<string> lines = new();
        private int index = 0;
        private readonly StringBuilder sbMain = new ();
        private readonly StringBuilder sbRemark = new ();
        private readonly Regex regLink = new (@"{@link\s*\*?\s*#?(\w*)}", RegexOptions.Compiled);
        private readonly Regex regLink2 = new(@"{@link\s*([^#]*)?#?(.*?)}", RegexOptions.Compiled);
        private readonly Regex regMultiLineLink = new(@"{@link\s*(///)?\s*#?(\w*)}", RegexOptions.Compiled);
        private readonly Regex regCode = new (@"{@code\s*\*?\s*(\w*)}", RegexOptions.Compiled);
        private readonly Regex regCode2 = new(@"{@code\s*(.*?)\s*}", RegexOptions.Compiled);
        private readonly Regex regSee = new("@see\\s*([^#]*)?#?(.*)", RegexOptions.Compiled);
        private readonly Regex regSeeATag = new("<a href=\"(.*?)\">(.*?)</a>", RegexOptions.Compiled|RegexOptions.Singleline);

        private void RemoveBlankLineBeginTag(string beginTag, string endTag)
        {
            List<int> toRemove = new();
            bool inTag = false;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line.IndexOf("<ul>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    inTag = true;
                }

                if (inTag && string.IsNullOrEmpty(line.Trim()))
                {
                    toRemove.Add(i);
                }

                if (line.IndexOf("</ul>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    inTag = false;
                }
            }

            if (toRemove.Count > 0)
            {
                toRemove.Reverse();
                foreach (var x in toRemove)
                {
                    lines.RemoveAt(x);
                }
            }
        }
        private void ProcessULTag()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line.IndexOf("<ul>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    line = line.Replace("<ul>", "<list type=\"bullet\">", StringComparison.OrdinalIgnoreCase);
                }
                if (line.IndexOf("</ul>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    line = line.Replace("</ul>", "</list>", StringComparison.OrdinalIgnoreCase);
                }
                lines[i] = line;
            }
        }

        private void ProcessOLTag()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line.IndexOf("<ol>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    line = line.Replace("<ol>", "<list type=\"number\">", StringComparison.OrdinalIgnoreCase);
                }
                if (line.IndexOf("</ol>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    line = line.Replace("</ol>", "</list>", StringComparison.OrdinalIgnoreCase);
                }
                lines[i] = line;
            }
        }

        private void ProcessPTag()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line.IndexOf("<p>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    line = line.Replace("<p>", " \r\n/* ", StringComparison.OrdinalIgnoreCase);
                }
                if (line.IndexOf("</p>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    line = line.Replace("</p>", "\r\n/* ", StringComparison.OrdinalIgnoreCase);
                }
                lines[i] = line;
            }
        }

        private void ProcessPreTag()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line.IndexOf("<pre>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    line = line.Replace("<pre>", "<code>", StringComparison.OrdinalIgnoreCase);
                }
                if (line.IndexOf("</pre>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    line = line.Replace("</pre>", "</code>", StringComparison.OrdinalIgnoreCase);
                }
                lines[i] = line;
            }
        }

        private void ProcessCodeTag()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line.IndexOf("<code>", StringComparison.OrdinalIgnoreCase) >= 0
                    && line.IndexOf("</code>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    line = line.Replace("<code>", "<c>", StringComparison.OrdinalIgnoreCase)
                        .Replace("</code>", "</c>", StringComparison.OrdinalIgnoreCase);
                }
                lines[i] = line;
            }
        }

        private void ProcessLITag()
        {
            bool liTag = false;
            bool inListTag = false;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (!inListTag) 
                {
                    inListTag = line.IndexOf("<ul>", StringComparison.OrdinalIgnoreCase) >= 0|| 
                        line.IndexOf("<ol>", StringComparison.OrdinalIgnoreCase) >= 0;
                    lines[i] = line;
                    continue;
                }
                if (inListTag)
                {
                    if (line.IndexOf("<li>", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        liTag = true;
                        line = line.Replace("<li>", "<item><description>", StringComparison.OrdinalIgnoreCase);
                    }
                    if (liTag && (string.IsNullOrEmpty(line.Trim()) || 
                        lines[i+1].IndexOf("<li>", StringComparison.OrdinalIgnoreCase) >=0)) //next line is <li>
                    {
                        line += "</description></item>";
                        liTag = false;
                    }
                    line = line.Replace("</li>", "", StringComparison.OrdinalIgnoreCase);
                    inListTag = line.IndexOf("</ul>", StringComparison.OrdinalIgnoreCase) == -1 && 
                        line.IndexOf("</ol>", StringComparison.OrdinalIgnoreCase) == -1;
                    if(!inListTag)
                    {
                        lines[i-1] = lines[i - 1] + "</description></item>";
                    }
                    lines[i] = line;
                }
                
            }
        }

        private string ProcessTableTag(string text)
        {
            int begin = text.IndexOf("<table ", StringComparison.OrdinalIgnoreCase);
            if (begin == -1)
                return text;
            int end = text.IndexOf("</table>", begin + 1, StringComparison.OrdinalIgnoreCase);
            if (end == -1) 
                throw new ArgumentException("文档错误");
            string tableString = text.Substring(begin, end - begin + 8);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(tableString);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<list type=\"table\">");
            var nodes = htmlDoc.DocumentNode.SelectNodes("//th");
            if(nodes.Count > 0)
            {
                WriteTableRow(sb, nodes, "listheader");
            }
            nodes = htmlDoc.DocumentNode.SelectNodes("//tr");
            if (nodes.Count > 0)
            {
                foreach (var node in nodes)
                {
                    WriteTableRow(sb, node.ChildNodes, "item");
                }
            }
            return text.Replace(tableString, sb.ToString());
        }

        private void WriteTableRow(StringBuilder sb, HtmlNodeCollection nodes, string rowTag)
        {
            sb.Append($"<{rowTag}>");
            foreach (var tdr in nodes)
            {
                if (nodes.IndexOf(tdr) == nodes.Count - 1)
                {
                    sb.Append("<description>").Append(tdr.InnerText).Append("</description>");
                }
                else
                {
                    sb.Append("<term>").Append(tdr.InnerText).Append("</term>");
                }
            }
            sb.AppendLine($"</{rowTag}>");
        }

        private void Clean()
        {
            index = 0;
            sbMain.Length = 0;
            sbRemark.Length = 0;
            lines.Clear();
            
            string newComment = comment.Replace("<p>", "\r", StringComparison.OrdinalIgnoreCase)
                .Replace("</p>", "\r", StringComparison.OrdinalIgnoreCase)
                .Replace("<p/>", "\r", StringComparison.OrdinalIgnoreCase);
            newComment = ProcessTableTag(newComment);
            string[] arrInput = newComment.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            if(arrInput.Length > 1 )
            {
                ParseLeadingBlank(arrInput[1]);
            }
            
            foreach (string line in arrInput)
            {
                int begin = line.IndexOf("/*");
                if (begin == -1) begin = line.IndexOf(" *");
                string x = begin >= 0 ? line.Substring(begin + 2) : line;
                x = x.TrimStart('*', '/');
                if (x.Length > 0 && x[0] == ' ')
                {
                    x = x.Substring(1);
                }
                lines.Add(x.TrimEnd());
            }
            ProcessPTag();
            ProcessLITag();
            ProcessOLTag();
            RemoveBlankLineBeginTag("<ul>", "</ul>");
            ProcessULTag();
            ProcessCodeTag();
            ProcessPreTag();

            if (lines.Count > 0 && string.IsNullOrEmpty(lines[^1]))
                lines.RemoveAt(lines.Count - 1);
            while(lines.Count > 0 && string.IsNullOrEmpty(lines[0]))
                lines.RemoveAt(0);

            //移除连续的空白行
            string temp = string.Join("@|@", lines.ToArray());
            temp = temp.Replace("@|@@|@@|@", "@|@@|@");
            lines.Clear();
            lines.AddRange(temp.Split("@|@", StringSplitOptions.None).ToList());
        }

        private void ParseSummary()
        {
            sbMain.AppendLine("/// <summary>");
            
            List<string> summaryLines = new ();
            int paraCount = 1;
            while (index < lines.Count)
            {
                string line = lines[index];
                if (line.StartsWith("@") && line.IndexOf("@see", StringComparison.OrdinalIgnoreCase) ==-1)
                    break;
                line = line.Replace("</p>", "\r\n", StringComparison.OrdinalIgnoreCase);
                if (line.Contains("@see"))
                {
                    line = ParseSeeTag(line);
                }
                summaryLines.Add(line);
                if (string.IsNullOrEmpty(line))
                {
                    paraCount++;
                }
            
                index++;
                if (index >= lines.Count)
                    break;
                string nextLine = lines[index];
                if (nextLine.StartsWith("@") && nextLine.IndexOf("@see", StringComparison.OrdinalIgnoreCase) == -1)
                    break;
            }
            if (summaryLines.Count > 0)
            {
                while (string.IsNullOrEmpty(summaryLines[summaryLines.Count - 1]))
                {
                    paraCount--;
                    summaryLines.RemoveAt(summaryLines.Count - 1);
                    if (summaryLines.Count == 0)
                        break;
                }
                int paraStart = sbMain.Length;
                bool nextBlankLineIsNewPara = true;
                bool inCode = false;
                foreach (var l in summaryLines)
                {
                    if(!inCode)
                    {
                        inCode = l.IndexOf("<code>", StringComparison.OrdinalIgnoreCase) > -1;
                    }
                    if (!inCode && string.IsNullOrEmpty(l) && nextBlankLineIsNewPara)
                    {
                        nextBlankLineIsNewPara = false;
                        sbMain.AppendLine("".PadLeft(leadingBlank) + "/// </para>");
                        sbMain.AppendLine("".PadLeft(leadingBlank) + "/// <para>");
                    }
                    else
                    {
                        if(!string.IsNullOrEmpty(l))
                            sbMain.AppendLine("".PadLeft(leadingBlank) + $"/// {l}");
                        nextBlankLineIsNewPara = true;
                    }
                    if (inCode)
                    {
                        inCode = !(l.IndexOf("</code>", StringComparison.OrdinalIgnoreCase) > -1);
                    }
                }
                if (paraCount > 1)
                {
                    sbMain.Insert(paraStart, "".PadLeft(leadingBlank) + "/// <para>\r\n", 1);
                    sbMain.AppendLine("".PadLeft(leadingBlank) + "/// </para>");
                }
            }

            sbMain.AppendLine("".PadLeft(leadingBlank) + "/// </summary>");
        }
        private string ParseSeeTag(string text)
        {
            if (text.IndexOf("@see", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (text.Contains("<a"))
                {
                    string seeText = text;
                    while (!seeText.Contains("</a>", StringComparison.OrdinalIgnoreCase))
                    {
                        index++;
                        seeText += " " + lines[index];
                    }
                    var m = regSeeATag.Match(seeText);
                    if (m.Success)
                        seeText = seeText.Replace(m.Value, $"<see href=\"{m.Groups[1].Value}\">{m.Groups[2].Value}</see>");
                    return seeText.Replace("@see", "").Trim();
                }
                else
                {
                    var m = regSee.Match(text);
                    if (m.Success)
                    {
                        bool part1 = !string.IsNullOrEmpty(m.Groups[1].Value);
                        bool part2 = !string.IsNullOrEmpty(m.Groups[2].Value);

                        if (part1 && part2)
                        {
                            text = regSee.Replace(text, "<see cref=\"$1.$2\" />");
                        }
                        if (part1 && !part2)
                        {
                            text = regSee.Replace(text, "<see cref=\"$1\" />");
                        }
                        if (!part1 && part2)
                        {
                            text = regSee.Replace(text, "<see cref=\"$2\" />");
                        }
                    }
                    return text.Replace("@see", "").Trim();
                }
            }
            return text;
        }

        public void Parse(string comment)
        {
            if (string.IsNullOrEmpty(comment)) { return; }
            this.comment = comment;
            Clean();
            Parse();
        }

        private int leadingBlank = 0;

        private void ParseLeadingBlank(string text)
        {
            int i = 0;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
            {
                i++;
            }
            leadingBlank = i / 4 * 4;
        }
        private Regex regBlankSummary = new Regex("/// <summary>(.*)/// </summary>", RegexOptions.Compiled | RegexOptions.Singleline);
        private Regex regReturns = new Regex("<returns>(.*)</returns>");
        private void Parse()
        {
            
            ParseSummary();
            while (index < lines.Count)
            {
                string text = lines[index];

                if (text.IndexOf("@param", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    //@param <name> <description statement>
                    
                    ProcessTagLineWithName(text, sbMain, index, "param");
                    index = ToTagEnd(lines, sbMain, index, "param");
                }
                else if (text.IndexOf("@return", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string newLine = text.Replace("@return", "").Trim();
                    sbMain.Append("".PadLeft(leadingBlank) + $"/// <returns>{newLine}");
                    //ProcessTagLine(text, sbMain, index, "return");
                    index = ToTagEnd(lines, sbMain, index, "returns");
                }
                else if (text.IndexOf("@since", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    index = ToRemark(lines, sbRemark, index);
                }
                else if (text.IndexOf("@author", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    index = ToRemark(lines, sbRemark, index);
                }
                else if (text.IndexOf("@version", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    index = ToRemark(lines, sbRemark, index);
                }
                else if (text.IndexOf("@deprecated", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    index = ToRemark(lines, sbRemark, index);
                }
                else if (text.IndexOf("@throws", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ProcessTagLineWithName(text, sbMain, index, "throws", "exception");
                    index = ToTagEnd(lines, sbMain, index, "exception");
                    //throw new NotImplementedException();
                }
                else if (text.IndexOf("@exception", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ProcessTagLineWithName(text, sbMain, index, "exception");
                    index = ToTagEnd(lines, sbMain, index, "exception");
                    //throw new NotImplementedException();
                }
                else
                {
                    sbMain.Append("".PadLeft(leadingBlank) + "/// ").AppendLine(text);
                }
                index++;
            }
            string newComment = regLink.Replace(sbMain.ToString(), "<see cref=\"$1\"/>");
            var mc = regLink2.Matches(newComment);
            foreach(Match m in mc)
            {
                if(m.Success)
                {
                    bool part1 = !string.IsNullOrEmpty(m.Groups[1].Value);
                    bool part2 = !string.IsNullOrEmpty(m.Groups[2].Value);

                    if (part1 && part2)
                    {
                        newComment = newComment.Replace(m.Value, $"<see cref=\"{m.Groups[1].Value}.{m.Groups[2].Value}\" />");
                    }
                    if (part1 && !part2)
                    {
                        newComment = newComment.Replace(m.Value, $"<see cref=\"{m.Groups[1].Value}\" />");
                    }
                    if (!part1 && part2)
                    {
                        newComment = newComment.Replace(m.Value, $"<see cref=\"{m.Groups[2].Value}\" />");
                    }
                }
            }

            newComment = regMultiLineLink.Replace(newComment, "<see cref=\"$2\"/>\r\n///");
            newComment = regCode2.Replace(newComment, "<c>$1</c>");
            newComment = regCode.Replace(newComment, "<c>$1</c>");
            mc = regBlankSummary.Matches(newComment);
            foreach(Match m in mc)
            {
                if (string.IsNullOrEmpty(m.Groups[1].Value.Trim()))
                {
                    var mm = regReturns.Match(newComment);
                    newComment = Regex.Replace(newComment, "/// <summary>(.*)/// </summary>",
                        "".PadLeft(leadingBlank) + $"/// <summary>\r\n/// {mm.Groups[1].Value}\r\n"
                        + "".PadLeft(leadingBlank) + "/// </summary>");
                    
                }
            }
            sbMain.Length = 0;
            sbMain.AppendLine(newComment.Trim());
        }


        private int ToTagEnd(List<string> lines, StringBuilder sb, int index, string tag)
        {
            int start = index;
            int ix = index + 1;

            while (ix < lines.Count)
            {
                string nextText = lines[ix].Trim().TrimStart('/', '*', ' ');
                if (string.IsNullOrEmpty(nextText))
                    break;
                if (nextText.StartsWith("@"))
                    break;
                sb.AppendLine().Append("".PadLeft(leadingBlank) + $"/// " + nextText);
                ix++;
            }
            index = ix - 1;
            if (string.IsNullOrEmpty(tag))
            {
                sb.AppendLine();
            }
            else
            {
                if (start == index)
                    sb.AppendLine($"</{tag}>");
                else
                    sb.AppendLine().AppendLine("".PadLeft(leadingBlank) + $"/// </{tag}>");
            }

            return index;
        }

        private void ProcessTagLineWithName(string text, StringBuilder sb, int index, string tag, string newTag = null)
        {
            if (newTag == null)
                newTag = tag;
            string newLine = text.Replace($"@{tag}", "").Trim();
            int nextBlank = newLine.IndexOf(" ");
            string paramName = newLine.Substring(0);
            string desc = string.Empty;
            if (nextBlank > 0)
            {
                paramName = newLine.Substring(0, nextBlank);
                desc = newLine.Substring(nextBlank + 1);
            }
            
            string attrName = newTag == "exception" ? "cref" : "name";
            sb.Append("".PadLeft(leadingBlank) + $"/// <{newTag} {attrName}=\"{paramName}\">{desc}");
        }

        private void ProcessTagLine(string text, StringBuilder sb, int index, string tag)
        {
            string newLine = text.Replace($"@{tag}", "").Trim();
            //string desc = newLine.Substring(newLine.IndexOf(" ") + 1);
            sb.Append("".PadLeft(leadingBlank) + $"/// <{tag}>{newLine}");
        }

        private int ToRemark(List<string> lines, StringBuilder sb, int index)
        {
            string text = lines[index].Trim().TrimStart('*', '/', ' ');
            sb.Append("".PadLeft(leadingBlank) + "/// " + text);
            return ToTagEnd(lines, sb, index, string.Empty);
        }

        public override string ToString()
        {
            string remark = sbRemark.ToString().Trim();
            if (string.IsNullOrEmpty(remark))
            {
                return sbMain.ToString().Trim();
            }
            else
            {
                sbRemark.Insert(0, "".PadLeft(leadingBlank) + "/// <remarks>\r\n");
                sbRemark.AppendLine("".PadLeft(leadingBlank) + "/// </remarks>");
                return sbMain.ToString() + sbRemark.ToString();
            }
        }
    }
}
