using System;
using UnityEditor;
using UnityEngine;
//xml
using System.Xml;
using System.IO;

public class Injector : AssetPostprocessor
{
    /*
  public static string OnGeneratedSlnSolution(string path, string content)
  {
    // TODO: process solution content
    return content;
  }
  */

    public static string OnGeneratedCSProject(string path, string content)
    {
        //check if the assembly matches ones in the documentation folder in our package
        const string basePackagePath = "Packages/com.happyrobot33.vrcsdkdocumentation";

        //parse the xml
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(content);

        //get the include item groups, which are under the Project node
        XmlNodeList itemGroups = xmlDoc.SelectNodes("Project/ItemGroup");

        //get all of the files in the documentation folder
        string[] files = System.IO.Directory.GetFiles(basePackagePath + "/Editor/Documentation");


        //loop through for each file
        for (int f = 0; f < files.Length; f++)
        {
            //get the file name without the extension
            string documentationFileName = System.IO.Path.GetFileNameWithoutExtension(files[f]);

            //loop through the item groups
            for (int i = 0; i < itemGroups.Count; i++)
            {
                //get the item group
                XmlNode itemGroup = itemGroups[i];

                /*<ItemGroup>
                <None Include="Packages\com.vrchat.base\Runtime\VRCSDK\Plugins\UniTask\Licence.txt" />*/

                //get the include nodes
                XmlNodeList includes = itemGroup.SelectNodes("None");


                //loop through the includes
                for (int j = 0; j < includes.Count; j++)
                {
                    //get the include
                    XmlNode include = includes[j];

                    //get the include path
                    string includePath = include.Attributes["Include"].Value;

                    //criteria
                    bool isDLL = includePath.Contains(".dll");
                    //is in packages
                    bool isInPackages = includePath.Contains("Packages");
                    //contains the assembly we are looking for as its file, without the extension, exactly
                    bool containsAssembly = includePath.Contains(documentationFileName + ".dll");

                    if (isDLL && isInPackages && containsAssembly)
                    {
                        Debug.Log("Found " + documentationFileName + " in " + includePath);

                        //copy the documentation file to be next to the assembly, overwriting if it exists
                        File.Copy(basePackagePath + "/Editor/Documentation/" + documentationFileName + ".xml", includePath.Replace(".dll", ".xml"), true);
                    }
                }

            }
        }
        // TODO: process project content
        return content;
    }
}
