using System;

namespace CK.SqlServer;

/// <summary>
/// Numeric values of Sql Server messages.
/// </summary>
public class SqlErrorMessages
{
    /// <summary>
    /// The Extended property does not exist.
    /// </summary>
    public const int ExtendedPropertyDoesNotExist = 15217;

    /// <summary>
    /// When calling an in-existing stored procedure.
    /// </summary>
    public const int CouldNotFindStoredProcedure = 2812;

    /// <summary>
    /// Unfortunately classical 'invalid object name'.
    /// </summary>
    public const int InvalidObjectName = 208;

    /// <summary>
    /// Cannot resolve collation conflict for UNION operation.
    /// </summary>
    public const int CollationConflictForUnion = 446;

    /// <summary>
    /// Cannot resolve collation conflict for UNION operation (SQL Server 2005).
    /// </summary>
    public const int CollationConflictForUnion2 = 468;

    /// <summary>
    /// The text, ntext, or image data type cannot be selected as DISTINCT.
    /// </summary>
    public const int NoSelectTextDistinct = 8163;

    /// <summary>
    /// When calling sp_helpuser: "The name supplied (XXX) is not a user, role, or aliased login."
    /// </summary>
    public const int InvalidSecurityAccount = 15198;

    /// <summary>
    /// When calling sp_grantdbaccess: "User or role 'XXX' already exists in the current database."
    /// </summary>
    public const int UserAlreadyExist = 15023;

    /// <summary>
    /// When calling sp_grantlogin: "Windows NT user or group 'SPICANRUN\ASPNET' not found. Check the name again."
    /// </summary>
    public const int UnknownNTUserOrGroup = 15401;


}
