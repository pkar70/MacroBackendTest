using Macrix_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Macrix_Backend.Models;

public interface IPersonsRepository
{
    // helpers

    /// <summary>
    /// get count of non-deleted, non-RODO-block records in database
    /// </summary>
    /// <returns>count of records</returns>
    public int Count();

    /// <summary>
    ///  for testing: simple way to add one, default, record
    /// </summary>
    /// <returns></returns>
    public void InitWithDefault();


    // Crud

    /// <summary>
    /// add new record to database, after some checking
    /// </summary>
    /// <param name="newPerson">Person to be added</param>
    /// <returns>ID of new record, or negative value indicating error (-1: RODOblock, -2: user already exists)</returns>
    public int AddPerson(Person newPerson);


    // cRud

    /// <summary>
    /// try to find record for given persons
    /// </summary>
    /// <returns>null if not found (or deleted,or RODO-blocked), or Person record</returns>
    public Person? FindPerson(string firstName, string lastName, DateTime dateOfBirth);

    /// <summary>
    /// get record with ID - also if deleted or RODO-blocker
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Person? GetPerson(int id);

    /// <summary>
    /// get page of results (25 records), not deleted and not RODOblocked
    /// </summary>
    /// <param name="id">first ID to be returned</param>
    /// <returns>max. 25 records</returns>
    public List<Person> GetPageOfResults(int id);


    // crUd

    /// <summary>
    /// update record
    /// </summary>
    /// <param name="newPerson">new user data</param>
    /// <returns>true if record was found and updated, else: false </returns>
    public bool UpdatePerson(Person newPerson);


    // cruD

    /// <summary>
    /// mark as removed record with this ID
    /// </summary>
    /// <param name="id">ID of record to be deleted</param>
    /// <returns>true if record was found and deleted, else: false </returns>
    public bool DeletePerson(int id);


    /// <summary>
    /// convert record to be RODO blocker
    /// </summary>
    /// <param name="id"></param>
    /// <returns>true if set, false if not found</returns>
    public bool SetRODO(int id);
}
