using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ensciens.Models;
using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace Ensciens.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClubApiController : ControllerBase
    {
        private readonly EnsciensContext _context;

        public ClubApiController(EnsciensContext context)
        {
            _context = context;
        }

        // GET: api/ClubApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Club>>> GetClub()
        {
            List<Club> listeClubs = await _context.Club.ToListAsync();
            return new JsonResult(listeClubs, new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }

        // GET: api/ClubApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Club>> GetClub(int id)
        {
            var club = await _context.Club.FindAsync(id);

            if (club == null)
            {
                return NotFound();
            }

            return new JsonResult(club, new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }

        // PUT: api/ClubApi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutClub(int id, Club club)
        {
            if (id != club.Id)
            {
                return BadRequest();
            }
            Club clubAModifier = await _context.Club.FindAsync(id);
            if (!await IsAuthorized(clubAModifier))
            {
                return Unauthorized();
            }

            _context.Entry(club).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClubExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/ClubApi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Club>> PostClub(Club club)
        {
            _context.Club.Add(club);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetClub", new { id = club.Id }, club);
        }

        // DELETE: api/ClubApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClub(int id)
        {
            var club = await _context.Club.FindAsync(id);
            if (club == null)
            {
                return NotFound();
            }

            if (!await IsAuthorized(club))
            {
                return Unauthorized();
            }

            _context.Club.Remove(club);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ClubExists(int id)
        {
            return _context.Club.Any(e => e.Id == id);
        }

        public async Task<bool> IsAuthorized(Club club)
        {
            // R??cup??ration des champs Mail et Password en ent??te de requ??te
            StringValues mailHeader = new StringValues();
            Request.Headers.TryGetValue("Mail", out mailHeader);
            StringValues passwordHeader = new StringValues();
            Request.Headers.TryGetValue("Password", out passwordHeader);
            // Pas d'autorisation ?? l'API si n'y a pas exactement 1 champ mail et password
            if (mailHeader.Count != 1 || passwordHeader.Count != 1)
            {
                return false;
            }

            return await IsAuthorized(club, mailHeader[0], passwordHeader[0]);
        }


        public async Task<bool> IsAuthorized(Club club, String mail, String motDePasse)
        {
            // On r??cup??re tous les ??l??ves autoris??s ?? faire qqchose au Club
            // ??a comprend ici : Pr??sident, Pr??sidente, Vice-Pr??sident, Vice-Pr??sidente 
            List<LienClubEleve> lienElevesAutorises = (await _context.LienClubEleve.ToListAsync())
                .FindAll((lienCE) =>
                {
                    return lienCE.ClubId == club.Id && lienCE.Role.Contains("Pr??sident");
                });

            // Pour chaque lien club-??l??ve d'??l??ve autoris?? ?? modifier le club,
            foreach (LienClubEleve lienClubEleve in lienElevesAutorises)
            {
                // on r??cup??re l'??l??ve
                Eleve eleve = await _context.Eleve.FindAsync(lienClubEleve.EleveId);
                // et si les id/mdp envoy??s en requ??te correspondent ?? l'??l??ve autoris??, c'est ok
                if (eleve.Email == mail && eleve.MotDePasse == motDePasse)
                {
                    return true;
                }
            }
            // sinon, c'est interdit
            return false;
        }
    }
}
