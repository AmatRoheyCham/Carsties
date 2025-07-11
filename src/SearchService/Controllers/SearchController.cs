using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using SearchService.Model;
using SearchService.RequestHelpers;

namespace SearchService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<Item>>> SearchItems([FromQuery] SearchParams searchParams)
        {
            var query = DB.PagedSearch<Item, Item>();

            if (!string.IsNullOrEmpty(searchParams.SearchTerm)) {
                query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
            }

            query = searchParams.OrdeyBy switch
            {
                "make" => query.Sort(x => x.Ascending(a => a.Make)),
                "new" => query.Sort(x => x.Descending(a => a.CreatedAt)),
                "model" => query.Sort(x => x.Ascending(a => a.Model)),
                "year" => query.Sort(x => x.Ascending(a => a.Year)),
                "color" => query.Sort(x => x.Ascending(a => a.Color)),
                "mileage" => query.Sort(x => x.Ascending(a => a.Mileage)),
                _ => query.Sort(x => x.Ascending(a => a.AuctionEnd))
            };

            query = searchParams.FilterBy switch
            {
                //"active" => query.Match(x => x.AuctionEnd > DateTime.UtcNow && x.Status == "active"),
                //"finised" => query.Match(x => x.AuctionEnd < DateTime.UtcNow && x.Status == "ended"),
                //"sold" => query.Match(x => x.Status == "sold"),
                //_ => query
                "finished" => query.Match(x => x.AuctionEnd < DateTime.UtcNow),
                "endingSoon" => query.Match(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6) && x.AuctionEnd > DateTime.UtcNow),
                _ => query.Match(x => x.AuctionEnd > DateTime.UtcNow)
            };

            if (!string.IsNullOrEmpty(searchParams.Seller))
            {
                query.Match(x => x.Seller == searchParams.Seller);
            }
            if (!string.IsNullOrEmpty(searchParams.Winner))
            {
                query.Match(x => x.Winner == searchParams.Winner);
            }

            if (string.IsNullOrEmpty(searchParams.PageNumber.ToString()))
                query.PageNumber(searchParams.PageNumber);
            if (string.IsNullOrEmpty(searchParams.PageSize.ToString()))
                query.PageSize(searchParams.PageSize);

            var result = await query.ExecuteAsync();

            return Ok(
                new { 
                    result = result.Results,
                    pageCount = result.PageCount,
                    totalCount = result.TotalCount
                });
        }
    }
}
