using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroTech.Messages.FlightFlow.AsyncCommands;

public interface IMessage
{
    public string MessageId { get; set; } 
}