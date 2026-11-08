using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.Infrastructures.Sql;

// Shared allocation used by SepidarService and invoice exports.
// The visible invoice Number is separate from the internal spGetNextId ID.
public static class SepidarSequenceIds
{

    // Call only inside the same serializable transaction that inserts the invoice.
    
}
