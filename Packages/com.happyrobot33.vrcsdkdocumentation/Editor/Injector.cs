using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
//xml
using System.Xml;
using VRC.PackageManagement.Core;
using VRC.Udon.EditorBindings;
using System.Linq;
using VRC.Udon.Graph;

//TODO: Make the VSCode compatibility features optional and controllable in the unity editor preferences section
namespace Happyrobot33.VRCSDKDocumentationProject
{
    public class Injector : AssetPostprocessor
    {
        private static readonly List<string> PathsToCheck = new List<string>();
        private static readonly List<string> PathsChecked = new List<string>();


        private static int s_startTick = 0;
        public static string OnGeneratedSlnSolution(string path, string content)
        {
            Debug.Log("Injecting XML documentation....");

            GenerateExternalNodeDefinitionsList();

            //save the start tick
            s_startTick = Environment.TickCount;
            //clear the excludes
            PathsChecked.Clear();
            PathsToCheck.Clear();
            return content;
        }

        /// <summary>
        /// Generates a external file list of all the node definitions
        /// </summary>
        private static void GenerateExternalNodeDefinitionsList()
        {
            try {
                //Ripped directly from U#, hopefully doesnt break
                UdonEditorInterface _editorInterfaceInstance = new UdonEditorInterface();
                var nodeDefinitions = new HashSet<string>(_editorInterfaceInstance.GetNodeDefinitions().Select(e => e.fullName));
                IEnumerable<UdonNodeDefinition> nodeDefinitionsArray = _editorInterfaceInstance.GetNodeDefinitions();

                string output = "";
                foreach (UdonNodeDefinition nodeDefinition in nodeDefinitionsArray)
                {
                    output += nodeDefinition.fullName + "\n";
                }

                //print size
                Debug.Log("Node Definitions Parsed: " + nodeDefinitions.Count);
                //get the root of the unity project
                string root = System.IO.Path.GetDirectoryName(Application.dataPath);

                //write to file at root
                System.IO.File.WriteAllText(root + "/NodeDefinitions.txt", output);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to generate node definitions list: " + e.Message);
            }
        }


        public static string OnGeneratedCSProject(string path, string content)
        {
            XmlWriterSettings settings = null;
            if (SettingsObject.GetSerializedSettings().FindProperty("m_minifyDocumentation").boolValue)
            {
                //Setup the xml writer settings to minify, keeping whitespace but removing newlines
                settings = new XmlWriterSettings
                {
                    Indent = false,
                    NewLineChars = "",
                    NewLineHandling = NewLineHandling.Replace,
                    NewLineOnAttributes = false
                };
            }
            else
            {
                //Setup the xml writer settings to be indented
                settings = new XmlWriterSettings
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
            XmlDocument xmlDoc = new XmlDocument();
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

                    if (isDLL && isInPackages && !PathsToCheck.Contains(includePath) && !PathsChecked.Contains(includePath))
                    {
                        //Debug.Log("Adding " + includePath);
                        PathsToCheck.Add(includePath);
                    }
                }
            }
            //make sure no excludes are in the paths to check
            PathsToCheck.RemoveAll((string p) => PathsChecked.Contains(p));

            foreach (string includePath in PathsToCheck)
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
                        XmlDocument xml = new XmlDocument();
                        xml.Load(xmlPath);
                        //check if the tick count matches
                        XmlNode tickNode = xml.SelectSingleNode("doc/tick");

                        //if it matches with the current tick, skip this assembly, as we already generated it this tick
                        if (tickNode != null && tickNode.InnerText == s_startTick.ToString())
                        {
                            //Debug.Log("Skipping " + assemblyName);
                            //add to the excludes list
                            PathsChecked.Add(includePath);
                            continue;
                        }
                        else
                        {
                            //Debug.Log("Regenerating " + assemblyName);
                        }
                    }

                    Debug.Log(string.Format("Generating XML Doc for {0}...", assemblyName));

                    //early return if we are not generating documentation
                    if (SettingsObject.GetSerializedSettings().FindProperty("m_generateDocumentation").boolValue)
                    {
                        //create a new doc object
                        Doc newDoc = new Doc(assemblyName, assemblyPath, s_startTick);

                        //now loop over the files in the folder, and add them to the doc object
                        string[] files = System.IO.Directory.GetFiles(basePackagePath + "/Editor/Documentation/" + assemblyName, "*.xml");

                        for (int f = 0; f < files.Length; f++)
                        {
                            newDoc.LoadInClass(files[f]);
                        }
                        //generate the xml into the correct location
                        XmlDocument generatedDoc = newDoc.Generate();
                        generatedDoc.PreserveWhitespace = false;

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
