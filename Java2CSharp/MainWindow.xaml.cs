using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using Java2CSharp.JsDoc;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Xml.Linq;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Themes;

namespace Java2CSharp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
        
        public MainWindow()
        {
            this.WindowState = WindowState.Maximized;
            InitializeComponent();
            //this.dockingManager.Theme = new AeroTheme();
            j2csEngine.LoadRules();
            FileNew_Click(null, null);
        }

        private PageSourceCode CreateSourceCodePage(string title, ConvertType convertType, string sourceCode)
        {
            LayoutAnchorable layOutAnc = new LayoutAnchorable() { Title = title, CanClose = true };
            PageSourceCode sc = new PageSourceCode()
            {
                ConvertType = convertType,
            };

            sc.edtSourceCode.Text = sourceCode;
            layOutAnc.Content = new Frame
            {
                Content = sc
            };
            layOutAnc.Closed += LayOutAnc_Closed;
            layOutPane.Children.Add(layOutAnc);
            return sc;
        }

        private PageSourceCode CreateCodeTab(string fileName, string sourceCode)
        {
            string name = System.IO.Path.GetFileName(fileName);
            string ext = System.IO.Path.GetExtension(fileName);
            var sc = CreateSourceCodePage(name, ext.ToLower() == ".java" ? ConvertType.Java2CSharp : ConvertType.CSharp, sourceCode);

            //sc.edtSourceCode.Text = sourceCode;
            sc.Convert();
            return sc;
            
        }

        private void LayOutAnc_Closed(object sender, EventArgs e)
        {
            if(sender is LayoutAnchorable layOutAnc)
            {
                PageSourceCode sc = (layOutAnc.Content as Frame).Content as PageSourceCode;
                layOutAnc.Content = null;
            }
            Console.WriteLine(layOutPane.Children.Count);
        }

        private Java2CSharpCodeConvert converter = new Java2CSharpCodeConvert();
        private RuleEngine j2csEngine = new RuleEngine();
        private void FileOpen_Click(object sender, ExecutedRoutedEventArgs e)
        {
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                string[] fileNames = openFileDialog.FileNames;
                foreach (var item in fileNames)
                {
                    using StreamReader sr = new(item);
                    string sourceCode = sr.ReadToEnd();
                    var sc = CreateCodeTab(item, sourceCode);
                    
                }
            }
        }
        OpenFileDialog openFileDialog = new OpenFileDialog()
        {
            Filter = "java|*.java|C#|*.cs",
            Multiselect = true
        };

        private string lastSelectFolder = null;
        private void OpenConvert_Click(object sender, ExecutedRoutedEventArgs e)
        {
            string folderName = e.Parameter.ToString();
            if(lastSelectFolder != folderName)
            {
                var fBase = App.Folders.Single(x => x.Name == "base");
                var fSelect = App.Folders.SingleOrDefault(x => x.Name == folderName);
                string path = System.IO.Path.Combine(fBase.Source, fSelect?.Source);
                if (Directory.Exists(path))
                {
                    openFileDialog.InitialDirectory = path;
                }
            }
            else
            {
                openFileDialog.InitialDirectory = string.Empty;
            }
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                string[] fileNames = openFileDialog.FileNames;
                foreach (var item in fileNames)
                {
                    using StreamReader sr = new(item);
                    string sourceCode = sr.ReadToEnd();
                    var sc = CreateCodeTab(item, sourceCode);
                    sc.SourceFile = item;
                    sc.BaseMapping = App.Folders.Single(x => x.Name == "base");
                    sc.CodeMapping = App.Folders.SingleOrDefault(x => x.Name == folderName);
                }
                layOutPane.SelectedContentIndex = layOutPane.ChildrenCount - 1;
            }
            lastSelectFolder = folderName;
        }

        private void Convert_Click(object sender, ExecutedRoutedEventArgs e)
        {
            if (layOutPane.SelectedContent == null)
                return;
            ConvertType ct = e.Parameter?.ToString() == "java" ? ConvertType.Java2CSharp : ConvertType.JavaDoc;
            var sc = ((layOutPane.SelectedContent.Content as Frame).Content as PageSourceCode);
            sc.ConvertType = ct;
            sc.Convert();
        }

        private OpenFileDialog openFileDialog2 = null;
        private void Convert_FolderComment(object sender, ExecutedRoutedEventArgs e)
        {
            var fBase = App.Folders.Single(x => x.Name == "base");
            if (openFileDialog2 == null)
            {
                openFileDialog2 = new OpenFileDialog()
                {
                    Filter = "C#|*.cs",
                    InitialDirectory = fBase.Target,
                    Multiselect = true,
                    RestoreDirectory = true,
                };
            }
            else
            {
                openFileDialog2.InitialDirectory = string.Empty;
            }
            var result = openFileDialog2.ShowDialog();
            if (result == true)
            {
                string[] fileNames = openFileDialog2.FileNames;
                foreach (var item in fileNames)
                {
                    string sourceCode = string.Empty;
                    using FileStream fs = new FileStream(item, FileMode.Open, FileAccess.Read);
                    {
                        using StreamReader sr = new(fs);
                        {
                            sourceCode = sr.ReadToEnd();
                        }
                    }
                    //check license
                    string[] lines = sourceCode.Split("\r", StringSplitOptions.TrimEntries);
                    if (!(lines.Length > 20 && sourceCode.Trim().StartsWith("/*") &&
                        sourceCode.Contains("Apache Software Foundation (ASF)", StringComparison.OrdinalIgnoreCase)))
                    {
                        sourceCode = licenseText + sourceCode;
                    }
                      
                    SimpleParser simpleParser = new SimpleParser(sourceCode);
                    JavaDocComment jdoc = new JavaDocComment();
                    string comment = simpleParser.Extract("/**", "*/");
                    if(comment != null)
                    {
                        do
                        {
                            jdoc.Parse(comment);
                            string newComment = jdoc.ToString();
                            simpleParser.Replace(comment, newComment);
                            comment = simpleParser.Extract("/**", "*/");
                        }
                        while (comment != null);
                    }
                    
                    using FileStream fs1 = new FileStream(item, FileMode.Truncate, FileAccess.ReadWrite);
                    using StreamWriter sw = new StreamWriter(fs1);
                    sw.Write(simpleParser.NewConent);
                }
            }
        }

        private void FileLicenseText_Click(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFileDialog = new System.Windows.Forms.FolderBrowserDialog();  //选择文件夹
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = openFileDialog.SelectedPath;
                ProcessPath(path);
            }
        }

        private void ProcessPath(string path)
        {
            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                ProcessPath(dir);
            }
            string[] files =Directory.GetFiles(path);
            foreach(string file in files)
            {
                string sourceCode = string.Empty;
                using FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                {
                    using StreamReader sr = new(fs);
                    {
                        sourceCode = sr.ReadToEnd();
                    }
                }
                //check license
                string[] lines = sourceCode.Split("\r", StringSplitOptions.TrimEntries);
                if (lines.Length > 20 && sourceCode.Trim().StartsWith("/*") &&
                    sourceCode.Contains("Apache Software Foundation (ASF)", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                sourceCode = licenseText + sourceCode;
                using FileStream fs1 = new FileStream(file, FileMode.Truncate, FileAccess.ReadWrite);
                using StreamWriter sw = new StreamWriter(fs1);
                sw.Write(sourceCode);
            }
        }

        private void FileSave_Click(object sender, ExecutedRoutedEventArgs e)
        {
            if (layOutPane.SelectedContent == null)
                return;
            //ConvertType ct = e.Parameter?.ToString() == "java" ? ConvertType.Java2CSharp : ConvertType.JsDoc;
            var sc = ((layOutPane.SelectedContent.Content as Frame).Content as PageSourceCode);
            sc.Save();
        }

        private void FileClose_Click(object sender, ExecutedRoutedEventArgs e)
        {
            layOutPane.SelectedContent?.Close();
        }

        private void FileExit_Click(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }
        private int fileIndex = 0;
        private void FileNew_Click(object sender, ExecutedRoutedEventArgs e)
        {
            fileIndex++;
            var sc = CreateSourceCodePage("New File "+ fileIndex, ConvertType.Java2CSharp, string.Empty);
            sc.edtSourceCode.TextInput += EdtSourceCode_TextInput;
        }

        private void EdtSourceCode_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextEditor textEditor)
            {
                var sc = ((textEditor.Parent as FrameworkElement).Parent as FrameworkElement) as PageSourceCode;
                sc?.Convert();

            }
        }

        private string licenseText = @"/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the ""License""); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an ""AS IS"" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

";
    }
}
