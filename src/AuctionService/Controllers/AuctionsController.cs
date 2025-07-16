using AuctionService.Data;
using AuctionService.DTOs;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuctionsController : ControllerBase
    {
        private readonly AuctionDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;

        public AuctionsController(AuctionDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
        { 
            _context = context;
            _mapper = mapper;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<ActionResult<List<AuctionDto>>> GetAuctions(string date)
        {
            var query = _context.Auctions.OrderBy(x=>x.Item.Make).AsQueryable();

            if (!string.IsNullOrEmpty(date))
            {
                query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
            }

            //var auctions = await _context.Auctions
            //    .Include(x => x.Item)
            //    .OrderBy(x => x.Item.Make).ToListAsync();
               
            //var auctionDtos = _mapper.Map<List<AuctionDto>>(auctions);

            //return Ok(auctionDtos);

            return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
        {
            var auction = await _context.Auctions
                .Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (auction == null)
            {
                return NotFound();
            }
            var auctionDto = _mapper.Map<AuctionDto>(auction);
            return Ok(auctionDto);
        }

        [HttpPost]
        public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
        {
            var auction = _mapper.Map<Entities.Auction>(auctionDto);
            //TODO: add current user as seller
            auction.Seller = "Test";
            _context.Auctions.Add(auction);

            var newAuction = _mapper.Map<AuctionDto>(auction);
            await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

            var result = await _context.SaveChangesAsync() > 0;

            //var newAuction = _mapper.Map<AuctionDto>(auction);
            //await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

            if (!result) return BadRequest("Could not save changes to DB");
            // Map the created auction to AuctionDto
            var createdAuctionDto = _mapper.Map<AuctionDto>(auction);
            return CreatedAtAction(nameof(GetAuctionById), new { id = createdAuctionDto.Id }, createdAuctionDto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<AuctionDto>> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
        {
            var auction = await _context.Auctions.Include(x=>x.Item).FirstOrDefaultAsync(x=>x.Id == id);
            if (auction == null)
            {
                return NotFound();
            }
            //TODO: check seller == username

            auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
            auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
            auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
            auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
            auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

            await _publishEndpoint.Publish(_mapper.Map<AuctionUpdated>(auction));

            var result = await _context.SaveChangesAsync() > 0;
            if (!result) return BadRequest("Could not update auction");
            var updatedAuctionDto = _mapper.Map<AuctionDto>(auction);
            return Ok(updatedAuctionDto);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuction(Guid id)
        {
            var auction = await _context.Auctions.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == id);

            if (auction == null) { 
                return NotFound();
            }
            //TODO: check seller == username
            _context.Auctions.Remove(auction);

            //publishing to message Bus
            await _publishEndpoint.Publish<AuctionDeleted>(new { Id = auction.Id.ToString() });

            var result = await _context.SaveChangesAsync() > 0;
            if(!result) return BadRequest("Could not delete auction");

            return Ok();
        }
    }
}
