using System;
using SqlCi.ScriptRunner.Exceptions;
using Xunit;

namespace SqlCi.ScriptRunner.Tests
{
    public class ExecutorTests
    {
        [Fact]
        public void NotVerifyingBeforeExecuringThrowsException()
        {
            Assert.Throws<NotVerifiedException>(() =>
            {
                var config = new Configuration();
                var executor = new Executor();
                executor.Execute(config);
            });
        } 

        [Fact]
        public void NullConfigurationThrowsAnExceptionOnExecute()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                var executor = new Executor();
                executor.Execute(null);
            });

            Assert.True(exception.ParamName.Equals("scriptConfiguration"));
        }
    }
}