#region Imports

#endregion

namespace Server.Application.Clients
{
    #region DTOs

    /// <summary>
    /// Filter parameters for listing clients.
    /// </summary>
    public class ClientFilterDto
    {
        #region Properties

        public string? Q { get; set; }
        public string? ClientType { get; set; }
        public string? City { get; set; }

        #endregion
    }

    #endregion
}
