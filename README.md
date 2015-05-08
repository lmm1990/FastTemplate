# FastTemplate

FastTemplate是一个超轻量级C#模版引擎，基于CSharpCodeProvider动态编译实现，通过和RazorEngine对比测试，FastTemplate比RazorEngine快8倍

##      	目录


*	[快速上手](#快速上手)
*	[模板语法](#模板语法)
*	[方法](#方法)

## 快速上手

### 编写模板

test.html：
	
	<h1>{{data["title"]}}</h1>
	<ul>
	    {{each data["cityList"] as city i}}
	        <li>{{city}}</li>
	    {{/each}}
	</ul>

### 渲染模板
	
	TemplateEngine.IsDebug = false;//是否启用调试模式，生产环境设为：false
	TemplateEngine.Init(new HashSet<string>(),"/static/fastTemplate"); //预编译
	//param1:程序集列表，模版中有用到第二方dll时需要设置
	//param2:模版根目录（相对于程序根目录）
	
	//编译模版
	string content = TemplateEngine.Compile("/static/template/demo.html", new Dictionary<string, dynamic>
	{
		{"title","cityHome"},
		{"cityList",new List<string>{"shanghai","beijing"}}
	});
	Console.WriteLine(content);


##	模板语法

输出变量

	<h1>I'm:{{data["name"]}}</h1>
	
if

	<h1>我是：{{if data["index"] == 1}}张三{{else}}李四{{/if}}</h1>
	
elseif

	<h1>我是：{{if data["index"] == 1}}张三{{else if data["index"] == 2}}王五{{else}}李四{{/if}}</h1>
	
each

	    {{each data["cityList"] as city i}}
	        <li>{{city}}</li>
	    {{/each}}
	    
include

	    {{include "/static/template/base.txt"}}
	    
Layout

	    {{layout "/static/template/Layout.html"}}
	    
using

	    {{using Common;}}

## 方法

###	TemplateEngine.``Compile``(templatePath, data)

templatePath：相对于程序根目录

###	TemplateEngine.``CompileOutResponse``(response,templatePath, data)