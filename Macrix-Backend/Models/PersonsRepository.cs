using Macrix_Backend.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;

namespace Macrix_Backend.Models;


public class PersonsRepository :IPersonsRepository
{
    private readonly MyDbContext _dbContext;

    public PersonsRepository(MyDbContext dbContext, bool noPersistence = false)
    {
        _dbContext = dbContext;
        if(!noPersistence) _dbContext.LoadData();
    }

    #region "helpers"

    public int Count()
    {
        //_dbContext.LoadData();
        return _dbContext.Persons.Count(p => p.RODOblock.Year < 2000 && p.deleted.Year < 2000);
    }


    public void InitWithDefault()
    {
        if (GetPerson(0) != null) return;   // already have such item

        Person newPerson = GetTestDefaultPerson();

        Person? oldPerson = GetPerson(newPerson.ID);
        if(oldPerson != null)
            // we already have such, but deleted - so we 'undeleting' it
            oldPerson.deleted = DateTime.MinValue;
        else
        _dbContext.Persons.Add(newPerson);

        _dbContext.SaveChanges();
    }

    /// <summary>
    ///  get test record, used here and in TEST
    /// </summary>
    /// <returns></returns>
    public Person GetTestDefaultPerson()
    {
        return new Person() { ApartmentNumber = "132", DateOfBirth = new DateTime(1970, 03, 25), FirstName = "ja", LastName = "Macrix", HouseNumber = "11a", ID = 1, PhoneNumber = "+48 (601) 12-34-56", PostalCode = "30-147", StreetName = "Nowosądecka", Town = "Kraków" };
    }

    #endregion

    #region "Crud"
    public int AddPerson(Person newPerson)
    {
        Person? prevPerson = FindPerson(newPerson.FirstName, newPerson.LastName, newPerson.DateOfBirth);

        if (prevPerson != null)
        {
            if (prevPerson.IsRodoBlocked())
                return -1; // "ERROR: we cannot process data for this user";
            return -2; // "ERROR: such user already exist";
        }

        int id = 0;
        if(_dbContext.Persons.Count() != 0)
            id = _dbContext.Persons.Max(p => p.ID);

        id = Math.Max(id, 1);   // ID=1 is reserved

        newPerson.ID = id + 1;
        _dbContext.Persons.Add(newPerson);

        _dbContext.SaveChanges();

        return newPerson.ID;
    }

    #endregion

    #region "cRud"

    public Person? FindPerson(string firstName, string lastName, DateTime dateOfBirth)
    {
        if(Count() == 0) return null;
        Person? person = _dbContext.Persons.Where(p => p.FirstName == firstName && p.LastName == lastName && p.DateOfBirth == dateOfBirth).FirstOrDefault();
        if(person == null) return null;
        if (person.IsDeleted()) return null;
        if (person.IsRodoBlocked()) return null;
        return person;
    }

    public Person? GetPerson(int id)
    {
        return _dbContext.Persons.Where(p => p.ID == id).FirstOrDefault();
    }

    public List<Person> GetPageOfResults(int id)
    {
        return _dbContext.Persons.Where(p => p.ID >= id && p.RODOblock.Year < 2000 && p.deleted.Year < 2000).Take(25).ToList();
    }

    #endregion

    #region "crUd"
    public bool UpdatePerson(Person newPerson)
    {
        if (Count() == 0) return false;

        Person? currPerson = _dbContext.Persons.Where(p => p.ID == newPerson.ID).FirstOrDefault();
        if (currPerson == null) return false;

        if (currPerson.IsRodoBlocked()) return false;
        if (currPerson.IsDeleted()) return false;

        _dbContext.Persons.Remove(currPerson);
        _dbContext.Persons.Add(newPerson);

        _dbContext.SaveChanges();

        return true;
    }

    #endregion

    #region "cruD"

    public bool DeletePerson(int id)
    {
        if (Count() == 0) return false;

        Person? person = _dbContext.Persons.Where(p => p.ID == id).FirstOrDefault();
        if (person == null) return false;

        if (person.IsRodoBlocked()) return true;
        if (person.IsDeleted()) return true;

        person.deleted = DateTime.UtcNow;

        _dbContext.SaveChanges();

        return true;
    }

    public bool SetRODO(int id)
    {
        // RODO-block can be set also on Deleted records!

        Person? person = _dbContext.Persons.Where(p => p.ID == id).FirstOrDefault();
        if (person == null) return false;

        if (person.IsRodoBlocked()) return true;

        person.RODOblock = DateTime.UtcNow;

        // remove all attributes we should not deal with in future
        person.StreetName = "";
        person.HouseNumber = "";
        person.ApartmentNumber = null;
        person.PostalCode = "";
        person.Town = "";
        person.PhoneNumber = "";

        _dbContext.SaveChanges();

        return true;

    }

    #endregion

}
