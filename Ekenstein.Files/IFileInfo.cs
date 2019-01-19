namespace Ekenstein.Files
{
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
