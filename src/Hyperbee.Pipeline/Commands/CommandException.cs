namespace Hyperbee.Pipeline.Commands;

[Serializable]
public class CommandException : Exception
{
    public CommandException()
        : base( "Command exception" )
    {
    }

    public CommandException( string message )
        : base( message )
    {
    }

    public CommandException( string message, Exception innerException )
        : base( message, innerException )
    {
    }
}