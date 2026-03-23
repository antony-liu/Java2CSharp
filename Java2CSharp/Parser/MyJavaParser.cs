using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr.Runtime;

namespace Java2CSharp.Parser
{
    public class MyJavaParser: JavaParser
    {
        private StringBuilder sb = new StringBuilder();
        public MyJavaParser(ITokenStream input) :base(input)
        {
        }
        public override object Match(IIntStream input, int ttype, BitSet follow)
        {
            sb.Append(GetCurrentInputSymbol(input).ToString());
            return base.Match(input, ttype, follow);
        }
    }
}
