using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PukulTikus.Data;
using PukulTikus.Domain;
using PukulTikus.Dto;

namespace PukulTikus.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScoresController : ControllerBase
{
    private readonly GameDbContext _db;
    public ScoresController(GameDbContext db) => _db = db;

    // =========================
    // POST /api/scores
    // =========================
    [HttpPost]
    public async Task<ActionResult<ScoreDto>> Post([FromBody] ScoreCreateDto dto)
    {
        // --- Validasi dasar ---
        var name = (dto.PlayerName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 20)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["playerName"] = new[] { "PlayerName is required and must be ≤ 20 characters." }
            }));
        }

        if (dto.Score < 0 || dto.Kills < 0 || dto.MaxCombo < 0 ||
            dto.DurationSec <= 0 || dto.ValidHits < 0 ||
            dto.MissClicks < 0 || dto.PunishmentHits < 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["values"] = new[] { "Numbers must be non-negative; durationSec must be > 0." }
            }));
        }

        if (dto.ValidHits < dto.Kills)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["validHits"] = new[] { "ValidHits must be ≥ Kills (armored moles need extra hits)." }
            }));
        }

        // --- Server compute accuracy ---
        var den = dto.ValidHits + dto.MissClicks + dto.PunishmentHits;
        var accuracy = den == 0 ? 0.0 : (double)dto.ValidHits / den;

        var entity = new PlayerScore
        {
            PlayerName = name,
            Score = Math.Max(0, dto.Score),   // clamp min 0
            Kills = dto.Kills,
            MaxCombo = dto.MaxCombo,
            Accuracy = accuracy,
            DurationSec = dto.DurationSec,
            ValidHits = dto.ValidHits,
            MissClicks = dto.MissClicks,
            PunishmentHits = dto.PunishmentHits,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.PlayerScores.Add(entity);
        await _db.SaveChangesAsync();

        var result = new ScoreDto
        {
            Id = entity.Id,
            PlayerName = entity.PlayerName,
            Score = entity.Score,
            Kills = entity.Kills,
            MaxCombo = entity.MaxCombo,
            Accuracy = entity.Accuracy,
            DurationSec = entity.DurationSec,
            ValidHits = entity.ValidHits,
            MissClicks = entity.MissClicks,
            PunishmentHits = entity.PunishmentHits,
            CreatedAt = entity.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
    }

    // =========================
    // GET /api/scores/{id}
    // =========================
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ScoreDto>> GetById([FromRoute] int id)
    {
        var s = await _db.PlayerScores.FindAsync(id);
        if (s == null) return NotFound();

        return new ScoreDto
        {
            Id = s.Id,
            PlayerName = s.PlayerName,
            Score = s.Score,
            Kills = s.Kills,
            MaxCombo = s.MaxCombo,
            Accuracy = s.Accuracy,
            DurationSec = s.DurationSec,
            ValidHits = s.ValidHits,
            MissClicks = s.MissClicks,
            PunishmentHits = s.PunishmentHits,
            CreatedAt = s.CreatedAt
        };
    }

    // =========================
    // GET /api/scores/top/{n}
    // =========================
    [HttpGet("top/{n:int}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetTop([FromRoute] int n)
    {
        if (n < 1) n = 1;
        if (n > 100) n = 100;

        var list = await _db.PlayerScores
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.Kills)
            .ThenByDescending(s => s.MaxCombo)
            .ThenByDescending(s => s.CreatedAt)
            .Take(n)
            .AsNoTracking()
            .ToListAsync();

        var result = list.Select((s, i) => new LeaderboardEntryDto
        {
            Rank = i + 1,
            Id = s.Id,
            PlayerName = s.PlayerName,
            Score = s.Score,
            Kills = s.Kills,
            MaxCombo = s.MaxCombo,
            CreatedAt = s.CreatedAt
        });

        return Ok(result);
    }

    // =========================
    // (Bonus) GET /api/highscore
    // =========================
    [HttpGet("~/api/highscore")]
    public async Task<IActionResult> GetHighscore()
    {
        var s = await _db.PlayerScores
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.Kills)
            .ThenByDescending(s => s.MaxCombo)
            .ThenByDescending(s => s.CreatedAt)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (s == null) return NoContent();

        var dto = new LeaderboardEntryDto
        {
            Rank = 1,
            Id = s.Id,
            PlayerName = s.PlayerName,
            Score = s.Score,
            Kills = s.Kills,
            MaxCombo = s.MaxCombo,
            CreatedAt = s.CreatedAt
        };
        return Ok(dto);
    }

    // =========================
    // GET /api/scores  (list + filter + paging + sort)
    // =========================
    // contoh: /api/scores?player=Biken&page=1&pageSize=10&sort=-score,-kills,-maxCombo,-createdAt
    [HttpGet]
    public async Task<ActionResult<PagedScoresDto>> GetList(
        [FromQuery] string? player,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "-score,-kills,-maxCombo,-createdAt")
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        IQueryable<PlayerScore> q = _db.PlayerScores.AsNoTracking();

        // filter by player (contains)
        if (!string.IsNullOrWhiteSpace(player))
        {
            var term = player.Trim();
            q = q.Where(s => EF.Functions.Like(s.PlayerName, $"%{term}%"));
        }

        if (from.HasValue) q = q.Where(s => s.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(s => s.CreatedAt <= to.Value);

        // sorting (whitelist)
        q = ApplySort(q, sort);

        var totalItems = await q.CountAsync();
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ScoreDto
            {
                Id = s.Id,
                PlayerName = s.PlayerName,
                Score = s.Score,
                Kills = s.Kills,
                MaxCombo = s.MaxCombo,
                Accuracy = s.Accuracy,
                DurationSec = s.DurationSec,
                ValidHits = s.ValidHits,
                MissClicks = s.MissClicks,
                PunishmentHits = s.PunishmentHits,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return Ok(new PagedScoresDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        });
    }

    // =========================
    // PUT /api/scores/{id}
    // =========================
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ScoreDto>> Put([FromRoute] int id, [FromBody] ScoreUpdateDto dto)
    {
        var s = await _db.PlayerScores.FindAsync(id);
        if (s == null) return NotFound();

        // validasi
        var name = (dto.PlayerName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 20)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["playerName"] = new[] { "PlayerName is required and must be ≤ 20 characters." }
            }));

        if (dto.Score < 0 || dto.Kills < 0 || dto.MaxCombo < 0 ||
            dto.DurationSec <= 0 || dto.ValidHits < 0 ||
            dto.MissClicks < 0 || dto.PunishmentHits < 0)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["values"] = new[] { "Numbers must be non-negative; durationSec must be > 0." }
            }));

        if (dto.ValidHits < dto.Kills)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["validHits"] = new[] { "ValidHits must be ≥ Kills (armored moles need extra hits)." }
            }));

        // recompute accuracy
        var den = dto.ValidHits + dto.MissClicks + dto.PunishmentHits;
        var accuracy = den == 0 ? 0.0 : (double)dto.ValidHits / den;

        // update fields (CreatedAt & Id tidak diubah)
        s.PlayerName = name;
        s.Score = Math.Max(0, dto.Score);
        s.Kills = dto.Kills;
        s.MaxCombo = dto.MaxCombo;
        s.Accuracy = accuracy;
        s.DurationSec = dto.DurationSec;
        s.ValidHits = dto.ValidHits;
        s.MissClicks = dto.MissClicks;
        s.PunishmentHits = dto.PunishmentHits;

        await _db.SaveChangesAsync();

        return new ScoreDto
        {
            Id = s.Id,
            PlayerName = s.PlayerName,
            Score = s.Score,
            Kills = s.Kills,
            MaxCombo = s.MaxCombo,
            Accuracy = s.Accuracy,
            DurationSec = s.DurationSec,
            ValidHits = s.ValidHits,
            MissClicks = s.MissClicks,
            PunishmentHits = s.PunishmentHits,
            CreatedAt = s.CreatedAt
        };
    }

    // =========================
    // DELETE /api/scores/{id}
    // =========================
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var s = await _db.PlayerScores.FindAsync(id);
        if (s == null) return NotFound();

        _db.PlayerScores.Remove(s);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // =========================
    // Helper: Sorting whitelist
    // =========================
    private static IQueryable<PlayerScore> ApplySort(IQueryable<PlayerScore> q, string sort)
    {
        // default
        if (string.IsNullOrWhiteSpace(sort))
            sort = "-score,-kills,-maxCombo,-createdAt";

        IOrderedQueryable<PlayerScore>? ordered = null;

        foreach (var raw in sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var desc = raw.StartsWith("-");
            var key = desc ? raw[1..] : raw;

            switch (key.ToLowerInvariant())
            {
                case "score":
                    ordered = ordered == null
                        ? (desc ? q.OrderByDescending(s => s.Score) : q.OrderBy(s => s.Score))
                        : (desc ? ordered.ThenByDescending(s => s.Score) : ordered.ThenBy(s => s.Score));
                    break;
                case "kills":
                    ordered = ordered == null
                        ? (desc ? q.OrderByDescending(s => s.Kills) : q.OrderBy(s => s.Kills))
                        : (desc ? ordered.ThenByDescending(s => s.Kills) : ordered.ThenBy(s => s.Kills));
                    break;
                case "maxcombo":
                    ordered = ordered == null
                        ? (desc ? q.OrderByDescending(s => s.MaxCombo) : q.OrderBy(s => s.MaxCombo))
                        : (desc ? ordered.ThenByDescending(s => s.MaxCombo) : ordered.ThenBy(s => s.MaxCombo));
                    break;
                case "createdat":
                    ordered = ordered == null
                        ? (desc ? q.OrderByDescending(s => s.CreatedAt) : q.OrderBy(s => s.CreatedAt))
                        : (desc ? ordered.ThenByDescending(s => s.CreatedAt) : ordered.ThenBy(s => s.CreatedAt));
                    break;
                default:
                    // field tidak dikenal → diabaikan
                    break;
            }

            if (ordered != null) q = ordered;
        }

        // fallback bila tidak ada field valid
        return ordered ?? q.OrderByDescending(s => s.Score)
                          .ThenByDescending(s => s.Kills)
                          .ThenByDescending(s => s.MaxCombo)
                          .ThenByDescending(s => s.CreatedAt);
    }
}
