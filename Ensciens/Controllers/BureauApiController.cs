using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ensciens.Models;
using Microsoft.Extensions.Primitives;

namespace Ensciens.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BureauApiController : ControllerBase
    {
        private readonly EnsciensContext _context;

        public BureauApiController(EnsciensContext context)
        {
            _context = context;
        }

        // GET: api/BureauApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Bureau>>> GetBureau()
        {
            return await _context.Bureau.ToListAsync();
        }

        // GET: api/BureauApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Bureau>> GetBureau(int id)
        {
            var bureau = await _context.Bureau.FindAsync(id);

            if (bureau == null)
            {
                return NotFound();
            }

            return bureau;
        }

        // PUT: api/BureauApi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBureau(int id, Bureau bureau)
        {
            if (id != bureau.Id)
            {
                return BadRequest();
            }
            var bureauAModifier = await _context.Bureau.FindAsync(id);
            if (!await IsAuthorized(bureauAModifier))
            {
                return Unauthorized();
            }

            _context.Entry(bureau).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BureauExists(id))
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

        // POST: api/BureauApi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Bureau>> PostBureau(Bureau bureau)
        {
            _context.Bureau.Add(bureau);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBureau", new { id = bureau.Id }, bureau);
        }

        // DELETE: api/BureauApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBureau(int id)
        {
            var bureau = await _context.Bureau.FindAsync(id);
            if (bureau == null)
            {
                return NotFound();
            }

            if (!await IsAuthorized(bureau))
            {
                return Unauthorized();
            }

            _context.Bureau.Remove(bureau);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool BureauExists(int id)
        {
            return _context.Bureau.Any(e => e.Id == id);
        }

        private async Task<bool> IsAuthorized(Bureau bureau)
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
            return await IsAuthorized(bureau, mailHeader[0], passwordHeader[0]);
        }

        public async Task<bool> IsAuthorized(Bureau bureau, String mail, String motDePasse)
        {
            // On r??cup??re tous les ??l??ves autoris??s ?? faire qqchose au Bureau
            // ??a comprend ici : Pr??sident, Pr??sidente, Vice-Pr??sident, Vice-Pr??sidente 
            // Ainsi que les Respo Famille (pour le BDF)
            List<LienBureauEleve> lienElevesAutorises = (await _context.LienBureauEleve.ToListAsync())
                .FindAll((lienBE) =>
                {
                    return lienBE.BureauId == bureau.Id
                       && (
                           lienBE.Role.Contains("Pr??sident")
                        || lienBE.Role.Contains("Respo Famille"))
                        ;
                });

            // Pour chaque lien bureau-??l??ve d'??l??ve autoris?? ?? modifier le bureau,
            foreach (LienBureauEleve lienBureauEleve in lienElevesAutorises)
            {
                // on r??cup??re l'??l??ve
                Eleve eleve = await _context.Eleve.FindAsync(lienBureauEleve.EleveId);
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
