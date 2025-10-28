using System.Collections.Generic;

namespace PukulTikus.Dto;

public class PagedScoresDto
{
    public List<ScoreDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
