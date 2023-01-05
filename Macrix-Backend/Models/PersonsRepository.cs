using Macrix_Backend.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Macrix_Backend.Models;


public class PersonsRepository :IPersonsRepository
{
    private readonly MyDbContext _dbContext;

    public PersonsRepository(MyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// get count of non-deleted, non-RODO-block records in database
    /// </summary>
    /// <returns>count of records</returns>
    public int Count()
    {
        return _dbContext.Persons.Count(p => p.RODOblock.Year < 2000 && p.deleted.Year < 2000);
    }

    /// <summary>
    ///  for testing: simple way to add one, default, record
    /// </summary>
    /// <returns></returns>
    public void InitWithDefault()
    {
        Person newPerson = new Person() { ApartmentNumber = "132", DateOfBirth = new DateTime(1970, 03, 25), FirstName = "ja", LastName = "Macrix", HouseNumber = "11a", ID = 0, PhoneNumber = "+48 (601) 12-34-56", PostalCode = "30-147", StreetName = "Nowosądecka", Town = "Kraków" };
        _dbContext.Persons.Add(newPerson);
        _dbContext.SaveChanges();
    }


    /// <summary>
    /// try to find record for given persons
    /// </summary>
    /// <returns>null if not found, or Person record</returns>
    private Person? FindPerson(string firstName, string lastName, DateTime dateOfBirth)
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
}
