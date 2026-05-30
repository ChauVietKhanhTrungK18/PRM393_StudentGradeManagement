using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class FGImportResultDto
    {
        public int SubjectClassCount { get; set; }

        public int StudentCount { get; set; }

        public int ComponentCount { get; set; }

        public int MarkCount { get; set; }
    }
}
