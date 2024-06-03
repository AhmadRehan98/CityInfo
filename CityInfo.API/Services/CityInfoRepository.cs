using CityInfo.API.DbContexts;
using CityInfo.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CityInfo.API.Services
{
    public class CityInfoRepository : ICityInfoRepository
    {
        private readonly CityInfoContext _context;
        public CityInfoRepository(CityInfoContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        // have to add async since the method will await since it's async.
        public async Task<IEnumerable<City>> GetCitiesAsync()
        {
            return await _context.Cities.OrderBy(c=>c.Name).ToListAsync();
        }

        public async Task<(IEnumerable<City>, PaginationMetaData)> GetCitiesAsync(string? name, string? searchQuery, int pageNumber, int pageSize)
        {
            // commented out after pagination has been implemented
            //if (string.IsNullOrEmpty(name) && string.IsNullOrWhiteSpace(searchQuery))
            //{
            //    return await GetCitiesAsync();
            //}

            // collection to start from
            var collection = _context.Cities as IQueryable<City>; // allows defered execution by casting to IQueryable

            // only filter
            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim(); // get rid of leading and trailing spaces
                collection = collection.Where(c=> c.Name == name);
            }

            // only search query
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.Trim(); // get rid of leading and trailing spaces
                collection = collection.Where(a => a.Name.Contains(searchQuery) || (a.Description != null && a.Description.Contains(searchQuery)));
            }

            var totalItemCount = await collection.CountAsync();

            var paginationMetadata = new PaginationMetaData(totalItemCount, pageSize, pageNumber);

            var collectionToReturn = await collection.OrderBy(c => c.Name)
                .Skip(pageSize * (pageNumber - 1))
                .Take(pageSize)
                .ToListAsync();

            return (collectionToReturn, paginationMetadata);
        }

        public async Task<City?> GetCityAsync(int cityId, bool includePointsOfInterest)
        {
            if (includePointsOfInterest)
            {
                return await _context.Cities.Include(c => c.PointsOfInterest).Where(c => c.Id == cityId).FirstOrDefaultAsync();
            }
            return await _context.Cities.Where(c => c.Id == cityId).FirstOrDefaultAsync();
        }

        public async Task<bool> CityExistsAsync(int cityId)
        {
            return await _context.Cities.AnyAsync(c => c.Id == cityId);
        }

        public async Task<PointOfInterest?> GetPointOfInterestForCityAsync(int cityId, int pointOfInterest)
        {
            return await _context.PointOfInterests.Where(p => p.CityId == cityId && p.Id == pointOfInterest).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<PointOfInterest>> GetPointsOfInterestForCityAsync(int cityId)
        {
            return await _context.PointOfInterests.Where(p => p.CityId == cityId).ToListAsync();
        }

        public async Task AddPointOfInterestForCityAsync(int cityId, PointOfInterest pointOfInterest)
        {
            var city = await GetCityAsync(cityId, false);
            if (city != null)
            {
                city.PointsOfInterest.Add(pointOfInterest);
            }
        }

        public void DeletePointOfInterest(PointOfInterest pointOfInterest)
        {
            _context.PointOfInterests.Remove(pointOfInterest);
        }

        public async Task<bool> CityNameMatchesCityId(string? cityName, int cityId)
        {
            return await _context.Cities.AnyAsync(c => c.Id == cityId && c.Name == cityName);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return (await _context.SaveChangesAsync() >= 0); // return true if zero or more entities have been successfully been saved
        }
    }
}
