namespace CommandR
{
    public interface ICommand
    {
        //Placeholder interfae for Command requests
    };

    public interface IQuery
    {
        //Placeholder interface for Query requests
    };

    public interface ITask : ICommand
    {
        //Placeholder interface for background Task request
    };

    public interface IPatchable : ICommand
    {
        string[] PatchFields { get; set; }
    };
}
