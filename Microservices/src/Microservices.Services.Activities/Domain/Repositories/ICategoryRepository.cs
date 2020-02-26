using Microservices.Services.Activities.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microservices.Services.Activities.Domain.Repositories
{
    public interface ICategoryRepository
    {
        Task<Category> GetAsync(string name);

        Task<IEnumerable<Category>> BrowseAsync();

        Task AddAsync(Category category);
    }
}
