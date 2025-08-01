using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Domain.Exceptions
{
    /// <summary>
    /// Base class for domain-specific exceptions.
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }
}
