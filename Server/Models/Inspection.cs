#region Imports

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace Server.Models
{
    #region Inspection Model

    public class Inspection
    {
        #region Declarations

        #endregion

        #region Constructor

        #endregion

        #region Properties

        [Key]
        public long Id { get; set; }
        public required string VehiclePlate { get; set; }
        public required string InspectorName { get; set; }
        public DateTime InspectionDate { get; set; }
        public string? Notes { get; set; }
        public List<InspectionItem> Items { get; set; } = new();

        #endregion

        #region Methods - Public

        #endregion

        #region Methods - Private

        #endregion
    }

    #endregion
}

