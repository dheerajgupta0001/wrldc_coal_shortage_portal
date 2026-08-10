using System.Collections.Generic;
using System.Linq;

namespace CoalShortagePortal.WebApp.Models.GenMUs
{
    public class SaveMUResult
    {
        public int SuccessCount { get; set; }

        public int EditCount { get; set; }

        public List<string> ErrorMessages { get; set; }
            = new List<string>();

        public bool HasSuccess
        {
            get
            {
                return SuccessCount > 0 || EditCount > 0;
            }
        }

        public bool HasErrors
        {
            get
            {
                return ErrorMessages != null && ErrorMessages.Count > 0;
            }
        }
    }
}