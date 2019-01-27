namespace Fileicsh.Abstraction
{
    /// <inheritdoc />
    /// <summary>
    /// Information of a file together with extra data
    /// which describes the file in a more complex way.
    /// </summary>
    /// <typeparam name="TExtra">The type encapsulating the extra data about the file.</typeparam>
    public interface IFileInfo<out TExtra> : IFileInfo
    {
        /// <summary>
        /// Extra data describing the file.
        /// </summary>
        TExtra Extra { get; }
    }
}
