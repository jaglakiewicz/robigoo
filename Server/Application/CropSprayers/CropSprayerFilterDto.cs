#region Imports

#endregion

namespace Server.Application.CropSprayers
{
    #region DTOs

    /// <summary>
    /// Filter parameters for listing crop sprayers.
    /// </summary>
    public class CropSprayerFilterDto
    {
        #region Properties

        public string? Q { get; set; }
        public string? Type { get; set; }
        public string? Kind { get; set; }
        public string? Manufacturer { get; set; }
        public string? YearFrom { get; set; }
        public string? YearTo { get; set; }

        #endregion
    }

    #endregion
}
