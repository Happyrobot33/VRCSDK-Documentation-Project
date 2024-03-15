// import ICSharpCode.Decompiler
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.CSharp.Syntax;
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
    
    public static void Main(string[] args)
    {
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
        //TODO: Add in VRC.Udon.common and VRC.Udon.Editor

        List<assemblyPublicData> assemblyData = new List<assemblyPublicData>();
        foreach (string testAssemblyPath in assemblys)
        {
            var data = GetAssemblyPublicData(testAssemblyPath);
            assemblyData.Add(data);
        }

        Console.WriteLine("Loaded " + assemblyData.Count + " assemblies");

        List<coverageData> coverageData = new List<coverageData>();

        //compare the assembly data to the XML data
        foreach (assemblyPublicData data in assemblyData)
        {
            //merge all of the lists into a single INamedElement list
            List<INamedElement> allElements = new List<INamedElement>();
            allElements.AddRange(data.publicFields);
            allElements.AddRange(data.publicProperties);
            allElements.AddRange(data.publicMethods);
            allElements.AddRange(data.publicEvents);
            allElements.AddRange(data.publicTypes);

            //loop through each element
            int fieldCount = 0;
            int propertyCount = 0;
            int methodCount = 0;
            int eventCount = 0;
            int typeCount = 0;
            foreach (INamedElement element in allElements)
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

                //find if it is in the XML by doing *:FullName
                if (typeMembers.Contains("T:" + searchString))
                {
                    typeCount++;
                }
                else if (fieldMembers.Contains("F:" + searchString))
                {
                    fieldCount++;
                }
                else if (propertyMembers.Contains("P:" + searchString))
                {
                    propertyCount++;
                }
                else if (methodMembers.Contains("M:" + searchString))
                {
                    methodCount++;
                }
                else if (eventMembers.Contains("E:" + searchString))
                {
                    eventCount++;
                }
            }

            //add the coverage data to the list
            coverageData.Add(new coverageData(data, typeCount, fieldCount, propertyCount, methodCount, eventCount));
        }

        //tally up total coverage data and print it
        foreach (coverageData data in coverageData)
        {
            Console.WriteLine(data);
        }
    }

    private class coverageData
    {
        public assemblyPublicData assemblyData;
        public int typeCount;
        public int fieldCount;
        public int propertyCount;
        public int methodCount;
        public int eventCount;

        public coverageData(assemblyPublicData assemblyData, int typeCount, int fieldCount, int propertyCount, int methodCount, int eventCount)
        {
            this.assemblyData = assemblyData;
            this.typeCount = typeCount;
            this.fieldCount = fieldCount;
            this.propertyCount = propertyCount;
            this.methodCount = methodCount;
            this.eventCount = eventCount;
        }

        public override string ToString()
        {
            return "Assembly: " + assemblyData.assemblyName + "\n" +
                "   Type Count: " + typeCount + "/" + assemblyData.publicTypes.Count + "(" + (typeCount / (float)assemblyData.publicTypes.Count * 100) + "%)\n" +
                "   Field Count: " + fieldCount + "/" + assemblyData.publicFields.Count + "(" + (fieldCount / (float)assemblyData.publicFields.Count * 100) + "%)\n" +
                "   Property Count: " + propertyCount + "/" + assemblyData.publicProperties.Count + "(" + (propertyCount / (float)assemblyData.publicProperties.Count * 100) + "%)\n" +
                "   Method Count: " + methodCount + "/" + assemblyData.publicMethods.Count + "(" + (methodCount / (float)assemblyData.publicMethods.Count * 100) + "%)\n" +
                "   Event Count: " + eventCount + "/" + assemblyData.publicEvents.Count + "(" + (eventCount / (float)assemblyData.publicEvents.Count * 100) + "%)";
        }
    }

    private class assemblyPublicData
    {
        public string assemblyName;
        public List<IField> publicFields;
        public List<IProperty> publicProperties;
        public List<IEvent> publicEvents;
        public List<ITypeDefinition> publicTypes;
        public List<IMethod> publicMethods;

        public assemblyPublicData(string assemblyName, List<IField> publicFields, List<IProperty> publicProperties, List<IEvent> publicEvents, List<ITypeDefinition> publicTypes, List<IMethod> publicMethods)
        {
            this.assemblyName = assemblyName;
            this.publicFields = publicFields;
            this.publicProperties = publicProperties;
            this.publicEvents = publicEvents;
            this.publicTypes = publicTypes;
            this.publicMethods = publicMethods;
        }

        public override string ToString()
        {
            return "Assembly: " + assemblyName + "\n" +
                "   Public Types: " + publicTypes.Count + "\n" +
                "   Public Fields: " + publicFields.Count + "\n" +
                "   Public Properties: " + publicProperties.Count + "\n" +
                "   Public Methods: " + publicMethods.Count + "\n" +
                "   Public Events: " + publicEvents.Count;
        }
    }

    private static assemblyPublicData GetAssemblyPublicData(string testAssemblyPath)
    {
        var resolver = new UniversalAssemblyResolver(testAssemblyPath, false, testAssemblyPath);
        resolver.AddSearchDirectory(Path.GetDirectoryName(testAssemblyPath));

        //load the solution
        var decompiler = new CSharpDecompiler(testAssemblyPath, resolver, new DecompilerSettings());

        //get all public methods
        var typeDefs = decompiler.TypeSystem.MainModule.TypeDefinitions;
        //tally up all the public methods
        List<IMethod> publicMethods = new List<IMethod>();
        foreach (var type in typeDefs)
        {
            foreach (IMethod method in type.Methods)
            {
                //get the member
                IEntity member = method;
                if (member.Accessibility == Accessibility.Public)
                {
                    publicMethods.Add(method);
                }
            }
        }

        //tally up all the public fields
        List<IField> publicFields = new List<IField>();
        foreach (var type in typeDefs)
        {
            foreach (IField field in type.Fields)
            {
                //get the member
                IEntity member = field;
                if (member.Accessibility == Accessibility.Public)
                {
                    //get the field type
                    IType symbol = field.ReturnType;
                    //skip if a delegate
                    if (symbol.Kind == TypeKind.Delegate)
                    {
                        continue;
                    }
                    publicFields.Add(field);
                }
            }
        }

        //tally up all the public properties
        List<IProperty> publicProperties = new List<IProperty>();
        foreach (var type in typeDefs)
        {
            foreach (IProperty property in type.Properties)
            {
                //get the member
                IEntity member = property;
                if (member.Accessibility == Accessibility.Public)
                {
                    publicProperties.Add(property);
                }
            }
        }

        //tally up all the public events
        List<IEvent> publicEvents = new List<IEvent>();
        foreach (var type in typeDefs)
        {
            foreach (IEvent ev in type.Events)
            {
                //get the member
                IEntity member = ev;
                if (member.Accessibility == Accessibility.Public)
                {
                    publicEvents.Add(ev);
                }
            }
        }

        //tally up all type definitions
        List<ITypeDefinition> publicTypes = new List<ITypeDefinition>();
        foreach (var type in typeDefs)
        {
            //get the member
            IEntity member = type;
            if (member.Accessibility == Accessibility.Public)
            {
                publicTypes.Add(type);
            }
        }

        //make the object
        assemblyPublicData data = new assemblyPublicData(Path.GetFileName(testAssemblyPath), publicFields, publicProperties, publicEvents, publicTypes, publicMethods);
        return data;
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
