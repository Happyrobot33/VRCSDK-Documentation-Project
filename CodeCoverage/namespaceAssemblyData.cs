// import ICSharpCode.Decompiler
using ICSharpCode.Decompiler.TypeSystem;

public partial class Program
{
    /// <summary>
    /// A class to store the public data of a namespace
    /// </summary>
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

            publicFields.Sort((x, y) => x.Name.CompareTo(y.Name));
            publicProperties.Sort((x, y) => x.Name.CompareTo(y.Name));
            publicEvents.Sort((x, y) => x.Name.CompareTo(y.Name));
            publicTypes.Sort((x, y) => x.Name.CompareTo(y.Name));
            publicMethods.Sort((x, y) => x.Name.CompareTo(y.Name));
        }

        public int GetTotalElements()
        {
            return publicFields.Count + publicProperties.Count + publicEvents.Count + publicTypes.Count + publicMethods.Count;
        }
    }
}
