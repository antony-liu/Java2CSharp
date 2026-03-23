using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Java2CSharp.JsDoc
{
    /// <summary>
    /// 目前用于提取源码中的JsDoc内容
    /// </summary>
    public class SimpleParser
    {
        private int _index;
        private string _sourceCode;
        StringBuilder _newContent = new StringBuilder();
        public SimpleParser(string sourceCode) {
            _sourceCode = sourceCode;
            _newContent.Append(sourceCode);
        }
        public string Extract(string beginTag, string endTag)
        {
            int beginPos = _sourceCode.IndexOf(beginTag, _index, StringComparison.Ordinal);
            if(beginPos == -1)
            {
                return null;
            }
            int endPos = _sourceCode.IndexOf(endTag, beginPos + 1, StringComparison.Ordinal);
            _index = endPos + endTag.Length;
            return _sourceCode.Substring(beginPos, endPos - beginPos + endTag.Length);
        }

        public void Replace(string oldContent, string newContent)
        {
            _newContent.Replace(oldContent, newContent);
        }

        public string NewConent
        {
            get { return _newContent.ToString(); }
        }
    }
}
