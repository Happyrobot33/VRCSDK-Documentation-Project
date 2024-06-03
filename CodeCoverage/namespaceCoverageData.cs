// import ICSharpCode.Decompiler
using ICSharpCode.Decompiler.TypeSystem;

public partial class Program
{
    /// <summary>
    /// A class to store the coverage data of a namespace
    /// </summary>
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

            //determine the total coverage of the namespace
            int total = GetTotalElements();
            int totalData = data.GetTotalElements();

            result += string.Format("Namespace: {0} ({1}/{2} ({3}%)\n", namespaceName, total, totalData, CalculateCoveragePercentage(total, totalData));
            if (data.publicTypes.Count > 0)
            {
                result += string.Format("{0}Public Types: {1}/{2} ({3}%)\n", Tab, types.Count, data.publicTypes.Count, CalculateCoveragePercentage(types.Count, data.publicTypes.Count));
                result += CreateDefinitionList(types, data.publicTypes);
            }

            if (data.publicFields.Count > 0)
            {
                result += string.Format("{0}Public Fields: {1}/{2} ({3}%)\n", Tab, fields.Count, data.publicFields.Count, CalculateCoveragePercentage(fields.Count, data.publicFields.Count));
                result += CreateDefinitionList(fields, data.publicFields);
            }
            if (data.publicProperties.Count > 0)
            {
                result += string.Format("{0}Public Properties: {1}/{2} ({3}%)\n", Tab, propertys.Count, data.publicProperties.Count, CalculateCoveragePercentage(propertys.Count, data.publicProperties.Count));
                result += CreateDefinitionList(propertys, data.publicProperties);
            }
            if (data.publicMethods.Count > 0)
            {
                result += string.Format("{0}Public Methods: {1}/{2} ({3}%)\n", Tab, methods.Count, data.publicMethods.Count, CalculateCoveragePercentage(methods.Count, data.publicMethods.Count));
                result += CreateDefinitionList(methods, data.publicMethods);
            }
            if (data.publicEvents.Count > 0)
            {
                result += string.Format("{0}Public Events: {1}/{2} ({3}%)\n", Tab, events.Count, data.publicEvents.Count, CalculateCoveragePercentage(events.Count, data.publicEvents.Count));
                result += CreateDefinitionList(events, data.publicEvents);
            }


            return result;
        }

        public int GetTotalElements()
        {
            return types.Count + fields.Count + propertys.Count + methods.Count + events.Count;
        }


        private string CalculateCoveragePercentage(int namespaceDataCount, int assemblyDataCount)
        {
            return (namespaceDataCount / (float)assemblyDataCount * 100).ToString("0.00");
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
                string definedString = defined ? Green + "Y" : Red + "N";
                definedString += Reset;

                IField field = namedElement as IField;
                IType symbol = field != null ? field.ReturnType : null;
                string kind = symbol != null ? symbol.Kind.ToString() : "";

                //make sure the kind ends on the same ammount of characters
                while (kind.Length < 10)
                {
                    kind += " ";
                }

                //format the kind as yellow
                kind = Yellow + kind + Reset;


                result += String.Format("{0}{0}{1} {2} {3}\n", Tab, definedString, kind, GetXMLNameString(namedElement));
            }

            return result;
        }

        public override string ToString()
        {
            return CompareTo(publicData);
        }
    }
}
