namespace SqlCi.ScriptRunner
{
    public class EnvironmentConfiguration
    {
        public string ConnectionString { get; set; }
        public string Name { get; set; }
        public string ResetConnectionString { get; set; }
        public bool ResetDatabase { get; set; }
    }
}