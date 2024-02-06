using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
//xml
using System.Xml;
using VRC.PackageManagement.Core;

//TODO: Make the VSCode compatibility features optional and controllable in the unity editor preferences section
namespace Happyrobot33.VRCSDKDocumentationProject
{
    public class Injector : AssetPostprocessor
    {
        private class Doc
        {
            public string AssemblyName = "";
            public string DllPath = "";
            public int creationTick = 0;
            public List<XmlDocument> ClassDocs = new List<XmlDocument>();

            public XmlDocument Generate()
            {
                //create a new xml doc
                XmlDocument xmldoc = new();

                //populate it with the base information
                XmlNode doc = xmldoc.CreateElement("doc");
                XmlNode assembly = xmldoc.CreateElement("assembly");
                XmlNode name = xmldoc.CreateElement("name");
                name.InnerText = AssemblyName;
                assembly.AppendChild(name);
                doc.AppendChild(assembly);
                xmldoc.AppendChild(doc);

                //create the members node
                XmlNode members = xmldoc.CreateElement("members");
                doc.AppendChild(members);

                //loop through each class
                foreach (XmlDocument classDoc in ClassDocs)
                {
                    //get all of the nodes in the members node
                    XmlNodeList memberNodes = classDoc.SelectNodes("members/member");

                    //loop through each member
                    foreach (XmlNode memberNode in memberNodes)
                    {
                        XmlNode processedMember = ProcessMember(classDoc, memberNode);

                        //add the member to the main xml, under the members node
                        members.AppendChild(xmldoc.ImportNode(processedMember, true));
                    }
                }

                //append the tick count to the xml
                XmlNode tickNode = xmldoc.CreateElement("tick");
                tickNode.InnerText = creationTick.ToString();
                doc.AppendChild(tickNode);

                return xmldoc;
            }

            public void LoadInClass(string classFilePath)
            {
                XmlDocument newXmlDoc = new XmlDocument();
                newXmlDoc.Load(classFilePath);
                ClassDocs.Add(newXmlDoc);
            }

            private static string determineMemberID(XmlNode member)
            {
                //get the member name string
                string memberName = member.Attributes["name"].Value;

                //parse the first string to determine member type
                switch (memberName[0])
                {
                    case 'T':
                        return "Type";
                    case 'F':
                        return "Field";
                    case 'P':
                        return "Property";
                    case 'M':
                        return "Method";
                    case 'E':
                        return "Event";
                    default:
                        return null;
                }
            }

