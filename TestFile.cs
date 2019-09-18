namespace unit_test_generator
{
    public class TestFile
    {
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string[] AsyncMethods { get; set; }
        public string[] NonAsyncMethods { get; set; }
        public string[] Dependencies { get; set; }
    }
}
