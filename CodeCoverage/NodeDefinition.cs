using ICSharpCode.Decompiler.TypeSystem;

public class NodeDefinition
{
    public List<string> possibleDefinitions;

    //implicit conversion to string
    public static implicit operator string(NodeDefinition nodeDefinition)
    {
        //if null, return INVALID
        if (nodeDefinition == null)
        {
            return "INVALID";
        }

        //if no definitions, return INVALID
        if (nodeDefinition.possibleDefinitions == null || nodeDefinition.possibleDefinitions.Count == 0)
        {
            return "INVALID";
        }

        //return the set of definitions
        return string.Join(", ", nodeDefinition.possibleDefinitions);
    }

    public NodeDefinition(List<string> definitions)
    {
        possibleDefinitions = definitions;
    }

    public NodeDefinition(string v)
    {
        possibleDefinitions = new List<string> { v };
    }

    //equality is if the definition matches
    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        NodeDefinition other = (NodeDefinition)obj;
        return possibleDefinitions.Contains(other.possibleDefinitions[0]);
    }

    static public NodeDefinition ConvertToNodeDefinition(IEvent ev)
    {
        return new NodeDefinition(string.Format("Event_{0}", ev.Name.Replace(".", "")));
    }

    static public NodeDefinition ConvertToNodeDefinition(IType type)
    {
        //will look something like this
        //Type_VRCSDK3ComponentsVRCUrlInputField
        string typeName = type.FullName.Replace(".", "");
        
        return new NodeDefinition(string.Format("Type_{0}", typeName));
    }

    static public NodeDefinition ConvertToNodeDefinition(IProperty property)
    {
        string name = property.Name.Replace(".", "");
        string nameSpace = property.FullName;
        nameSpace = nameSpace.Replace(".", "");
        nameSpace = nameSpace.Replace(name, "");

        //get the return type
        string returnType = SanitizeType(property.ReturnType);

        //VRCSDK3MidiMidiDataMidiTrack.__get_maxNote__SystemByte
        return new NodeDefinition(new List<string> { string.Format("{0}.__get_{1}__{2}", nameSpace, name, returnType), string.Format("Variable_{0}", nameSpace), string.Format("{0}.__set_{1}__{2}", nameSpace, name, returnType) });
    }

    static public NodeDefinition ConvertToNodeDefinition(IField field)
    {
        string fieldNameSpace = field.FullName.Replace(".", "");
        string fieldName = field.Name.Replace(".", "");
        fieldNameSpace = fieldNameSpace.Replace(fieldName, "");
        string returnType = SanitizeType(field.ReturnType);
        
        return new NodeDefinition(new List<string> { string.Format("Variable_{0}", fieldNameSpace), string.Format("{0}.__get_{1}__{2}", fieldNameSpace, fieldName, returnType), string.Format("{0}.__set_{1}__{2}", fieldNameSpace, fieldName, returnType) });
    }

    static public NodeDefinition ConvertToNodeDefinition(IMethod method)
    {
        string methodName = method.Name.Replace(".", "");
        //get the namespace
        string methodNameSpace = method.FullName;
        //remove all .
        methodNameSpace = methodNameSpace.Replace(".", "");
        //remove the method name
        methodNameSpace = methodNameSpace.Replace(methodName, "");

        //get the return type
        string returnType = SanitizeType(method.ReturnType);
        //if its a constructor, return the class type
        if (method.IsConstructor)
        {
            returnType = SanitizeType(method.DeclaringType);
        }
        //get the parameters
        string[] parameters = new string[method.Parameters.Count];
        for (int i = 0; i < method.Parameters.Count; i++)
        {
            parameters[i] = SanitizeType(method.Parameters[i].Type);
        }

        //if no params and is a constructor, add a blank param
        if (parameters.Length == 0 && method.IsConstructor)
        {
            parameters = new string[] { "" };
        }

        string result = string.Format("{0}.__{1}{2}__{3}", methodNameSpace, methodName, parameters.Length > 0 ? "__" + string.Join("_", parameters) : "", returnType);
        return new NodeDefinition(result);
    }

    private static string SanitizeType(IType intype)
    {
        //get the class name
        string className = intype.FullName;
        return className.Replace(",", "")
                .Replace(".", "")
                .Replace("[]", "Array")
                .Replace("&", "Ref")
                .Replace("+", "");
        //return string.Format("{0}{1}", ns, typeName);
    }
}
