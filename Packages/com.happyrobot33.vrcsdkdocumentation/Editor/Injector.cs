using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
//xml
using System.Xml;
using System.IO;

//TODO: Make the VSCode compatibility features optional and controllable in the unity editor preferences section

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

        //create a list for each include and its path
        List<string> includesToCheck = new List<string>();

        //populate the list, only adding the ones that are dlls
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

                if (isDLL && isInPackages)
                {
                    //add the include path to the list
                    includesToCheck.Add(includePath);
                }
            }
        }

        /*TODO: Make each assembly a folder, and then each class in the assembly can be a individual XML file in that folder
        This would make it significantly nicer to edit individual classes, and would make it easier to manage the documentation
        */


        //loop through for each file
        for (int f = 0; f < files.Length; f++)
        {
            //get the file name without the extension
            string documentationFileName = System.IO.Path.GetFileNameWithoutExtension(files[f]);

            //find the include that matches the file name
            for (int i = 0; i < includesToCheck.Count; i++)
            {
                //get the include path
                string includePath = includesToCheck[i];

                //criteria
                bool containsAssembly = includePath.Contains(documentationFileName + ".dll");

                if (containsAssembly)
                {
                    //copy the documentation file to be next to the assembly, overwriting if it exists
                    //File.Copy(basePackagePath + "/Editor/Documentation/" + documentationFileName + ".xml", includePath.Replace(".dll", ".xml"), true);

                    #region Automated XML Processing

                    //get the xml
                    XmlDocument newXmlDoc = new XmlDocument();
                    newXmlDoc.Load(basePackagePath + "/Editor/Documentation/" + documentationFileName + ".xml");

                    //we want to edit all member nodes
                    XmlNodeList members = newXmlDoc.SelectNodes("doc/members/member");

                    //loop through each member
                    for (int m = 0; m < members.Count; m++)
                    {
                        //get the member
                        XmlNode member = members[m];

                        //check if a remarks node exists
                        XmlNode remarks = member.SelectSingleNode("remarks");

                        //if it doesnt, add it
                        if (remarks == null)
                        {
                            //create the remarks node
                            remarks = newXmlDoc.CreateElement("remarks");
                            //append the remarks node to the member
                            member.AppendChild(remarks);
                        }

                        const string githubInfo = "Docs generated by the <see href=\"https://github.com/Happyrobot33/VRCSDK-Documentation-Project\">VRChat SDK Documentation Project</see>.";
                        //append a new line and then the information to the github page, keeping any < and > characters
                        remarks.InnerXml += "<br/>" + githubInfo;

                        //process the parameters into the main summary, since VSCode doesnt render param tags by default in hover
                        XmlNode summary = member.SelectSingleNode("summary");

                        //if no summary exists, create one
                        if (summary == null)
                        {
                            //create the summary node
                            summary = newXmlDoc.CreateElement("summary");
                            //append the summary node to the member
                            member.AppendChild(summary);
                        }

                        XmlNodeList paramNodes = member.SelectNodes("param");
                        if (paramNodes.Count > 0)
                        {
                            XmlNode listNode = CreateDocList(newXmlDoc, summary, "Parameter", "Description");

                            for (int p = 0; p < paramNodes.Count; p++)
                            {
                                XmlNode paramNode = paramNodes[p];
                                string paramName = paramNode.Attributes["name"].Value;
                                string paramDescription = paramNode.InnerXml;
                                //create a list item
                                XmlNode listItem = newXmlDoc.CreateElement("item");
                                listNode.AppendChild(listItem);
                                //create a term
                                XmlNode term = newXmlDoc.CreateElement("term");
                                listItem.AppendChild(term);
                                term.InnerXml = paramName;
                                //create a description
                                XmlNode description = newXmlDoc.CreateElement("description");
                                listItem.AppendChild(description);
                                description.InnerXml = paramDescription;
                            }
                        }
                    }

                    //save the xml
                    newXmlDoc.Save(includePath.Replace(".dll", ".xml"));
                    #endregion

                    Debug.Log("Documentation for " + documentationFileName + " has been injected into " + includePath);
                }
            }
        }
        // TODO: process project content
        return content;
    }

    /// <summary>
    /// Creates a list node in the xml provided
    /// </summary>
    /// <param name="docReference"></param>
    /// <param name="parentNode"></param>
    /// <param name="col1Name"></param>
    /// <param name="col2Name"></param>
    /// <returns></returns>
    private static XmlNode CreateDocList(XmlDocument docReference, XmlNode parentNode, string col1Name, string col2Name)
    {
        /*
        <list type="bullet">
            <listheader>
                <term>Parameters</term>
                <description>description</description>
            </listheader>
            <item>
                <term>name</term>
                <description>description</description>
            </item>
        </list>
        */
        //add a list node
        XmlNode listNode = docReference.CreateElement("list");
        listNode.Attributes.Append(docReference.CreateAttribute("type"));
        listNode.Attributes["type"].Value = "table";
        parentNode.AppendChild(listNode);

        //set up the header
        XmlNode headerNode = docReference.CreateElement("listheader");
        listNode.AppendChild(headerNode);
        XmlNode termNode = docReference.CreateElement("term");
        headerNode.AppendChild(termNode);
        termNode.InnerXml = col1Name;
        XmlNode descriptionNode = docReference.CreateElement("description");
        headerNode.AppendChild(descriptionNode);
        descriptionNode.InnerXml = col2Name;
        return listNode;
    }
}