            private static XmlNode ProcessMember(XmlDocument classDoc, XmlNode memberNode)
            {
                //check if a summary node exists
                XmlNode summary = memberNode.SelectSingleNode("summary");

                //if it doesnt, add it
                if (summary == null)
                {
                    //create the summary node
                    summary = classDoc.CreateElement("summary");
                    //append the summary node to the member
                    memberNode.AppendChild(summary);
                }

                //check if a remarks node exists
                XmlNode remarks = memberNode.SelectSingleNode("remarks");

                //if it doesnt, add it
                if (remarks == null)
                {
                    //create the remarks node
                    remarks = classDoc.CreateElement("remarks");
                    //append the remarks node to the member
                    memberNode.AppendChild(remarks);
                }

                //process the documentation link into the remarks
                const string officialDocInfo = "Check the <see href=\"{0}\">VRChat documentation</see> for more information.";
                //get the <docURL> tag
                XmlNode docURL = memberNode.SelectSingleNode("docURL");
                //if it exists, add the official documentation link
                if (docURL != null)
                {
                    //get the url
                    string url = docURL.InnerText;
                    //strip any whitespace
                    url = url.Trim();
                    //if it is empty, throw a warning
                    if (url == "")
                    {
                        Debug.LogWarning("Empty docURL tag found for member " + memberNode.Attributes["name"].Value);
                    }
                    else
                    {
                        //append a new line and then the information to the official documentation, keeping any < and > characters
                        remarks.InnerXml += "<br/>" + string.Format(officialDocInfo, url);
                    }
                }
                else
                {
                    //check if there is a inheritdoc tag
                    XmlNode inheritDoc = memberNode.SelectSingleNode("inheritdoc");
                    if (inheritDoc == null)
                    {
                        //if there is no inheritdoc tag, throw a warning
                        Debug.LogWarning("No docURL or inheritdoc tag found for member " + memberNode.Attributes["name"].Value);
                    }
                }


                const string githubInfo = "Docs generated by the <see href=\"https://github.com/Happyrobot33/VRCSDK-Documentation-Project\">VRChat SDK Documentation Project</see>.";
                //append a new line and then the information to the github page, keeping any < and > characters
                remarks.InnerXml += "<br/>" + githubInfo;

                if (SettingsObject.GetSerializedSettings().FindProperty("m_embedParamsInSummary").boolValue)
                {
                    XmlNodeList paramNodes = memberNode.SelectNodes("param");
                    if (paramNodes.Count > 0)
                    {
                        XmlNode listNode = CreateDocList(classDoc, summary, "Parameter", "Description");

                        for (int p = 0; p < paramNodes.Count; p++)
                        {
                            XmlNode paramNode = paramNodes[p];
                            string paramName = paramNode.Attributes["name"].Value;
                            string paramDescription = paramNode.InnerXml;
                            //create a list item
                            XmlNode listItem = classDoc.CreateElement("item");
                            listNode.AppendChild(listItem);
                            //create a term
                            XmlNode term = classDoc.CreateElement("term");
                            listItem.AppendChild(term);
                            term.InnerXml = paramName;
                            //create a description
                            XmlNode description = classDoc.CreateElement("description");
                            listItem.AppendChild(description);
                            description.InnerXml = paramDescription;
                        }
                    }
                }

                if (SettingsObject.GetSerializedSettings().FindProperty("m_warnOnCannys").boolValue)
                {
                    //check if this member has any known canny posts
                    XmlNode cannyPosts = memberNode.SelectSingleNode("cannyPosts");

                    if (cannyPosts != null)
                    {
                        //get all the canny posts in it
                        XmlNodeList cannyPostNodes = cannyPosts.SelectNodes("cannyPost");

                        //if ANY canny posts exist, append to the top of the summary a warning to make sure they understand any possible bugs
                        const string cannyWarning = "This {0} has known issues, check the listed cannys for more information, as its behaviour may not work as documented/expected.";
                        //determine member type
                        string memberType = determineMemberID(memberNode);
                        //append the warning to the summary
                        summary.InnerXml = string.Format(cannyWarning, memberType) + "<br/><br/>" + summary.InnerXml;

                        //create list
                        XmlNode listNode = CreateDocList(classDoc, summary, "Canny Post", "Description");

                        //loop through each canny post
                        for (int c = 0; c < cannyPostNodes.Count; c++)
                        {
                            //get the canny post
                            XmlNode cannyPost = cannyPostNodes[c];

                            //get the url
                            string url = cannyPost.Attributes["url"].Value;

                            //get the description
                            string description = cannyPost.InnerXml;

                            //create a list item
                            XmlNode listItem = classDoc.CreateElement("item");
                            listNode.AppendChild(listItem);
                            //create a term
                            XmlNode term = classDoc.CreateElement("term");
                            listItem.AppendChild(term);
                            //nest the url into a see tag
                            XmlNode seeNode = classDoc.CreateElement("see");
                            term.AppendChild(seeNode);
                            seeNode.Attributes.Append(classDoc.CreateAttribute("href"));
                            seeNode.Attributes["href"].Value = url;
                            seeNode.InnerXml = string.Format("Canny Post #{0}", c + 1);
                            //create a description
                            XmlNode descriptionNode = classDoc.CreateElement("description");
                            listItem.AppendChild(descriptionNode);
                            descriptionNode.InnerXml = description;
                        }
                    }
                }

                return memberNode;
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
        private static readonly List<string> pathsToCheck = new List<string>();
        private static readonly List<string> pathsChecked = new List<string>();


        private static int s_startTick = 0;
        public static string OnGeneratedSlnSolution(string path, string content)
        {
            //save the start tick
            s_startTick = Environment.TickCount;
            //clear the excludes
            pathsChecked.Clear();
            pathsToCheck.Clear();
            // TODO: process solution content
            return content;
        }

        public static string OnGeneratedCSProject(string path, string content)
        {
            XmlWriterSettings settings = null;
            if (SettingsObject.GetSerializedSettings().FindProperty("m_minifyDocumentation").boolValue)
            {
                //Setup the xml writer settings to minify, keeping whitespace but removing newlines
                settings = new()
                {
                    Indent = false,
                    NewLineChars = "",
                    NewLineHandling = NewLineHandling.Replace,
                    NewLineOnAttributes = false,
                };
            }
            else
            {
                //Setup the xml writer settings to be indented
                settings = new()
                {
                    Indent = true,
                    NewLineChars = "\n",
                    NewLineHandling = NewLineHandling.Replace,
                    NewLineOnAttributes = false,
                };
            }

            //check if the assembly matches ones in the documentation folder in our package
            const string basePackagePath = "Packages/com.happyrobot33.vrcsdkdocumentation";

            //parse the xml
            XmlDocument xmlDoc = new();
            xmlDoc.LoadXml(content);

            //get the include item groups, which are under the Project node
            XmlNodeList itemGroups = xmlDoc.SelectNodes("Project/ItemGroup");

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

                    if (isDLL && isInPackages && !pathsToCheck.Contains(includePath) && !pathsChecked.Contains(includePath))
                    {
                        pathsToCheck.Add(includePath);
                    }

                }
            }
            //make sure no excludes are in the paths to check
            pathsToCheck.RemoveAll((string path) => pathsChecked.Contains(path));

