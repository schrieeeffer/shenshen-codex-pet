namespace ShenshenPet.Core;

public static class ShenshenDataPaths
{
    public const string DataHomeEnvironmentVariable = "SHENSHEN_DATA_HOME";

    public static string DataRoot
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable(DataHomeEnvironmentVariable);
            return string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ShenshenPet")
                : Path.GetFullPath(configured);
        }
    }
}
