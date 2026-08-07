using Demo.Models;
using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure;

public static class QueryIncludes
{
    extension(DbSet<Invoice> dbSet)
    {
        public IQueryable<Invoice> IncludeLines() => dbSet.Include("_lines");
    }

    extension(IQueryable<Invoice> query)
    {
        public IQueryable<Invoice> IncludeLines() => query.Include("_lines");
    }
}