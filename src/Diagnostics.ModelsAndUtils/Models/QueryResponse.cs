namespace Diagnostics.ModelsAndUtils.Models
{
    public class QueryResponse<T>
    {
        public CompilerResponse CompilationOutput;

        public bool RuntimeSucceeded;

        public T InvocationOutput;
    }
}
