using CityInfo.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace CityInfo.API.Controllers
{
    [ApiController]
    [Route("api/cities")]
    //[Route("api/[controller]")] // [controller] matches "Cities" from CitiesController
    public class CitiesController: ControllerBase
    {
        private readonly CitiesDataStore _citiesdataStore;

        public CitiesController(CitiesDataStore citiesdataStore)
        {
            _citiesdataStore = citiesdataStore ?? throw new ArgumentNullException(nameof(citiesdataStore));
        }

        //[HttpGet("api/cities")]
        [HttpGet]
        public ActionResult<IEnumerable<CityDto>> GetCities()
        {
            return Ok(_citiesdataStore.Cities);
        }

        [HttpGet("{id}")]
        public ActionResult<CityDto> GetCity(int id) 
        {
            var cityToReturn = _citiesdataStore.Cities.FirstOrDefault(c => c.Id == id);

            if (cityToReturn == null)
            {
                return NotFound();
            }

            return Ok(cityToReturn);
        }
    }
}
