using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom; 
using System.CodeDom.Compiler;
using System.Linq;
using System.Web;

namespace FastTemplate
{
    public class TemplateEngine
    {
        public TemplateEngine()
        {
            IsDebug = false;
            IsWeb = File.Exists(string.Format("{0}/Web.config"));
        }

        //引用的DLL
        private static HashSet<string> referencedAssemblieList = new HashSet<string> { 
            "System.dll","System.Core.dll","Microsoft.CSharp.dll"
        };
        
        /// <summary>
        ///     <para>是否是调试模式</para>
        /// </summary>
        public static bool IsDebug { get; set; }

        /// <summary>
        ///     <para>是否是web站点</para>
        /// </summary>
        private static bool IsWeb { get; set; }

        private static readonly string rootPath = AppDomain.CurrentDomain.BaseDirectory.Replace("\\","/");
        private static Dictionary<string, MethodInfo> templateMethodList = new Dictionary<string, MethodInfo>();

        #region 模版初始化，预编译
        /// <summary>
        ///     <para>模版初始化，预编译</para>
        /// </summary>
        /// <param name="_referencedAssemblieList">程序集列表（程序集必须存在Bin目录）</param>
        /// <param name="templatePath">模版路径（相对于根目录）</param>
        public static void Init(HashSet<string> _referencedAssemblieList,string templatePath = "/static/template")
        {
            if(_referencedAssemblieList == null)
            {
                throw new Exception("_referencedAssemblieList 不能为NULL");
            }
            foreach (string item in _referencedAssemblieList)
            {
                if(IsWeb)
                {
                    referencedAssemblieList.Add(string.Format(@"{0}\bin\{1}",rootPath,item));
                }
                else
                {
                    referencedAssemblieList.Add(string.Format(@"{0}\{1}",rootPath,item));
                }
            }
            templatePath = string.Format("{0}{1}", rootPath, templatePath);
            List<string> fileList = new List<string>();
            GetAll(new DirectoryInfo(templatePath), fileList);
            List<string> methodNameList = new List<string>();
            string fileName;
            string methodName;
            List<string> templateBlockList;
            //封装成Class
            StringBuilder classContent = new StringBuilder();
            HashSet<string> usingContent = new HashSet<string>();
            usingContent.Add("using System;");
            usingContent.Add("using System.Collections.Generic;");
            usingContent.Add("using Microsoft.CSharp;");
            usingContent.Add("using System.Text;");
            classContent.Append("public static class Template{");
            foreach (string item in fileList)
            {
                if(item.IndexOf("Layout") > -1)
                {
                    continue;
                }
                fileName = string.Format("/{0}", item.Replace("\\", "/").Replace(rootPath, ""));
                methodName = GetMethodName(fileName);
                methodNameList.Add(methodName);
                templateBlockList = PreProcessTemplate(File.ReadAllText(rootPath+fileName));
                //预处理 include
                PreProcessIncludeBlock(ref templateBlockList);
                classContent.Append(GenerateTemplateMethodCode(methodName,templateBlockList,ref usingContent));
            }
            classContent.Append(Environment.NewLine);
            classContent.Append("}");

            classContent.Insert(0, string.Join("",usingContent));
            //编译
            Assembly assembly = CompileTemplate(classContent.ToString());
            Type classType = assembly.GetTypes()[0];
            foreach (string item in methodNameList)
            {
                templateMethodList.Add(item,classType.GetMethod(item));
            }
        }
        #endregion

        #region 获得文件夹下的所有文件
        /// <summary>
        ///     <para>获得文件夹下的所有文件</para>
        /// </summary>
        /// <param name="dir">目录</param>
        /// <param name="fileList">文件列表</param>
        private static void GetAll(DirectoryInfo dir,List<string> fileList)
        {
            foreach (FileInfo item in dir.GetFiles())
            {
                fileList.Add(item.FullName);
            }
            foreach (DirectoryInfo item in dir.GetDirectories())
            {
                GetAll(item,fileList);
            }
        }
        #endregion

        #region 编译模版
        /// <summary>
        ///     <para>编译模版</para>
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="data">数据</param>
        /// <returns></returns>
        public static string Compile(string fileName,Dictionary<string, dynamic> data)
        {
            string methodName = GetMethodName(fileName);
            if(templateMethodList.ContainsKey(methodName) && !IsDebug)
            {
                return templateMethodList[methodName].Invoke(null, new object[] {data}).ToString();
            }
            return Compile(methodName,fileName,data);
        }
        #endregion

