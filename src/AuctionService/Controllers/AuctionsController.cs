using AuctionService.Data;
using AuctionService.DTOs;
using AutoMapper;
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

        public AuctionsController(AuctionDbContext context, IMapper mapper)
        { 
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<AuctionDto>>> GetAuctions()
        {
            var auctions = await _context.Auctions
                .Include(x => x.Item)
                .OrderBy(x => x.Item.Make).ToListAsync();
               
            var auctionDtos = _mapper.Map<List<AuctionDto>>(auctions);
            return Ok(auctionDtos);
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
            var result = await _context.SaveChangesAsync() > 0;

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
            auction.Item.Model = updateAuctionDto.Make ?? auction.Item.Model;
            auction.Item.Color = updateAuctionDto.Make ?? auction.Item.Make;
            auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
            auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

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

            var result = await _context.SaveChangesAsync() > 0;
            if(!result) return BadRequest("Could not delete auction");

            return Ok();
        }
    }
}
