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
    const string basePath = @"E:\Storage\Personal\Coding\VRChat\VRCSDK-Documentation-Project";
    static List<string> assemblys = new List<string>();
    //static string testAssemblyPath = basePath + @"\Packages\com.vrchat.base\Runtime\VRCSDK\Plugins\VRCSDKBase.dll";

    static string XMLPath = basePath + @"\Packages\com.happyrobot33.vrcsdkdocumentation\Editor\Documentation";
    
    static List<string> typeMembers = new List<string>();
    static List<string> fieldMembers = new List<string>();
    static List<string> propertyMembers = new List<string>();
    static List<string> methodMembers = new List<string>();
    static List<string> eventMembers = new List<string>();

    /// <summary>
    /// A list of namespaces that should be ignored. a * can be used as a wildcard
    /// </summary>
    static List<string> namespaceBlacklist = new List<string>();
    
    public static void Main(string[] args)
    {
        //populate the namespace blacklist
        namespaceBlacklist.Add("VRC.SDKBase.Validation.*");

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
        assemblys.Add(basePath + @"\Packages\com.vrchat.base\Runtime\VRCSDK\Plugins\VRCSDKBase.dll");
        assemblys.Add(basePath + @"\Packages\com.vrchat.base\Runtime\VRCSDK\Plugins\VRCSDKBase-Editor.dll");
        assemblys.Add(basePath + @"\Packages\com.vrchat.worlds\Runtime\VRCSDK\Plugins\VRCSDK3.dll");
        assemblys.Add(basePath + @"\Packages\com.vrchat.worlds\Runtime\VRCSDK\Plugins\VRCSDK3-Editor.dll");
        assemblys.Add(basePath + @"\Packages\com.vrchat.worlds\Runtime\Udon\External\VRC.Udon.Common.dll");

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
                string searchString = GetSearchString(element);

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

        //tally up total coverage data and print it
        foreach (namespaceCoverageData data in coverageData)
        {
            Console.WriteLine(data);
        }
    }

    private static string GetSearchString(INamedElement element)
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

    private class namespaceCoverageData
    {
        public namespaceAssemblyData publicData;
        public string namespaceName;
        public List<string> types;
        public List<string> fields;
        public List<string> propertys;
        public List<string> methods;
        public List<string> events;

        public namespaceCoverageData(namespaceAssemblyData publicData, string namespaceName, List<string> typeCount, List<string> fieldCount, List<string> propertyCount, List<string> methodCount, List<string> eventCount)
        {
            this.publicData = publicData;
            this.namespaceName = namespaceName;
            this.types = typeCount;
            this.fields = fieldCount;
            this.propertys = propertyCount;
            this.methods = methodCount;
            this.events = eventCount;

            //if namespace is empty, then just represent it with "root"
            if (namespaceName == "")
            {
                this.namespaceName = "<root>";
            }
        }

        const string Tab = "   ";
        
        //make a comparer to a namespacePublicData object
        public string CompareTo(namespaceAssemblyData data)
        {
            //only compare if there is any reason to, for example if 0/0, then just dont compare
            string result = "";
            result += "Namespace: " + namespaceName + "\n";
            if (data.publicTypes.Count > 0)
            {
                result += Tab + "Public Types: " + types.Count + "/" + data.publicTypes.Count + " (" + (types.Count / (float)data.publicTypes.Count * 100).ToString("0.00") + "%)\n";
                result += CreateDefinitionList(types, data.publicTypes);
            }

            if (data.publicFields.Count > 0)
            {
                result += Tab + "Public Fields: " + fields.Count + "/" + data.publicFields.Count + " (" + (fields.Count / (float)data.publicFields.Count * 100).ToString("0.00") + "%)\n";
                result += CreateDefinitionList(fields, data.publicFields);
            }
            if (data.publicProperties.Count > 0)
            {
                result += Tab + "Public Properties: " + propertys.Count + "/" + data.publicProperties.Count + " (" + (propertys.Count / (float)data.publicProperties.Count * 100).ToString("0.00") + "%)\n";
                result += CreateDefinitionList(propertys, data.publicProperties);
            }
            if (data.publicMethods.Count > 0)
            {
                result += Tab + "Public Methods: " + methods.Count + "/" + data.publicMethods.Count + " (" + (methods.Count / (float)data.publicMethods.Count * 100).ToString("0.00") + "%)\n";
                result += CreateDefinitionList(methods, data.publicMethods);
            }
            if (data.publicEvents.Count > 0)
            {
                result += Tab + "Public Events: " + events.Count + "/" + data.publicEvents.Count + " (" + (events.Count / (float)data.publicEvents.Count * 100).ToString("0.00") + "%)\n";
                result += CreateDefinitionList(events, data.publicEvents);
            }


            return result;
        }

        private string CreateDefinitionList<T>(List<string> definedList, List<T> namedElements)
        {
            string result = "";
            foreach (INamedElement namedElement in namedElements)
            {
                //determine if we have this type defined already
                bool defined = false;
                foreach (string name in definedList)
                {
                    if (name == namedElement.Name)
                    {
                        defined = true;
                        break;
                    }
                }
                //display a checkmark or a X depending on if it is defined. Make sure its only in ascii, and no unicode
                string definedString = defined ? "\x1b[32mY" : "\x1b[31mN";
                definedString += "\x1b[39m";

                result += Tab + Tab + definedString + " " + GetSearchString(namedElement) + "\n";
            }

            return result;
        }

        public override string ToString()
        {
            return CompareTo(publicData);
        }
    }

    private class namespaceAssemblyData
    {
        public string namespaceName;
        public List<IField> publicFields;
        public List<IProperty> publicProperties;
        public List<IEvent> publicEvents;
        public List<ITypeDefinition> publicTypes;
        public List<IMethod> publicMethods;

        public namespaceAssemblyData(string namespaceName)
        {
            this.namespaceName = namespaceName;
            publicFields = new List<IField>();
            publicProperties = new List<IProperty>();
            publicEvents = new List<IEvent>();
            publicTypes = new List<ITypeDefinition>();
            publicMethods = new List<IMethod>();
        }

        public override string ToString()
        {
            return "Namespace: " + namespaceName + "\n" +
                "   Public Types: " + publicTypes.Count + "\n" +
                "   Public Fields: " + publicFields.Count + "\n" +
                "   Public Properties: " + publicProperties.Count + "\n" +
                "   Public Methods: " + publicMethods.Count + "\n" +
                "   Public Events: " + publicEvents.Count + "\n";
        }
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
            entry.publicMethods.AddRange(type.Methods.Where(x => x.Accessibility == Accessibility.Public));
            //do some extra logic to only add fields that are not delegates
            foreach (IField field in type.Fields)
            {
                if (field.Accessibility == Accessibility.Public)
                {
                    IType symbol = field.ReturnType;
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
