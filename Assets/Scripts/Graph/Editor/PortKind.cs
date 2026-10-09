namespace GraphEditor
{
    /// <summary>
    /// Tags a GraphView <c>Port</c> (via <c>Port.userData</c>) as carrying either
    /// execution flow or a typed value, so connections can only be made within a kind.
    /// </summary>
    internal enum PortKind
    {
        Exec,
        Value,
    }
}
