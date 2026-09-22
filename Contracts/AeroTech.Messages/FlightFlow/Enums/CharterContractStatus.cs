using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroTech.Messages.FlightFlow.Enums;

public enum CharterContractStatus
{
    [Display(Name = "Draft")] Draft =1,
    [Display(Name = "Release")] Release =2
}