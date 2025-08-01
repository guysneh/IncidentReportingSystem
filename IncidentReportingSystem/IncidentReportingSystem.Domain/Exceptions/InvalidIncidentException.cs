using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Domain.Exceptions
{
    /// <summary>
    /// Exception indicating an invalid operation on an incident.
    /// </summary>
    public class InvalidIncidentOperationException : DomainException
    {
        public InvalidIncidentOperationException(string message) : base(message) { }
    }
}
