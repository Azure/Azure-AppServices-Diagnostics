using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.IO;
using Diagnostics.Scripts.CompilationService.Utilities;

namespace Diagnostics.Tests.ScriptsTests
{
    public class DetectorParserTests
    {
        [Fact]

        public async void TestParserForGistClass()
        {
            string code = await File.ReadAllTextAsync(@"TestData/AutoHeal.csx");
            var gistClass = DetectorParser.GetLoadDirectiveNames(code);
            Assert.NotEmpty(gistClass);
            Assert.Equal(2, gistClass.Count);

            string programText = @"
                                namespace HelloWorld
                                {
                                    class Program
                                    {
                                        static void Main(string[] args)
                                        {
                                            Console.WriteLine(""Hello, World!"");
                                        }
                                    }
                                }";
            var gistClassEmpty = DetectorParser.GetLoadDirectiveNames(programText);
            Assert.Equal(0, gistClassEmpty.Count);
        }
    }
}
