using System;

namespace SqlCi.ScriptRunner
{
    public class Script
    {
        internal Script(string id, string name, string release, DateTime appliedOnUtc)
        {
            Id = id;
            Name = name;
            Release = release;
            AppliedOnUtc = appliedOnUtc;
        }

        public DateTime AppliedOnUtc { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Release { get; set; }
    }
}