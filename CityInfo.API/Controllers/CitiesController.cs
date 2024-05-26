using Microsoft.AspNetCore.Mvc;

namespace CityInfo.API.Controllers
{
    [ApiController]
    [Route("api/cities")]
    //[Route("api/[controller]")] // [controller] matches "Cities" from CitiesController
    public class CitiesController: ControllerBase
    {
        //[HttpGet("api/cities")]
        [HttpGet]
        public JsonResult GetCities()
        {
            return new JsonResult(
                new List<object>
                {
                    new { id=1, Name="New York City"},
                    new { id=2, Name="Antwerp"}
                });
        }
    }
}
