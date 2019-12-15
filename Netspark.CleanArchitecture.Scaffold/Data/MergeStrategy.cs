namespace Netspark.CleanArchitecture.Scaffold
{
    public enum MergeStrategy
    {
        /// <summary>
        ///  Append missing files only
        /// </summary>
        Append,

        /// <summary>
        /// Write files regardless of target existence
        /// </summary>
        Overwrite,

        /// <summary>
        /// Write files only if target directory initially did not exist
        /// </summary>
        Skip,

        /// <summary>
        /// Not supported yet
        /// </summary>
        Sync
    }
}
