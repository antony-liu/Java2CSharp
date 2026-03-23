using Antlr.Runtime;
using Java2CSharp.Rules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Configuration;
using System.CodeDom;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using FilePath = System.IO.Path;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit;
using Java2CSharp.JsDoc;

namespace Java2CSharp
{
    /// <summary>
    /// PageSourceCode.xaml 的交互逻辑
    /// </summary>
    public partial class PageSourceCode : Page
    {
        private RuleEngine j2csEngine = new RuleEngine();
        public PageSourceCode()
        {
            InitializeComponent();
            j2csEngine.LoadRules();
            //this.edtSourceCode.;
            //this.edtJava;
        }
        public FolderMapping BaseMapping { get; set; }
        public FolderMapping CodeMapping { get; set; }
        public string SourceFile { get; set; }
        public string TargetFile { get; set; }
        private ConvertType _convertType;
        public ConvertType ConvertType
        {
            get
            {
                return _convertType;
            }
            set
            {
                if(_convertType != value)
                {
                    _convertType = value;
                    switch (_convertType)
                    {
                        case ConvertType.Java2CSharp:
                            edtSourceCode.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Java");
                            edtTargetCode.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                            break;
                        case ConvertType.JavaDoc:
                            edtSourceCode.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Java");
                            edtTargetCode.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                            break;
                        case ConvertType.CSharp:
                            edtSourceCode.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                            edtTargetCode.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                            break;
                        default:
                            break;
                    }
                }
                
            }
        }
        //public string? SourceFile { get; set; }
        //public string? TargetFile { get; set; }
        private Java2CSharpCodeConvert converter = new Java2CSharpCodeConvert();
        JavaDocComment jdoc = new JavaDocComment();
        public void Convert()
        {
            string javaSource = this.edtSourceCode.Text;
            string targetString;
            switch(_convertType)
            {
                case ConvertType.Java2CSharp:
                    targetString = converter.ConvertText(javaSource);
                    targetString = j2csEngine.Parse(targetString);
                    targetString = converter.ProcessSource(targetString);
                    break;
                case ConvertType.JavaDoc:
                    SimpleParser simpleParser = new SimpleParser(javaSource);
                    string comment = simpleParser.Extract("/**", "*/");
                    do
                    {
                        jdoc.Parse(comment);
                        string newComment = jdoc.ToString();
                        simpleParser.Replace(comment, newComment);
                        comment = simpleParser.Extract("/**", "*/");
                    }
                    while (comment != null);
                    targetString = simpleParser.NewConent;
                    //targetString = j2csEngine.Parse(targetString);
                    break;
                case ConvertType.CSharp:
                    targetString = converter.ConvertText(javaSource);
                    targetString = j2csEngine.Parse(targetString);
                    break;
                default:
                    targetString = string.Empty;
                    break;
            }
            edtTargetCode.Text = targetString;
        }

        internal void Save()
        {
            //
            string sourcePath = FilePath.Combine(BaseMapping.Source, CodeMapping.Source);
            string targetPath = FilePath.Combine(BaseMapping.Target, CodeMapping.Target);
            
            string targetFile = FilePath.ChangeExtension(SourceFile.Replace(sourcePath, targetPath), "cs");

            if (File.Exists(targetFile))
            {
                targetFile = FilePath.ChangeExtension(targetFile, null);
                targetFile += "(1).cs";
            }

            var dir = FilePath.GetDirectoryName(targetFile);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            //MessageBox.Show(targetFile);
            File.WriteAllText(targetFile, edtTargetCode.Text, Encoding.UTF8);
        }

        private void edtSourceCode_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            //Console.WriteLine(e.VerticalOffset);
            
        }

        private void edtTargetCode_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            //Console.WriteLine(e.VerticalOffset);
            //TextView textView = edtTargetCode.TextArea.TextView;
            edtSourceCode.ScrollToVerticalOffset(e.VerticalOffset);
            //bool isAtEnd = textView.VerticalOffset + textView.ActualHeight + 1 >= textView.DocumentHeight;
            //edtTargetCode.ActualHeight> edtTargetCode.ViewportHeight
        }
    }

    public enum ConvertType
    {
        Unknown,
        Java2CSharp,
        JavaDoc,
        CSharp
    }
}
