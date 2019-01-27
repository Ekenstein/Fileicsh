namespace Fileicsh.Abstraction
{
    /// <inheritdoc cref="IFileInfo{TExtra}" />
    /// <inheritdoc cref="IFile" />
    /// <summary>
    /// An abstraction of a file that contains extra information about it.
    /// </summary>
    /// <typeparam name="TExtra">The type of the extra information.</typeparam>
    public interface IFile<out TExtra> : IFileInfo<TExtra>, IFile
    {
    }
}
