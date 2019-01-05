namespace Ekenstein.Files
{
    public interface IFileInfo<TExtra> : IFileInfo
    {
        TExtra Extra { get; }
    }
}
