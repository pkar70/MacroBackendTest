using MacroBackendTest_Backend.Models;
using MacroBackendTest_Backend.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using System;
using System.ComponentModel.DataAnnotations;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MacroBackendTest_Backend.Controllers;

[ApiVersion("1.0")]
[Route("api")]
[ApiController]
public class MacroBackendTestController : ControllerBase
{

    private IPersonsRepository _repository;

    public MacroBackendTestController(IPersonsRepository repository)
    {
        _repository = repository;
    }


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
    public string InitWithDefault()
    {
        _repository.InitWithDefault();
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
    public string GetCount()
    {
        return _repository.Count().ToString();
    }

    /// <summary>
    /// get record with given ID (only if not deleted nor RODOblocker)
    /// </summary>
    /// <param name="id"></param>
    /// <returns>JSON with record, or 404</returns>
    [ApiVersion("1.0")]
    [HttpGet("get/{id}")]
    public IActionResult GetPerson(int id)
    {
        Person? person = _repository.GetPerson(id);
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
    public IActionResult GetPageOfResults(int id)
    {
        if (_repository.Count() == 0) return NotFound("no users");

        List<Person> persons = _repository.GetPageOfResults(id);
        return Ok(persons.ToJson());
    }



    /// <summary>
    /// update record
    /// </summary>
    /// <param name="json">updated person data</param>
    /// <returns>http 200/400 status</returns>
    [ApiVersion("1.0")]
    [HttpPut]
    [Route("")]
    public IActionResult Put([FromBody] Person newPerson)
    {
        if (newPerson is null)
            return BadRequest("no Person data");

        if(_repository.UpdatePerson(newPerson)) return Ok("OK");

        return new StatusCodeResult(StatusCodes.Status500InternalServerError); // "ERROR updating database";
    }

    /// <summary>
    /// add new record
    /// </summary>
    /// <param name="json">new person data</param>
    /// <returns>http 200/400 status</returns>
    [ApiVersion("1.0")]
    [HttpPost]
    [Route("")]
    public IActionResult Add([FromBody] Person newPerson)
    {
        if(newPerson is null)
            return BadRequest("no Person data");

        int id = _repository.AddPerson(newPerson);
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
    public IActionResult Find(string firstName, string lastName, string birthDate)
    {
        DateTime? dateOfBirth = DateNet2Dbase(birthDate);
        if (!dateOfBirth.HasValue)
            return BadRequest("bad date format");

        Person? person = _repository.FindPerson(firstName, lastName, dateOfBirth.Value);
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
    public IActionResult SetRODO(int id)
    {
        if(_repository.SetRODO(id)) return Ok("OK");
        
        return NotFound("no such user");

    }


    /// <summary>
    /// mark record as deleted
    /// </summary>
    /// <param name="id">ID to be deleted</param>
    /// <returns>http 200/404</returns>
    [ApiVersion("1.0")]
    [HttpGet("del/{id}")]
    public IActionResult DeleteFromGet(int id)
    {
        if (_repository.DeletePerson(id))
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
    public IActionResult DeleteFromDel(int id) => DeleteFromGet(id);


    #endregion 

    #region "my helpers"

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

    #endregion

}
