namespace SwiftCookDb.Models
{
    /// <summary>
    /// Unit family used for conversion (Ticket 21). Only Mass and Volume units are
    /// convertible, and only within their own dimension. Count and Other units are
    /// compared by identity (same unit) only.
    /// </summary>
    public enum UnitDimension
    {
        Other = 0,
        Mass = 1,
        Volume = 2,
        Count = 3
    }
}
