using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PukulTikus.Data;
using PukulTikus.Domain;
using PukulTikus.Dto;

namespace PukulTikus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SavesController : ControllerBase
    {
        private readonly GameDbContext _db;
        public SavesController(GameDbContext db) => _db = db;

        // GET /api/saves/{playerName}
        [HttpGet("{playerName}")]
        public async Task<ActionResult<SaveResponseDto>> GetByName([FromRoute] string playerName)
        {
            var name = (playerName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 20)
                return BadRequest("Invalid playerName.");

            var s = await _db.PlayerSaves.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.PlayerName == name);
            if (s == null) return NotFound();

            return Ok(ToDto(s));
        }

        // POST /api/saves   (UPSERT by PlayerName)
        [HttpPost]
        public async Task<ActionResult<SaveResponseDto>> Upsert([FromBody] SaveSnapshotDto dto)
        {
            var name = (dto.PlayerName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 20)
                return BadRequest(new { playerName = "Required, ≤ 20 chars." });

            var now = DateTimeOffset.UtcNow;

            var s = await _db.PlayerSaves.FirstOrDefaultAsync(x => x.PlayerName == name);
            if (s == null)
            {
                s = new PlayerSave
                {
                    PlayerName = name,
                    CreatedAt = now
                };
                _db.PlayerSaves.Add(s);
            }

            // overwrite snapshot
            s.Score = Math.Max(0, dto.Score);
            s.Kills = Math.Max(0, dto.Kills);
            s.MaxCombo = Math.Max(0, dto.MaxCombo);
            s.ValidHits = Math.Max(0, dto.ValidHits);
            s.MissClicks = Math.Max(0, dto.MissClicks);
            s.PunishmentHits = Math.Max(0, dto.PunishmentHits);
            s.Hearts = Math.Max(0, dto.Hearts);
            s.PhaseIndex = Math.Clamp(dto.PhaseIndex, 0, 2);
            s.TimeLeftSec = Math.Max(0, dto.TimeLeftSec);
            s.UpdatedAt = now;

            await _db.SaveChangesAsync();

            return Ok(ToDto(s));
        }

        // DELETE /api/saves/{playerName}
        [HttpDelete("{playerName}")]
        public async Task<IActionResult> Delete([FromRoute] string playerName)
        {
            var name = (playerName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 20)
                return BadRequest("Invalid playerName.");

            var s = await _db.PlayerSaves.FirstOrDefaultAsync(x => x.PlayerName == name);
            if (s == null) return NotFound();

            _db.PlayerSaves.Remove(s);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static SaveResponseDto ToDto(PlayerSave s) => new SaveResponseDto
        {
            Id = s.Id,
            PlayerName = s.PlayerName,
            Score = s.Score,
            Kills = s.Kills,
            MaxCombo = s.MaxCombo,
            ValidHits = s.ValidHits,
            MissClicks = s.MissClicks,
            PunishmentHits = s.PunishmentHits,
            Hearts = s.Hearts,
            PhaseIndex = s.PhaseIndex,
            TimeLeftSec = s.TimeLeftSec,
            CreatedAt = s.CreatedAt.ToUnixTimeSeconds(),
            UpdatedAt = s.UpdatedAt.ToUnixTimeSeconds()
        };
    }
}
