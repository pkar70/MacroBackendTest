using Macrix_Backend.Models;
using Macrix_Backend.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using System;
using System.ComponentModel.DataAnnotations;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Macrix_Backend.Controllers
{
    [ApiVersion("1.0")]
    [Route("api")]
    [ApiController]
    public class MacrixController : ControllerBase
    {

        #region "basic endpoints, for developing"

        /// <summary>
        /// this is similar to PING/PONG messages, displayed when run with default settings from Visual Studio
        /// </summary>
        /// <returns></returns>
        [ApiVersion("1.0")]
        [HttpGet]
        public string GetDefault()
        {
            return "This is API, so use it accordingly";
        }

        /// <summary>
        /// display version of module
        /// </summary>
        /// <returns></returns>
        [ApiVersion("1.0")]
        [HttpGet]
        [Route("vers")]
        public string GetVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
        }

        /// <summary>
        ///  for testing: simple way to add one, default, record
        /// </summary>
        /// <returns></returns>
        [ApiVersion("1.0")]
        [HttpGet]
        [Route("default")]
        public string InitWithDefault(MyDbContext context)
        {
            Person newPerson = new Person() { ApartmentNumber = "132", DateOfBirth = new DateTime(1970, 03, 25), FirstName = "ja", LastName = "Macrix", HouseNumber = "11a", ID = 0, PhoneNumber = "+48 (601) 12-34-56", PostalCode = "30-147", StreetName = "Nowosądecka", Town = "Kraków" };
            context.Persons.Add(newPerson);
            context.SaveChanges();
            return "probably OK";
        }


        #endregion


        #region "real API"


        /// <summary>
        /// get count of records
        /// </summary>
        /// <returns>number of records in database</returns>
        [ApiVersion("1.0")]
        [HttpGet]
        [Route("count")]
        public string GetCount(MyDbContext context)
        {
            return CountItems(context).ToString();
        }

        /// <summary>
        /// get record with given ID (only if not deleted nor RODOblocker)
        /// </summary>
        /// <param name="id"></param>
        /// <returns>JSON with record, or 404</returns>
        [ApiVersion("1.0")]
        [HttpGet("get/{id}")]
        public IActionResult GetPerson(MyDbContext context, int id)
        {
            if (CountItems(context) == 0) return NotFound("no such user");

            Person? person = context.Persons.Where(p => p.ID == id).FirstOrDefault();
            if (person == null) return NotFound("no such user");

            if (person.IsRodoBlocked()) return NotFound("no such user");
            if (person.IsDeleted()) return NotFound("no such user");

            return Ok(person.ToJson());
        }

        /// <summary>
        /// get page of results (25 records), not deleted and not RODOblocked
        /// </summary>
        /// <param name="id">first ID to be returned</param>
        /// <returns>max. 25 records, or 404</returns>
        [ApiVersion("1.0")]
        [HttpGet("page/{id}")]
        public IActionResult GetPageOfResults(MyDbContext context, int id)
        {
            if (CountItems(context) == 0) return NotFound("no users");

            List<Person> persons = context.Persons.Where(p => p.ID >= id && p.RODOblock.Year < 2000 && p.deleted.Year < 2000).Take(25).ToList();

            return Ok(persons.ToJson());
        }



        /// <summary>
        /// update record
        /// </summary>
        /// <param name="json">updated person data</param>
        /// <returns>http 200/400 status</returns>
        [ApiVersion("1.0")]
        [HttpPut("{id}")]
        public IActionResult Put(MyDbContext context, [FromBody] string json)
        {
            if (json is null)
                return BadRequest("no Person data");

            Person? newPerson;
            try
            {
                newPerson = Newtonsoft.Json.JsonConvert.DeserializeObject<Person>(json);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            if (newPerson is null)
                return BadRequest("no Person data");

            if(UpdatePerson(context, newPerson)) return Ok("OK");

            return new StatusCodeResult(StatusCodes.Status500InternalServerError); // "ERROR updating database";
        }

        /// <summary>
        /// add new record
        /// </summary>
        /// <param name="json">new person data</param>
        /// <returns>http 200/400 status</returns>
        [ApiVersion("1.0")]
        [HttpPost]
        public IActionResult Add(MyDbContext context, [FromBody] string json)
        {
            if (json is null)
                return BadRequest("no Person data");

            Person? newPerson;
            try
            {
                newPerson = Newtonsoft.Json.JsonConvert.DeserializeObject<Person>(json);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            if(newPerson is null)
                return BadRequest("no Person data");

            int id = AddPerson(context, newPerson);
            if(id == -1) return BadRequest("we cannot process data for this user");
            if (id == -2) return BadRequest("such user already exist");

            return Ok(id.ToString());
        }

        /// <summary>
        /// find record, all parameters have to be matched
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="birthDate"></param>
        /// <returns>http 200/400/404 status</returns>
        [ApiVersion("1.0")]
        [HttpGet]
        [Route("find")]
        public IActionResult Find(MyDbContext context, string firstName, string lastName, string birthDate)
        {
            DateTime? dateOfBirth = DateNet2Dbase(birthDate);
            if (!dateOfBirth.HasValue)
                return BadRequest("bad date format");

            Person? person = FindPerson(context, firstName, lastName, dateOfBirth.Value);
            if (person == null) return NotFound("no such user");
            if (person.IsDeleted()) return NotFound("no such user");
            if (person.IsRodoBlocked()) return BadRequest("invalid user");

            return Ok(person.DumpAsJSON());
        }

        /// <summary>
        /// convert record to be RODO blocker
        /// </summary>
        /// <param name="id"></param>
        /// <returns>http 200/404 status</returns>
        [ApiVersion("1.0")]
        [HttpGet("rodo/{id}")]
        public IActionResult SetRODO(MyDbContext context, int id)
        {
            if (CountItems(context) == 0) return NotFound("no such user");

            Person? person = context.Persons.Where(p => p.ID == id).FirstOrDefault();
            if (person == null) return NotFound("no such user");

            if (person.IsRodoBlocked()) return Ok("OK");

            //context.Persons.Remove(persons);

            person.RODOblock = DateTime.UtcNow;

            // remove all attributes we should not deal with in future
            person.StreetName = "";
            person.HouseNumber = "";
            person.ApartmentNumber = null;
            person.PostalCode = "";
            person.Town = "";
            person.PhoneNumber = "";

            //context.Persons.Add(persons);

            context.SaveChanges();

            return Ok("OK");

        }


        /// <summary>
        /// mark record as deleted
        /// </summary>
        /// <param name="id">ID to be deleted</param>
        /// <returns>http 200/404</returns>
        [ApiVersion("1.0")]
        [HttpGet("del/{id}")]
        public IActionResult DeleteFromGet(MyDbContext context, int id)
        {
            if (DeletePerson(context, id))
                return Ok("OK");

            return NotFound("no such user");
        }

        /// <summary>
        /// mark record as deleted
        /// </summary>
        /// <param name="id">ID to be deleted</param>
        /// <returns>http 200/404</returns>
        [ApiVersion("1.0")]
        [HttpDelete("{id}")]
        public IActionResult DeleteFromDel(MyDbContext context, int id)
        {
            if (DeletePerson(context, id))
                return Ok("OK");

            return NotFound("ERROR: no such user");
        }




        #endregion 

        #region "my helpers"

        /// <summary>
        /// get count of records in database
        /// </summary>
        /// <returns>count of records</returns>
        private int CountItems(MyDbContext context)
        {
            // not simple count, as we got exception when Persons is null (in Jet and in JSON)
            try
            {
                return context.Persons.Count();
            }
            catch { return 0; }
        }

        /// <summary>
        /// Convert ISO 8601 date to DateTime
        /// </summary>
        /// <param name="date">date in ISO format (yyyyMMdd)</param>
        /// <returns>null if error, or DateTime with converted date</returns>
        private DateTime? DateNet2Dbase(string date)
        {
            // very simple and crude conversion from network format to DateTime
            if (date.Length != 8)
                return null;
            DateTime dateOfBirth;
            if (!DateTime.TryParseExact(date, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out dateOfBirth))
                return null;
            return dateOfBirth;
        }

        /// <summary>
        /// try to find record for given persons
        /// </summary>
        /// <returns>null if not found, or Person record</returns>
        private Person? FindPerson(MyDbContext context, string firstName, string lastName, DateTime dateOfBirth)
        {
            if (CountItems(context) == 0) return null;
            return context.Persons.Where(p => p.FirstName == firstName && p.LastName == lastName && p.DateOfBirth == dateOfBirth).FirstOrDefault();
        }

        /// <summary>
        /// add new record to database, after some checking
        /// </summary>
        /// <param name="newPerson">Person to be added</param>
        /// <returns>ID of new record, or negative value indicating error (-1: RODOblock, -2: user already exists)</returns>
        private int AddPerson(MyDbContext context, Person newPerson)
        {
            Person? prevPerson = FindPerson(context, newPerson.FirstName, newPerson.LastName, newPerson.DateOfBirth);

            if (prevPerson != null)
            {
                if (prevPerson.IsRodoBlocked())
                    return -1; // "ERROR: we cannot process data for this user";
                return -2; // "ERROR: such user already exist";
            }

            int id = 0;
            id = context.Persons.Max(p => p.ID);

            newPerson.ID = id;
            context.Persons.Add(newPerson);

            return id;
        }

        /// <summary>
        /// mark as removed record with this ID
        /// </summary>
        /// <param name="id">ID of record to be deleted</param>
        /// <returns>true if record was found and deleted, else: false </returns>
        private bool DeletePerson(MyDbContext context, int id)
        {
            if (CountItems(context) == 0) return false;

            Person? person = context.Persons.Where(p => p.ID == id).FirstOrDefault();
            if (person == null) return false;

            if (person.IsRodoBlocked()) return true;
            if (person.IsDeleted()) return true;

            //context.Persons.Remove(persons);

            person.deleted = DateTime.UtcNow;
            //context.Persons.Add(persons);

            context.SaveChanges();

            return true;
        }

        /// <summary>
        /// update record
        /// </summary>
        /// <param name="newPerson">new user data</param>
        /// <returns>true if record was found and updated, else: false </returns>
        private bool UpdatePerson(MyDbContext context, Person newPerson)
        {
            if (CountItems(context) == 0) return false;

            Person? currPerson = context.Persons.Where(p => p.ID == newPerson.ID).FirstOrDefault();
            if (currPerson == null) return false;

            if (currPerson.IsRodoBlocked()) return false;
            if (currPerson.IsDeleted()) return false;

            context.Persons.Remove(currPerson);
            context.Persons.Add(newPerson);

            context.SaveChanges();

            return true;
        }

        #endregion

    }
}
