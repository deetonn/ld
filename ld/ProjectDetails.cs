
namespace Language;

public class ProjectDetails
{
    public string RootDir { get; set; }
    public string DependenciesDir { get; set; }

    public ProjectDetails(string rootDir, string dependenciesDir)
    {
        RootDir = rootDir;
        DependenciesDir = dependenciesDir;
    }
}
