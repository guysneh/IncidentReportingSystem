using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Domain.Enums
{
    /// <summary>
    /// Defines known incident categories.
    /// </summary>
    public enum IncidentCategory
    {
        Unknown,
        Infrastructure,
        Transportation,
        Security,
        PowerOutage,
        WaterSupply,
        ITSystems
    }
}
