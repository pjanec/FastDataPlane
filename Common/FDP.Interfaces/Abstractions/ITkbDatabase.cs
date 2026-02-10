using System.Collections.Generic;

namespace Fdp.Interfaces
{
    public interface ITkbDatabase
    {
        // Template registration
        void Register(TkbTemplate template);
        
        // Lookup by TkbType (primary key)
        TkbTemplate GetByType(long tkbType);
        bool TryGetByType(long tkbType, out TkbTemplate template);
        
        // Lookup by name (secondary key)  
        TkbTemplate GetByName(string name);
        bool TryGetByName(string name, out TkbTemplate template);
        
        // Enumeration
        IEnumerable<TkbTemplate> GetAll();
    }
}
