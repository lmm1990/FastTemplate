using System;
using System.Collections.Generic;
using FastTemplate;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            TemplateEngine.IsDebug = false;
            TemplateEngine.Init(new HashSet<string> {"Common.dll" });
            //BaseTest();
            //IfTest();
            //ElseIfTest();
            //EachTest();
            //IncludeTest();
            //LayoutTest();
            UsingTest();
        }

        static void BaseTest()
        {
            string content = TemplateEngine.Compile("/static/template/base.txt", new Dictionary<string, dynamic>
            {
                {"name","jack"}
            });
            Console.WriteLine(string.Format("BaseTest:{0}", content));
            Console.Read();
        }

        static void IfTest()
        {
            string content = TemplateEngine.Compile("/static/template/if.txt", new Dictionary<string, dynamic>
            {
                {"index",1}
            });
            Console.WriteLine(string.Format("IfTest:{0}", content));
            Console.Read();
        }

        static void ElseIfTest()
        {
            string content = TemplateEngine.Compile("/static/template/elseif.txt", new Dictionary<string, dynamic>
            {
                {"index",10}
            });
            Console.WriteLine(string.Format("ElseIfTest:{0}", content));
            Console.Read();
        }

        static void EachTest()
        {
            string content = TemplateEngine.Compile("/static/template/each.txt", new Dictionary<string, dynamic>
            {
                {"title","index"},
                {"indexList",new List<int>{
                    1,2,3,4
                }}
            });
            Console.WriteLine(string.Format("ElseIfTest:{0}", content));
            Console.Read();
        }

        static void IncludeTest()
        {
            string content = TemplateEngine.Compile("/static/template/include.txt", new Dictionary<string, dynamic>
            {
                {"name","jack"},
                {"list",new List<string>{
                    "shanghai","beijing"
                }}
            });
            Console.WriteLine(string.Format("IncludeTest:{0}", content));
            Console.Read();
        }

        static void LayoutTest()
        {
            string content = TemplateEngine.Compile("/static/template/index.html", new Dictionary<string, dynamic>
            {
                {"id",1},
                {"cityName","shanghai"}
            });
            Console.WriteLine(string.Format("LayoutTest:{0}", content));
            Console.Read();
        }

        static void UsingTest()
        {
            string content = TemplateEngine.Compile("/static/template/using.txt", new Dictionary<string, dynamic>
            {
                {"time",DateTime.Now}
            });
            Console.WriteLine(string.Format("UsingTest:{0}", content));
            Console.Read();
        }
    }
}
