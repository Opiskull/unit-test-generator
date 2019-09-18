namespace unit_test_generator
{
    public static class StringExtensions
    {
        public static string ToCamelCase(this string classOrInterface)
        {
            if (classOrInterface.StartsWith("I"))
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
