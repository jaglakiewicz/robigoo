#region Imports

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace Server.Models
{
    #region InspectionItem Model

    public class InspectionItem
    {
        #region Declarations

        #endregion

        #region Constructor

        #endregion

        #region Properties

        [Key]
        public long Id { get; set; }
        public long InspectionId { get; set; }
        public required string Description { get; set; }
        public bool Passed { get; set; }

        public Inspection? Inspection { get; set; }

        #endregion

        #region Methods - Public

        #endregion

        #region Methods - Private

        #endregion
    }

    #endregion
}

