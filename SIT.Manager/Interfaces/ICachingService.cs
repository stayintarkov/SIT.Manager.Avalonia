using SIT.Manager.Services.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Interfaces;
public interface ICachingService
{
    public ICachingProvider InMemory { get; }
}
