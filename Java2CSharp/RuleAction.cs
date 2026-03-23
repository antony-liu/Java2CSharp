using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Java2CSharp
{
    public abstract class RuleAction
    {
        public abstract string Execute(string lineText);
    }

    public class GetterAction : RuleAction
    {
        private Regex getter = new Regex(@"\.[gG]et([a-zA-Z0-9_]+)\(\)");
        private readonly string patten;
        private readonly string newText;

        public GetterAction(string patten, string newText)
        {
            this.patten = patten;
            this.newText = newText;
        }

        public override string Execute(string lineText)
        {
            if (getter.Match(lineText).Success)
            {
                return getter.Replace(lineText, newText);
            }
            else
            {
                return lineText;
            }
        }
    }

    public class FirstLetterUpCaseAction2 : RuleAction
    {
        public override string Execute(string lineText)
        {
            return string.Concat(lineText[0].ToString().ToUpper(), lineText.AsSpan(1));
        }
    }

    public class ReplaceAction : RuleAction
    {
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        private ReplaceAction()
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        {
        }

        private readonly string oldText;
        private readonly string newText;
        private Regex reg;
        public ReplaceAction(string oldText, string newText)
        {
            this.oldText = oldText;
            this.newText = newText;
            reg = new Regex(oldText);
        }

        public override string Execute(string lineText)
        {
            string ret = lineText.Replace(oldText, newText);
            return reg.Replace(ret, newText);
            
        }
    }

    public class DiscardAction : RuleAction
    {
        private readonly string discard;

        public DiscardAction(string discard)
        {
            this.discard = discard;
        }

        public override string Execute(string lineText)
        {
            if (lineText.Contains(discard))
                return string.Empty;
            else
                return lineText;
        }
    }

    public class FirstLetterUpCaseAction : RuleAction
    {
        public override string Execute(string lineText)
        {
            string temp = string.Empty;
            int pos = 0;
            int lastpos = 0;
            while (pos >= 0)
            {
                pos = lineText.IndexOf('.', pos);
                if (pos == -1)
                    break;
                pos++;

                temp += string.Concat(lineText.AsSpan(lastpos, pos - lastpos), lineText[pos].ToString().ToUpper());
                pos++;
                lastpos = pos;
            }
            return string.Concat(temp, lineText.AsSpan(lastpos));
        }
    }

    public class TrimLastDotAction : RuleAction
    {
        public override string Execute(string lineText)
        {
            return lineText.Remove(lineText.LastIndexOf('.'));
        }
    }

}