            foreach (string includePath in pathsToCheck)
            {
                //check if we have a folder for the assembly
                string assemblyName = System.IO.Path.GetFileNameWithoutExtension(includePath);

                List<string> docAssemblyFolders = new List<string>();
                string[] folders = System.IO.Directory.GetDirectories(basePackagePath + "/Editor/Documentation/");
                //convert to just being the folder name
                for (int f = 0; f < folders.Length; f++)
                {
                    docAssemblyFolders.Add(System.IO.Path.GetFileName(folders[f]));
                }

                //check if the assembly has a folder
                bool hasFolder = docAssemblyFolders.Contains(assemblyName);
                if (hasFolder)
                {
                    //get the assembly path
                    string assemblyPath = System.IO.Path.GetDirectoryName(includePath);

                    string xmlPath = includePath.Replace(".dll", ".xml");

                    //check if the XML file exists already
                    if (System.IO.File.Exists(xmlPath))
                    {
                        //load the xml to do a quick check on the tick count
                        XmlDocument xml = new();
                        xml.Load(xmlPath);
                        //check if the tick count matches
                        XmlNode tickNode = xml.SelectSingleNode("doc/tick");

                        //if it matches with the current tick, skip this assembly, as we already generated it this tick
                        if (tickNode != null && tickNode.InnerText == s_startTick.ToString())
                        {
                            //Debug.Log("Skipping " + assemblyName);
                            //add to the excludes list
                            pathsChecked.Add(includePath);
                            continue;
                        }
                        else
                        {
                            //Debug.Log("Regenerating " + assemblyName);
                        }
                    }

                    //early return if we are not generating documentation
                    if (SettingsObject.GetSerializedSettings().FindProperty("m_generateDocumentation").boolValue)
                    {
                        //create a new doc object
                        Doc newDoc = new()
                        {
                            AssemblyName = assemblyName,
                            DllPath = includePath,
                            creationTick = s_startTick
                        };

                        //now loop over the files in the folder, and add them to the doc object
                        string[] files = System.IO.Directory.GetFiles(basePackagePath + "/Editor/Documentation/" + assemblyName, "*.xml");

                        for (int f = 0; f < files.Length; f++)
                        {
                            newDoc.LoadInClass(files[f]);
                        }
                        //generate the xml into the correct location
                        XmlDocument generatedDoc = newDoc.Generate();

                        XmlWriter writer = XmlWriter.Create(xmlPath, settings);
                        generatedDoc.Save(writer);
                        writer.Close();
                    }
                    else
                    {
                        if (System.IO.File.Exists(xmlPath))
                        {
                            System.IO.File.Delete(xmlPath);
                        }
                    }
                }
            }

            return content;
        }
    }
}
