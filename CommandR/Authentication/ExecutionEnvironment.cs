namespace CommandR.Authentication
{
    /// <summary>
    /// ExecutionEnvironment is stored in the Container with LifeStyle scope and maintains
    /// the execution context.
    /// </summary>
    public class ExecutionEnvironment
    {
        public AppContext AppContext { get; set; }
    };
}
