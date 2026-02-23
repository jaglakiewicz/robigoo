#region Imports

#endregion

namespace Server.Application.History
{
    #region DTOs

    /// <summary>
    /// DTO for a change log entry with optional user details.
    /// </summary>
    public class HistoryEntryDto
    {
        #region Properties

        public long Id { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Changes { get; set; } = string.Empty;
        public string Who { get; set; } = string.Empty;
        public DateTime When { get; set; }
        public string? UserFirstName { get; set; }
        public string? UserLastName { get; set; }
        public string? UserAvatarData { get; set; }

        #endregion
    }

    #endregion
}
