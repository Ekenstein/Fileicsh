namespace Ekenstein.Files
{
    /// <summary>
    /// Information about a file.
    /// </summary>
    public interface IFileInfo
    {
        /// <summary>
        /// The name of the file including possible file extension.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// The content type of the file.
        /// </summary>
        string ContentType { get; }
    }
}
