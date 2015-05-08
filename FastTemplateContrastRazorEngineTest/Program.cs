using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using FastTemplate;

namespace FastTemplateContrastRazorEngineTest
{
    class Program
    {
        private static readonly string rootPath = AppDomain.CurrentDomain.BaseDirectory;

        static void Main(string[] args)
        {
            Dictionary<Tree, Dictionary<Tree, Dictionary<Tree, List<Tree>>>>
            treeList = new Dictionary<Tree, Dictionary<Tree, Dictionary<Tree, List<Tree>>>>();
            for (int i = 0; i < 10; i++)
            {
                treeList.Add(new Tree
                {
                    Id = i,
                    Name = "一级节点"
                }, new Dictionary<Tree, Dictionary<Tree, List<Tree>>>{
                    {new Tree{
                        Id = 2+i,
                        Name = "二级节点"
                    },new Dictionary<Tree, List<Tree>>{
                        {new Tree{
                            Id = 3+i,
                            Name = "三级节点"
                        },new List<Tree>{
                            new Tree{
                                Id = 4+i,
                                Name = "四级节点1"
                            },
                            new Tree{
                                Id = 4+1+i,
                                Name = "四级节点2"
                            }
                        }}
                    }}
                });
            }
            FastTemplateTest(treeList);
            RazorTemplateTest(treeList);
            Console.Read();
        }

        static void FastTemplateTest(Dictionary<Tree, Dictionary<Tree, Dictionary<Tree, List<Tree>>>> treeList)
        {
            TemplateEngine.Init(new HashSet<string>(),"/static/fastTemplate");
            List<long> timeList = new List<long>();
            long milliseconds = 0;
            Stopwatch watch = new Stopwatch();
            for (int i = 0; i < 10000; i++)
            {
                watch.Reset();
                watch.Start();
                TemplateEngine.Compile("/static/fastTemplate/list.html", new Dictionary<string, dynamic>{
                {"list",treeList}
                });
                watch.Stop();
                milliseconds = watch.ElapsedMilliseconds;
                if(milliseconds == 0)
                {
                    continue;
                }
                timeList.Add(milliseconds);
            }
                
            Console.WriteLine(string.Format("FastTemplateTest min:{0},max:{1} totalCount:10000 milliseconds > 0 count:{2}",timeList.Min(),timeList.Max(),timeList.Count));
        }

        static void RazorTemplateTest(Dictionary<Tree, Dictionary<Tree, Dictionary<Tree, List<Tree>>>> treeList)
        {
            var config = new TemplateServiceConfiguration
            {
                CachingProvider = new DefaultCachingProvider(t => { })
            };
            Engine.Razor = RazorEngineService.Create(config); // new API
            string template = File.ReadAllText(string.Format("{0}/static/razorTemplate/list.html", rootPath));
            long milliseconds = 0;
            List<long> timeList = new List<long>();
            Stopwatch watch = new Stopwatch();
            for (int i = 0; i < 10000; i++)
            {
                watch.Reset();
                watch.Start();
                Engine.Razor.RunCompile(template, "templateKey", null, treeList);
                watch.Stop();
                milliseconds = watch.ElapsedMilliseconds;
                if(milliseconds == 0)
                {
                    continue;
                }
                timeList.Add(milliseconds);
            }

            Console.WriteLine(string.Format("RazorTemplateTest min:{0},max:{1} totalCount:10000 milliseconds > 0 count:{2}", timeList.Min(), timeList.Max(),timeList.Count));
        }
    }

    public class Tree
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
