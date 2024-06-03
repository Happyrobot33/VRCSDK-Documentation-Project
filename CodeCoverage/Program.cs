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
    const string Green = "\x1b[32m";
    const string Yellow = "\x1b[33m";
    const string Orange = "\x1b[38;5;208m";
    const string Red = "\x1b[31m";
    const string Blue = "\x1b[34m";
    const string Reset = "\x1b[39m";

    //static string basePath = @"E:\Storage\Personal\Coding\VRChat\VRCSDK-Documentation-Project";
    static List<string> assemblys = new List<string>();
    //static string testAssemblyPath = workingDirectory + @"\Packages\com.vrchat.base\Runtime\VRCSDK\Plugins\VRCSDKBase.dll";
    static string workingDirectory = Environment.CurrentDirectory;

    static string XMLPath = "Packages/com.happyrobot33.vrcsdkdocumentation/Editor/Documentation";

    static List<string> typeMembers = new List<string>();
    static List<string> fieldMembers = new List<string>();
    static List<string> propertyMembers = new List<string>();
    static List<string> methodMembers = new List<string>();
    static List<string> eventMembers = new List<string>();

    const string incompleteMarker = "incomplete";

    /// <summary>
    /// A list of namespaces that should be ignored. a * can be used as a wildcard
    /// </summary>
    static List<string> namespaceBlacklist = new List<string>();

    public static void Main(string[] args)
    {
        //clear the terminal
        Console.Clear();

        //check if we can see the Packages folder
        Console.WriteLine("Initial Working Directory: " + workingDirectory);
        //see if the packages folder exists
        if(!Directory.Exists(Path.Combine(workingDirectory, "Packages")))
        {
            //if we cant, then we are in the wrong directory, so go up a few directories
            workingDirectory = Directory.GetParent(workingDirectory).Parent.Parent.Parent.FullName;
        }
        Console.WriteLine("Working Directory: " + workingDirectory);
        //list everything in this directory
        string[] directories = Directory.GetDirectories(workingDirectory);
        foreach (string directory in directories)
        {
            Console.WriteLine("Directory: " + directory);
        }

        XMLPath = Path.Combine(workingDirectory, XMLPath);

        //populate the namespace blacklist
        namespaceBlacklist.Add("VRC.SDKBase.Validation.*");
        //namespaceBlacklist.Add("VRC.SDK3");
        namespaceBlacklist.Add("VRC.SDKBase.RPC");
        namespaceBlacklist.Add("VRC.SDKBase.Network");
        namespaceBlacklist.Add("VRC.SDKBase.Editor.Attributes");
        namespaceBlacklist.Add("VRC.Core.Burst");
        namespaceBlacklist.Add("VRC.SDKBase.Editor.Source");
        namespaceBlacklist.Add("VRC.SDKBase.Editor.V3");
        namespaceBlacklist.Add("VRC.SDKBase.Editor.Validation");
        namespaceBlacklist.Add("VRC.SDKBase.Source.Validation.Performance.Scanners");

        #region XML Handling
        //load ALL XML files recursively at once and merge them into a single XML
        string[] xmlFiles = Directory.GetFiles(XMLPath, "*.xml", SearchOption.AllDirectories);
        XmlDocument doc = new XmlDocument();
        //add the members node
        doc.LoadXml("<members></members>");
        //get a reference to the members list
        XmlNode membersNode = doc.GetElementsByTagName("members")[0];
        int loadedFiles = 0;
        Console.WriteLine("Loading XML files...");
        //merge the members into a single members list
        foreach (string file in xmlFiles)
        {
            //load the XML file
            XmlDocument fileDoc = new XmlDocument();
            fileDoc.Load(file);

            //get the members node
            XmlNode fileMembersNode = fileDoc.GetElementsByTagName("members")[0];
            //add each member to the main members list
            foreach (XmlNode member in fileMembersNode.ChildNodes)
            {
                membersNode.AppendChild(doc.ImportNode(member, true));
            }

            Console.WriteLine("Loaded " + Path.GetFileName(file));
            loadedFiles++;
        }
        //print total size
        Console.WriteLine("Total XML size: " + doc.InnerXml.Length);
        Console.WriteLine("Loaded " + loadedFiles + " XML files");

        //convert to individual lists of member types. All we care about is the name field of the member
        foreach (XmlNode member in membersNode.ChildNodes)
        {
            //check if there is a node called incomplete, if there is, then skip this member
            if (member[incompleteMarker] != null)
            {
                continue;
            }

            string memberID = DetermineMemberID(member);
            if (memberID == "Type")
            {
                typeMembers.Add(member.Attributes["name"].Value);
            }
            else if (memberID == "Field")
            {
                fieldMembers.Add(member.Attributes["name"].Value);
            }
            else if (memberID == "Property")
            {
                propertyMembers.Add(member.Attributes["name"].Value);
            }
            else if (memberID == "Method")
            {
                methodMembers.Add(member.Attributes["name"].Value);
            }
            else if (memberID == "Event")
            {
                eventMembers.Add(member.Attributes["name"].Value);
            }
        }

        Console.WriteLine("Loaded Types: " + typeMembers.Count);
        Console.WriteLine("Loaded Fields: " + fieldMembers.Count);
        Console.WriteLine("Loaded Properties: " + propertyMembers.Count);
        Console.WriteLine("Loaded Methods: " + methodMembers.Count);
        Console.WriteLine("Loaded Events: " + eventMembers.Count);
        #endregion

        //load assemblys
        assemblys.Add(workingDirectory + "/Packages/com.vrchat.base/Runtime/VRCSDK/Plugins/VRCSDKBase.dll");
        assemblys.Add(workingDirectory + "/Packages/com.vrchat.base/Runtime/VRCSDK/Plugins/VRCSDKBase-Editor.dll");
        assemblys.Add(workingDirectory + "/Packages/com.vrchat.worlds/Runtime/VRCSDK/Plugins/VRCSDK3.dll");
        assemblys.Add(workingDirectory + "/Packages/com.vrchat.worlds/Runtime/VRCSDK/Plugins/VRCSDK3-Editor.dll");
        //assemblys.Add(workingDirectory + @"\Packages\com.vrchat.worlds\Runtime\Udon\External\VRC.Udon.Common.dll");

        //generate the source code for documentation purposes
        GenerateSourceCode();

        //make the dictionary to store the public data
        List<namespaceAssemblyData> assemblyData = new List<namespaceAssemblyData>();
        foreach (string testAssemblyPath in assemblys)
        {
            PopulateAssemblyData(testAssemblyPath, ref assemblyData);
        }

        Console.WriteLine("Loaded " + assemblyData.Count + " namespaces");

        List<namespaceCoverageData> coverageData = new List<namespaceCoverageData>();

        //compare the assembly data to the XML data
        foreach (namespaceAssemblyData data in assemblyData)
        {
            //merge all of the lists into a single INamedElement list
            List<INamedElement> allElements = new List<INamedElement>();
            allElements.AddRange(data.publicFields);
            allElements.AddRange(data.publicProperties);
            allElements.AddRange(data.publicMethods);
            allElements.AddRange(data.publicEvents);
            allElements.AddRange(data.publicTypes);

            //loop through each element
            List<string> fields = new List<string>();
            List<string> propertys = new List<string>();
            List<string> methods = new List<string>();
            List<string> events = new List<string>();
            List<string> types = new List<string>();
            foreach (INamedElement element in allElements)
            {
                string searchString = GetXMLNameString(element);

                //find if it is in the XML by doing *:FullName
                if (typeMembers.Contains("T:" + searchString))
                {
                    types.Add(element.Name);
                }
                else if (fieldMembers.Contains("F:" + searchString))
                {
                    fields.Add(element.Name);
                }
                else if (propertyMembers.Contains("P:" + searchString))
                {
                    propertys.Add(element.Name);
                }
                else if (methodMembers.Contains("M:" + searchString))
                {
                    methods.Add(element.Name);
                }
                else if (eventMembers.Contains("E:" + searchString))
                {
                    events.Add(element.Name);
                }
            }

            //add the coverage data to the list
            coverageData.Add(new namespaceCoverageData(data, data.namespaceName, types, fields, propertys, methods, events));
        }

        //total up the complete coverage data
        int totalCovered = 0;
        int totalToCover = 0;
        foreach (namespaceCoverageData data in coverageData)
        {
            totalCovered += data.GetTotalElements();
            totalToCover += data.publicData.GetTotalElements();
        }

        Console.WriteLine(string.Format("Total Coverage: {0}/{1} ({2}%)", totalCovered, totalToCover, totalCovered / (float)totalToCover * 100));

        //tally up total coverage data and print it
        foreach (namespaceCoverageData data in coverageData)
        {
            Console.WriteLine(data);
        }

        //System to generate XML files automatically
        //const string pathToGenerate = "VRC.SDK3.Data.DataDictionary";

        //get all classes
        foreach (namespaceAssemblyData data in assemblyData)
        {
            Console.WriteLine(string.Format("{0}Generating XML files for {1}{2}", Blue, data.namespaceName, Reset));
            foreach (ITypeDefinition type in data.publicTypes)
            {
                //skip if the type is not a normal class, so skipping things like enums
                if (type.Kind != TypeKind.Class)
                {
                    continue;
                }
                //make sure its the top level class
                if (type.DeclaringType != null)
                {
                    continue;
                }
                BlankDocGen(assemblyData, type.FullName);
                //Console.WriteLine(type.FullName);
            }
        }

        //BlankDocGen(assemblyData, pathToGenerate);
    }

    /// <summary>
    /// Generate a blank XML file for a specific namespace
    /// </summary>
    /// <param name="assemblyData"></param>
    /// <param name="pathToGenerate"></param>
    private static void BlankDocGen(List<namespaceAssemblyData> assemblyData, string pathToGenerate)
    {
        //gather up all the elements in the namespace
        List<IField> fieldsGen = new List<IField>();
        List<IProperty> propertysGen = new List<IProperty>();
        List<IMethod> methodsGen = new List<IMethod>();
        List<IEvent> eventsGen = new List<IEvent>();
        List<ITypeDefinition> typesGen = new List<ITypeDefinition>();
        foreach (namespaceAssemblyData data in assemblyData)
        {
            if (pathToGenerate.Contains(data.namespaceName))
            {
                foreach (IField field in data.publicFields)
                {
                    if (field.FullName.Contains(pathToGenerate))
                    {
                        fieldsGen.Add(field);
                    }
                }
                foreach (IProperty property in data.publicProperties)
                {
                    if (property.FullName.Contains(pathToGenerate))
                    {
                        propertysGen.Add(property);
                    }
                }
                foreach (IMethod method in data.publicMethods)
                {
                    if (method.FullName.Contains(pathToGenerate))
                    {
                        methodsGen.Add(method);
                    }
                }
                foreach (IEvent @event in data.publicEvents)
                {
                    if (@event.FullName.Contains(pathToGenerate))
                    {
                        eventsGen.Add(@event);
                    }
                }
                foreach (ITypeDefinition type in data.publicTypes)
                {
                    if (type.FullName.Contains(pathToGenerate))
                    {
                        typesGen.Add(type);
                    }
                }
            }
        }

        Console.WriteLine(string.Format("{0}Found {1} fields, {2} propertys, {3} methods, {4} events, {5} types to generate for {6}{7}", Orange, fieldsGen.Count, propertysGen.Count, methodsGen.Count, eventsGen.Count, typesGen.Count, pathToGenerate, Reset));

        //generate a XML file next to where this is
        //string xmlPath = workingDirectory + @"\GeneratedXML\" + pathToGenerate + ".xml";
        //generate the name as just the class name
        string xmlPath = workingDirectory + @"\GeneratedXML\" + pathToGenerate.Split('.').Last() + ".xml";

        //make sure the folder exists
        Directory.CreateDirectory(Path.GetDirectoryName(xmlPath));

        //create the XML document
        XmlDocument generatedDoc = new XmlDocument();
        generatedDoc.LoadXml("<members></members>");
        XmlNode generatedMembersNode = generatedDoc.GetElementsByTagName("members")[0];

        //add all the fields
        foreach (IField field in fieldsGen)
        {
            GenXML(generatedDoc, generatedMembersNode, field, "F:", fieldMembers);
        }
        foreach (IProperty property in propertysGen)
        {
            GenXML(generatedDoc, generatedMembersNode, property, "P:", propertyMembers);
        }
        foreach (IMethod method in methodsGen)
        {
            GenXML(generatedDoc, generatedMembersNode, method, "M:", methodMembers);
        }
        foreach (IEvent @event in eventsGen)
        {
            GenXML(generatedDoc, generatedMembersNode, @event, "E:", eventMembers);
        }
        foreach (ITypeDefinition type in typesGen)
        {
            GenXML(generatedDoc, generatedMembersNode, type, "T:", typeMembers);
        }

        //check if there is anything actually in the XML
        if (generatedMembersNode.ChildNodes.Count == 0)
        {
            Console.WriteLine(string.Format("{0}Skipping {1} as it has no members left to document!{2}", Green, pathToGenerate, Reset));
            //delete the file if it exists
            if (File.Exists(xmlPath))
            {
                File.Delete(xmlPath);
            }
            return;
        }

        //save it
        generatedDoc.Save(xmlPath);
    }


    private static void GenXML(XmlDocument generatedDoc, XmlNode generatedMembersNode, INamedElement element, string type, List<string> existingMembers)
    {
        if (existingMembers.Contains(type + GetXMLNameString(element)))
        {
            Console.WriteLine(string.Format("{0}Skipping {1} as it is already documented{2}", Yellow, element.Name, Reset));
            return;
        }

        XmlNode fieldNode = generatedDoc.CreateElement("member");
        XmlAttribute fieldAttribute = generatedDoc.CreateAttribute("name");
        fieldAttribute.Value = type + GetXMLNameString(element);
        fieldNode.Attributes.Append(fieldAttribute);
        //add the docURL node
        XmlNode docURLNode = generatedDoc.CreateElement("docURL");
        docURLNode.InnerText = "???";
        fieldNode.AppendChild(docURLNode);
        //add the incomplete node
        XmlNode incompleteNode = generatedDoc.CreateElement(incompleteMarker);
        fieldNode.AppendChild(incompleteNode);
        //add a summary
        XmlNode summaryNode = generatedDoc.CreateElement("summary");
        summaryNode.InnerText = "This is not documented properly yet";
        fieldNode.AppendChild(summaryNode);

        //determine if there is any parameters
        IParameterizedMember method = element as IParameterizedMember;
        if (method != null)
        {
            foreach (IParameter parameter in method.Parameters)
            {
                XmlNode paramNode = generatedDoc.CreateElement("param");
                XmlAttribute paramName = generatedDoc.CreateAttribute("name");
                paramName.Value = parameter.Name;
                paramNode.Attributes.Append(paramName);
                paramNode.InnerText = "???";
                fieldNode.AppendChild(paramNode);
            }
        }

        generatedMembersNode.AppendChild(fieldNode);
    }


    private static void GenerateSourceCode()
    {
        Console.WriteLine("Generating source code...");

        //clear the folder of previous C# files
        string decompiledSourcePath = workingDirectory + "/DecompiledSource";
        if (Directory.Exists(decompiledSourcePath))
        {
            Directory.Delete(decompiledSourcePath, true);
        }

        foreach (string assembly in assemblys)
        {
            var resolver = new UniversalAssemblyResolver(assembly, false, assembly);
            resolver.AddSearchDirectory(Path.GetDirectoryName(assembly));

            //load the solution
            var decompiler = new CSharpDecompiler(assembly, resolver, new DecompilerSettings());

            //we want to save the decompiled code to a file
            string code = decompiler.DecompileWholeModuleAsString();

            //save the code to a folder
            string path = workingDirectory + "/DecompiledSource/" + Path.GetFileNameWithoutExtension(assembly) + ".cs";

            //make sure folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            File.WriteAllText(path, code);
        }

        //make a gitignore file
        string gitignore = workingDirectory + "/DecompiledSource/.gitignore";
        File.WriteAllText(gitignore, "*");

        Console.WriteLine("Generated source code");
    }

    private static string GetXMLNameString(INamedElement element)
    {
        //get the named element as a member
        IMethod member = element as IMethod;
        IParameterizedMember parameterizedMember = member as IParameterizedMember;
        List<string> parameterNames = new List<string>();
        if (parameterizedMember != null)
        {
            IReadOnlyList<IParameter> parameters = parameterizedMember.Parameters;
            foreach (IParameter parameter in parameters)
            {
                IVariable variable = parameter as IVariable;
                parameterNames.Add(variable.Type.FullName);
            }
        }

        //assemble a full name, which looks something like "M:VRC.SDKBase.VRCPlayerApi.PlayHapticEventInHand(VRC.SDKBase.VRC_Pickup.PickupHand,System.Single,System.Single,System.Single)"
        string fullName = element.FullName;
        string searchString = fullName;
        if (parameterNames.Count > 0)
        {
            searchString += "(";
            foreach (string parameter in parameterNames)
            {
                //swap & to @
                string newparameter = parameter.Replace("&", "@");
                searchString += newparameter + ",";
            }
            searchString = searchString.TrimEnd(',');
            searchString += ")";
        }

        return searchString;
    }

    private static void PopulateAssemblyData(string testAssemblyPath, ref List<namespaceAssemblyData> dict)
    {
        var resolver = new UniversalAssemblyResolver(testAssemblyPath, false, testAssemblyPath);
        resolver.AddSearchDirectory(Path.GetDirectoryName(testAssemblyPath));

        //load the solution
        var decompiler = new CSharpDecompiler(testAssemblyPath, resolver, new DecompilerSettings());

        //get all public methods
        IEnumerable<ITypeDefinition> typeDefs = decompiler.TypeSystem.MainModule.TypeDefinitions;

        foreach (var type in typeDefs)
        {
            //the type must be public
            if (type.Accessibility != Accessibility.Public)
            {
                continue;
            }
            //the type must not be a delegate
            if (type.Kind == TypeKind.Delegate)
            {
                continue;
            }

            string namespaceName = type.Namespace;

            //check if the namespace is blacklisted
            bool blacklisted = false;
            foreach (string blacklist in namespaceBlacklist)
            {
                //replace * and .* with nothing
                if (namespaceName.Replace(".*", "").StartsWith(blacklist.Replace(".*", "")))
                {
                    blacklisted = true;
                    break;
                }
            }
            if (blacklisted)
            {
                continue;
            }

            if (!dict.Exists(x => x.namespaceName == namespaceName))
            {
                dict.Add(new namespaceAssemblyData(namespaceName));
            }

            //find the correct entry for this namespace
            namespaceAssemblyData entry = dict.Find(x => x.namespaceName == namespaceName);

            entry.publicProperties.AddRange(type.Properties.Where(x => x.Accessibility == Accessibility.Public));

            //loop through the public methods, and get all .ctor methods
            foreach (IMethod method in type.Methods)
            {
                if (method.Accessibility == Accessibility.Public)
                {
                    if (method.Name == ".ctor")
                    {
                        //determine if the .ctor is part of a enum
                        if (type.Kind == TypeKind.Enum)
                        {
                            //if it is, skip it
                            continue;
                        }
                    }

                    entry.publicMethods.Add(method);
                }
            }

            //entry.publicMethods.AddRange(type.Methods.Where(x => x.Accessibility == Accessibility.Public));
            //do some extra logic to only add fields that are not delegates
            foreach (IField field in type.Fields)
            {
                if (field.Accessibility == Accessibility.Public)
                {
                    IType symbol = field.ReturnType;
                    //check if its a struct
                    if (symbol.Kind == TypeKind.Struct)
                    {
                        //if it is, it might be the value__ field of a enum, in which case skip it
                        if (field.Name == "value__")
                        {
                            continue;
                        }
                    }

                    if (symbol.Kind != TypeKind.Delegate)
                    {
                        entry.publicFields.Add(field);
                    }
                }
            }
            entry.publicEvents.AddRange(type.Events.Where(x => x.Accessibility == Accessibility.Public));
            entry.publicTypes.Add(type);
        }
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
}
