namespace SqlCi.ScriptRunner
{
    public class ExecutionResults
    {
        private bool _wasSuccessful;

        public ExecutionResults(bool wasSuccessful)
        {
            _wasSuccessful = wasSuccessful;
        }

        public bool WasSuccessful
        {
            get { return _wasSuccessful; }
        }
    }
}