using Antlr.Runtime;
using Java2CSharp.JsDoc;
using Java2CSharp.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Java2CSharp
{
    internal class Java2CSharpCodeConvert
    {
        private List<RuleAction> packageRule, importRule, identifierRule, methodRule;
        private RuleAction firstLetter = new FirstLetterUpCaseAction();
        private RuleAction firstLetter2 = new FirstLetterUpCaseAction2();
        
        private XmlDocument xmlDoc = new XmlDocument();
        public Java2CSharpCodeConvert()
        {
            LoadJava2CSharpRule();
        }
        private void LoadJava2CSharpRule()
        {
            xmlDoc.Load(Environment.CurrentDirectory + "\\rules.xml");
            packageRule = LoadRule("package");
            packageRule.Add(firstLetter);

            importRule = LoadRule("import");
            importRule.Add(new TrimLastDotAction());
            importRule.Add(firstLetter);

            methodRule = LoadRule("method");
            methodRule.Add(firstLetter2);

            identifierRule = LoadRule("identifier");
        }
        private List<RuleAction> LoadRule(string tag)
        {
            XmlNodeList list = xmlDoc.SelectNodes("/rules/" + tag + "/rule");
            List<RuleAction> rules = new List<RuleAction>();
            foreach (XmlNode node in list)
            {
                if (node.Attributes["action"].InnerText == "replace")
                {
                    rules.Add(new ReplaceAction(node.Attributes["text"].InnerText, node.Attributes["newtext"].InnerText));
                }
                if (node.Attributes["action"].InnerText == "discard")
                {
                    rules.Add(new DiscardAction(node.Attributes["text"].InnerText));
                }
                if (node.Attributes["action"].InnerText == "getter")
                {
                    rules.Add(new GetterAction(node.Attributes["text"].InnerText, node.Attributes["newtext"].InnerText));
                }
            }
            return rules;
        }
        

        
        private void TrimEnd(StringBuilder sb)
        {
            int pBack = sb.Length - 1;
            while (char.IsWhiteSpace(sb[pBack]))
            {
                pBack--;
            }
            sb.Length = pBack + 1;
        }

        public string ConvertText(string sourceText)
        {
            JavaLexer lexer = new JavaLexer(new Antlr.Runtime.ANTLRStringStream(sourceText));
            CommonTokenStream tokens = new CommonTokenStream();
            tokens.TokenSource = lexer;
            MyJavaParser parser = new MyJavaParser(tokens);
            parser.Start();

            StringBuilder sb = new StringBuilder();
            StringBuilder sbLine = new StringBuilder();
            List<IToken> tkList = tokens.GetTokens();
            //tkList.ForEach(t => Console.WriteLine($"type = {t.Type}, text = {t.Text}"));
            int index = 0;
            int pos;
            bool hasExtends = false;
            //bool isOverride = false;
            bool isClass = false;
            string temp;
            Stack<string> endText = new Stack<string>();
            var jDoc = new JavaDocComment();
            while (index < tkList.Count)
            {
                string key = "final";
                //Removal
                IToken t = tkList[index];
                if (t.Text == key)
                {
                    Console.WriteLine(t);
                }
                if (t.Type == JavaLexer.COMMENT)
                {
                    if (t.Text.IndexOf("/**") >= 0)
                    {
                        string leadingBlank = string.Empty;
                        //查找注释前面的文本中有多少, sb中是已经处理的文本，最后几个字符就是注释前面的空白 
                        int xIndex = sb.Length - 1;
                        char c = '\0';
                        if(xIndex>=0)
                        {
                            c = sb[xIndex];
                        }
                        
                        while (xIndex >= 0 && (c == ' ' || c == '\t'))
                        {
                            xIndex--;
                            c = sb[xIndex];
                        }
                        
                        if (xIndex == sb.Length - 1)
                        {
                            // no blank space
                        }
                        else
                        {
                            char[] dest = new char[sb.Length - 1 - xIndex];
                            sb.CopyTo(xIndex + 1, dest, 0, dest.Length);

                            foreach (char c1 in dest)
                            {
                                if (c1 == '\t')
                                {
                                    leadingBlank += "    ";
                                }
                                else
                                {
                                    leadingBlank += c1;
                                }
                            }
                        }

                        jDoc.Parse(t.Text);
                        string comment = jDoc.ToString().TrimEnd();
                        if (leadingBlank.Length > 0)
                            comment = comment.Replace("\r\n/// ", $"\r\n{leadingBlank}/// ");
                        sb.Append(comment);
                    }
                    else
                    {
                        sb.Append(t.Text);
                    }
                }
                else if (t.Type == JavaLexer.WS)
                {
                    sb.Append(t.Text);
                }
                else if (t.Type == JavaLexer.T__54)
                {
                    if (t.Text == "@")
                    {
                        IToken nextT = tkList[index + 1];
                        if (nextT.Text == "Removal" || nextT.Text == "Deprecated")
                        {
                            sb.Append("// @").Append(nextT.Text);
                        }
                        else if (nextT.Text == "Beta"|| nextT.Text == "Internal")
                        {
                            //ignore this line, remove leading whitespace
                            TrimEnd(sb);
                        }
                        else if (nextT.Text == "Override")
                        {
                            TrimEnd(sb);
                            //isOverride = true;
                        }
                        else if (nextT.Text == "Test")
                        {
                            sb.Append("[Test]");
                        }
                        else if (nextT.Text == "SuppressWarnings")
                        {
                            TrimEnd(sb);
                            while (nextT.Text != "\n")
                            {
                                index++;
                                nextT = tkList[index];
                            }
                                
                            index++;
                            //isOverride = true;
                        }
                        else
                        {
                            //throw new NotImplementedException();
                        }

                        index++;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else if (t.Type == JavaLexer.T__80) //import
                {
                    pos = index;
                    //find the end of this line
                    pos++;
                    sbLine.Length = 0;
                    while (pos < tkList.Count && tkList[pos].Text != ";")
                    {
                        sbLine.Append(tkList[pos].Text);
                        pos++;
                    }
                    temp = ProcessLine(sbLine.ToString(), importRule);
                    if (!string.IsNullOrEmpty(temp))
                        sb.AppendLine("using" + temp + ";");

                    index = pos + 1; // skip char ; and \n
                }
                else if (t.Type == JavaLexer.T__88) //package
                {
                    sb.AppendLine()
                        .AppendLine("using System;")
                        .AppendLine("using System.Collections;")
                        .AppendLine("using System.Collections.Generic;")
                        .AppendLine("using System.IO;")
                        .AppendLine("using System.Text;")
                        .AppendLine()
                        .Append("namespace");
                    pos = index;
                    pos++;
                    //find the end of this line
                    sbLine.Length = 0;
                    while (pos < tkList.Count && tkList[pos].Text != "\n")
                    {
                        sbLine.Append(tkList[pos].Text);
                        pos++;
                    }
                    sb.Append(ProcessLine(sbLine.ToString(), packageRule));
                    sb.Append("{");
                    endText.Push("\r\n");
                    endText.Push("}");
                    index = pos;
                }
                else if (t.Type == JavaLexer.T__60)
                {
                    sb.Append("bool");
                }
                else if (t.Type == JavaLexer.T__72)
                {
                    hasExtends = true;
                    sb.Append(":");
                }
                else if (t.Type == JavaLexer.T__79)
                {
                    if (hasExtends)
                        sb.Append(",");
                    else
                        sb.Append(":");
                }
                else if (t.Type == JavaLexer.T__81)
                {
                    sb.Append("is");
                }
                else if (t.Type == JavaLexer.T__101) //throws
                {
                    index++;
                    //find first '{', that means class body , skip " throws XXXexception, XXXexception, \n  XXXexception"
                    while (index < tkList.Count && tkList[index].Text != ";")
                    {
                        if (tkList[index].Text == "{")
                        {
                            sb.AppendLine().Append("{");
                            index++;
                            break;
                        }
                           
                        index++;
                    }
                    sb.AppendLine();
                }
                else if (t.Type == JavaLexer.T__32) // '('
                {
                    if (tkList[index - 1].Type == JavaLexer.Identifier &&
                        (tkList[index - 2].Text == " " 
                        || tkList[index - 2].Type == JavaLexer.T__43 //'.'
                        || t.Type == JavaLexer.T__32 // '('
                        ))
                    {
                        //sb.Length -= tkList[index - 1].Text.Length;
                        temp = tkList[index - 1].Text;
                        if(EndWith(sb, tkList[index - 2].Text + temp))
                        {
                            sb.Length -= temp.Length;
                            sb.Append(temp);
                        }
                        
                        sb.Append(t.Text);
                    }
                    else
                        sb.Append(t.Text);
                }
                else if (t.Type == -1)
                {
                    //end of file, do nothing
                }
                else if (t.Type == JavaLexer.T__74) //final
                {
                    //如果后继中有class，将final转为sealed;
                    string fText = t.Text;
                    int succeedIndex = index + 1;
                    IToken nextT = tkList[succeedIndex];

                    while (nextT.Type == JavaLexer.WS )
                    {
                        succeedIndex++;
                        nextT = tkList[succeedIndex];
                    }

                    if (nextT.Text == "class")
                    {
                        sb.Append("sealed class");
                        index = succeedIndex;
                    }
                    
                }
                else if (t.Type == JavaLexer.T__108 || t.Type == JavaLexer.T__112)
                    sb.Append(t.Text);
                else if(t.Type == JavaLexer.Identifier)
                {
                    if(t.Text == "getClass" && tkList[index+1].Text == "(" && tkList[index + 2].Text == ")")
                    {
                        ///getClass().getName()
                        sb.Append("GetType()");
                        index += 2;
                        if (tkList[index + 1].Text=="."&& tkList[index + 2].Text == "getName" 
                            && tkList[index + 3].Text == "("&& tkList[index + 4].Text == ")")
                        {
                            sb.Append(".Name");
                            index += 4;
                        }
                    }
                    else
                    {
                        sb.Append(t.Text);
                    }
                }
                else if(t.Type == JavaLexer.ENUM)
                {
                    var flag = ProcessEnumTypeDefinition(sb, index, t, tkList);
                    if (flag == -1) //file has been processed
                        break;
                    index += flag;
                }
                else
                    sb.Append(t.Text);
                if (t.Type == JavaLexer.T__66)
                {
                    isClass = true;
                }
                if (t.Type == JavaLexer.T__66 && isClass)
                {
                    isClass = false;
                    hasExtends = false;
                }
                index++;
            }
            while (endText.Count > 0)
            {
                sb.Append(endText.Pop());
            }
            pos = 0;
            while (pos < sb.Length)
            {
                if (sb[pos] == '\r' && sb[pos + 1] != '\n')
                {
                    sb.Insert(pos + 1, '\n');
                    pos++;
                }
                pos++;
            }

            RemoveDuplicateUsing(sb);
            string sourceCode = sb.ToString();
            sourceCode = regCTFactory.Replace(sourceCode, " new CT_$1()");
            return sourceCode;
        }
        private Regex regCTFactory = new Regex(@"\sCT(\w+)\.Factory\.newInstance\(\);", RegexOptions.Compiled);
        private void RemoveDuplicateUsing(StringBuilder sb)
        {
            var lines = sb.ToString().Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            sb.Length = 0;
            HashSet<string> usingSet = new HashSet<string>();
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("using"))
                {
                    if (usingSet.Contains(line))
                    {
                        continue;
                    }
                    usingSet.Add(line);
                    sb.AppendLine(line);
                }
                else
                {
                    sb.AppendLine(line);
                }
            }
        }

        private string FindFirstIdentifier(List<IToken> tkList, int startIndex)
        {
            for (int i = startIndex; i < tkList.Count; i++)
            {
                IToken t = tkList[i];
                if (t.Type == JavaParser.Identifier)
                    return t.Text;
            }
            return "";
        }

        private bool NextSymbalIs(List<IToken> tkList, int startIndex, int tokenType)
        {
            for (int i = startIndex; i < tkList.Count; i++)
            {
                IToken t = tkList[i];
                if (t.Type == JavaParser.WS)  // skip white space
                    continue;
                if (t.Type == tokenType)
                    return true;
                else
                    break;
            }
            return false;
        }

        private int ProcessEnumTypeDefinition(StringBuilder sb, int index, IToken t, List<IToken> tkList)
        {
            sb.Append(t.Text);
            index++;
            Dictionary<string, string> dicEnumMembers = new Dictionary<string, string>();
            string poiEnumName = FindFirstIdentifier(tkList, index);
            string poiXmlEnumName = "";
            string ooxmlEnumName = "";
            bool isOOXMLEnum = false;
            // find enum members
            int pos = index;
            while (pos < tkList.Count)
            {
                t = tkList[pos];
                if (t.Type == -1)
                {
                    break;
                }
                if (t.Type == JavaParser.Identifier)
                {
                    if (tkList[pos + 1].Text == "("
                        && tkList[pos + 2].Type == JavaParser.Identifier
                        && tkList[pos + 3].Text == "."
                        && tkList[pos + 4].Type == JavaParser.Identifier
                        && tkList[pos + 5].Text == ")"
                        && (NextSymbalIs(tkList, pos + 6, JavaParser.T__39) //,
                            || NextSymbalIs(tkList, pos + 6, JavaParser.T__48))) //；
                    {
                        poiXmlEnumName = tkList[pos + 2].Text;
                        isOOXMLEnum = poiXmlEnumName.StartsWith("ST");
                        if (isOOXMLEnum)
                        {
                            ooxmlEnumName = "ST_" + poiXmlEnumName.Substring(2);
                        }
                        // BETWEEN(STCrossBetween.BETWEEN), or
                        // MIDPOINT_CATEGORY(STCrossBetween.MID_CAT);
                        dicEnumMembers.Add(t.Text, tkList[pos + 4].Text);
                        pos += 6;
                    }
                    else
                    {
                        sb.Append(t.Text);
                    }
                    if (NextSymbalIs(tkList, pos, JavaParser.T__48) && isOOXMLEnum)
                    {
                        //成员解析完成,
                        break;
                    }
                }
                else if (t.Text == "{")
                {
                    sb.AppendLine().Append(t.Text);
                }
                else
                {
                    sb.Append(t.Text);
                }
                pos++;
            }
            if (!isOOXMLEnum) return pos;
            if(isOOXMLEnum)
            {
                // back to "{"
                BackToSymbal(sb, '{');
                sb.AppendLine();
                foreach (var kv in dicEnumMembers)
                {
                    sb.AppendLine($"    {UpperCaseToPascal(kv.Key)},");
                }
                BackToSymbal(sb, ',');
                sb.Length--;
                sb.AppendLine().AppendLine("}");
                ///扩展枚举
                sb.AppendLine($"    public static class {poiEnumName}Extensions");
                sb.AppendLine("    {");
                sb.AppendLine($"        private static Dictionary<{ooxmlEnumName}, {poiEnumName}> reverse = new Dictionary<{ooxmlEnumName}, {poiEnumName}>()");
                sb.AppendLine(  "        {");
                foreach(var kv in dicEnumMembers)
                {
                    sb.Append(  "            ").Append($"{{ {ooxmlEnumName}.{UpperCaseToCamel(kv.Value)}, {poiEnumName}.{UpperCaseToPascal(kv.Key)} }},");
                    sb.AppendLine();
                }

                sb.AppendLine(  "        };");
                sb.AppendLine($@"    public static {poiEnumName} ValueOf({ooxmlEnumName} value)
    {{
        if(reverse.TryGetValue(value, out var result))
        {{
            return result;        
        }}
            
        throw new ArgumentException(""Invalid {ooxmlEnumName}"", nameof(value));
    }}");
                sb.AppendLine($@"    public static {ooxmlEnumName} To{ooxmlEnumName}(this {poiEnumName} value)
    {{
        switch (value)
        {{");
                foreach (var kv in dicEnumMembers)
                {
                    sb.AppendLine($@"            case {poiEnumName}.{UpperCaseToPascal(kv.Key)}:
                return {ooxmlEnumName}.{UpperCaseToCamel(kv.Value)};");
                }
                sb.AppendLine($@"            default:
                throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }}
    }}");
                sb.AppendLine("}"); 
            }

            return -1;
        }
        private void BackToSymbal(StringBuilder sb, char symbal)
        {
            while (sb.Length > 0 && sb[sb.Length - 1] != symbal)
            {
                sb.Length--;
            }
        }
        private string UpperCaseToCamel(string text)
        {
            string[] parts = text.Split('_');
            var arr = parts.Select(x => x.Substring(0, 1) + x.Substring(1).ToLower());
            var ret = string.Join("", arr);
            return ret.Substring(0, 1).ToLower() + ret.Substring(1);
        }

        private string UpperCaseToPascal(string text)
        {
            string[] parts = text.Split('_');
            var arr = parts.Select(x => x.Substring(0, 1) + x.Substring(1).ToLower());
            return string.Join("", arr);
        }

        private bool EndWith(StringBuilder sb, string text)
        {
            int pos = sb.Length - text.Length;
            for(int i = 0;i<text.Length;i++)
            {
                if (sb[pos] != text[i])
                {
                    return false;
                }
            }
            return true;
        }

        //public string ProcessIdentifier(string text, List<RuleAction> rules)
        //{
        //    if (rules == null)
        //        return text;
        //    string temp = text;
        //    foreach (RuleAction action in rules)
        //    {
        //        temp = action.Execute(temp);
        //        if (string.IsNullOrEmpty(temp))
        //            return string.Empty;
        //    }
        //    return temp;
        //}

        public string ProcessSource(string sourceCode)
        {
            string text = ProcessLine(sourceCode, methodRule);
            text = ProcessLine(text, identifierRule);
            return text;
        }

        public string ProcessLine(string lineText, List<RuleAction> rules)
        {
            if (rules == null)
                return lineText;
            string temp = lineText;
            foreach (RuleAction action in rules)
            {
                temp = action.Execute(temp);
                if (string.IsNullOrEmpty(temp))
                    return string.Empty;
            }
            return temp;
        }
    }
}