        #region 编译并输出到Response
        /// <summary>
        ///     <para>编译并输出到Response</para>
        ///     <para>author:刘明明</para>
        ///     <para>updateTime:2015年4月22日09:48:55</para>
        /// </summary>
        /// <param name="response"></param>
        /// <param name="fileName">文件名</param>
        /// <param name="data">数据</param>
        public static void CompileOutResponse(HttpResponse response,string fileName, Dictionary<string, dynamic> data)
        {
            response.Write(Compile(fileName,data));
        }
        #endregion

        #region 预处理模版标签
        /// <summary>
        ///     <para>预处理模版标签</para>
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        private static List<string> PreProcessTemplate(string templateContent)
        {
            string flog;
            //预处理Layout标签
            if (templateContent.StartsWith("{{layout"))
            {
                flog = templateContent.Substring(2, templateContent.IndexOf("}}") - 3).Split(' ')[1];
                templateContent = templateContent.Substring(templateContent.IndexOf("}}") + 2);
                flog = flog.Substring(1);
                flog = File.ReadAllText(rootPath + flog);
                templateContent = flog.Replace("{{RenderBody}}", templateContent);
            }
            List<string> tempList = new List<string>();
            templateContent = Regex.Replace(templateContent,"\r", "", RegexOptions.Multiline);
            templateContent = Regex.Replace(templateContent,"\n", "", RegexOptions.Multiline);
            templateContent = Regex.Replace(templateContent,"\t", "", RegexOptions.Multiline);
            templateContent = Regex.Replace(templateContent,"  ", "", RegexOptions.Multiline);
            
            int index = 0;
            int newIndex = 0;
            while(true)
            {
                index = templateContent.IndexOf("{{",index);
                if(index == -1)
                {
                    if(newIndex != templateContent.Length-1)
                    {
                        tempList.Add(templateContent.Substring(newIndex));
                    }
                    break;
                }
                tempList.Add(templateContent.Substring(newIndex, index - newIndex));
                newIndex = templateContent.IndexOf("}}",index)+2;
                tempList.Add(templateContent.Substring(index, newIndex - index));
                index = newIndex;
            }
            List<string> list = new List<string>();
            foreach (string item in tempList)
            {
                if (item.Length < 6)
                {
                    list.Add(item);
                    continue;
                }
                flog = item.Substring(2, item.Length - 4).Trim();
                if (!flog.StartsWith("setData"))
                {
                    list.Add(item);
                    continue;
                }
                flog = flog.Substring(flog.IndexOf(' ') + 1);
                if(flog.IndexOf("= [") > -1)
                {
                    flog = flog.Replace("[", "new List<string>{").Replace("]", "}");
                }
                flog = flog.Replace(" = ", "\"] = ");
                list.Insert(0, string.Format("setData[\"{0};",flog ));
            }
            return list;
        }
        #endregion

        #region 预处理 include标签
        /// <summary>
        ///     <para>预处理 include标签</para>
        /// </summary>
        /// <param name="_templateBlockList">模版块列表</param>
        private static void PreProcessIncludeBlock(ref List<string> templateBlockList)
        {
            string flog;
            List<string> tempBlockList;
            for (int i = 0,length = templateBlockList.Count; i < length; i++)
			{
                if(templateBlockList[i].Length < 6)
                {
                    continue;
                }
                flog = templateBlockList[i].Substring(2,templateBlockList[i].Length-4).Trim();
                if (!templateBlockList[i].StartsWith("{{include"))
                {
                    continue;
                }
                if(templateBlockList.Count > 100000)
                {
                    throw new Exception("代码块不能超过10万行");
                }
                flog = flog.Split(' ')[1].Replace("\"","");
                tempBlockList = PreProcessTemplate(File.ReadAllText(rootPath+flog));
                templateBlockList.RemoveAt(i);
                templateBlockList.InsertRange(i, tempBlockList);
                PreProcessIncludeBlock(ref templateBlockList);
            }
        }
        #endregion

        #region 根据文件名获得方法名
        /// <summary>
        ///     <para>根据文件名获得方法名</para>
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        private static string GetMethodName(string fileName)
        {
            string methodName = fileName.Replace("/","_");
            int index = methodName.IndexOf('.');
            if(index > -1)
            {
                methodName = methodName.Substring(0, index);
            }
            return methodName;
        }
        #endregion

