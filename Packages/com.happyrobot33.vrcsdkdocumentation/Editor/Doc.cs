using UnityEngine;
using System.Collections.Generic;
//xml
using System.Xml;

//TODO: Make the VSCode compatibility features optional and controllable in the unity editor preferences section
namespace Happyrobot33.VRCSDKDocumentationProject
{
    public class Doc
    {
        /// <summary>
        /// The name of the assembly to be documented
        /// </summary>
        public string AssemblyName = "";
        /// <summary>
        /// The path to the dll to be documented, XML will be placed next to it
        /// </summary>
        public string DllPath = "";
        /// <summary>
        /// The tick count of when the doc was created, so we can skip generating multiple times
        /// </summary>
        public int CreationTick = 0;
        /// <summary>
        /// A list of all the class documentation xmls to merge into the main xml
        /// </summary>
        public List<XmlDocument> ClassDocs = new List<XmlDocument>();

        /// <summary>
        /// Constructor for the Doc class
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="dllPath"></param>
        /// <param name="creationTick"></param>
        public Doc(string assemblyName, string dllPath, int creationTick)
        {
            AssemblyName = assemblyName;
            DllPath = dllPath;
            CreationTick = creationTick;
        }

        public XmlDocument Generate()
        {
            //create a new xml doc
            XmlDocument xmldoc = new XmlDocument();

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
                    //get the name attribute of the member
                    string memberName = memberNode.Attributes["name"].Value;

                    ValidateMemberName(memberName);

                    XmlNode processedMember = ProcessMember(classDoc, memberNode);

                    //add the member to the main xml, under the members node
                    members.AppendChild(xmldoc.ImportNode(processedMember, true));
                }
            }

            //append the tick count to the xml
            XmlNode tickNode = xmldoc.CreateElement("tick");
            tickNode.InnerText = CreationTick.ToString();
            doc.AppendChild(tickNode);

            return xmldoc;
        }

        /// <summary>
        /// Validates the member name to make sure it will render properly, and logs a warning if it will not
        /// </summary>
        /// <param name="memberName"></param>
        private static void ValidateMemberName(string memberName)
        {
            //log a warning if the member has any whitespace in the name
            if (memberName.Contains(" "))
            {
                Debug.LogWarning("Member " + memberName + " has whitespace in its name, this will cause this member to not render properly");
            }
            
            //check if it is a method
            if (memberName[0] == 'M')
            {
                //if it ends in (), that means it is a malformed method, as methods without any parameters dont have the () at the end
                if (memberName.EndsWith("()"))
                {
                    Debug.LogWarning("Member " + memberName + " is a method with no parameters, but has a () at the end, this will cause this member to not render properly");
                }
            }
        }

        public void LoadInClass(string classFilePath)
        {
            XmlDocument newXmlDoc = new XmlDocument();
            newXmlDoc.Load(classFilePath);
            ClassDocs.Add(newXmlDoc);
        }

        private static string DetermineMemberID(XmlNode member)
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
            const string officialDocInfo = "Check the <a href=\"{0}\">VRChat documentation</a> for more information.";
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

                //remove the docURL tag
                memberNode.RemoveChild(docURL);
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


            const string githubInfo = "Docs generated by the <a href=\"https://github.com/Happyrobot33/VRCSDK-Documentation-Project\">VRChat SDK Documentation Project</a>.";
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
                    string memberType = DetermineMemberID(memberNode);
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
                    //remove the cannyPosts node
                    memberNode.RemoveChild(cannyPosts);
                }
            }

            //clean up summary, by finding all new lines, and making the subsequent spaces into a single space
            //TODO: Implement a better version of this since its messing with doxygen
            //summary.InnerXml = System.Text.RegularExpressions.Regex.Replace(summary.InnerXml, @"\n\s+", "\n");
            //remarks.InnerXml = System.Text.RegularExpressions.Regex.Replace(remarks.InnerXml, @"\n\s+", "\n");

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
}
