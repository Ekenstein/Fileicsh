namespace Fileicsh.Abstraction
{
    /// <inheritdoc cref="IAuthenticatedFile" />
    /// <inheritdoc cref="IFile{TExtra}" />
    /// <summary>
    /// An authenticated file containing extra data about the file.
    /// </summary>
    /// <typeparam name="TExtra">The type of the extra data.</typeparam>
    public interface IAuthenticatedFile<TExtra> : IAuthenticatedFile, IFile<TExtra>
    {
    }
}