        #region 编译模版
        /// <summary>
        ///     <para>编译模版</para>
        /// </summary>
        /// <param name="methodName">方法名</param>
        /// <param name="fileName">文件名</param>
        /// <param name="data">数据</param>
        /// <returns></returns>
        private static string Compile(string methodName,string fileName,Dictionary<string, dynamic> data)
        {
            List<string> templateBlockList = PreProcessTemplate(File.ReadAllText(rootPath+fileName));
            
            //预处理 include
            PreProcessIncludeBlock(ref templateBlockList);

            //封装成Class
            StringBuilder classContent = new StringBuilder();
            HashSet<string> usingContent = new HashSet<string>();
            usingContent.Add("using System;");
            usingContent.Add("using System.Collections.Generic;");
            usingContent.Add("using Microsoft.CSharp;");
            usingContent.Add("using System.Text;");
            classContent.Append("public static class Template{");
            classContent.Append(GenerateTemplateMethodCode(methodName,templateBlockList,ref usingContent));
            classContent.Append(Environment.NewLine);
            classContent.Append("}");

            classContent.Insert(0, string.Join("",usingContent));
            //编译
            Assembly assembly = CompileTemplate(classContent.ToString());
            MethodInfo objMI = assembly.GetTypes()[0].GetMethod(methodName);
            if(!IsDebug)
            {
                templateMethodList.Add(methodName, objMI);
            }
            return objMI.Invoke(null, new object[] { data }).ToString();
        }
        #endregion

        #region 生成模版方法
        /// <summary>
        ///     <para>生成模版方法</para>
        /// </summary>
        /// <param name="methodName">方法名</param>
        /// <param name="templateBlockList">模版块列表</param>
        /// <returns></returns>
        private static string GenerateTemplateMethodCode(string methodName,List<string> templateBlockList,ref HashSet<string> usingContent)
        {
            StringBuilder code = new StringBuilder();
            code.AppendFormat("public static string {0}(dynamic data)",methodName);
            code.Append("{");
            code.Append("if (data == null){");
            code.Append("data = new Dictionary<string, dynamic> { };}");
            code.Append("StringBuilder code = new StringBuilder();");
            
            string flog;
            foreach (string item in templateBlockList)
            {
                if (item.StartsWith("setData"))
                {
                    code.Append(item.Replace("setData", "data"));
                    continue;
                }
                if (item.IndexOf("{{") == -1)
                {
                    code.Append(string.Format(@"code.Append(""{0}"");", item.Replace("\"", "\\\"")));
                    continue;
                }
                flog = item.Substring(2, item.Length - 4).Trim();
                //using
                if(flog.StartsWith("using"))
                {
                    usingContent.Add(flog);
                    continue;
                }
                if (flog.StartsWith("code"))
                {
                    code.Append(flog.Substring(5));
                    continue;
                }
                if (flog.StartsWith("each"))
                {
                    string[] eachInfoList = flog.Split(' ');
                    if(eachInfoList.Length == 4)
                    {
                        code.AppendFormat("foreach (var {0} in {1})",eachInfoList[3], eachInfoList[1]);
                        code.Append("{");
                        continue;
                    }
                    code.AppendFormat("for (int {0} = 0,{0}length = {1}.Count; {0} < {0}length; {0}++)", eachInfoList[4], eachInfoList[1]);
                    code.Append("{");
                    code.AppendFormat("var {0} = {1}[{2}];", eachInfoList[3], eachInfoList[1], eachInfoList[4]);
                    continue;
                }
                if (flog.StartsWith("/each"))
                {
                    code.Append("}");

                    continue;
                }
                if (flog.StartsWith("if"))
                {
                    code.Append(flog.Replace("if", "if(") + "){");

                    continue;
                }
                if (flog.StartsWith("else if"))
                {
                    code.Append(flog.Replace("else if", "}else if(") + "){");

                    continue;
                }
                if (flog.StartsWith("else"))
                {
                    code.Append("}else{");

                    continue;
                }
                if (flog.StartsWith("/if"))
                {
                    code.Append("}");

                    continue;
                }
                code.Append(string.Format(@"code.Append({0});", flog));

            }
            code.Append("return code.ToString();");
            
            code.Append("}");
            return code.ToString();
        }
        #endregion

        #region 编译模版
        /// <summary>
        ///     <para>编译模版</para>
        /// </summary>
        /// <param name="content">模版内容</param>
        /// <returns></returns>
        private static Assembly CompileTemplate(string content)
        {
            CSharpCodeProvider objCSharpCodePrivoder = new CSharpCodeProvider();
            CompilerParameters objCompilerParameters = new CompilerParameters();
            objCompilerParameters.ReferencedAssemblies.AddRange(referencedAssemblieList.ToArray());
            objCompilerParameters.GenerateExecutable = false;
            objCompilerParameters.GenerateInMemory = true;
            CompilerResults result = objCSharpCodePrivoder.CompileAssemblyFromSource(objCompilerParameters,content);
            if (result.Errors.HasErrors)
            {
                StringBuilder errorList = new StringBuilder();
                errorList.Append("编译错误：");
                foreach (CompilerError err in result.Errors)
                {
                    errorList.AppendFormat("{0}\r\n",err.ErrorText);
                }
                throw new Exception(errorList.ToString());
            }
            return result.CompiledAssembly;
        }
        #endregion
    }
}
