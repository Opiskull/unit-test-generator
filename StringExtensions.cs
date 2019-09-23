namespace unit_test_generator
{
    public static class StringExtensions
    {
        public static string ToTestNamespace(this string namespaceName)
        {
            var namespaces = namespaceName.Split(".");
            namespaces[0] = namespaces[0] + "Test";
            return string.Join(".", namespaces);
        }

        public static string ToTestMethodName(this string method)
        {
            return "ShouldTest" + method;
        }

        public static string ToTestClass(this string classOrInterface)
        {
            return classOrInterface + "Test";
        }

        public static string ToCamelCase(this string classOrInterface)
        {
            var secondChar = classOrInterface.Substring(1, 1);
            if (classOrInterface.StartsWith("I") && secondChar == secondChar.ToUpper())
            {
                classOrInterface = classOrInterface.Substring(1);
            }
            return classOrInterface.Substring(0, 1).ToLower() + classOrInterface.Substring(1);
        }

        public static string ToMemberName(this string classOrInterface)
        {
            return "_" + ToCamelCase(classOrInterface);
        }
    }
}
