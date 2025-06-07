namespace SysadminsLV.Asn1Editor.Controls
{
    /// <summary>
    /// Indicates the active panel in the main window. Could be extended in the future to support more panels.
    /// </summary>
    public enum ActivePanel
    {
        /// <summary>
        /// Just for the case
        /// </summary>
        Reserved = 0,

        /// <summary>
        /// Left Panel is active
        /// </summary>
        Left = 1,

        /// <summary>
        /// Right Panel is active
        /// </summary>
        Right = 2,
    }
}