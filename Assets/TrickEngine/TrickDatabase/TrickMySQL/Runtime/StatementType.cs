#if UNITY_EDITOR || USE_TRICK_MYSQL
namespace TrickCore
{
    public enum StatementType
    {
        Select,
        Insert,
        Update,
        Delete,
    }
}
#endif