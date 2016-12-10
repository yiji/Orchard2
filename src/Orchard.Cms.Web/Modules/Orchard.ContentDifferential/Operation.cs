namespace Orchard.ContentDifferential
{
    /**-
     * The data structure representing a diff is a List of Diff objects:
     * {Diff(Operation.DELETE, "Hello"), Diff(Operation.INSERT, "Goodbye"),
     *  Diff(Operation.EQUAL, " world.")}
     * which means: delete "Hello", add "Goodbye" and keep " world."
     */
    public enum Operation
    {
        DELETE = -1, EQUAL = 0, INSERT = 1
    }
}
