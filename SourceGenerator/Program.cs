// import ICSharpCode.Decompiler
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

public partial class Program
{
    static string basePath = @"E:\Storage\Personal\Coding\VRChat\VRCSDK-Documentation-Project";
    static List<string> assemblys = new List<string>();
    
    public static void Main(string[] args)
    {
        //try to make a basepath
        string workingDirectory = Environment.CurrentDirectory;
        string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.Parent.FullName;
        Console.WriteLine("Working Directory: " + projectDirectory);
        basePath = projectDirectory;

        //load assemblys
        assemblys.Add(basePath + @"\Packages\com.vrchat.base\Runtime\VRCSDK\Plugins\VRCSDKBase.dll");
        assemblys.Add(basePath + @"\Packages\com.vrchat.base\Runtime\VRCSDK\Plugins\VRCSDKBase-Editor.dll");
        assemblys.Add(basePath + @"\Packages\com.vrchat.worlds\Runtime\VRCSDK\Plugins\VRCSDK3.dll");
        assemblys.Add(basePath + @"\Packages\com.vrchat.worlds\Runtime\VRCSDK\Plugins\VRCSDK3-Editor.dll");
        assemblys.Add(basePath + @"\Packages\com.vrchat.worlds\Runtime\Udon\External\VRC.Udon.Common.dll");

        foreach(string assembly in assemblys)
        {
            var resolver = new UniversalAssemblyResolver(assembly, false, assembly);
            resolver.AddSearchDirectory(Path.GetDirectoryName(assembly));

            //load the solution
            var decompiler = new CSharpDecompiler(assembly, resolver, new DecompilerSettings());

            //we want to save the decompiled code to a file
            string code = decompiler.DecompileWholeModuleAsString();

            //save the code to a folder
            string path = basePath + @"\DecompiledSource\" + Path.GetFileNameWithoutExtension(assembly) + ".cs";

            //make sure folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            File.WriteAllText(path, code);
        }

        //make a gitignore file
        string gitignore = basePath + @"\DecompiledSource\.gitignore";
        File.WriteAllText(gitignore, "*");
    }
}
