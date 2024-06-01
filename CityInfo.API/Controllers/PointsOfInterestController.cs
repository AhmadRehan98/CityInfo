using AutoMapper;
using CityInfo.API.Entities;
using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace CityInfo.API.Controllers
{
    [Route("api/cities/{cityId}/pointsofinterest")]
    [ApiController]
    public class PointsOfInterestController : ControllerBase
    {
        private readonly ILogger<PointsOfInterestController> _logger;
        private readonly IMailService _mailService;
        private readonly ICityInfoRepository _cityInfoRepository;
        private readonly IMapper _mapper;

        public PointsOfInterestController(
            ILogger<PointsOfInterestController> logger, 
            IMailService mailService,
            ICityInfoRepository cityInfoRepository,
            IMapper mapper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // this throws exception if the null check fails (no local mail service = it's null)
            _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
            _cityInfoRepository = cityInfoRepository ?? throw new ArgumentNullException(nameof(cityInfoRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));


            // if you can't inject dependencies in the ctor, you can use this instead. syntax might be wrong:
            // HttpContext.RequestServices.GetService<Logger<PointsOfInterestController>>();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PointOfInterestDto>>> GetPointsOfInterest(int cityId)
        {
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                _logger.LogInformation($"City with id {cityId} wasn't found when accessing points of interest.");
                return NotFound();
            }

            var pointsOfInterestForCity = await _cityInfoRepository.GetPointsOfInterestForCityAsync(cityId);
            return Ok(_mapper.Map<IEnumerable<PointOfInterestDto>>(pointsOfInterestForCity)); // src type is infered from pointsOfInterestForCity. dest type is IEnumerable<PointOfInterestDto>
        }
        [HttpGet("{pointofinterestid}", Name="GetPointOfInterest")]
        public async Task<ActionResult<PointOfInterestDto>> GetPointOfInterest(int cityId, int pointOfInterestId)
        {
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterest = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);
            if (pointOfInterest == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<PointOfInterestDto>(pointOfInterest));
        }

        [HttpPost]
        public async Task <ActionResult<PointOfInterestDto>> CreatePointOfInterest(int cityId, [FromBody] PointOfInterestForCreationDto pointOfInterest)
        {
            // not needed. [ApiController] does this automatically
            //if (!ModelState.IsValid)
            //{
            //    return BadRequest();
            //}

            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var finalPointOfInterest = _mapper.Map<Entities.PointOfInterest>(pointOfInterest);

            await _cityInfoRepository.AddPointOfInterestForCityAsync(cityId, finalPointOfInterest);
            await _cityInfoRepository.SaveChangesAsync();

            // map entity back to dto
            var createdPointOfInterestToReturn = _mapper.Map<Models.PointOfInterestDto>(finalPointOfInterest);

            // Created at route => 201 created code
            // basically call the GET request named GetPointOfInterest, that is the GET request above this function
            // give it the parameters in a new object
            // then the last parameter is the response body.
            return CreatedAtRoute("GetPointOfInterest", new
            {
                cityId = cityId,
                pointOfInterestId = createdPointOfInterestToReturn.Id
            },
            createdPointOfInterestToReturn);
        }


        //// put should fully update a resource
        [HttpPut("{pointofinterestid}")]
        public async Task<ActionResult> UpdatePointOfInterest(int cityId, int pointOfInterestId, PointOfInterestForUpdateDto pointOfInterest)
        {
            // can either return 204 with nothing, or 200 with the updated resource in the body

            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            // find point of interest
            var pointOfInterestEntity = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);
            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            // here the src is pointOfInterest, dest is pointOfInterestEntity
            // this will overwrite the dest object's fields with the fields from pointOfInterest
            _mapper.Map(pointOfInterest, pointOfInterestEntity);

            await _cityInfoRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{pointofinterestid}")]
        public async Task<ActionResult> PartiallyUpdatePointOfInterest(int cityId, int pointOfInterestId,
            JsonPatchDocument<PointOfInterestForUpdateDto> patchDocument)
        {
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);
            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            var pointOfInterestToPatch = _mapper.Map<PointOfInterestForUpdateDto>(pointOfInterestEntity);
            
            patchDocument.ApplyTo(pointOfInterestToPatch, ModelState);

            // checks if the JsonPatchDocument has any errors 
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // checks the pointOfInterestToPatch against the data annotations of PointOfInterestForUpdateDto
            if (!TryValidateModel(pointOfInterestToPatch))
            {
                return BadRequest(ModelState);
            }

            _mapper.Map(pointOfInterestToPatch, pointOfInterestEntity);
            await _cityInfoRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{pointofinterestid}")]
        public async Task<ActionResult> DeletePointOfInterest(int cityId, int pointOfInterestId)
        {
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            // get point of interest we want to delete
            var pointOfInterestEntity = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);
            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            _cityInfoRepository.DeletePointOfInterest(pointOfInterestEntity);
            await _cityInfoRepository.SaveChangesAsync();

            _mailService.Send("Point of interest deleted.", $"Point of interest {pointOfInterestEntity.Name} with id {pointOfInterestEntity.Id} was deleted");

            return NoContent();
        }
    }
}
